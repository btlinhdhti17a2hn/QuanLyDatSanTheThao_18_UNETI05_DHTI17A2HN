
// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Controller TaiKhoan - Xử lý đăng nhập, đăng xuất và phân quyền

using Microsoft.AspNetCore.Mvc;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;
using System.Security.Cryptography;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Services;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly ITaiKhoanRepository _repository;
        private readonly IEmailService _emailService;

        public TaiKhoanController(ITaiKhoanRepository repository, IEmailService emailService)
        {
            _repository = repository;
            _emailService = emailService;
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
        // Bước 1: nhập tên đăng nhập + email -> gửi mã 6 số về email
        // Bước 2: nhập mã + mật khẩu mới -> đúng mã mới được đổi mật khẩu
        // ==========================================================
        private const int SoLanSaiToiDa = 5;        // sai thông tin ở bước 1
        private const int PhutKhoaSauSaiNhieu = 5;
        private const int SoPhutHieuLucMa = 5;      // mã có hiệu lực 5 phút
        private const int SoGiayGuiLai = 60;        // 2 lần gửi mã cách nhau tối thiểu 60 giây
        private const int SoLanNhapMaToiDa = 5;     // nhập sai mã tối đa 5 lần

        // Xóa toàn bộ thông tin mã xác nhận trong Session
        private void XoaPhienQuenMatKhau()
        {
            HttpContext.Session.Remove("QMK_MaTaiKhoan");
            HttpContext.Session.Remove("QMK_Ma");
            HttpContext.Session.Remove("QMK_HetHan");
            HttpContext.Session.Remove("QMK_SoLanSai");
            HttpContext.Session.Remove("QMK_EmailAn");
        }

        // kh1@gmail.com -> k***@gmail.com
        private static string AnEmail(string email)
        {
            int viTriAt = email.IndexOf('@');
            if (viTriAt <= 1) return email;
            return email[0] + new string('*', Math.Min(viTriAt - 1, 5)) + email.Substring(viTriAt);
        }

        private static string NoiDungEmailMa(string hoTen, string ma)
        {
            string ten = System.Net.WebUtility.HtmlEncode(hoTen);
            return $@"
<div style=""font-family:'Segoe UI',Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #bbf7d0;border-radius:16px"">
  <h2 style=""color:#14532d;margin:0 0 12px"">🏟️ SPORTS GROUND</h2>
  <p>Xin chào <b>{ten}</b>,</p>
  <p>Mã xác nhận đặt lại mật khẩu của bạn là:</p>
  <p style=""font-size:34px;font-weight:700;letter-spacing:8px;color:#16a34a;text-align:center;margin:16px 0"">{ma}</p>
  <p>Mã có hiệu lực trong <b>{SoPhutHieuLucMa} phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
  <p style=""color:#6b7280;font-size:13px"">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
</div>";
        }

        // ---------- BƯỚC 1: nhập tên đăng nhập + email ----------
        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            if (HttpContext.Session.GetInt32("MaTaiKhoan") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (TempData["ThongBaoQuenMK"] != null)
            {
                ViewBag.Error = TempData["ThongBaoQuenMK"];
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(string? tenDangNhap, string? email)
        {
            // Chặn thử nhiều lần liên tiếp
            var khoaDen = HttpContext.Session.GetString("QuenMK_KhoaDen");
            if (long.TryParse(khoaDen, out long ticksKhoa) && DateTime.UtcNow.Ticks < ticksKhoa)
            {
                ViewBag.Error = $"Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau {PhutKhoaSauSaiNhieu} phút!";
                return View();
            }

            tenDangNhap = tenDangNhap?.Trim();
            email = email?.Trim();

            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và email!";
                return View();
            }

            // Chỉ tài khoản khách hàng, email phải trùng với email đã đăng ký
            var taiKhoan = _repository.GetByTenDangNhap(tenDangNhap);
            bool hopLe = taiKhoan != null
                && taiKhoan.VaiTro == "KhachHang"
                && !string.IsNullOrEmpty(taiKhoan.Email)
                && string.Equals(taiKhoan.Email.Trim(), email, StringComparison.OrdinalIgnoreCase);

            if (!hopLe || taiKhoan == null)
            {
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

                // Thông báo chung, không tiết lộ sai ở tên đăng nhập hay ở email
                ViewBag.Error = "Tên đăng nhập hoặc email không đúng, hoặc tài khoản không được hỗ trợ đặt lại mật khẩu!";
                return View();
            }

            if (!taiKhoan.TrangThai)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin!";
                return View();
            }

            // Giới hạn tần suất gửi mã
            if (long.TryParse(HttpContext.Session.GetString("QMK_GuiLuc"), out long guiLuc))
            {
                double daTroi = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - guiLuc).TotalSeconds;
                if (daTroi < SoGiayGuiLai)
                {
                    ViewBag.Error = $"Vui lòng đợi {Math.Ceiling(SoGiayGuiLai - daTroi)} giây rồi yêu cầu gửi lại mã!";
                    return View();
                }
            }

            // Sinh mã 6 số ngẫu nhiên và gửi về email đã đăng ký
            string ma = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            try
            {
                await _emailService.GuiEmailAsync(
                    taiKhoan.Email.Trim(),
                    "Mã xác nhận đặt lại mật khẩu - SPORTS GROUND",
                    NoiDungEmailMa(taiKhoan.HoTen, ma));
            }
            catch (Exception)
            {
                ViewBag.Error = "Không gửi được email lúc này. Vui lòng thử lại sau!";
                return View();
            }

            // Chỉ lưu mã sau khi gửi thành công (mã nằm trong Session phía máy chủ)
            HttpContext.Session.SetInt32("QMK_MaTaiKhoan", taiKhoan.MaTaiKhoan);
            HttpContext.Session.SetString("QMK_Ma", ma);
            HttpContext.Session.SetString("QMK_HetHan", DateTime.UtcNow.AddMinutes(SoPhutHieuLucMa).Ticks.ToString());
            HttpContext.Session.SetInt32("QMK_SoLanSai", 0);
            HttpContext.Session.SetString("QMK_EmailAn", AnEmail(taiKhoan.Email.Trim()));
            HttpContext.Session.SetString("QMK_GuiLuc", DateTime.UtcNow.Ticks.ToString());
            HttpContext.Session.Remove("QuenMK_SoLanSai");

            return RedirectToAction(nameof(DatLaiMatKhau));
        }

        // ---------- BƯỚC 2: nhập mã + mật khẩu mới ----------
        [HttpGet]
        public IActionResult DatLaiMatKhau()
        {
            if (HttpContext.Session.GetInt32("QMK_MaTaiKhoan") == null
                || string.IsNullOrEmpty(HttpContext.Session.GetString("QMK_Ma")))
            {
                TempData["ThongBaoQuenMK"] = "Vui lòng nhập thông tin để nhận mã xác nhận!";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            ViewBag.EmailAn = HttpContext.Session.GetString("QMK_EmailAn");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiMatKhau(string? maXacNhan, string? matKhauMoi, string? xacNhanMatKhau)
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("QMK_MaTaiKhoan");
            string? maDung = HttpContext.Session.GetString("QMK_Ma");

            if (maTaiKhoan == null || string.IsNullOrEmpty(maDung))
            {
                TempData["ThongBaoQuenMK"] = "Vui lòng nhập thông tin để nhận mã xác nhận!";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            ViewBag.EmailAn = HttpContext.Session.GetString("QMK_EmailAn");

            // Mã hết hạn
            if (!long.TryParse(HttpContext.Session.GetString("QMK_HetHan"), out long hetHan)
                || DateTime.UtcNow.Ticks > hetHan)
            {
                XoaPhienQuenMatKhau();
                TempData["ThongBaoQuenMK"] = "Mã xác nhận đã hết hạn. Vui lòng yêu cầu gửi mã mới!";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            maXacNhan = maXacNhan?.Trim();

            if (string.IsNullOrEmpty(maXacNhan) || string.IsNullOrEmpty(matKhauMoi) || string.IsNullOrEmpty(xacNhanMatKhau))
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

            // Kiểm tra mã, sai quá số lần cho phép thì hủy mã
            if (maXacNhan != maDung)
            {
                int soLanSai = (HttpContext.Session.GetInt32("QMK_SoLanSai") ?? 0) + 1;
                if (soLanSai >= SoLanNhapMaToiDa)
                {
                    XoaPhienQuenMatKhau();
                    TempData["ThongBaoQuenMK"] = "Bạn đã nhập sai mã quá nhiều lần. Vui lòng yêu cầu gửi mã mới!";
                    return RedirectToAction(nameof(QuenMatKhau));
                }

                HttpContext.Session.SetInt32("QMK_SoLanSai", soLanSai);
                ViewBag.Error = $"Mã xác nhận không đúng! Bạn còn {SoLanNhapMaToiDa - soLanSai} lần thử.";
                return View();
            }

            var taiKhoan = await _repository.GetByIdAsync(maTaiKhoan.Value);
            if (taiKhoan == null || !taiKhoan.TrangThai)
            {
                XoaPhienQuenMatKhau();
                TempData["ThongBaoQuenMK"] = "Tài khoản không tồn tại hoặc đã bị khóa!";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            // Cập nhật mật khẩu mới (đồ án lưu plain text giống chức năng Đăng ký)
            taiKhoan.MatKhau = matKhauMoi;
            await _repository.SaveChangesAsync();

            XoaPhienQuenMatKhau();
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
