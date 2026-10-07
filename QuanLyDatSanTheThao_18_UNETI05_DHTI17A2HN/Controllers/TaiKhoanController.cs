// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Controller TaiKhoan - Xử lý đăng nhập, đăng xuất và phân quyền

using Microsoft.AspNetCore.Mvc;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TaiKhoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị giao diện Đăng nhập
        [HttpGet]
        public IActionResult DangNhap()
        {
            // Nếu đã đăng nhập rồi thì không cho vào trang này nữa, đẩy về Trang chủ
            if (HttpContext.Session.GetInt32("MaTaiKhoan") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Xử lý dữ liệu khi người dùng bấm nút Đăng nhập
        [HttpPost]
        public IActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            if (!string.IsNullOrEmpty(tenDangNhap) && !string.IsNullOrEmpty(matKhau))
            {
                // Sử dụng LINQ để truy vấn kiểm tra tài khoản trong Database
                var user = _context.TaiKhoans.FirstOrDefault(x => x.TenDangNhap == tenDangNhap && x.MatKhau == matKhau);

                if (user != null)
                {
                    // Kiểm tra tài khoản có bị khóa không
                    if (user.TrangThai == false)
                    {
                        ViewBag.Error = "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin!";
                        return View();
                    }

                    // Lưu thông tin vào Session khi đăng nhập thành công
                    HttpContext.Session.SetInt32("MaTaiKhoan", user.MaTaiKhoan);
                    HttpContext.Session.SetString("HoTen", user.HoTen);
                    HttpContext.Session.SetString("VaiTro", user.VaiTro);

                    // Chuyển hướng về trang chủ (Sau này Admin có thể chuyển hướng về trang Dashboard)
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
            // Xóa toàn bộ Session
            HttpContext.Session.Clear();

            // Đẩy về trang chủ
            return RedirectToAction("Index", "Home");
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
            var existingUser = _context.TaiKhoans.FirstOrDefault(x => x.TenDangNhap == tenDangNhap);
            if (existingUser != null)
            {
                ViewBag.Error = "Tên đăng nhập này đã có người sử dụng, vui lòng chọn tên khác!";
                return View();
            }

            // 1. Tạo bản ghi Tài khoản mới (Vai trò mặc định là KhachHang)
            var taiKhoanMoi = new TaiKhoan
            {
                TenDangNhap = tenDangNhap,
                MatKhau = matKhau,
                HoTen = hoTen,
                Email = email ?? "",
                VaiTro = "KhachHang",
                TrangThai = true
            };

            _context.TaiKhoans.Add(taiKhoanMoi);
            await _context.SaveChangesAsync(); // Lưu để lấy ra MaTaiKhoan vừa sinh ra

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

            _context.KhachHangs.Add(khachHangMoi);
            await _context.SaveChangesAsync();

            // Đăng ký thành công thì thông báo và chuyển hướng sang trang Đăng nhập
            TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }
    }
}
