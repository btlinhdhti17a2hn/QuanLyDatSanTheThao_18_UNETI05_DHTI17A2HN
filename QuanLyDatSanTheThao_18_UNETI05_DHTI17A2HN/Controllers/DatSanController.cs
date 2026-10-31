// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Controller Đặt sân (Nghiệp vụ tạo đặt sân, kiểm tra trùng lịch LINQ, giờ mở cửa,
//                     theo dõi lịch sử đặt và hủy đặt sân, quy trình thanh toán mô phỏng quét mã QR trên điện thoại)

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class DatSanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm tiện ích: Lấy thông tin khách hàng đang đăng nhập dựa trên Session MaTaiKhoan
        private async Task<KhachHang?> LayKhachHangHienTaiAsync()
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null) return null;

            return await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);
        }

        // ==========================================================
        // 1. TẠO ĐẶT SÂN - GET
        // Hỗ trợ cả 2 Action 'Create' (do các thành viên khác gọi) và 'TaoMoi' (tiếng Việt)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> TaoMoi(int? maSan)
        {
            return await XuLyGiaoDienDatSan(maSan);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? maSan)
        {
            return await XuLyGiaoDienDatSan(maSan);
        }

        private async Task<IActionResult> XuLyGiaoDienDatSan(int? maSan)
        {
            var khachHang = await LayKhachHangHienTaiAsync();
            if (khachHang == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thực hiện đặt sân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (!khachHang.TrangThai)
            {
                TempData["Error"] = "Tài khoản khách hàng của bạn đang bị khóa, không thể đặt sân!";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách các sân đang hoạt động để hiển thị trên dropdown
            ViewBag.DanhSachSan = await _context.SanTheThaos
                .Include(s => s.LoaiSan)
                .Where(s => s.TrangThai)
                .OrderBy(s => s.TenSan)
                .ToListAsync();

            var model = new DatSanViewModel
            {
                NgaySuDung = DateTime.Today.AddDays(1), // Mặc định gợi ý ngày mai
                GioBatDau = new TimeSpan(17, 0, 0),     // Mặc định gợi ý 17:00
                GioKetThuc = new TimeSpan(19, 0, 0)     // Mặc định gợi ý 19:00
            };

            if (maSan.HasValue)
            {
                model.MaSan = maSan.Value;
                var san = await _context.SanTheThaos.Include(s => s.LoaiSan).FirstOrDefaultAsync(s => s.MaSan == maSan.Value);
                if (san != null)
                {
                    model.TenSan = san.TenSan;
                    model.TenLoaiSan = san.LoaiSan?.TenLoai;
                    model.DonGiaTheoGio = san.DonGia;
                }
            }

            return View("TaoMoi", model);
        }

        // ==========================================================
        // 2. TẠO ĐẶT SÂN - POST
        // Hỗ trợ cả 2 Action 'Create' và 'TaoMoi'
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMoi(DatSanViewModel model)
        {
            return await XuLyLuuDatSan(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DatSanViewModel model)
        {
            return await XuLyLuuDatSan(model);
        }

        private async Task<IActionResult> XuLyLuuDatSan(DatSanViewModel model)
        {
            var khachHang = await LayKhachHangHienTaiAsync();
            if (khachHang == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Nạp lại danh sách sân phòng trường hợp validation thất bại
            ViewBag.DanhSachSan = await _context.SanTheThaos
                .Include(s => s.LoaiSan)
                .Where(s => s.TrangThai)
                .OrderBy(s => s.TenSan)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return View("TaoMoi", model);
            }

            // 1. Kiểm tra đối tượng sân thể thao tồn tại và đang hoạt động
            var san = await _context.SanTheThaos.Include(s => s.LoaiSan).FirstOrDefaultAsync(s => s.MaSan == model.MaSan);
            if (san == null || !san.TrangThai)
            {
                ModelState.AddModelError("MaSan", "Sân thể thao được chọn hiện không tồn tại hoặc đã ngừng hoạt động!");
                return View("TaoMoi", model);
            }

            // 2. Tính toán mốc thời gian bắt đầu và kết thúc
            DateTime dtBatDau = model.NgaySuDung.Date + model.GioBatDau;
            DateTime dtKetThuc = model.NgaySuDung.Date + model.GioKetThuc;

            // Ràng buộc thời gian: Không đặt thời điểm trong quá khứ
            if (dtBatDau < DateTime.Now)
            {
                ModelState.AddModelError("GioBatDau", "Thời gian bắt đầu đặt sân phải từ thời điểm hiện tại trở đi!");
                return View("TaoMoi", model);
            }

            // Ràng buộc thời gian: Giờ kết thúc phải sau giờ bắt đầu
            if (dtKetThuc <= dtBatDau)
            {
                ModelState.AddModelError("GioKetThuc", "Giờ kết thúc phải sau giờ bắt đầu!");
                return View("TaoMoi", model);
            }

            // Ràng buộc thời lượng: Tối thiểu 30 phút
            double soGio = (dtKetThuc - dtBatDau).TotalHours;
            if (soGio < 0.5)
            {
                ModelState.AddModelError("GioKetThuc", "Thời lượng thuê sân tối thiểu là 30 phút!");
                return View("TaoMoi", model);
            }

            // Ràng buộc Mục 8.3: Chỉ được đặt trong khung giờ mở/đóng cửa của sân
            if (model.GioBatDau < san.GioMoCua || model.GioKetThuc > san.GioDongCua)
            {
                ModelState.AddModelError("", $"Sân chỉ hoạt động trong khung giờ mở cửa từ {san.GioMoCua:hh\\:mm} đến {san.GioDongCua:hh\\:mm}!");
                return View("TaoMoi", model);
            }

            // Ràng buộc Mục 8.3 & 15: KIỂM TRA TRÙNG LỊCH BẰNG LINQ
            // Không cho cùng 1 sân có 2 lịch đặt giao nhau về thời gian (loại trừ các đơn 'Đã hủy' hoặc 'Từ chối')
            bool daBiTrungLich = await _context.DatSans.AnyAsync(d =>
                d.MaSan == model.MaSan
                && d.TrangThai != "Đã hủy"
                && d.TrangThai != "Từ chối"
                && d.GioBatDau < dtKetThuc
                && d.GioKetThuc > dtBatDau);

            if (daBiTrungLich)
            {
                ModelState.AddModelError("", "Sân đã có người đặt trong khoảng thời gian này! Vui lòng chọn khung giờ hoặc sân khác.");
                return View("TaoMoi", model);
            }

            // 3. Tính toán chi phí: Tổng tiền dự kiến và Tiền cọc (30% tổng tiền, không vượt tổng tiền dự kiến theo Mục 8.3)
            decimal tongTien = Math.Round((decimal)soGio * san.DonGia, 0);
            decimal tienCoc = Math.Round(tongTien * 0.3m, 0);

            // 4. Khởi tạo đối tượng DatSan theo đúng quy định:
            // Trạng thái đặt sân khởi tạo là 'Chờ duyệt'
            var datSanMoi = new DatSan
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaSan = model.MaSan,
                NgayDat = DateTime.Now,
                GioBatDau = dtBatDau,
                GioKetThuc = dtKetThuc,
                DonGia = san.DonGia,
                TienCoc = tienCoc,
                TrangThai = "Chờ duyệt"
            };

            _context.DatSans.Add(datSanMoi);
            await _context.SaveChangesAsync();

            // 5. Khởi tạo bản ghi ThanhToan với trạng thái 'Chưa thanh toán'
            var thanhToanMoi = new ThanhToan
            {
                MaDatSan = datSanMoi.MaDatSan,
                SoTien = tienCoc,
                PhuongThuc = "Chuyển khoản QR",
                TrangThai = "Chưa thanh toán",
                GhiChu = $"DATSAN{datSanMoi.MaDatSan}"
            };
            _context.ThanhToans.Add(thanhToanMoi);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt sân thành công (Mã đơn: #{datSanMoi.MaDatSan})! Quét mã QR bằng điện thoại để mở trang thanh toán và đặt cọc.";
            
            // Chuyển hướng ngay sang màn hình Thanh toán mô phỏng qua mã QR
            return RedirectToAction(nameof(ThanhToan), new { id = datSanMoi.MaDatSan });
        }

        // ==========================================================
        // 3. THEO DÕI LỊCH ĐẶT SÂN (LỊCH SỬ ĐẶT SÂN CỦA KHÁCH HÀNG)
        // Mục 7.5: Chỉ được xem dữ liệu của chính mình
        // Tách riêng 2 trạng thái: Trạng thái đặt sân & Trạng thái thanh toán
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> LichSuDatSan(string? trangThai, string? trangThaiThanhToan, int trang = 1, int kichThuocTrang = 8)
        {
            var khachHang = await LayKhachHangHienTaiAsync();
            if (khachHang == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem lịch sử đặt sân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // LINQ: Truy vấn danh sách giao dịch thuộc chính khách hàng này, nạp kèm Sân và ThanhToan
            var query = _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.ThanhToans)
                .Where(d => d.MaKhachHang == khachHang.MaKhachHang);

            // Lọc theo trạng thái đặt sân
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "Chờ duyệt" || trangThai == "Chờ xử lý")
                    query = query.Where(d => d.TrangThai == "Chờ duyệt" || d.TrangThai == "Chờ xử lý");
                else if (trangThai == "Đã duyệt" || trangThai == "Đang xử lý")
                    query = query.Where(d => d.TrangThai == "Đã duyệt" || d.TrangThai == "Đang xử lý");
                else if (trangThai == "Từ chối" || trangThai == "Đã hủy")
                    query = query.Where(d => d.TrangThai == "Từ chối" || d.TrangThai == "Đã hủy");
                else
                    query = query.Where(d => d.TrangThai == trangThai);
            }

            // Lọc theo trạng thái thanh toán
            if (!string.IsNullOrEmpty(trangThaiThanhToan))
            {
                query = query.Where(d => d.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault()!.TrangThai == trangThaiThanhToan);
            }

            // Sắp xếp đơn mới nhất lên đầu
            query = query.OrderByDescending(d => d.NgayDat);

            int tongSoBanGhi = await query.CountAsync();
            int tongSoTrang = (int)Math.Ceiling((double)tongSoBanGhi / kichThuocTrang);
            trang = Math.Max(1, Math.Min(trang, Math.Max(1, tongSoTrang)));

            var dsLichDat = await query
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();

            ViewBag.TrangThai = trangThai;
            ViewBag.TrangThaiThanhToan = trangThaiThanhToan;
            ViewBag.TrangHienTai = trang;
            ViewBag.TongSoTrang = tongSoTrang;
            ViewBag.TongSoBanGhi = tongSoBanGhi;

            return View(dsLichDat);
        }

        // ==========================================================
        // 4. XEM CHI TIẾT ĐƠN ĐẶT SÂN
        // Bảo mật URL Mục 7.5: Chặn đổi id xem trộm đơn người khác
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var donDat = await _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatSans)
                    .ThenInclude(ct => ct.DichVu)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (donDat == null)
            {
                return NotFound();
            }

            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin" && vaiTro != "NhanVien")
            {
                var khachHang = await LayKhachHangHienTaiAsync();
                if (khachHang == null || donDat.MaKhachHang != khachHang.MaKhachHang)
                {
                    TempData["Error"] = "Bạn không có quyền xem thông tin đơn đặt sân của người khác!";
                    return RedirectToAction(nameof(LichSuDatSan));
                }
            }

            return View(donDat);
        }

        // ==========================================================
        // 5. HỦY ĐẶT SÂN
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var khachHang = await LayKhachHangHienTaiAsync();
            if (khachHang == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var donDat = await _context.DatSans.FindAsync(id);
            if (donDat == null)
            {
                return NotFound();
            }

            if (donDat.MaKhachHang != khachHang.MaKhachHang)
            {
                TempData["Error"] = "Bạn không thể hủy đơn đặt sân của người khác!";
                return RedirectToAction(nameof(LichSuDatSan));
            }

            if (donDat.TrangThai != "Chờ duyệt" && donDat.TrangThai != "Chờ xử lý")
            {
                TempData["Error"] = $"Không thể hủy đơn đặt sân đang ở trạng thái '{donDat.TrangThai}'. Vui lòng liên hệ ban quản lý để được hỗ trợ!";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            donDat.TrangThai = "Từ chối";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã hủy thành công đơn đặt sân #{donDat.MaDatSan}!";
            return RedirectToAction(nameof(LichSuDatSan));
        }

        // ==========================================================
        // 6. API AJAX: KIỂM TRA TRÙNG LỊCH VÀ TÍNH TIỀN NHANH
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraGiaVaTrungLich(int maSan, string ngay, string gioBatDau, string gioKetThuc)
        {
            if (DateTime.TryParse(ngay, out DateTime dtNgay) &&
                TimeSpan.TryParse(gioBatDau, out TimeSpan tsBatDau) &&
                TimeSpan.TryParse(gioKetThuc, out TimeSpan tsKetThuc))
            {
                var san = await _context.SanTheThaos.FindAsync(maSan);
                if (san == null) return Json(new { hopLe = false, thongBao = "Không tìm thấy sân!" });

                DateTime start = dtNgay.Date + tsBatDau;
                DateTime end = dtNgay.Date + tsKetThuc;

                if (end <= start)
                {
                    return Json(new { hopLe = false, thongBao = "Giờ kết thúc phải sau giờ bắt đầu!" });
                }

                if (tsBatDau < san.GioMoCua || tsKetThuc > san.GioDongCua)
                {
                    return Json(new { hopLe = false, thongBao = $"Sân chỉ mở cửa từ {san.GioMoCua:hh\\:mm} đến {san.GioDongCua:hh\\:mm}!" });
                }

                bool biTrung = await _context.DatSans.AnyAsync(d =>
                    d.MaSan == maSan &&
                    d.TrangThai != "Đã hủy" &&
                    d.TrangThai != "Từ chối" &&
                    d.GioBatDau < end &&
                    d.GioKetThuc > start);

                if (biTrung)
                {
                    return Json(new { hopLe = false, thongBao = "Khung giờ này sân đã có người đặt!" });
                }

                double hours = (end - start).TotalHours;
                decimal tongTien = Math.Round((decimal)hours * san.DonGia, 0);
                decimal tienCoc = Math.Round(tongTien * 0.3m, 0);

                return Json(new { hopLe = true, soGio = hours, tongTien, tienCoc, donGia = san.DonGia });
            }

            return Json(new { hopLe = false, thongBao = "Dữ liệu ngày giờ không hợp lệ!" });
        }

        // ==========================================================
        // 7. TRANG THANH TOÁN TIỀN CỌC TRÊN MÁY TÍNH (HIỂN THỊ MÃ QR ĐỂ ĐIỆN THOẠI QUÉT)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ThanhToan(int id)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            bool laQuanLy = vaiTro == "Admin" || vaiTro == "NhanVien";
            var khachHang = await LayKhachHangHienTaiAsync();

            if (khachHang == null && !laQuanLy)
            {
                TempData["Error"] = "Vui lòng đăng nhập để truy cập trang thanh toán!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var donDat = await _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.KhachHang)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (donDat == null)
            {
                return NotFound();
            }

            if (!laQuanLy && khachHang != null && donDat.MaKhachHang != khachHang.MaKhachHang)
            {
                TempData["Error"] = "Bạn không có quyền xem thông tin thanh toán của đơn đặt sân khác!";
                return RedirectToAction(nameof(LichSuDatSan));
            }

            var thanhToan = donDat.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (thanhToan == null)
            {
                thanhToan = new ThanhToan
                {
                    MaDatSan = donDat.MaDatSan,
                    SoTien = donDat.TienCoc,
                    PhuongThuc = "Chuyển khoản QR",
                    TrangThai = "Chưa thanh toán",
                    GhiChu = $"DATSAN{donDat.MaDatSan}"
                };
                _context.ThanhToans.Add(thanhToan);
                await _context.SaveChangesAsync();
            }

            double soGio = (donDat.GioKetThuc - donDat.GioBatDau).TotalHours;
            decimal tongTien = Math.Round((decimal)soGio * donDat.DonGia, 0);

            // Tự động nhận diện host/domain (kể cả khi chạy qua Dev Tunnels / ngrok / IP LAN)
            var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;
            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            var mobileUrl = $"{scheme}://{host}/DatSan/ThanhToanDiDong?id={donDat.MaDatSan}";

            var vm = new ThanhToanViewModel
            {
                MaDatSan = donDat.MaDatSan,
                MaThanhToan = thanhToan.MaThanhToan,
                TenSan = donDat.SanTheThao?.TenSan ?? $"Sân #{donDat.MaSan}",
                TenLoaiSan = donDat.SanTheThao?.LoaiSan?.TenLoai ?? "",
                NgaySuDung = donDat.GioBatDau.Date,
                GioBatDau = donDat.GioBatDau,
                GioKetThuc = donDat.GioKetThuc,
                HoTenKhachHang = donDat.KhachHang?.HoTen ?? "",
                SoDienThoai = donDat.KhachHang?.SoDienThoai ?? "",
                TongTien = tongTien,
                TienCoc = donDat.TienCoc,
                SoTienThanhToan = thanhToan.SoTien > 0 ? thanhToan.SoTien : donDat.TienCoc,
                PhuongThuc = string.IsNullOrEmpty(thanhToan.PhuongThuc) ? "Chuyển khoản QR" : thanhToan.PhuongThuc,
                NoiDungChuyenKhoan = !string.IsNullOrEmpty(thanhToan.GhiChu) ? thanhToan.GhiChu : $"DATSAN{donDat.MaDatSan}",
                TrangThaiThanhToan = thanhToan.TrangThai,
                TrangThaiDatSan = donDat.TrangThai,
                NgayThanhToan = thanhToan.NgayThanhToan,
                MaGiaoDich = thanhToan.MaGiaoDich,
                GhiChu = thanhToan.GhiChu,
                MobileUrl = mobileUrl
            };

            return View(vm);
        }

        // ==========================================================
        // 8. KHÁCH NHẤN "TÔI ĐÃ THANH TOÁN" TRÊN MÁY TÍNH (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToan(ThanhToanViewModel model)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            bool laQuanLy = vaiTro == "Admin" || vaiTro == "NhanVien";
            var khachHang = await LayKhachHangHienTaiAsync();

            if (khachHang == null && !laQuanLy)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var donDat = await _context.DatSans
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == model.MaDatSan);

            if (donDat == null)
            {
                return NotFound();
            }

            if (!laQuanLy && khachHang != null && donDat.MaKhachHang != khachHang.MaKhachHang)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thanh toán cho đơn này!";
                return RedirectToAction(nameof(LichSuDatSan));
            }

            var thanhToan = donDat.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (thanhToan == null)
            {
                thanhToan = new ThanhToan
                {
                    MaDatSan = donDat.MaDatSan
                };
                _context.ThanhToans.Add(thanhToan);
            }

            thanhToan.SoTien = model.SoTienThanhToan > 0 ? model.SoTienThanhToan : donDat.TienCoc;
            thanhToan.PhuongThuc = model.PhuongThuc ?? "Chuyển khoản QR";
            thanhToan.TrangThai = "Chờ xác nhận";
            thanhToan.NgayThanhToan = DateTime.Now;
            thanhToan.GhiChu = string.IsNullOrWhiteSpace(model.NoiDungChuyenKhoan) ? $"DATSAN{donDat.MaDatSan}" : model.NoiDungChuyenKhoan.Trim();
            thanhToan.MaGiaoDich = $"MB{DateTime.Now:yyyyMMddHHmmss}";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận yêu cầu thanh toán cho đơn #{donDat.MaDatSan}! Trạng thái thanh toán: 'Chờ xác nhận'. Vui lòng đợi quản trị viên kiểm tra và duyệt đơn.";
            return RedirectToAction(nameof(LichSuDatSan));
        }

        // ==========================================================
        // 9. TRANG THANH TOÁN DÀNH CHO DI ĐỘNG (QUÉT MÃ QR BẰNG ĐIỆN THOẠI)
        // Khi dùng camera điện thoại quét mã QR, điện thoại sẽ mở trực tiếp trang web này
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ThanhToanDiDong(int id)
        {
            var donDat = await _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.KhachHang)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (donDat == null)
            {
                return NotFound();
            }

            var thanhToan = donDat.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (thanhToan == null)
            {
                thanhToan = new ThanhToan
                {
                    MaDatSan = donDat.MaDatSan,
                    SoTien = donDat.TienCoc,
                    PhuongThuc = "Chuyển khoản QR",
                    TrangThai = "Chưa thanh toán",
                    GhiChu = $"DATSAN{donDat.MaDatSan}"
                };
                _context.ThanhToans.Add(thanhToan);
                await _context.SaveChangesAsync();
            }

            double soGio = (donDat.GioKetThuc - donDat.GioBatDau).TotalHours;
            decimal tongTien = Math.Round((decimal)soGio * donDat.DonGia, 0);

            var vm = new ThanhToanViewModel
            {
                MaDatSan = donDat.MaDatSan,
                MaThanhToan = thanhToan.MaThanhToan,
                TenSan = donDat.SanTheThao?.TenSan ?? $"Sân #{donDat.MaSan}",
                TenLoaiSan = donDat.SanTheThao?.LoaiSan?.TenLoai ?? "",
                NgaySuDung = donDat.GioBatDau.Date,
                GioBatDau = donDat.GioBatDau,
                GioKetThuc = donDat.GioKetThuc,
                HoTenKhachHang = donDat.KhachHang?.HoTen ?? "",
                SoDienThoai = donDat.KhachHang?.SoDienThoai ?? "",
                TongTien = tongTien,
                TienCoc = donDat.TienCoc,
                SoTienThanhToan = thanhToan.SoTien > 0 ? thanhToan.SoTien : donDat.TienCoc,
                PhuongThuc = string.IsNullOrEmpty(thanhToan.PhuongThuc) ? "Chuyển khoản QR" : thanhToan.PhuongThuc,
                NoiDungChuyenKhoan = !string.IsNullOrEmpty(thanhToan.GhiChu) ? thanhToan.GhiChu : $"DATSAN{donDat.MaDatSan}",
                TrangThaiThanhToan = thanhToan.TrangThai,
                TrangThaiDatSan = donDat.TrangThai,
                NgayThanhToan = thanhToan.NgayThanhToan,
                MaGiaoDich = thanhToan.MaGiaoDich,
                GhiChu = thanhToan.GhiChu
            };

            return View(vm);
        }

        // ==========================================================
        // 10. XÁC NHẬN THANH TOÁN TỪ ĐIỆN THOẠI (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToanDiDong(ThanhToanViewModel model)
        {
            var donDat = await _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.KhachHang)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == model.MaDatSan);

            if (donDat == null)
            {
                return NotFound();
            }

            var thanhToan = donDat.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (thanhToan == null)
            {
                thanhToan = new ThanhToan { MaDatSan = donDat.MaDatSan };
                _context.ThanhToans.Add(thanhToan);
            }

            thanhToan.SoTien = model.SoTienThanhToan > 0 ? model.SoTienThanhToan : donDat.TienCoc;
            thanhToan.PhuongThuc = model.PhuongThuc ?? "Chuyển khoản QR";
            thanhToan.TrangThai = "Chờ xác nhận"; // Cập nhật sang Chờ xác nhận
            thanhToan.NgayThanhToan = DateTime.Now;
            thanhToan.GhiChu = string.IsNullOrWhiteSpace(model.NoiDungChuyenKhoan) ? $"DATSAN{donDat.MaDatSan}" : model.NoiDungChuyenKhoan.Trim();
            thanhToan.MaGiaoDich = $"MB{DateTime.Now:yyyyMMddHHmmss}";

            await _context.SaveChangesAsync();

            // Nạp lại thông tin để hiển thị màn hình chúc mừng trên điện thoại
            model.TrangThaiThanhToan = "Chờ xác nhận";
            model.NgayThanhToan = thanhToan.NgayThanhToan;
            model.MaGiaoDich = thanhToan.MaGiaoDich;
            model.TenSan = donDat.SanTheThao?.TenSan ?? "";
            model.TenLoaiSan = donDat.SanTheThao?.LoaiSan?.TenLoai ?? "";
            model.HoTenKhachHang = donDat.KhachHang?.HoTen ?? "";
            ViewBag.ThanhCong = true;

            return View("ThanhToanDiDong", model);
        }

        // ==========================================================
        // 11. API KIỂM TRA TRẠNG THÁI THANH TOÁN THỜI GIAN THỰC (REALTIME POLLING)
        // Màn hình máy tính gọi liên tục mỗi 2.5s: khi điện thoại bấm xong, máy tính tự nhận diện ngay lập tức
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraTrangThai(int id)
        {
            var donDat = await _context.DatSans
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (donDat == null)
            {
                return Json(new { hopLe = false });
            }

            var tt = donDat.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            string ttThanhToan = tt?.TrangThai ?? "Chưa thanh toán";

            return Json(new
            {
                hopLe = true,
                maDatSan = donDat.MaDatSan,
                trangThaiDatSan = donDat.TrangThai,
                trangThaiThanhToan = ttThanhToan,
                daXacNhan = ttThanhToan == "Chờ xác nhận" || ttThanhToan == "Đã thanh toán"
            });
        }
    }
}
