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
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;
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

        private readonly IThongKeRepository _thongKeRepository;

        // Trạng thái - dùng chung với module Đặt sân (TrangThaiDatSan)
        private const string CHO_XU_LY = "Chờ xử lý";
        private const string DANG_XU_LY = "Đang xử lý";
        private const string HOAN_THANH = "Hoàn thành";
        private const string HUY = "Đã hủy";
        private const bool SAN_KHA_DUNG = true;
        private const int NGAY_NGUY_CO = 30;          // chưa quay lại từ ngần ấy ngày => nguy cơ rời bỏ
        private const int CUA_SO_CHUYEN_DOI = 30;     // đăng ký -> có đơn trong ngần ấy ngày

        public ThongKeController(IThongKeRepository thongKeRepository)
        {
            _thongKeRepository = thongKeRepository;
        }

        // Ngày hiện tại theo giờ Việt Nam (UTC+7), không phụ thuộc múi giờ máy chủ
        private static DateTime HomNayVn() => DateTime.UtcNow.AddHours(7).Date;

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

            var homNay = HomNayVn();
            var ngayMai = homNay.AddDays(1);

            var vm = new DashboardViewModel
            {
                TongLoaiSan = await _thongKeRepository.LayLoaiSans().CountAsync(),
                TongSan = await _thongKeRepository.LaySanTheThaos().CountAsync(),
                SanKhaDung = await _thongKeRepository.LaySanTheThaos().CountAsync(s => s.TrangThai == SAN_KHA_DUNG),
                TongKhachHang = await _thongKeRepository.LayKhachHangs().CountAsync(),
                TongDatSan = await _thongKeRepository.LayDatSans().CountAsync(),
                DonChoXuLy = await _thongKeRepository.LayDatSans().CountAsync(d => d.TrangThai == CHO_XU_LY),
                DonDangXuLy = await _thongKeRepository.LayDatSans().CountAsync(d => d.TrangThai == DANG_XU_LY),
                DonHoanThanh = await _thongKeRepository.LayDatSans().CountAsync(d => d.TrangThai == HOAN_THANH),
                DonHomNay = await _thongKeRepository.LayDatSans().CountAsync(d => d.NgayDat >= homNay && d.NgayDat < ngayMai)
            };

            // Doanh thu hoàn thành: cộng DonGia của các đơn "Hoàn thành"
            // (cùng cách tính với trang Thống kê để hai nơi luôn khớp nhau)
            vm.DoanhThuHoanThanh = await _thongKeRepository.LayDatSans()
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
            var homNay = HomNayVn();
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
                // (Khoảng tùy chọn kéo dài sang tương lai cũng được cắt về hôm nay ở bước "cắt kỳ" bên dưới)
                default:
                    kieu = "nam";
                    // Giới hạn năm hợp lệ để URL ?nam=0 hoặc ?nam=99999 không làm DateTime văng lỗi
                    int namChon = Math.Clamp(nam ?? homNay.Year, 2000, 2100);
                    tuNgay = new DateTime(namChon, 1, 1);
                    denNgay = new DateTime(namChon, 12, 31);
                    break;
            }
            // Cắt kỳ về hôm nay: không thống kê ngày chưa diễn ra
            // (VD chọn "Năm 2026" khi hôm nay 10/11 thì kỳ là 01/01 - 10/11, không phải đến 31/12).
            // Kỳ trước được lấy cùng độ dài để so sánh công bằng.
            if (denNgay > homNay) denNgay = homNay;
            if (tuNgay > denNgay) tuNgay = denNgay;

            var denLoaiTru = denNgay.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;

            // Kỳ trước: cùng giai đoạn (cùng số ngày đã trôi qua) của năm / tháng / quý trước
            DateTime tuTruoc, denTruoc;
            if (kieu == "nam")
            {
                tuTruoc = tuNgay.AddYears(-1);
                denTruoc = denNgay.AddYears(-1);
            }
            else if (kieu == "thangnay")
            {
                tuTruoc = tuNgay.AddMonths(-1);
                denTruoc = denNgay.AddMonths(-1);
            }
            else if (kieu == "quynay")
            {
                tuTruoc = tuNgay.AddMonths(-3);
                denTruoc = denNgay.AddMonths(-3);
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
            vm.DanhSachNam = await _thongKeRepository.LayDatSans()
                                .Select(d => d.NgayDat.Year)
                                .Distinct()
                                .OrderByDescending(y => y)
                                .ToListAsync();

            if (!vm.DanhSachNam.Contains(vm.Nam)) vm.DanhSachNam.Insert(0, vm.Nam);

            var datTrongKy = LocTheoKy(_thongKeRepository.LayDatSans().AsNoTracking(), tuNgay, denLoaiTru);

            // ----- 2. Kỳ trước (để hiện ▲/▼ so sánh) -----
            var datKyTruoc = LocTheoKy(_thongKeRepository.LayDatSans().AsNoTracking(), tuTruoc, denTruoc.AddDays(1));
            vm.LuotDatKyTruoc = await datKyTruoc.CountAsync();
            vm.DoanhThuKyTruoc = await datKyTruoc
                .Where(d => d.TrangThai == HOAN_THANH)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;

            // ----- 3. Các thống kê trong kỳ -----

            // Số sân theo loại (thông tin hiện có, không phụ thuộc thời gian)
            vm.SanTheoLoai = await _thongKeRepository.LaySanTheThaos().AsNoTracking()
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
            var tatCaSan = await _thongKeRepository.LaySanTheThaos().AsNoTracking()
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

            // Tab "Khách hàng" (xem partial _ThongKeKhachHang.cshtml)
            vm.KhachHang = await TinhThongKeKhachHangAsync(datTrongKy, datKyTruoc, tuNgay, denNgay, tuTruoc, denTruoc, vm.DonViBieuDo);

            return View(vm);
        }

        // ================== THỐNG KÊ KHÁCH HÀNG ==================
        // Hạng thành viên suy ra từ điểm tích lũy (100.000đ = 10 điểm => 1 triệu đồng = 100 điểm)
        private static readonly (string Ten, string MoTa, string Css, int TuDiem)[] CacHang =
        {
            ("Thành viên", "0 - 99 điểm",      "tv",   0),
            ("Bạc",        "100 - 499 điểm",   "bac",  100),
            ("Vàng",       "500 - 1.499 điểm", "vang", 500),
            ("Kim cương",  "từ 1.500 điểm",    "kc",   1500)
        };

        private static string XepHang(int diem)
        {
            string ten = CacHang[0].Ten;
            foreach (var h in CacHang)
                if (diem >= h.TuDiem) ten = h.Ten;
            return ten;
        }

        // Một đơn đặt sân rút gọn (mọi trạng thái) dùng cho thống kê khách hàng
        internal sealed class DonKhach
        {
            public int MaDatSan { get; set; }
            public int MaKhachHang { get; set; }
            public string TrangThai { get; set; } = "";
            public decimal DonGia { get; set; }
            public DateTime NgayDat { get; set; }
            public DateTime GioKetThuc { get; set; }
            public decimal TienCoc { get; set; }
        }

        private static IQueryable<DonKhach> ChieuDon(IQueryable<DatSan> q)
        {
            return q.Select(d => new DonKhach
            {
                MaDatSan = d.MaDatSan,
                MaKhachHang = d.MaKhachHang,
                TrangThai = d.TrangThai,
                DonGia = d.DonGia,
                NgayDat = d.NgayDat,
                GioKetThuc = d.GioKetThuc,
                TienCoc = d.TienCoc
            });
        }

        // Chỉ số cốt lõi của một kỳ (dùng cho cả kỳ này và kỳ trước để so sánh)
        internal sealed record ChiSoKy(int KhachCoDon, int KhachQuayLai, double TyLeQuayLai,
                                      int TongDon, int SoHuy, double TyLeHuy);

        private static ChiSoKy TinhChiSoKy(List<DonKhach> don)
        {
            var luotMoiKhach = don
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.MaKhachHang)
                .Select(g => g.Count())
                .ToList();

            int khach = luotMoiKhach.Count;
            int quayLai = luotMoiKhach.Count(n => n >= 2);
            int soHuy = don.Count(d => d.TrangThai == HUY);

            return new ChiSoKy(
                khach,
                quayLai,
                khach == 0 ? 0 : Math.Round(quayLai * 100.0 / khach, 1),
                don.Count,
                soHuy,
                don.Count == 0 ? 0 : Math.Round(soHuy * 100.0 / don.Count, 1));
        }

        private async Task<ThongKeKhachHangViewModel> TinhThongKeKhachHangAsync(
            IQueryable<DatSan> datTrongKy, IQueryable<DatSan> datKyTruoc,
            DateTime tuNgay, DateTime denNgay, DateTime tuTruoc, DateTime denTruoc, string donVi)
        {
            var denLoaiTru = denNgay.AddDays(1);
            var denTruocLoaiTru = denTruoc.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;

            var vm = new ThongKeKhachHangViewModel
            {
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TuNgayTruoc = tuTruoc,
                DenNgayTruoc = denTruoc,
                DonVi = donVi
            };

            // Khách hàng: chỉ lấy các cột cần dùng (hệ thống quản lý sân nên số lượng khách nhỏ)
            var khachHangs = await _thongKeRepository.LayKhachHangs().AsNoTracking()
                .Select(k => new
                {
                    k.MaKhachHang,
                    k.HoTen,
                    k.SoDienThoai,
                    k.DiemTichLuy,
                    k.NgayDangKy
                })
                .ToListAsync();
            var thongTin = khachHangs.ToDictionary(k => k.MaKhachHang);
            vm.TongKhachHang = khachHangs.Count;

            // Các đơn trong kỳ và kỳ trước (mọi trạng thái) - gom theo khách ở bộ nhớ cho đơn giản, dễ kiểm chứng
            var don = await ChieuDon(datTrongKy).ToListAsync();
            var donTruoc = await ChieuDon(datKyTruoc).ToListAsync();

            // ----- 0. Kỳ trước (để hiện ▲/▼ trên thẻ KPI) -----
            var csTruoc = TinhChiSoKy(donTruoc);
            vm.KhachCoDonTruoc = csTruoc.KhachCoDon;
            vm.TyLeQuayLaiTruoc = csTruoc.TyLeQuayLai;
            vm.TongDonTruoc = csTruoc.TongDon;
            vm.TyLeHuyTruoc = csTruoc.TyLeHuy;
            vm.KhachMoiTruoc = khachHangs.Count(k => k.NgayDangKy >= tuTruoc && k.NgayDangKy < denTruocLoaiTru);
            vm.CoDuLieuKyTruoc = csTruoc.TongDon > 0 || vm.KhachMoiTruoc > 0;

            // ----- 1. Top khách hàng theo doanh thu / số lượt đặt (bỏ đơn hủy) -----
            var theoKhach = don
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.MaKhachHang)
                .Select(g => new
                {
                    Ma = g.Key,
                    Luot = g.Count(),
                    DoanhThu = g.Where(x => x.TrangThai == HOAN_THANH).Sum(x => x.DonGia)
                })
                .ToList();

            KhachVipItem TaoVip(int ma, int luot, decimal doanhThu)
            {
                if (thongTin.TryGetValue(ma, out var k))
                    return new KhachVipItem
                    {
                        MaKhachHang = ma,
                        HoTen = k.HoTen,
                        SoDienThoai = k.SoDienThoai,
                        SoLuot = luot,
                        DoanhThu = doanhThu,
                        DiemTichLuy = k.DiemTichLuy,
                        Hang = XepHang(k.DiemTichLuy)
                    };
                return new KhachVipItem
                {
                    MaKhachHang = ma,
                    HoTen = $"Khách #{ma}",
                    SoLuot = luot,
                    DoanhThu = doanhThu,
                    Hang = XepHang(0)
                };
            }

            vm.TopDoanhThu = theoKhach
                .Where(x => x.DoanhThu > 0)
                .OrderByDescending(x => x.DoanhThu).ThenByDescending(x => x.Luot)
                .Take(10)
                .Select(x => TaoVip(x.Ma, x.Luot, x.DoanhThu))
                .ToList();

            vm.TopLuotDat = theoKhach
                .OrderByDescending(x => x.Luot).ThenByDescending(x => x.DoanhThu)
                .Take(10)
                .Select(x => TaoVip(x.Ma, x.Luot, x.DoanhThu))
                .ToList();

            vm.KhachCoDon = theoKhach.Count;
            vm.TongDoanhThu = theoKhach.Sum(x => x.DoanhThu);
            vm.TyTrongDoanhThuTop = vm.TongDoanhThu == 0
                ? 0
                : Math.Round((double)(vm.TopDoanhThu.Sum(x => x.DoanhThu) / vm.TongDoanhThu) * 100.0, 1);

            // ----- 2. Khách quay lại (>= 2 đơn không hủy trong kỳ) so với khách đặt 1 lần -----
            var quayLai = theoKhach.Where(x => x.Luot >= 2).ToList();
            var motLan = theoKhach.Where(x => x.Luot == 1).ToList();

            vm.KhachQuayLai = quayLai.Count;
            vm.TyLeQuayLai = vm.KhachCoDon == 0
                ? 0
                : Math.Round(vm.KhachQuayLai * 100.0 / vm.KhachCoDon, 1);
            vm.DoanhThuQuayLai = quayLai.Sum(x => x.DoanhThu);
            vm.TyTrongDoanhThuQuayLai = vm.TongDoanhThu == 0
                ? 0
                : Math.Round((double)(vm.DoanhThuQuayLai / vm.TongDoanhThu) * 100.0, 1);
            vm.KhachMotLan = motLan.Count;
            vm.DoanhThuMotLan = motLan.Sum(x => x.DoanhThu);

            // ----- 3. Hạng khách hàng: số khách là tích lũy hiện tại, doanh thu là trong kỳ -----
            var demHang = khachHangs
                .GroupBy(k => XepHang(k.DiemTichLuy))
                .ToDictionary(g => g.Key, g => g.Count());

            var doanhThuHang = new Dictionary<string, decimal>();
            var khachChiTieuHang = new Dictionary<string, int>();
            foreach (var it in theoKhach.Where(x => x.DoanhThu > 0))
            {
                string hangCuaKhach = thongTin.TryGetValue(it.Ma, out var kh)
                    ? XepHang(kh.DiemTichLuy)
                    : XepHang(0);
                doanhThuHang[hangCuaKhach] = doanhThuHang.GetValueOrDefault(hangCuaKhach) + it.DoanhThu;
                khachChiTieuHang[hangCuaKhach] = khachChiTieuHang.GetValueOrDefault(hangCuaKhach) + 1;
            }

            vm.Hang = CacHang.Select(h =>
            {
                demHang.TryGetValue(h.Ten, out var soLuong);
                decimal dt = doanhThuHang.GetValueOrDefault(h.Ten);
                int khachCoChiTieu = khachChiTieuHang.GetValueOrDefault(h.Ten);
                return new HangKhachItem
                {
                    Ten = h.Ten,
                    MoTa = h.MoTa,
                    Css = h.Css,
                    SoLuong = soLuong,
                    TyLe = vm.TongKhachHang == 0 ? 0 : Math.Round(soLuong * 100.0 / vm.TongKhachHang, 1),
                    KhachCoChiTieu = khachCoChiTieu,
                    DoanhThu = dt,
                    ChiTieuTrungBinh = khachCoChiTieu == 0 ? 0 : Math.Round(dt / khachCoChiTieu, 0),
                    TyTrongDoanhThu = vm.TongDoanhThu == 0 ? 0 : Math.Round((double)(dt / vm.TongDoanhThu) * 100.0, 1)
                };
            }).ToList();

            // ----- 4. Các cột thời gian dùng chung cho biểu đồ (ngày / tuần / tháng) -----
            // Mỗi cột = [Tu, Den) đã được cắt trong phạm vi kỳ đang lọc
            var cot = new List<(string Ten, DateTime Tu, DateTime Den)>();
            if (donVi == "ngày")
            {
                for (int i = 0; i < soNgay; i++)
                {
                    var batDau = tuNgay.AddDays(i);
                    cot.Add((batDau.ToString("dd/MM"), batDau, batDau.AddDays(1)));
                }
            }
            else if (donVi == "tuần")
            {
                int soCot = (int)Math.Ceiling(soNgay / 7.0);
                for (int i = 0; i < soCot; i++)
                {
                    var batDau = tuNgay.AddDays(i * 7);
                    var ketThuc = batDau.AddDays(7);
                    if (ketThuc > denLoaiTru) ketThuc = denLoaiTru;
                    cot.Add((batDau.ToString("dd/MM"), batDau, ketThuc));
                }
            }
            else
            {
                var thangDau = new DateTime(tuNgay.Year, tuNgay.Month, 1);
                int soThang = (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;
                bool cungNam = tuNgay.Year == denNgay.Year;
                for (int i = 0; i < soThang; i++)
                {
                    var dauThang = thangDau.AddMonths(i);
                    var cuoiThang = dauThang.AddMonths(1);
                    var batDau = dauThang < tuNgay ? tuNgay : dauThang;
                    var ketThuc = cuoiThang > denLoaiTru ? denLoaiTru : cuoiThang;
                    string ten = cungNam ? $"T{dauThang.Month}" : $"T{dauThang.Month}/{dauThang.Year}";
                    cot.Add((ten, batDau, ketThuc));
                }
            }

            // ----- 5. Khách mới (NgayDangKy) và khách đặt lại theo từng cột -----
            var ngayDangKy = khachHangs
                .Where(k => k.NgayDangKy >= tuNgay && k.NgayDangKy < denLoaiTru)
                .Select(k => k.NgayDangKy)
                .ToList();
            vm.TongKhachMoi = ngayDangKy.Count;

            vm.KhachMoi = cot.Select(c => new ThongKeItem
            {
                Ten = c.Ten,
                SoLuong = ngayDangKy.Count(x => x >= c.Tu && x < c.Den)
            }).ToList();

            // Đơn ĐẦU TIÊN của mỗi khách trong toàn bộ lịch sử (không tính đơn hủy), xác định theo (NgayDat, MaDatSan).
            // Khách "đặt lại" trong một cột = có đơn trong cột đó KHÔNG phải đơn đầu tiên của họ.
            // Cách này tính cả khách đặt lần 2 ngay trong cùng cột (cùng tháng), nên khớp hơn với thẻ KPI "tái đặt"
            // (trước đây chỉ tính khi đơn đầu nằm trước ngày bắt đầu cột nên bỏ sót).
            var lichSuDat = await _thongKeRepository.LayDatSans().AsNoTracking()
                .Where(d => d.TrangThai != HUY && d.NgayDat < denLoaiTru)
                .Select(d => new { d.MaDatSan, d.MaKhachHang, d.NgayDat, d.TrangThai, d.DonGia })
                .ToListAsync();
            var donTheoKhach = lichSuDat
                .GroupBy(x => x.MaKhachHang)
                .ToDictionary(g => g.Key, g => g.ToList());
            var donDauTien = donTheoKhach
                .ToDictionary(kv => kv.Key, kv => kv.Value.OrderBy(x => x.NgayDat).ThenBy(x => x.MaDatSan).First().MaDatSan);

            // ----- 5b. Khách mới thực sự, chuyển đổi, nguy cơ rời bỏ, cohort (đều tính đến cuối kỳ) -----
            var ngayDauTien = donTheoKhach.ToDictionary(kv => kv.Key, kv => kv.Value.Min(x => x.NgayDat).Date);

            // Khách mới thực sự = đơn không hủy đầu tiên trong lịch sử rơi vào kỳ
            vm.KhachMoiThucSu = ngayDauTien.Count(kv => kv.Value >= tuNgay && kv.Value < denLoaiTru);
            vm.KhachMoiThucSuTruoc = ngayDauTien.Count(kv => kv.Value >= tuTruoc && kv.Value < denTruocLoaiTru);
            vm.KhachQuayLaiTuKyTruoc = theoKhach.Count(x => ngayDauTien.TryGetValue(x.Ma, out var ngayDau0) && ngayDau0 < tuNgay);
            vm.DoanhThuTruoc = donTruoc.Where(d => d.TrangThai == HOAN_THANH).Sum(d => d.DonGia);

            // Tỷ lệ chuyển đổi đăng ký -> có đơn trong 30 ngày.
            // Chỉ tính tài khoản đã đủ 30 ngày quan sát (tránh coi tài khoản mới đăng ký hôm qua là "chưa chuyển đổi").
            var dangKyDuThoiGian = khachHangs
                .Select(x => new { x.MaKhachHang, Ngay = Convert.ToDateTime(x.NgayDangKy).Date })
                .Where(x => x.Ngay >= tuNgay && x.Ngay.AddDays(CUA_SO_CHUYEN_DOI) <= denNgay.Date)
                .ToList();
            vm.SoDangKyDuThoiGian = dangKyDuThoiGian.Count;
            vm.SoDangKyChuyenDoi = dangKyDuThoiGian.Count(x =>
                donTheoKhach.TryGetValue(x.MaKhachHang, out var dsDonDk)
                && dsDonDk.Any(o => o.NgayDat >= x.Ngay && o.NgayDat < x.Ngay.AddDays(CUA_SO_CHUYEN_DOI)));
            vm.TyLeChuyenDoi = vm.SoDangKyDuThoiGian == 0
                ? 0
                : Math.Round(vm.SoDangKyChuyenDoi * 100.0 / vm.SoDangKyDuThoiGian, 1);

            // Nguy cơ rời bỏ: từng đặt >= 2 lần nhưng đã >= NGAY_NGUY_CO ngày chưa quay lại (tính đến cuối kỳ)
            vm.NguongNgayNguyCo = NGAY_NGUY_CO;
            var dsNguyCo = new List<KhachNguyCoItem>();
            int motLanLau = 0;
            foreach (var kv in donTheoKhach)
            {
                var lanCuoi = kv.Value.Max(x => x.NgayDat).Date;
                int ngayChua = (denNgay.Date - lanCuoi).Days;
                if (ngayChua < NGAY_NGUY_CO) continue;
                if (kv.Value.Count == 1) { motLanLau++; continue; }

                decimal chiTieu = kv.Value.Where(x => x.TrangThai == HOAN_THANH).Sum(x => x.DonGia);
                string hoTenNc = $"Khách #{kv.Key}", sdtNc = "", hangNc = XepHang(0);
                if (thongTin.TryGetValue(kv.Key, out var khNc))
                {
                    hoTenNc = khNc.HoTen;
                    sdtNc = khNc.SoDienThoai;
                    hangNc = XepHang(khNc.DiemTichLuy);
                }
                dsNguyCo.Add(new KhachNguyCoItem
                {
                    MaKhachHang = kv.Key,
                    HoTen = hoTenNc,
                    SoDienThoai = sdtNc,
                    Hang = hangNc,
                    SoDon = kv.Value.Count,
                    TongChiTieu = chiTieu,
                    LanDatGanNhat = lanCuoi,
                    SoNgayChuaQuayLai = ngayChua
                });
            }
            vm.SoKhachNguyCo = dsNguyCo.Count;
            vm.ChiTieuKhachNguyCo = dsNguyCo.Sum(x => x.TongChiTieu);
            vm.SoKhachMotLanLauChuaQuayLai = motLanLau;
            vm.KhachNguyCo = dsNguyCo
                .OrderByDescending(x => x.TongChiTieu).ThenByDescending(x => x.SoNgayChuaQuayLai)
                .Take(10)
                .ToList();

            // Cohort: nhóm khách theo THÁNG có đơn đầu tiên, 6 tháng gần nhất tính đến cuối kỳ.
            // Ô ở tháng chưa kết thúc là null (chưa đủ dữ liệu), không phải 0%.
            static int ChiSoThang(DateTime d) => d.Year * 12 + d.Month;
            var thangCuoi = new DateTime(denNgay.Year, denNgay.Month, 1);
            var thangCuaKhach = donTheoKhach.ToDictionary(
                kv => kv.Key, kv => kv.Value.Select(x => ChiSoThang(x.NgayDat)).ToHashSet());
            var nhomDau = ngayDauTien
                .GroupBy(kv => ChiSoThang(kv.Value))
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            for (int ci = 5; ci >= 0; ci--)
            {
                var dauThangC = thangCuoi.AddMonths(-ci);
                if (!nhomDau.TryGetValue(ChiSoThang(dauThangC), out var dsKhachC) || dsKhachC.Count == 0) continue;

                var hang = new CohortItem { Ten = $"T{dauThangC.Month}/{dauThangC.Year}", SoKhach = dsKhachC.Count };
                for (int kk = 0; kk <= 5; kk++)
                {
                    var thangK = dauThangC.AddMonths(kk);
                    if (kk == 0) { hang.TyLe.Add(100); continue; }
                    if (thangK.AddMonths(1) > denLoaiTru) { hang.TyLe.Add(null); continue; }  // tháng chưa kết thúc

                    int idxK = ChiSoThang(thangK);
                    int conLai = dsKhachC.Count(maKh => thangCuaKhach[maKh].Contains(idxK));
                    hang.TyLe.Add(Math.Round(conLai * 100.0 / dsKhachC.Count, 1));
                }
                vm.Cohort.Add(hang);
            }

            var donKhongHuy = don.Where(d => d.TrangThai != HUY).ToList();

            vm.KhachDatLai = cot.Select(c => new ThongKeItem
            {
                Ten = c.Ten,
                SoLuong = donKhongHuy
                    .Where(d => d.NgayDat >= c.Tu && d.NgayDat < c.Den
                                && donDauTien.TryGetValue(d.MaKhachHang, out var maDonDau) && maDonDau != d.MaDatSan)
                    .Select(d => d.MaKhachHang)
                    .Distinct()
                    .Count()
            }).ToList();

            // ----- 6. Hủy đơn: tổng quan, theo cột thời gian, và khách hủy nhiều lần -----
            vm.TongDon = don.Count;
            vm.SoDonHuy = don.Count(d => d.TrangThai == HUY);
            vm.TyLeHuy = vm.TongDon == 0 ? 0 : Math.Round(vm.SoDonHuy * 100.0 / vm.TongDon, 1);

            // Đơn hủy đã có tiền cọc (rủi ro mất/hoàn cọc) - dùng TienCoc có sẵn trong bảng DatSan
            var donHuyList = don.Where(d => d.TrangThai == HUY).ToList();
            vm.SoDonHuyCoCoc = donHuyList.Count(d => d.TienCoc > 0);
            vm.TienCocDonHuy = donHuyList.Sum(d => d.TienCoc);

            // Đơn đã quá giờ kết thúc nhưng vẫn "Chờ xử lý"/"Đang xử lý": có thể khách không đến hoặc nhân viên quên cập nhật.
            // Tách riêng khỏi đơn hủy vì đây là hai vấn đề vận hành khác nhau.
            var bayGio = DateTime.UtcNow.AddHours(7);
            vm.SoDonQuaGioChuaXuLy = don.Count(d =>
                (d.TrangThai == CHO_XU_LY || d.TrangThai == DANG_XU_LY) && d.GioKetThuc < bayGio);

            vm.HuyTheoKy = cot.Select(c =>
            {
                var donTrongCot = don.Where(d => d.NgayDat >= c.Tu && d.NgayDat < c.Den).ToList();
                int soHuyCot = donTrongCot.Count(d => d.TrangThai == HUY);
                return new HuyKyItem
                {
                    Ten = c.Ten,
                    TongDon = donTrongCot.Count,
                    SoHuy = soHuyCot,
                    TyLe = donTrongCot.Count == 0 ? 0 : Math.Round(soHuyCot * 100.0 / donTrongCot.Count, 1)
                };
            }).ToList();

            var huyTheoKhach = don
                .GroupBy(d => d.MaKhachHang)
                .Select(g => new
                {
                    Ma = g.Key,
                    Tong = g.Count(),
                    Huy = g.Count(x => x.TrangThai == HUY)
                })
                .Where(x => x.Huy > 0)
                .ToList();

            vm.SoKhachHuy = huyTheoKhach.Count;
            var lapLai = huyTheoKhach.Where(x => x.Huy >= 2).ToList();
            vm.SoKhachHuyLapLai = lapLai.Count;
            vm.TyLeHuyDoKhachLapLai = vm.SoDonHuy == 0
                ? 0
                : Math.Round(lapLai.Sum(x => x.Huy) * 100.0 / vm.SoDonHuy, 1);

            // Chỉ liệt kê khách hủy từ 2 đơn trở lên (khách hủy 1 đơn không đủ cơ sở để xếp hạng)
            vm.KhachHayHuy = lapLai
                .OrderByDescending(x => x.Huy)
                .ThenByDescending(x => (double)x.Huy / x.Tong)
                .Take(10)
                .Select(x =>
                {
                    double tyLe = Math.Round(x.Huy * 100.0 / x.Tong, 1);
                    string hoTen = $"Khách #{x.Ma}";
                    string sdt = "";
                    if (thongTin.TryGetValue(x.Ma, out var k))
                    {
                        hoTen = k.HoTen;
                        sdt = k.SoDienThoai;
                    }
                    return new KhachHuyItem
                    {
                        MaKhachHang = x.Ma,
                        HoTen = hoTen,
                        SoDienThoai = sdt,
                        SoDon = x.Tong,
                        SoHuy = x.Huy,
                        TyLeHuy = tyLe,
                        CanLuuY = tyLe >= 50
                    };
                })
                .ToList();

            return vm;
        }

        // ================== HÀM HỖ TRỢ ==================
        // Chỉ dùng cho "Tạm tính hôm nay" ở Dashboard:
        // TongTien = tiền sân (đơn giá x số giờ) + tiền dịch vụ
        private async Task<List<DonTien>> LayDonTienAsync(Expression<Func<DatSan, bool>> dieuKien)
        {
            var raw = await _thongKeRepository.LayDatSans().AsNoTracking()
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