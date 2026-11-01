// Họ và tên: 
// Mã sinh viên: [Điền mã sinh viên của bạn]
// Nội dung thực hiện: Controller ThongKe - Dashboard quản trị và thống kê nâng cao bằng LINQ
// (GroupBy, Sum, Count, OrderByDescending, Any): doanh thu theo tháng/loại sân,
// sân & loại sân ưa thích, dịch vụ phổ biến, số lượt hủy, khung giờ cao điểm.
// Có bộ lọc thời gian (hôm nay / tuần / tháng / quý / năm / tùy chọn) và so sánh với kỳ trước.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;
using System.Linq.Expressions;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    [AuthorizeRole("Admin", "NhanVien")]
    public class ThongKeController : Controller
    {
        // Tính số giờ thuê sân (chỉ dùng cho "Tạm tính hôm nay")
        private static double SoGio(DateTime gioBatDau, DateTime gioKetThuc)
        {
            return Math.Max((gioKetThuc - gioBatDau).TotalHours, 0);
        }

        // Tính tiền sân = đơn giá theo giờ x số giờ thuê (chỉ dùng cho "Tạm tính hôm nay")
        private static decimal TienSan(
            decimal donGia, DateTime gioBatDau, DateTime gioKetThuc)
        {
            return Math.Round(
                donGia * (decimal)SoGio(gioBatDau, gioKetThuc), 0);
        }

        private readonly ApplicationDbContext _context;

        // Trạng thái - dùng chung với module Đặt sân (TrangThaiDatSan)
        private const string CHO_XU_LY = "Chờ xử lý";
        private const string DANG_XU_LY = "Đang xử lý";
        private const string HOAN_THANH = "Hoàn thành";
        private const string HUY = "Đã hủy";
        private const bool SAN_KHA_DUNG = true;

        public ThongKeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== PHÂN QUYỀN ==================
        private string? VaiTro => HttpContext.Session.GetString("VaiTro");
        private bool LaQuanLy() => VaiTro == "Admin" || VaiTro == "NhanVien";
        private bool LaAdmin() => VaiTro == "Admin";
        private IActionResult TuChoi()
        {
            if (string.IsNullOrEmpty(VaiTro))
                return RedirectToAction("DangNhap", "TaiKhoan");
            TempData["Loi"] = "Bạn không có quyền truy cập trang thống kê.";
            return RedirectToAction("Index", "Home");
        }

        // ================== DASHBOARD ==================
        public async Task<IActionResult> Index()
        {
            if (!LaQuanLy()) return TuChoi();

            var homNay = DateTime.Today;
            var ngayMai = homNay.AddDays(1);

            var vm = new DashboardViewModel
            {
                TongLoaiSan = await _context.LoaiSans.CountAsync(),
                TongSan = await _context.SanTheThaos.CountAsync(),
                SanKhaDung = await _context.SanTheThaos.CountAsync(s => s.TrangThai == SAN_KHA_DUNG),
                TongKhachHang = await _context.KhachHangs.CountAsync(),
                TongDatSan = await _context.DatSans.CountAsync(),
                DonChoXuLy = await _context.DatSans.CountAsync(d => d.TrangThai == CHO_XU_LY),
                DonDangXuLy = await _context.DatSans.CountAsync(d => d.TrangThai == DANG_XU_LY),
                DonHoanThanh = await _context.DatSans.CountAsync(d => d.TrangThai == HOAN_THANH),
                DonHomNay = await _context.DatSans.CountAsync(d => d.NgayDat >= homNay && d.NgayDat < ngayMai)
            };

            // Doanh thu hoàn thành: cộng DonGia của các đơn "Hoàn thành"
            // (cùng cách tính với trang Thống kê để hai nơi luôn khớp nhau)
            vm.DoanhThuHoanThanh = await _context.DatSans
                .Where(d => d.TrangThai == HOAN_THANH)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;

            // Tạm tính hôm nay: bỏ đơn đã hủy
            var donHomNay = await LayDonTienAsync(d => d.NgayDat >= homNay && d.NgayDat < ngayMai && d.TrangThai != HUY);
            vm.DoanhThuTamTinhHomNay = donHomNay.Sum(x => x.TongTien);

            return View(vm);
        }

        // ================== LỌC THEO KỲ ==================
        // Chỗ DUY NHẤT quyết định thống kê tính theo ngày nào.
        // Đang dùng NgayDat (ngày tạo đơn). Muốn theo ngày đá thực tế thì đổi
        // d.NgayDat thành d.GioBatDau ở dòng Where bên dưới.
        private static IQueryable<DatSan> LocTheoKy(IQueryable<DatSan> q, DateTime tuNgay, DateTime denLoaiTru)
        {
            return q.Where(d => d.NgayDat >= tuNgay && d.NgayDat < denLoaiTru);
        }

        // ================== THỐNG KÊ LINQ ==================
        // kieu: homnay | tuannay | thangnay | quynay | nam | tuychon
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> ThongKe(string? kieu, int? nam, DateTime? tu, DateTime? den)
        {
            if (!LaAdmin()) return TuChoi();

            // ----- 1. Khoảng thời gian [tuNgay, denNgay] và kỳ trước -----
            var homNay = DateTime.Today;
            kieu = (kieu ?? "nam").ToLowerInvariant();

            DateTime tuNgay, denNgay;
            switch (kieu)
            {
                case "homnay":
                    tuNgay = denNgay = homNay;
                    break;
                case "tuannay": // tuần bắt đầu từ thứ Hai
                    int lui = ((int)homNay.DayOfWeek + 6) % 7;
                    tuNgay = homNay.AddDays(-lui);
                    denNgay = tuNgay.AddDays(6);
                    break;
                case "thangnay":
                    tuNgay = new DateTime(homNay.Year, homNay.Month, 1);
                    denNgay = tuNgay.AddMonths(1).AddDays(-1);
                    break;
                case "quynay":
                    int quy = (homNay.Month - 1) / 3;
                    tuNgay = new DateTime(homNay.Year, quy * 3 + 1, 1);
                    denNgay = tuNgay.AddMonths(3).AddDays(-1);
                    break;
                case "tuychon":
                    tuNgay = (tu ?? homNay).Date;
                    denNgay = (den ?? tuNgay).Date;
                    if (denNgay < tuNgay) (tuNgay, denNgay) = (denNgay, tuNgay);
                    if ((denNgay - tuNgay).TotalDays > 1830) denNgay = tuNgay.AddDays(1830); // tối đa ~5 năm
                    break;
                default:
                    kieu = "nam";
                    int namChon = nam ?? homNay.Year;
                    tuNgay = new DateTime(namChon, 1, 1);
                    denNgay = new DateTime(namChon, 12, 31);
                    break;
            }
            var denLoaiTru = denNgay.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;

            // Kỳ trước: cùng độ dài (hoặc lùi đúng 1 tháng / quý / năm với kỳ theo lịch)
            DateTime tuTruoc, denTruoc;
            if (kieu == "nam")
            {
                tuTruoc = tuNgay.AddYears(-1);
                denTruoc = denNgay.AddYears(-1);
            }
            else if (kieu == "thangnay")
            {
                tuTruoc = tuNgay.AddMonths(-1);
                denTruoc = tuNgay.AddDays(-1);
            }
            else if (kieu == "quynay")
            {
                tuTruoc = tuNgay.AddMonths(-3);
                denTruoc = tuNgay.AddDays(-1);
            }
            else
            {
                denTruoc = tuNgay.AddDays(-1);
                tuTruoc = denTruoc.AddDays(-(soNgay - 1));
            }

            var vm = new ThongKeViewModel
            {
                Kieu = kieu,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TuNgayTruoc = tuTruoc,
                DenNgayTruoc = denTruoc,
                Nam = tuNgay.Year,
                // Kỳ ngắn chia theo ngày, kỳ vừa chia theo tuần, kỳ dài chia theo tháng
                DonViBieuDo = soNgay <= 31 ? "ngày" : (soNgay <= 120 ? "tuần" : "tháng")
            };

            // Danh sách năm có dữ liệu để chọn
            vm.DanhSachNam = await _context.DatSans.Select(d => d.NgayDat.Year)
                                .Distinct().OrderByDescending(y => y).ToListAsync();
            if (!vm.DanhSachNam.Contains(vm.Nam)) vm.DanhSachNam.Insert(0, vm.Nam);

            var datTrongKy = LocTheoKy(_context.DatSans.AsNoTracking(), tuNgay, denLoaiTru);

            // ----- 2. Kỳ trước (để hiện ▲/▼ so sánh) -----
            var datKyTruoc = LocTheoKy(_context.DatSans.AsNoTracking(), tuTruoc, denTruoc.AddDays(1));
            vm.LuotDatKyTruoc = await datKyTruoc.CountAsync();
            vm.DoanhThuKyTruoc = await datKyTruoc
                .Where(d => d.TrangThai == HOAN_THANH)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;

            // ----- 3. Các thống kê trong kỳ -----

            // Số sân theo loại (thông tin hiện có, không phụ thuộc thời gian)
            vm.SanTheoLoai = await _context.SanTheThaos.AsNoTracking()
                .GroupBy(s => s.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // Số lượt đặt theo sân (không tính đơn đã hủy) + sân được dùng nhiều nhất
            vm.LuotDatTheoSan = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.TenSan)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.SanNhieuNhat = vm.LuotDatTheoSan.FirstOrDefault();

            // Sân ít khách nhất: tính cả sân chưa có lượt nào trong kỳ (SoLuong = 0)
            var luotTheoTen = vm.LuotDatTheoSan.ToDictionary(x => x.Ten, x => x.SoLuong);
            var tatCaSan = await _context.SanTheThaos.AsNoTracking()
                .Select(s => s.TenSan)
                .ToListAsync();
            vm.SanItKhach = tatCaSan.Distinct()
                .Select(ten => new ThongKeItem
                {
                    Ten = ten,
                    SoLuong = luotTheoTen.TryGetValue(ten, out var n) ? n : 0
                })
                .OrderBy(x => x.SoLuong).ThenBy(x => x.Ten)
                .Take(5)
                .ToList();

            // Loại sân được ưa thích nhất
            vm.LuotDatTheoLoai = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.LoaiSanUaThich = vm.LuotDatTheoLoai.FirstOrDefault();

            // Doanh thu: chỉ đơn Hoàn thành trong kỳ, cộng DonGia (giống Dashboard)
            var donHoanThanh = await datTrongKy
                .Where(d => d.TrangThai == HOAN_THANH)
                .Select(d => new
                {
                    d.NgayDat,
                    d.DonGia,
                    TenLoai = d.SanTheThao!.LoaiSan!.TenLoai
                })
                .ToListAsync();

            // Biểu đồ doanh thu: tự chia theo ngày / tuần / tháng (kỳ nào không có đơn thì cột = 0)
            if (vm.DonViBieuDo == "ngày")
            {
                var theoNgay = donHoanThanh
                    .GroupBy(x => x.NgayDat.Date)
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                vm.DoanhThuTheoThang = Enumerable.Range(0, soNgay)
                    .Select(i => tuNgay.AddDays(i))
                    .Select(d =>
                    {
                        theoNgay.TryGetValue(d, out var v);
                        return new ThongKeItem { Ten = d.ToString("dd/MM"), GiaTri = v.Tien, SoLuong = v.SoDon };
                    }).ToList();
            }
            else if (vm.DonViBieuDo == "tuần")
            {
                // Mỗi cột = 7 ngày liên tiếp tính từ tuNgay; nhãn là ngày bắt đầu cột
                int soCot = (int)Math.Ceiling(soNgay / 7.0);
                var theoTuan = donHoanThanh
                    .GroupBy(x => (x.NgayDat.Date - tuNgay).Days / 7)
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                vm.DoanhThuTheoThang = Enumerable.Range(0, soCot)
                    .Select(i =>
                    {
                        theoTuan.TryGetValue(i, out var v);
                        return new ThongKeItem { Ten = tuNgay.AddDays(i * 7).ToString("dd/MM"), GiaTri = v.Tien, SoLuong = v.SoDon };
                    }).ToList();
            }
            else
            {
                var theoThang = donHoanThanh
                    .GroupBy(x => (x.NgayDat.Year, x.NgayDat.Month))
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                var thangDau = new DateTime(tuNgay.Year, tuNgay.Month, 1);
                int soThang = (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;

                vm.DoanhThuTheoThang = Enumerable.Range(0, soThang)
                    .Select(i => thangDau.AddMonths(i))
                    .Select(m =>
                    {
                        theoThang.TryGetValue((m.Year, m.Month), out var v);
                        return new ThongKeItem
                        {
                            Ten = kieu == "nam" ? $"Tháng {m.Month}" : $"T{m.Month}/{m.Year}",
                            GiaTri = v.Tien,
                            SoLuong = v.SoDon
                        };
                    }).ToList();
            }

            vm.DoanhThuTheoLoai = donHoanThanh.GroupBy(x => x.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, GiaTri = g.Sum(x => x.DonGia), SoLuong = g.Count() })
                .OrderByDescending(x => x.GiaTri)
                .ToList();

            vm.TongDoanhThuNam = donHoanThanh.Sum(x => x.DonGia);
            vm.SoDonHoanThanh = donHoanThanh.Count;

            // Dịch vụ được sử dụng nhiều nhất (Top 10 theo số lượng) trong kỳ
            vm.DichVuPhoBien = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .SelectMany(d => d.ChiTietDatSans)
                .GroupBy(c => c.DichVu!.TenDichVu)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key,
                    SoLuong = g.Sum(c => c.SoLuong),
                    GiaTri = g.Sum(c => c.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToListAsync();

            // Số lượt hủy, tỷ lệ hủy và danh sách đơn hủy (để bấm vào xem chi tiết)
            vm.SoLuotHuy = await datTrongKy.CountAsync(d => d.TrangThai == HUY);
            vm.TongLuotDat = await datTrongKy.CountAsync();   // 0 nghĩa là kỳ này chưa có dữ liệu

            vm.DonHuy = await datTrongKy
                .Where(d => d.TrangThai == HUY)
                .OrderByDescending(d => d.NgayDat).ThenByDescending(d => d.GioBatDau)
                .Take(200)
                .Select(d => new DonHuyItem
                {
                    MaDatSan = d.MaDatSan,
                    TenSan = d.SanTheThao!.TenSan,
                    NgayDat = d.NgayDat,
                    GioBatDau = d.GioBatDau,
                    GioKetThuc = d.GioKetThuc,
                    DonGia = d.DonGia
                })
                .ToListAsync();

            // Tỷ lệ sử dụng theo khung giờ (theo giờ bắt đầu, bỏ đơn hủy)
            var gioBatDau = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .Select(d => d.GioBatDau)
                .ToListAsync();
            int tongGio = gioBatDau.Count;

            vm.KhungGio = gioBatDau.GroupBy(g => g.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new ThongKeItem
                {
                    Ten = $"{g.Key:00}:00 - {g.Key + 1:00}:00",
                    SoLuong = g.Count(),
                    TyLe = tongGio == 0 ? 0 : Math.Round(g.Count() * 100.0 / tongGio, 1)
                }).ToList();
            vm.KhungGioCaoDiem = vm.KhungGio.OrderByDescending(x => x.SoLuong).FirstOrDefault();

            return View(vm);
        }

        // ================== HÀM HỖ TRỢ ==================
        // Chỉ dùng cho "Tạm tính hôm nay" ở Dashboard:
        // TongTien = tiền sân (đơn giá x số giờ) + tiền dịch vụ
        private async Task<List<DonTien>> LayDonTienAsync(Expression<Func<DatSan, bool>> dieuKien)
        {
            var raw = await _context.DatSans.AsNoTracking()
                .Where(dieuKien)
                .Select(d => new
                {
                    d.MaDatSan,
                    d.NgayDat,
                    d.GioBatDau,
                    d.GioKetThuc,
                    d.DonGia,
                    TenLoai = d.SanTheThao!.LoaiSan!.TenLoai,
                    TienDichVu = d.ChiTietDatSans.Sum(c => (decimal?)c.ThanhTien) ?? 0m
                })
                .ToListAsync();

            return raw.Select(x => new DonTien
            {
                MaDatSan = x.MaDatSan,
                NgayDat = x.NgayDat,
                TenLoai = x.TenLoai,
                TongTien = TienSan(x.DonGia, x.GioBatDau, x.GioKetThuc) + x.TienDichVu
            }).ToList();
        }
    }
}
