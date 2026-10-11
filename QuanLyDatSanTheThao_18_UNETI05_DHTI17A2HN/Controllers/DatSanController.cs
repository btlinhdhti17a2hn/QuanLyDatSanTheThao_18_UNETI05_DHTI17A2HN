// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Controller Đặt sân (Nghiệp vụ tạo đặt sân, kiểm tra trùng lịch LINQ, giờ mở cửa, theo dõi lịch sử đặt và hủy đặt sân)

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
        // Hiển thị form cho khách hàng chọn sân, chọn ngày giờ thuê
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> TaoMoi(int? maSan)
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

            return View(model);
        }

        // ==========================================================
        // 2. TẠO ĐẶT SÂN - POST
        // Xử lý các ràng buộc nghiệp vụ (Mục 7 & 8 đề bài) bằng LINQ
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMoi(DatSanViewModel model)
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
                return View(model);
            }

            // 1. Kiểm tra đối tượng sân thể thao tồn tại và đang hoạt động
            var san = await _context.SanTheThaos.Include(s => s.LoaiSan).FirstOrDefaultAsync(s => s.MaSan == model.MaSan);
            if (san == null || !san.TrangThai)
            {
                ModelState.AddModelError("MaSan", "Sân thể thao được chọn hiện không tồn tại hoặc đã ngừng hoạt động!");
                return View(model);
            }

            // 2. Tính toán mốc thời gian bắt đầu và kết thúc
            DateTime dtBatDau = model.NgaySuDung.Date + model.GioBatDau;
            DateTime dtKetThuc = model.NgaySuDung.Date + model.GioKetThuc;

            // Ràng buộc thời gian: Không đặt thời điểm trong quá khứ
            if (dtBatDau < DateTime.Now)
            {
                ModelState.AddModelError("GioBatDau", "Thời gian bắt đầu đặt sân phải từ thời điểm hiện tại trở đi!");
                return View(model);
            }

            // Ràng buộc thời gian: Giờ kết thúc phải sau giờ bắt đầu
            if (dtKetThuc <= dtBatDau)
            {
                ModelState.AddModelError("GioKetThuc", "Giờ kết thúc phải sau giờ bắt đầu!");
                return View(model);
            }

            // Ràng buộc thời lượng: Tối thiểu 30 phút
            double soGio = (dtKetThuc - dtBatDau).TotalHours;
            if (soGio < 0.5)
            {
                ModelState.AddModelError("GioKetThuc", "Thời lượng thuê sân tối thiểu là 30 phút!");
                return View(model);
            }

            // Ràng buộc Mục 8.3: Chỉ được đặt trong khung giờ mở/đóng cửa của sân
            if (model.GioBatDau < san.GioMoCua || model.GioKetThuc > san.GioDongCua)
            {
                ModelState.AddModelError("", $"Sân chỉ hoạt động trong khung giờ mở cửa từ {san.GioMoCua:hh\\:mm} đến {san.GioDongCua:hh\\:mm}!");
                return View(model);
            }

            // Ràng buộc Mục 8.3 & 15: KIỂM TRA TRÙNG LỊCH BẰNG LINQ
            // Không cho cùng 1 sân có 2 lịch đặt giao nhau về thời gian (loại trừ các đơn 'Đã hủy')
            bool daBiTrungLich = await _context.DatSans.AnyAsync(d =>
                d.MaSan == model.MaSan
                && d.TrangThai != "Đã hủy"
                && d.GioBatDau < dtKetThuc
                && d.GioKetThuc > dtBatDau);

            if (daBiTrungLich)
            {
                ModelState.AddModelError("", "Sân đã có người đặt trong khoảng thời gian này! Vui lòng chọn khung giờ hoặc sân khác.");
                return View(model);
            }

            // 3. Tính toán chi phí: Tổng tiền dự kiến và Tiền cọc (30% tổng tiền, không vượt tổng tiền dự kiến theo Mục 8.3)
            decimal tongTien = Math.Round((decimal)soGio * san.DonGia, 0);
            decimal tienCoc = Math.Round(tongTien * 0.3m, 0);

            // 4. Khởi tạo đối tượng DatSan theo đúng quy định Mục 7:
            // Thời điểm tạo do hệ thống tự sinh (DateTime.Now), trạng thái khởi tạo là 'Chờ xử lý'
            var datSanMoi = new DatSan
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaSan = model.MaSan,
                NgayDat = DateTime.Now,
                GioBatDau = dtBatDau,
                GioKetThuc = dtKetThuc,
                DonGia = san.DonGia,
                TienCoc = tienCoc,
                TrangThai = "Chờ xử lý"
            };

            _context.DatSans.Add(datSanMoi);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt sân thành công (Mã đơn: #{datSanMoi.MaDatSan})! Vui lòng chuẩn bị tiền cọc {tienCoc:N0} VNĐ để nhân viên xác nhận.";
            return RedirectToAction(nameof(LichSuDatSan));
        }

        // ==========================================================
        // 3. THEO DÕI LỊCH ĐẶT SÂN (LỊCH SỬ ĐẶT SÂN CỦA KHÁCH HÀNG)
        // Mục 7.5: Chỉ được xem dữ liệu của chính mình
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> LichSuDatSan(string? trangThai, int trang = 1, int kichThuocTrang = 8)
        {
            var khachHang = await LayKhachHangHienTaiAsync();
            if (khachHang == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem lịch sử đặt sân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // LINQ: Truy vấn danh sách giao dịch thuộc chính khách hàng này
            var query = _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Where(d => d.MaKhachHang == khachHang.MaKhachHang);

            // Lọc theo trạng thái giao dịch
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            // Sắp xếp đơn mới nhất lên đầu
            query = query.OrderByDescending(d => d.NgayDat);

            // Phân trang bằng Skip và Take trên LINQ
            int tongSoBanGhi = await query.CountAsync();
            int tongSoTrang = (int)Math.Ceiling((double)tongSoBanGhi / kichThuocTrang);
            trang = Math.Max(1, Math.Min(trang, Math.Max(1, tongSoTrang)));

            var dsLichDat = await query
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();

            ViewBag.TrangThai = trangThai;
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
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (donDat == null)
            {
                return NotFound();
            }

            // BẢO MẬT PHÂN QUYỀN (Mục 7.5):
            // Admin/Nhân viên được xem tất cả; Khách hàng chỉ xem được đơn của chính mình
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
        // Mục 7.4: Chỉ được hủy trong các trạng thái cho phép
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

            // BẢO MẬT: Kiểm tra đơn thuộc chính khách hàng này
            if (donDat.MaKhachHang != khachHang.MaKhachHang)
            {
                TempData["Error"] = "Bạn không thể hủy đơn đặt sân của người khác!";
                return RedirectToAction(nameof(LichSuDatSan));
            }

            // RÀNG BUỘC MỤC 7.4: Chỉ được hủy khi đơn ở trạng thái 'Chờ xử lý'
            if (donDat.TrangThai != "Chờ xử lý")
            {
                TempData["Error"] = $"Không thể hủy đơn đặt sân đang ở trạng thái '{donDat.TrangThai}'. Vui lòng liên hệ ban quản lý để được hỗ trợ!";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            // Cập nhật trạng thái sang 'Đã hủy'
            donDat.TrangThai = "Đã hủy";
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
    }
}
