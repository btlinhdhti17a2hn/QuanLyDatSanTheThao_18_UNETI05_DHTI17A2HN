
// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Quản lý thông tin khách hàng, xem và cập nhật hồ sơ cá nhân, danh sách khách hàng, tìm kiếm và phân trang

using Microsoft.AspNetCore.Mvc;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly IKhachHangRepository _repository;

        public KhachHangController(IKhachHangRepository repository)
        {
            _repository = repository;
        }

        // Kiểm tra quyền Admin hoặc Nhân viên quản lý
        private bool CoQuyenQuanLy()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin" || vaiTro == "NhanVien";
        }

        // ==========================================================
        // 1. XEM HỒ SƠ CÁ NHÂN (DÀNH CHO KHÁCH HÀNG ĐANG ĐĂNG NHẬP)
        // ==========================================================
        [AuthorizeRole]
        [HttpGet]
        public async Task<IActionResult> HoSo()
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem hồ sơ cá nhân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Sử dụng LINQ truy vấn thông tin khách hàng liên kết với Tài khoản
            var khachHang = await _repository.GetByTaiKhoanAsync(maTaiKhoan.Value);

            if (khachHang == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng tương ứng với tài khoản này!";
                return RedirectToAction("Index", "Home");
            }

            // Map Entity sang ViewModel
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

        // ==========================================================
        // 2. CẬP NHẬT HỒ SƠ CÁ NHÂN (DÀNH CHO KHÁCH HÀNG ĐANG ĐĂNG NHẬP)
        // ==========================================================
        [AuthorizeRole]
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

            // BẢO MẬT PHÂN QUYỀN: Kiểm tra khách hàng chỉ được cập nhật hồ sơ của chính mình
            var khachHang = await _repository.GetHoSoAsync(model.MaKhachHang, maTaiKhoan.Value);

            if (khachHang == null)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa hồ sơ này!";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra ngày sinh hợp lệ (phải nhỏ hơn ngày hiện tại)
            if (model.NgaySinh.HasValue && model.NgaySinh.Value.Date >= DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh phải nhỏ hơn ngày hiện tại!");
            }

            if (!ModelState.IsValid)
            {
                // Giữ lại các thông tin không cho sửa để hiển thị lại trên View
                model.TenDangNhap = khachHang.TaiKhoan?.TenDangNhap;
                model.NgayDangKy = khachHang.NgayDangKy;
                model.DiemTichLuy = khachHang.DiemTichLuy;
                return View("HoSo", model);
            }

            // Cập nhật thông tin vào Entity KhachHang
            khachHang.HoTen = model.HoTen.Trim();
            khachHang.SoDienThoai = model.SoDienThoai.Trim();
            khachHang.Email = model.Email?.Trim();
            khachHang.NgaySinh = model.NgaySinh;
            khachHang.GioiTinh = model.GioiTinh;
            khachHang.DiaChi = model.DiaChi?.Trim();

            // Đồng bộ Họ tên và Email sang TaiKhoan nếu có liên kết
            if (khachHang.TaiKhoan != null)
            {
                khachHang.TaiKhoan.HoTen = khachHang.HoTen;
                if (!string.IsNullOrEmpty(khachHang.Email))
                {
                    khachHang.TaiKhoan.Email = khachHang.Email;
                }
                // Cập nhật lại Session HoTen để hiển thị đúng trên thanh menu
                HttpContext.Session.SetString("HoTen", khachHang.HoTen);
            }

            await _repository.UpdateAsync(khachHang);

            TempData["Success"] = "Cập nhật thông tin hồ sơ cá nhân thành công!";
            return RedirectToAction(nameof(HoSo));
        }

        // ==========================================================
        // 3. QUẢN LÝ DANH SÁCH KHÁCH HÀNG (DÀNH CHO ADMIN / NHÂN VIÊN)
        // Tìm kiếm + Lọc trạng thái + Phân trang bằng LINQ EF Core
        // ==========================================================
        [AuthorizeRole("Admin", "NhanVien")]
        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int trang = 1, int kichThuocTrang = 10)
        {
            // Bắt buộc kiểm tra quyền quản lý tại Controller
            if (!CoQuyenQuanLy())
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang quản lý khách hàng!";
                return RedirectToAction("Index", "Home");
            }

            int tongSoBanGhi = await _repository.DemTongSoAsync(tuKhoa, trangThai);
            int tongSoTrang = (int)Math.Ceiling((double)tongSoBanGhi / kichThuocTrang);
            trang = Math.Max(1, Math.Min(trang, Math.Max(1, tongSoTrang)));

            var dsKhachHang = await _repository.GetAllAsync(
                tuKhoa, trangThai, trang, kichThuocTrang);

            // Truyền dữ liệu tìm kiếm, lọc và phân trang sang View
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThai = trangThai;
            ViewBag.TrangHienTai = trang;
            ViewBag.TongSoTrang = tongSoTrang;
            ViewBag.TongSoBanGhi = tongSoBanGhi;

            return View(dsKhachHang);
        }

        // ==========================================================
        // 4. XEM CHI TIẾT KHÁCH HÀNG (KÈM LỊCH SỬ ĐẶT SÂN)
        // ==========================================================
        [AuthorizeRole]
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var khachHang = await _repository.GetDetailsAsync(id);

            if (khachHang == null)
            {
                return NotFound();
            }

            // BẢO MẬT URL (Mục 7.5): Admin/Nhân viên xem được tất cả; Khách hàng chỉ xem được của chính mình
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            string? vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin" && vaiTro != "NhanVien")
            {
                if (khachHang.MaTaiKhoan != maTaiKhoan)
                {
                    TempData["Error"] = "Bạn không có quyền xem thông tin của khách hàng khác!";
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(khachHang);
        }

        // ==========================================================
        // 5. KHÓA / MỞ KHÓA TÀI KHOẢN KHÁCH HÀNG (DÀNH CHO QUẢN TRỊ)
        // ==========================================================
        [AuthorizeRole("Admin", "NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThayDoiTrangThai(int id)
        {
            if (!CoQuyenQuanLy())
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index", "Home");
            }

            var khachHang = await _repository.GetByIdAsync(id);
            if (khachHang == null) return NotFound();

            // Đảo ngược trạng thái hoạt động
            khachHang.TrangThai = !khachHang.TrangThai;
            await _repository.SaveChangesAsync();

            TempData["Success"] = $"Đã {(khachHang.TrangThai ? "mở khóa" : "khóa")} khách hàng #{khachHang.MaKhachHang} thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
