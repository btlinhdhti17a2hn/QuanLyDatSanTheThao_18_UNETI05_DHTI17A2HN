// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Controller Đặt sân (Nghiệp vụ tạo đặt sân, kiểm tra trùng lịch LINQ, hiển thị mã QR TPBank chuyển cọc, theo dõi lịch sử đặt và hủy đặt sân)

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    [AuthorizeRole]
    public class DatSanController : Controller
    {
        private readonly IDatSanRepository _datSanRepository;

        // Thông tin ngân hàng của Nguyễn Thị Lan Nhi (SV3 - Module 3)
        private const string NGAN_HANG = "TPBank (Ngân hàng Tiên Phong)";
        private const string SO_TAI_KHOAN = "00004101429";
        private const string CHU_TAI_KHOAN = "NGUYEN THI LAN NHI";

        public DatSanController(IDatSanRepository datSanRepository)
        {
            _datSanRepository = datSanRepository;
        }

        // Hàm tiện ích: Lấy thông tin khách hàng đang đăng nhập dựa trên Session MaTaiKhoan
        private async Task<KhachHang?> LayKhachHangHienTaiAsync()
        {
            int? maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null) return null;

            return await _datSanRepository.LayKhachHangTheoTaiKhoanAsync(maTaiKhoan.Value);
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
            ViewBag.DanhSachSan = await _datSanRepository.LayDanhSachSanHoatDongAsync();

            var model = new DatSanViewModel
            {
                NgaySuDung = DateTime.Today.AddDays(1), // Mặc định gợi ý ngày mai
                GioBatDau = new TimeSpan(17, 0, 0),     // Mặc định gợi ý 17:00
                GioKetThuc = new TimeSpan(19, 0, 0)     // Mặc định gợi ý 19:00
            };

            if (maSan.HasValue)
            {
                model.MaSan = maSan.Value;
                var san = await _datSanRepository.LaySanTheoMaAsync(maSan.Value);

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
            ViewBag.DanhSachSan = await _datSanRepository.LayDanhSachSanHoatDongAsync();

            if (!ModelState.IsValid)
            {
                return View("TaoMoi", model);
            }

            // 1. Kiểm tra đối tượng sân thể thao tồn tại và đang hoạt động
            var san = await _datSanRepository.LaySanTheoMaAsync(model.MaSan);

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
            bool daBiTrungLich = await _datSanRepository.KiemTraTrungLichAsync(
                model.MaSan,
                dtBatDau,
                dtKetThuc);

            if (daBiTrungLich)
            {
                ModelState.AddModelError("", "Sân đã có người đặt trong khoảng thời gian này! Vui lòng chọn khung giờ hoặc sân khác.");
                return View("TaoMoi", model);
            }

            // 3. Tính toán chi phí: Tổng tiền dự kiến và Tiền cọc (30% tổng tiền)
            decimal tongTien = Math.Round((decimal)soGio * san.DonGia, 0);
            decimal tienCoc = Math.Round(tongTien * 0.3m, 0);

            // 4. Khởi tạo đối tượng DatSan theo đúng quy định Mục 7:
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

            await _datSanRepository.ThemDatSanAsync(datSanMoi);

            TempData["Success"] = $"Đặt sân thành công (Mã đơn: #{datSanMoi.MaDatSan})! Vui lòng quét mã QR để chuyển khoản tiền cọc.";
            
            // Chuyển hướng sang trang hiển thị mã QR đặt cọc của bạn
            return RedirectToAction(nameof(XacNhanDatCoc), new { id = datSanMoi.MaDatSan });
        }

        // ==========================================================
        // 3. HIỂN THỊ MÃ QR CHUYỂN KHOẢN TIỀN CỌC (TPBANK CỦA NGUYỄN THỊ LAN NHI)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> XacNhanDatCoc(int id)
        {
            var donDat = await _datSanRepository.LayChiTietDatSanAsync(id);
            if (donDat == null)
            {
                return NotFound();
            }

            var khachHang = await LayKhachHangHienTaiAsync();
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin" && vaiTro != "NhanVien")
            {
                if (khachHang == null || donDat.MaKhachHang != khachHang.MaKhachHang)
                {
                    TempData["Error"] = "Bạn không có quyền xem thông tin đặt sân này!";
                    return RedirectToAction(nameof(LichSuDatSan));
                }
            }

            // Sinh link VietQR chuẩn động của TPBank với số tiền cọc và mã đơn
            string noiDungChuyenKhoan = $"DATSAN{donDat.MaDatSan}";
            string vietQrUrl = $"https://img.vietqr.io/image/TPB-{SO_TAI_KHOAN}-compact2.png?amount={(long)donDat.TienCoc}&addInfo={noiDungChuyenKhoan}&accountName={Uri.EscapeDataString(CHU_TAI_KHOAN)}";

            ViewBag.NganHang = NGAN_HANG;
            ViewBag.SoTaiKhoan = SO_TAI_KHOAN;
            ViewBag.ChuTaiKhoan = CHU_TAI_KHOAN;
            ViewBag.NoiDungChuyenKhoan = noiDungChuyenKhoan;
            ViewBag.VietQrUrl = vietQrUrl;

            return View("XacNhanDatCoc", donDat);
        }

        [HttpGet]
        public async Task<IActionResult> ThanhToan(int id)
        {
            return await XacNhanDatCoc(id);
        }

        // ==========================================================
        // 4. THEO DÕI LỊCH ĐẶT SÂN (LỊCH SỬ ĐẶT SÂN CỦA KHÁCH HÀNG)
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

            int tongSoBanGhi = await _datSanRepository.DemLichSuDatSanAsync(
                khachHang.MaKhachHang,
                trangThai);

            int tongSoTrang = (int)Math.Ceiling((double)tongSoBanGhi / kichThuocTrang);
            trang = Math.Max(1, Math.Min(trang, Math.Max(1, tongSoTrang)));

            var dsLichDat = await _datSanRepository.LayLichSuDatSanAsync(
                khachHang.MaKhachHang,
                trangThai,
                trang,
                kichThuocTrang);

            ViewBag.TrangThai = trangThai;
            ViewBag.TrangHienTai = trang;
            ViewBag.TongSoTrang = tongSoTrang;
            ViewBag.TongSoBanGhi = tongSoBanGhi;

            return View(dsLichDat);
        }

        // ==========================================================
        // 5. XEM CHI TIẾT ĐƠN ĐẶT SÂN
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var donDat = await _datSanRepository.LayChiTietDatSanAsync(id);

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

            return View("ChiTiet", donDat);
        }

        // ==========================================================
        // 6. HỦY ĐẶT SÂN
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

            var donDat = await _datSanRepository.LayDatSanTheoMaAsync(id);

            if (donDat == null)
            {
                return NotFound();
            }

            if (donDat.MaKhachHang != khachHang.MaKhachHang)
            {
                TempData["Error"] = "Bạn không thể hủy đơn đặt sân của người khác!";
                return RedirectToAction(nameof(LichSuDatSan));
            }

            if (donDat.TrangThai != "Chờ xử lý")
            {
                TempData["Error"] = $"Không thể hủy đơn đặt sân đang ở trạng thái '{donDat.TrangThai}'. Vui lòng liên hệ ban quản lý để được hỗ trợ!";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            donDat.TrangThai = "Đã hủy";
            await _datSanRepository.LuuThayDoiAsync();

            TempData["Success"] = $"Đã hủy thành công đơn đặt sân #{donDat.MaDatSan}!";
            return RedirectToAction(nameof(LichSuDatSan));
        }

        // ==========================================================
        // 7. API AJAX: KIỂM TRA TRÙNG LỊCH VÀ TÍNH TIỀN NHANH
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraGiaVaTrungLich(int maSan, string ngay, string gioBatDau, string gioKetThuc)
        {
            if (DateTime.TryParse(ngay, out DateTime dtNgay) &&
                TimeSpan.TryParse(gioBatDau, out TimeSpan tsBatDau) &&
                TimeSpan.TryParse(gioKetThuc, out TimeSpan tsKetThuc))
            {
                var san = await _datSanRepository.LaySanTheoMaAsync(maSan);

                if (san == null)
                {
                    return Json(new { hopLe = false, thongBao = "Không tìm thấy sân!" });
                }

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

                bool biTrung = await _datSanRepository.KiemTraTrungLichAsync(
                    maSan,
                    start,
                    end);

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
