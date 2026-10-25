// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Bộ lọc phân quyền (AuthorizeRole)

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _acceptedRoles;

        // Nhận danh sách vai trò hợp lệ (để trống = chỉ cần đã đăng nhập)
        public AuthorizeRoleAttribute(params string[] roles)
        {
            _acceptedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var maTaiKhoan = session.GetInt32("MaTaiKhoan");
            var vaiTro = session.GetString("VaiTro");

            // 1. Chưa đăng nhập -> chuyển về trang Đăng nhập
            if (!maTaiKhoan.HasValue || string.IsNullOrEmpty(vaiTro))
            {
                if (context.Controller is Controller ctrl)
                {
                    ctrl.TempData["Error"] = "Vui lòng đăng nhập để sử dụng chức năng này!";
                }
                context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", null);
                return;
            }

            // 2. Kiểm tra trạng thái tài khoản từ CSDL ở mỗi request
            var db = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            if (db != null)
            {
                var user = db.TaiKhoans.AsNoTracking()
                             .FirstOrDefault(t => t.MaTaiKhoan == maTaiKhoan.Value);

                if (user == null || !user.TrangThai)
                {
                    // Tài khoản bị khóa/không tồn tại -> xóa Session và về Đăng nhập
                    session.Clear();
                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["Error"] = "Tài khoản của bạn đã bị khóa hoặc không còn hiệu lực. Vui lòng liên hệ Quản trị viên.";
                    }
                    context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", null);
                    return;
                }

                // Đồng bộ vai trò nếu Admin vừa đổi
                if (!string.Equals(user.VaiTro, vaiTro, StringComparison.OrdinalIgnoreCase))
                {
                    vaiTro = user.VaiTro;
                    session.SetString("VaiTro", vaiTro);
                    session.SetString("HoTen", user.HoTen);
                }
            }

            // 3. Không đúng quyền -> sang trang AccessDenied
            if (_acceptedRoles.Length > 0 && !_acceptedRoles.Contains(vaiTro))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "TaiKhoan", null);
                return;
            }

            // 4. Đủ quyền: ngăn trình duyệt lưu cache trang nội bộ
            var response = context.HttpContext.Response;
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";

            base.OnActionExecuting(context);
        }
    }
}