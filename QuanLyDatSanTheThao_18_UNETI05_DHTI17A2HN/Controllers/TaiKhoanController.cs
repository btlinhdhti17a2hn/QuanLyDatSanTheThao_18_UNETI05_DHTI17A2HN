
// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Controller TaiKhoan - Xử lý đăng nhập, đăng xuất và phân quyền

using Microsoft.AspNetCore.Mvc;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly ITaiKhoanRepository _repository;

        public TaiKhoanController(ITaiKhoanRepository repository)
        {
            _repository = repository;
        }

        // Hiển thị giao diện Đăng nhập
        [HttpGet]
        public IActionResult DangNhap()
        {
            if (HttpContext.Session.GetInt32("MaTaiKhoan") != null)
            {
                var vaiTroHienTai = HttpContext.Session.GetString("VaiTro");
                if (vaiTroHienTai == "Admin")
                {
                    return RedirectToAction("Index", "ThongKe");
                }
                if (vaiTroHienTai == "NhanVien")
                {
                    return RedirectToAction("Index", "TiepNhanDatSan");
                }
                return RedirectToAction("Index", "Home");
            }

            // Hiển thị thông báo do bộ lọc AuthorizeRole truyền sang (chưa đăng nhập / tài khoản bị khóa)
            if (TempData["ThongBaoDangNhap"] != null)
            {
                ViewBag.Error = TempData["ThongBaoDangNhap"];
            }
            return View();
        }

        // Xử lý dữ liệu khi người dùng bấm nút Đăng nhập
        [HttpPost]
        public IActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            if (!string.IsNullOrEmpty(tenDangNhap) && !string.IsNullOrEmpty(matKhau))
            {
                // Sử dụng LINQ để truy vấn kiểm tra tài khoản trong Database[cite: 1]
                var user = _repository.GetByDangNhap(tenDangNhap, matKhau);

                if (user != null)
                {
                    // Kiểm tra tài khoản có bị khóa không[cite: 1]
                    if (user.TrangThai == false)
                    {
                        ViewBag.Error = "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin!";
                        return View();
                    }

                    // Lưu thông tin vào Session khi đăng nhập thành công[cite: 1]
                    HttpContext.Session.SetInt32("MaTaiKhoan", user.MaTaiKhoan);
                    HttpContext.Session.SetString("HoTen", user.HoTen);
                    HttpContext.Session.SetString("VaiTro", user.VaiTro);

                    // Admin vào Dashboard thống kê, Nhân viên vào Tiếp nhận đặt sân, khách hàng về Trang chủ
                    if (user.VaiTro == "Admin")
                    {
                        return RedirectToAction("Index", "ThongKe");
                    }
                    if (user.VaiTro == "NhanVien")
                    {
                        return RedirectToAction("Index", "TiepNhanDatSan");
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }
            return View();
        }

        // Xử lý Đăng xuất
        public IActionResult DangXuat()
        {
            // Xóa toàn bộ Session[cite: 1]
            HttpContext.Session.Clear();

            // Đẩy về trang chủ
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Hiển thị trang Đăng ký
        [HttpGet]
        public IActionResult DangKy()
        {
            if (HttpContext.Session.GetInt32("MaTaiKhoan") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Xử lý thông tin Đăng ký
        [HttpPost]
        public async Task<IActionResult> DangKy(string tenDangNhap, string matKhau, string xacNhanMatKhau, string hoTen, string email, string soDienThoai)
        {
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(soDienThoai))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc!";
                return View();
            }

            if (matKhau != xacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            // Kiểm tra xem Tên đăng nhập đã tồn tại trong CSDL chưa (dùng LINQ)
            var existingUser = _repository.GetByTenDangNhap(tenDangNhap);
            if (existingUser != null)
            {
                ViewBag.Error = "Tên đăng nhập này đã có người sử dụng, vui lòng chọn tên khác!";
                return View();
            }

            // 1. Tạo bản ghi Tài khoản mới (Vai trò mặc định là KhachHang)
            var taiKhoanMoi = new TaiKhoan
            {
                TenDangNhap = tenDangNhap,
                MatKhau = matKhau, // (Đồ án cơ bản lưu plain text, nâng cao có thể mã hóa)
                HoTen = hoTen,
                Email = email ?? "",
                VaiTro = "KhachHang",
                TrangThai = true
            };

            await _repository.AddTaiKhoanAsync(taiKhoanMoi);

            // 2. Tạo bản ghi Khách hàng tương ứng liên kết với Tài khoản vừa tạo
            var khachHangMoi = new KhachHang
            {
                MaTaiKhoan = taiKhoanMoi.MaTaiKhoan,
                HoTen = hoTen,
                SoDienThoai = soDienThoai,
                Email = email ?? "",
                NgayDangKy = DateTime.Now,
                DiemTichLuy = 0,
                TrangThai = true
            };

            await _repository.AddKhachHangAsync(khachHangMoi);

            // Đăng ký thành công thì thông báo và chuyển hướng sang trang Đăng nhập
            TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // ==========================================================
        // QUÊN MẬT KHẨU (dành cho khách hàng)
        // Xác minh bằng tên đăng nhập + email hoặc số điện thoại đã đăng ký, sau đó đặt mật khẩu mới
        // ==========================================================
        private const int SoLanSaiToiDa = 5;
        private const int PhutKhoaSauSaiNhieu = 5;

        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            // Đã đăng nhập rồi thì không cần dùng chức năng này
            if (HttpContext.Session.GetInt32("MaTaiKhoan") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(string? tenDangNhap, string? thongTinXacMinh, string? matKhauMoi, string? xacNhanMatKhau)
        {
            // Chặn thử nhiều lần liên tiếp
            var khoaDen = HttpContext.Session.GetString("QuenMK_KhoaDen");
            if (long.TryParse(khoaDen, out long ticks) && DateTime.UtcNow.Ticks < ticks)
            {
                ViewBag.Error = $"Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau {PhutKhoaSauSaiNhieu} phút!";
                return View();
            }

            tenDangNhap = tenDangNhap?.Trim();
            thongTinXacMinh = thongTinXacMinh?.Trim();

            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(thongTinXacMinh)
                || string.IsNullOrEmpty(matKhauMoi) || string.IsNullOrEmpty(xacNhanMatKhau))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc!";
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            if (matKhauMoi.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            // Xác minh: chỉ tài khoản khách hàng, thông tin phải trùng email hoặc số điện thoại đã đăng ký
            var taiKhoan = _repository.GetByTenDangNhap(tenDangNhap);
            bool xacMinhDung = false;

            if (taiKhoan != null && taiKhoan.VaiTro == "KhachHang")
            {
                bool trungEmail = !string.IsNullOrEmpty(taiKhoan.Email)
                    && string.Equals(taiKhoan.Email.Trim(), thongTinXacMinh, StringComparison.OrdinalIgnoreCase);

                var khachHang = _repository.GetKhachHangByMaTaiKhoan(taiKhoan.MaTaiKhoan);
                bool trungSdt = khachHang != null && khachHang.SoDienThoai.Trim() == thongTinXacMinh;

                xacMinhDung = trungEmail || trungSdt;
            }

            if (!xacMinhDung || taiKhoan == null)
            {
                // Đếm số lần sai, đủ 5 lần thì khóa tạm thời
                int soLanSai = (HttpContext.Session.GetInt32("QuenMK_SoLanSai") ?? 0) + 1;
                if (soLanSai >= SoLanSaiToiDa)
                {
                    long hetKhoa = DateTime.UtcNow.AddMinutes(PhutKhoaSauSaiNhieu).Ticks;
                    HttpContext.Session.SetString("QuenMK_KhoaDen", hetKhoa.ToString());
                    HttpContext.Session.SetInt32("QuenMK_SoLanSai", 0);
                }
                else
                {
                    HttpContext.Session.SetInt32("QuenMK_SoLanSai", soLanSai);
                }

                // Thông báo chung, không tiết lộ sai ở tên đăng nhập hay ở thông tin xác minh
                ViewBag.Error = "Thông tin xác minh không đúng hoặc tài khoản không được hỗ trợ đặt lại mật khẩu!";
                return View();
            }

            if (!taiKhoan.TrangThai)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin!";
                return View();
            }

            // Cập nhật mật khẩu mới (đồ án lưu plain text giống chức năng Đăng ký)
            taiKhoan.MatKhau = matKhauMoi;
            await _repository.SaveChangesAsync();

            HttpContext.Session.Remove("QuenMK_SoLanSai");
            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction("DangNhap");
        }

        // ==========================================================
        // PHẦN QUẢN LÝ TÀI KHOẢN (DÀNH RIÊNG CHO ADMIN)
        // ==========================================================

        // Hàm kiểm tra quyền Admin tại Controller
        private bool KiemTraQuyenAdmin()
        {
            return HttpContext.Session.GetString("VaiTro") == "Admin";
        }

        // 1. Xem danh sách tài khoản
        [AuthorizeRole("Admin")]
        [HttpGet]
        public IActionResult QuanLyTaiKhoan()
        {
            // Bắt buộc kiểm tra quyền tại Controller theo đúng yêu cầu đề bài
            if (!KiemTraQuyenAdmin())
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var dsTaiKhoan = _repository.GetAll();
            return View(dsTaiKhoan);
        }

        // 2. Khóa / Mở khóa tài khoản
        [AuthorizeRole("Admin")]
        [HttpPost]
        public async Task<IActionResult> ThayDoiTrangThai(int id)
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var taiKhoan = await _repository.GetByIdAsync(id);
            if (taiKhoan == null) return NotFound();

            // Không cho phép Admin tự khóa tài khoản Admin khác để tránh lỗi hệ thống
            if (taiKhoan.VaiTro == "Admin")
            {
                TempData["Error"] = "Không thể khóa tài khoản Quản trị viên!";
                return RedirectToAction(nameof(QuanLyTaiKhoan));
            }

            // Đảo ngược trạng thái (Đang true thành false, đang false thành true)
            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _repository.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái tài khoản thành công!";
            return RedirectToAction(nameof(QuanLyTaiKhoan));
        }
    }
}
