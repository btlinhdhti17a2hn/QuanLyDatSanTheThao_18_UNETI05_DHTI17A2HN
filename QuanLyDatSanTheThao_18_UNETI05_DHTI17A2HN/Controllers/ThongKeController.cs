// Họ và tên: 
// Mã sinh viên: [Điền mã sinh viên của bạn]
// Nội dung thực hiện: Controller ThongKe - Dashboard quản trị và thống kê nâng cao bằng LINQ
// (GroupBy, Sum, Count, OrderByDescending, Any): doanh thu theo tháng/loại sân,
// sân & loại sân ưa thích, dịch vụ phổ biến, số lượt hủy, khung giờ cao điểm.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class ThongKeController : Controller
    {
        // Tính số giờ thuê sân
        private static double SoGio(DateTime gioBatDau, DateTime gioKetThuc)
        {
            return Math.Max((gioKetThuc - gioBatDau).TotalHours, 0);
        }

        // Tính tiền sân = đơn giá theo giờ x số giờ thuê
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

            // Tính chuẩn doanh thu hoàn thành bằng LINQ trực tiếp từ _context
            vm.DoanhThuHoanThanh = await _context.DatSans
                .Where(d => d.TrangThai == "Hoàn thành" || d.TrangThai == HOAN_THANH)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;
            // Tạm tính hôm nay: bỏ đơn đã hủy
            var donHomNay = await LayDonTienAsync(d => d.NgayDat >= homNay && d.NgayDat < ngayMai && d.TrangThai != HUY);
            vm.DoanhThuTamTinhHomNay = donHomNay.Sum(x => x.TongTien);

            return View(vm);
        }

        // ================== THỐNG KÊ LINQ ==================
        public async Task<IActionResult> ThongKe(int? nam)
        {
            if (!LaQuanLy()) return TuChoi();

            int namChon = nam ?? DateTime.Today.Year;
            var vm = new ThongKeViewModel { Nam = namChon };

            // Danh sách năm có dữ liệu để chọn
            vm.DanhSachNam = await _context.DatSans.Select(d => d.NgayDat.Year)
                                .Distinct().OrderByDescending(y => y).ToListAsync();
            if (!vm.DanhSachNam.Contains(namChon)) vm.DanhSachNam.Insert(0, namChon);

            // Tập đơn trong năm được chọn - dùng chung cho mọi thống kê theo năm
            var datTrongNam = _context.DatSans.AsNoTracking()
                .Where(d => d.NgayDat.Year == namChon);

            // 1. Số sân theo loại
            vm.SanTheoLoai = await _context.SanTheThaos.AsNoTracking()
                .GroupBy(s => s.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // 2. Số lượt đặt theo sân (không tính đơn đã hủy) + sân được dùng nhiều nhất
            vm.LuotDatTheoSan = await datTrongNam
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.TenSan)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.SanNhieuNhat = vm.LuotDatTheoSan.FirstOrDefault();

            // 3. Loại sân được ưa thích nhất
            vm.LuotDatTheoLoai = await datTrongNam
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.LoaiSanUaThich = vm.LuotDatTheoLoai.FirstOrDefault();

            // 4. Doanh thu theo tháng + theo loại sân (đơn Hoàn thành trong năm chọn)
            var donTrongNam = await LayDonTienAsync(d => d.TrangThai == HOAN_THANH && d.NgayDat.Year == namChon);

            var theoThang = donTrongNam.GroupBy(x => x.NgayDat.Month)
                                       .ToDictionary(g => g.Key, g => new { Tien = g.Sum(x => x.TongTien), SoDon = g.Count() });
            vm.DoanhThuTheoThang = Enumerable.Range(1, 12)
                .Select(m => new ThongKeItem
                {
                    Ten = $"Tháng {m}",
                    GiaTri = theoThang.ContainsKey(m) ? theoThang[m].Tien : 0,
                    SoLuong = theoThang.ContainsKey(m) ? theoThang[m].SoDon : 0
                }).ToList();

            vm.DoanhThuTheoLoai = donTrongNam.GroupBy(x => x.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, GiaTri = g.Sum(x => x.TongTien), SoLuong = g.Count() })
                .OrderByDescending(x => x.GiaTri)
                .ToList();
            vm.TongDoanhThuNam = donTrongNam.Sum(x => x.TongTien);

            // 5. Dịch vụ được sử dụng nhiều nhất (Top 5 theo số lượng)
            vm.DichVuPhoBien = await _context.ChiTietDatSans.AsNoTracking()
                .Where(c => c.DatSan!.TrangThai != HUY && c.DatSan.NgayDat.Year == namChon)
                .GroupBy(c => c.DichVu!.TenDichVu)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key,
                    SoLuong = g.Sum(c => c.SoLuong),
                    GiaTri = g.Sum(c => c.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToListAsync();

            // 6. Số lượt hủy và tỷ lệ hủy
            vm.SoLuotHuy = await datTrongNam.CountAsync(d => d.TrangThai == HUY);
            vm.TongLuotDat = await datTrongNam.CountAsync();

            // 7. Tỷ lệ sử dụng theo khung giờ (theo giờ bắt đầu, bỏ đơn hủy)
            var gioBatDau = await datTrongNam
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
        // Lấy các đơn theo điều kiện, tính sẵn TongTien = tiền sân + tiền dịch vụ
        // (tiền sân do TinhTienHelper của thành viên khác tính)
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
                TongTien = TienSan(
    x.DonGia, x.GioBatDau, x.GioKetThuc) + x.TienDichVu
            }).ToList();
        }
    }
}