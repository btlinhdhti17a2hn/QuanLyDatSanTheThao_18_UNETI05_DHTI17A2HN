// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Quản lý thông tin khách hàng, xem và cập nhật hồ sơ cá nhân

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KhachHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> HoSo()
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem hồ sơ cá nhân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng tương ứng với tài khoản này!";
                return RedirectToAction("Index", "Home");
            }

            var model = new HoSoKhachHangViewModel
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaTaiKhoan = khachHang.MaTaiKhoan,
                TenDangNhap = khachHang.TaiKhoan?.TenDangNhap,
                HoTen = khachHang.HoTen,
                NgaySinh = khachHang.NgaySinh,
                GioiTinh = khachHang.GioiTinh,
                SoDienThoai = khachHang.SoDienThoai,
                Email = khachHang.Email,
                DiaChi = khachHang.DiaChi,
                NgayDangKy = khachHang.NgayDangKy,
                DiemTichLuy = khachHang.DiemTichLuy,
                GhiChu = khachHang.GhiChu
            };

            return View(model);
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatHoSo(HoSoKhachHangViewModel model)
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKhachHang == model.MaKhachHang && k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa hồ sơ này!";
                return RedirectToAction("Index", "Home");
            }


            if (model.NgaySinh.HasValue && model.NgaySinh.Value.Date >= DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh phải nhỏ hơn ngày hiện tại!");
            }

            if (!ModelState.IsValid)
            {
 
                model.TenDangNhap = khachHang.TaiKhoan?.TenDangNhap;
                model.NgayDangKy = khachHang.NgayDangKy;
                model.DiemTichLuy = khachHang.DiemTichLuy;
                return View("HoSo", model);
            }

 
            khachHang.HoTen = model.HoTen.Trim();
            khachHang.SoDienThoai = model.SoDienThoai.Trim();
            khachHang.Email = model.Email?.Trim();
            khachHang.NgaySinh = model.NgaySinh;
            khachHang.GioiTinh = model.GioiTinh;
            khachHang.DiaChi = model.DiaChi?.Trim();

            if (khachHang.TaiKhoan != null)
            {
                khachHang.TaiKhoan.HoTen = khachHang.HoTen;
                if (!string.IsNullOrEmpty(khachHang.Email))
                {
                    khachHang.TaiKhoan.Email = khachHang.Email;
                }
 
                HttpContext.Session.SetString("HoTen", khachHang.HoTen);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin hồ sơ cá nhân thành công!";
            return RedirectToAction(nameof(HoSo));
        }
    }
}
