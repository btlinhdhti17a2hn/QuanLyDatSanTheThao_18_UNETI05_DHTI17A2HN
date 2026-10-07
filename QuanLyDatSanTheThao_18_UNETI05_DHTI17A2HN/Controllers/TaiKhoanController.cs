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
    }
}
