// Họ và tên: 
// Mã sinh viên: [Điền mã sinh viên của bạn]
// Nội dung thực hiện: Controller ThongKe - Dashboard quản trị và thống kê nâng cao bằng LINQ
// (GroupBy, Sum, Count, OrderByDescending, Any): doanh thu theo tháng/loại sân,
// sân & loại sân ưa thích, dịch vụ phổ biến, số lượt hủy, khung giờ cao điểm.
// Có bộ lọc thời gian (hôm nay / tuần / tháng / quý / năm / tùy chọn) và so sánh với kỳ trước.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    [AuthorizeRole("Admin", "NhanVien")]
    public class ThongKeController : Controller
    {
        // Tính số giờ thuê sân (chỉ dùng cho "Tạm tính hôm nay")
        private static double SoGio(DateTime gioBatDau, DateTime gioKetThuc)
        {
            return Math.Max((gioKetThuc - gioBatDau).TotalHours, 0);
        }

        // Tính tiền sân = đơn giá theo giờ x số giờ thuê (chỉ dùng cho "Tạm tính hôm nay")
        private static decimal TienSan(
            decimal donGia, DateTime gioBatDau, DateTime gioKetThuc)
        {
            return Math.Round(
                donGia * (decimal)SoGio(gioBatDau, gioKetThuc), 0);
        }

        private readonly IThongKeRepository _thongKeRepository;

        // Trạng thái - dùng chung với module Đặt sân (TrangThaiDatSan)
        private const string CHO_XU_LY = "Chờ xử lý";
        private const string DANG_XU_LY = "Đang xử lý";
        private const string HOAN_THANH = "Hoàn thành";
        private const string HUY = "Đã hủy";
        private const bool SAN_KHA_DUNG = true;
        private const int NGAY_NGUY_CO = 30;          // chưa quay lại từ ngần ấy ngày => nguy cơ rời bỏ
        private const int CUA_SO_CHUYEN_DOI = 30;     // đăng ký -> có đơn trong ngần ấy ngày

        public ThongKeController(IThongKeRepository thongKeRepository)
        {
            _thongKeRepository = thongKeRepository;
        }

        // Ngày hiện tại theo giờ Việt Nam (UTC+7), không phụ thuộc múi giờ máy chủ
        private static DateTime HomNayVn() => DateTime.UtcNow.AddHours(7).Date;

        // ================== PHÂN QUYỀN ==================
        private string? VaiTro => HttpContext.Session.GetString("VaiTro");
        private bool LaQuanLy() => VaiTro == "Admin" || VaiTro == "NhanVien";
        private bool LaAdmin() => VaiTro == "Admin";
        private IActionResult TuChoi()
        {
            if (string.IsNullOrEmpty(VaiTro))
                return RedirectToAction("DangNhap", "TaiKhoan");
            TempData["Loi"] = "Bạn không có quyền truy cập trang thống kê.";
            return RedirectToAction("Index", "Home");
        }

        // ================== DASHBOARD ==================
        // Ngưỡng nghiệp vụ của dashboard (đổi tại đây nếu quy trình thay đổi)
        private const int PHUT_SAP_DIEN_RA = 60;     // đơn bắt đầu trong ngần ấy phút tới = "sắp diễn ra"
        private const double NGUONG_SAN_THAP = 30;   // sân khai thác dưới ngần ấy % = cơ hội cải thiện
        private static readonly System.Globalization.CultureInfo Vi = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

        // Một đơn rút gọn (mọi trạng thái) để tính chỉ số theo kỳ ở bộ nhớ
        private sealed class DonDash
        {
            public int MaDatSan { get; set; }
            public int MaKhachHang { get; set; }
            public string TrangThai { get; set; } = "";
            public decimal DonGia { get; set; }
            public DateTime NgayDat { get; set; }
            public DateTime GioBatDau { get; set; }
        }

        private sealed class DashChiSo
        {
            public int Tong, Cho, Dang, Xong, Huy, Khac, DonKhongHuy, KhachCoDon, KhachQuayLai;
            public decimal DoanhThu;
        }

        private static DashChiSo TinhChiSoDash(List<DonDash> ds)
        {
            var cs = new DashChiSo
            {
                Tong = ds.Count,
                Cho = ds.Count(d => d.TrangThai == CHO_XU_LY),
                Dang = ds.Count(d => d.TrangThai == DANG_XU_LY),
                Xong = ds.Count(d => d.TrangThai == HOAN_THANH),
                Huy = ds.Count(d => d.TrangThai == HUY),
                DonKhongHuy = ds.Count(d => d.TrangThai != HUY),
                DoanhThu = ds.Where(d => d.TrangThai == HOAN_THANH).Sum(d => d.DonGia)
            };
            cs.Khac = Math.Max(0, cs.Tong - cs.Cho - cs.Dang - cs.Xong - cs.Huy);
            var luot = ds.Where(d => d.TrangThai != HUY).GroupBy(d => d.MaKhachHang).Select(g => g.Count()).ToList();
            cs.KhachCoDon = luot.Count;
            cs.KhachQuayLai = luot.Count(n => n >= 2);
            return cs;
        }

        // Lấy danh sách đơn kèm tên sân / tên khách (không phụ thuộc navigation property của khách)
        private async Task<List<DashDonItem>> LayDsDonAsync(IQueryable<DatSan> q, int top, bool theoGioTangDan)
        {
            var sapXep = theoGioTangDan ? q.OrderBy(d => d.GioBatDau).ThenBy(d => d.MaDatSan) : q.OrderByDescending(d => d.MaDatSan);
            var raw = await sapXep.Take(top)
                .Select(d => new
                {
                    d.MaDatSan,
                    d.MaKhachHang,
                    TenSan = d.SanTheThao!.TenSan,
                    d.TrangThai,
                    d.NgayDat,
                    d.GioBatDau,
                    d.GioKetThuc,
                    d.DonGia
                })
                .ToListAsync();

            var ids = raw.Select(x => x.MaKhachHang).Distinct().ToList();
            var ten = ids.Count == 0
                ? new Dictionary<int, string>()
                : (await _thongKeRepository.LayKhachHangs().AsNoTracking()
                    .Where(k => ids.Contains(k.MaKhachHang))
                    .Select(k => new { k.MaKhachHang, k.HoTen })
                    .ToListAsync()).ToDictionary(k => k.MaKhachHang, k => k.HoTen);

            return raw.Select(x => new DashDonItem
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.TenSan,
                TenKhach = ten.TryGetValue(x.MaKhachHang, out var hoTen) ? hoTen : $"Khách #{x.MaKhachHang}",
                TrangThai = x.TrangThai,
                NgayDat = x.NgayDat,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia
            }).ToList();
        }

        // phamVi: homnay (mặc định) | 7ngay | 30ngay | tuychon (kèm tu, den). Giá trị cũ "tatca" được đổi thành 30ngay.
        public async Task<IActionResult> Index(string? phamVi, DateTime? tu, DateTime? den)
        {
            if (!LaQuanLy()) return TuChoi();

            var bay = DateTime.UtcNow.AddHours(7);   // giờ Việt Nam, không phụ thuộc múi giờ máy chủ
            var homNay = bay.Date;

            // ----- 1. Khoảng thời gian đang xem và kỳ trước (cùng độ dài) -----
            var kieu = (phamVi ?? "homnay").Trim().ToLowerInvariant();
            DateTime tuNgay, denNgay;
            switch (kieu)
            {
                case "tuannay":   // từ thứ Hai của tuần hiện tại đến hôm nay
                    denNgay = homNay; tuNgay = homNay.AddDays(-(((int)homNay.DayOfWeek + 6) % 7));
                    break;
                case "7ngay":
                    denNgay = homNay; tuNgay = homNay.AddDays(-6);
                    break;
                case "30ngay":
                case "tatca":
                    kieu = "30ngay"; denNgay = homNay; tuNgay = homNay.AddDays(-29);
                    break;
                case "tuychon":
                    tuNgay = (tu ?? homNay).Date;
                    denNgay = (den ?? tuNgay).Date;
                    if (denNgay < tuNgay) (tuNgay, denNgay) = (denNgay, tuNgay);
                    if (denNgay > homNay) denNgay = homNay;          // không thống kê ngày chưa diễn ra
                    if (tuNgay > denNgay) tuNgay = denNgay;
                    if (tuNgay < new DateTime(2000, 1, 1)) tuNgay = new DateTime(2000, 1, 1);
                    if ((denNgay - tuNgay).TotalDays > 365) tuNgay = denNgay.AddDays(-365);   // tối đa 366 ngày
                    break;
                default:
                    kieu = "homnay"; tuNgay = denNgay = homNay;
                    break;
            }
            int soNgay = (denNgay - tuNgay).Days + 1;
            // Kỳ trước: "Tuần này" so với CÙNG các thứ của tuần trước (cùng số ngày đã trôi qua);
            // các kỳ khác so với khoảng liền trước có cùng độ dài.
            var denTruoc = kieu == "tuannay" ? denNgay.AddDays(-7) : tuNgay.AddDays(-1);
            var tuTruoc = kieu == "tuannay" ? tuNgay.AddDays(-7) : denTruoc.AddDays(-(soNgay - 1));
            int dichKyTruoc = (tuNgay - tuTruoc).Days;    // số ngày lùi để ghép cột kỳ này với cột kỳ trước
            var denLoaiTru = denNgay.AddDays(1);

            // Dữ liệu cho bản đồ nhiệt / xếp hạng sân: kỳ quá ngắn (hôm nay) thì lấy 7 ngày gần nhất để có dữ liệu.
            int soNgayBd = Math.Max(soNgay, 7);
            var tuBd = denNgay.AddDays(-(soNgayBd - 1));
            var tuBdTruoc = tuBd.AddDays(-soNgayBd);
            string donVi = soNgayBd <= 31 ? "ngày" : (soNgayBd <= 120 ? "tuần" : "tháng");

            // ----- Biểu đồ doanh thu: phạm vi = ĐÚNG bộ lọc, không tự kéo dài hay cắt ngắn -----
            // Quy tắc nhóm cố định theo độ dài khoảng thời gian (view luôn ghi rõ đơn vị và nhãn từng cột):
            //   1 ngày      -> mỗi cột 1 giờ (24 cột; tự chuyển sang "ngày" nếu hệ thống không lưu giờ tạo đơn, có ghi chú)
            //   2-31 ngày   -> mỗi cột 1 ngày
            //   32-120 ngày -> mỗi cột 7 ngày liên tiếp kể từ ngày bắt đầu (cột cuối có thể ngắn hơn)
            //   > 120 ngày  -> mỗi cột 1 tháng (tháng đầu / cuối có thể chưa đủ)
            string donViCh = soNgay == 1 ? "giờ" : (soNgay <= 31 ? "ngày" : (soNgay <= 120 ? "tuần" : "tháng"));
            string NhanCotCua(string dv) => dv == "giờ" ? "1 giờ" : dv == "ngày" ? "1 ngày" : dv == "tuần" ? "7 ngày" : "1 tháng";
            string nhanDonViCot = NhanCotCua(donViCh);

            string nhanSoSanh = kieu == "homnay" ? "hôm qua"
                : kieu == "tuannay" ? "tuần trước (cùng số ngày)"
                : kieu == "7ngay" ? "7 ngày trước đó"
                : kieu == "30ngay" ? "30 ngày trước đó"
                : $"kỳ trước ({tuTruoc:dd/MM} - {denTruoc:dd/MM})";
            string nhanKy = kieu == "homnay" ? $"Hôm nay · {homNay:dd/MM/yyyy}"
                : kieu == "tuannay" ? $"Tuần này · {tuNgay:dd/MM} - {denNgay:dd/MM/yyyy}"
                : kieu == "7ngay" ? $"7 ngày gần nhất · {tuNgay:dd/MM} - {denNgay:dd/MM/yyyy}"
                : kieu == "30ngay" ? $"30 ngày gần nhất · {tuNgay:dd/MM} - {denNgay:dd/MM/yyyy}"
                : (soNgay == 1 ? $"Ngày {tuNgay:dd/MM/yyyy}" : $"{tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}");

            var vm = new DashboardViewModel
            {
                Kieu = kieu,
                CapNhat = bay,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TuNgayTruoc = tuTruoc,
                DenNgayTruoc = denTruoc,
                SoNgay = soNgay,
                NhanKy = nhanKy,
                NhanSoSanh = nhanSoSanh,
                PhutSapDienRa = PHUT_SAP_DIEN_RA,
                DonViBieuDo = donViCh,
                NhanDonViCot = nhanDonViCot,
                NhanBieuDo = ""
            };

            // ----- 2. Quy mô hệ thống (tích lũy) -----
            vm.TongLoaiSan = await _thongKeRepository.LayLoaiSans().CountAsync();
            vm.TongSan = await _thongKeRepository.LaySanTheThaos().CountAsync();
            vm.SanKhaDung = await _thongKeRepository.LaySanTheThaos().CountAsync(s => s.TrangThai == SAN_KHA_DUNG);
            vm.TongKhachHang = await _thongKeRepository.LayKhachHangs().CountAsync();

            // ----- 3. Cần xử lý ngay: thời điểm hiện tại, không phụ thuộc bộ lọc -----
            var datSans = _thongKeRepository.LayDatSans().AsNoTracking();
            var sapToi = bay.AddMinutes(PHUT_SAP_DIEN_RA);

            vm.DonChoXuLy = await datSans.CountAsync(d => d.TrangThai == CHO_XU_LY);

            // Quá hạn xác nhận: vẫn "Chờ xử lý" dù đã tới giờ chơi
            var qQuaHan = datSans.Where(d => d.TrangThai == CHO_XU_LY && d.GioBatDau <= bay);
            vm.DonQuaHan = await qQuaHan.CountAsync();
            vm.DsQuaHan = await LayDsDonAsync(qQuaHan, 5, true);

            // Sắp diễn ra: chưa hoàn thành / chưa hủy, bắt đầu trong PHUT_SAP_DIEN_RA phút tới
            var qSap = datSans.Where(d => (d.TrangThai == CHO_XU_LY || d.TrangThai == DANG_XU_LY)
                                          && d.GioBatDau > bay && d.GioBatDau <= sapToi);
            vm.DonSapDienRa = await qSap.CountAsync();
            vm.DsSapDienRa = await LayDsDonAsync(qSap, 5, true);

            // Chờ xác nhận nhưng chưa tới giờ chơi (đơn quá hạn đã có ở DsQuaHan)
            var qChoChuaQuaHan = datSans.Where(d => d.TrangThai == CHO_XU_LY && d.GioBatDau > bay);
            vm.DsChoXacNhan = await LayDsDonAsync(qChoChuaQuaHan, 4, true);

            // Quá giờ chưa chốt: "Đang xử lý" nhưng đã quá giờ kết thúc (nghi không đến hoặc quên cập nhật)
            var qQuaGio = datSans.Where(d => d.TrangThai == DANG_XU_LY && d.GioKetThuc < bay);
            vm.DonQuaGioChuaChot = await qQuaGio.CountAsync();
            vm.DsQuaGioChuaChot = await LayDsDonAsync(qQuaGio, 5, true);

            // Phân loại "đơn cần xử lý" thành 2 nhóm KHÔNG TRÙNG NHAU theo NGÀY CHƠI (GioBatDau), mỗi đơn đúng một lần:
            //   - Tồn từ ngày trước: ngày chơi < 00:00 hôm nay mà vẫn còn Chờ xử lý, hoặc Đang xử lý đã quá giờ kết thúc.
            //   - Hôm nay & sắp tới: ngày chơi >= 00:00 hôm nay, cùng điều kiện trạng thái.
            // Cùng một bảng DatSan, không JOIN nên không thể đếm trùng. Không phụ thuộc bộ lọc thời gian.
            // Đơn đã Hoàn thành / Đã hủy / Đang xử lý còn trong giờ không thuộc nhóm nào. Sắp xếp: ngày chơi cũ nhất (quá hạn lâu nhất) trước.
            var qCanXuLy = datSans.Where(d => d.TrangThai == CHO_XU_LY || (d.TrangThai == DANG_XU_LY && d.GioKetThuc < bay));
            var qTon = qCanXuLy.Where(d => d.GioBatDau < homNay);
            var qNay = qCanXuLy.Where(d => d.GioBatDau >= homNay);
            vm.DonTonChoXacNhan = await qTon.CountAsync(d => d.TrangThai == CHO_XU_LY);
            vm.DonTonQuaGio = await qTon.CountAsync(d => d.TrangThai == DANG_XU_LY);
            vm.DonNayChoXacNhan = await qNay.CountAsync(d => d.TrangThai == CHO_XU_LY);
            vm.DonNayQuaGio = await qNay.CountAsync(d => d.TrangThai == DANG_XU_LY);
            vm.DsTon = await LayDsDonAsync(qTon, 4, true);
            vm.DsCanXuLyNay = await LayDsDonAsync(qNay, 4, true);

            // Đang diễn ra ngay lúc này (không tính đơn hủy)
            var sanDangDung = await datSans
                .Where(d => d.TrangThai != HUY && d.GioBatDau <= bay && d.GioKetThuc > bay)
                .Select(d => d.MaSan).Distinct().ToListAsync();
            vm.SanDangCoKhach = sanDangDung.Count;
            vm.DonDangDienRa = await datSans.CountAsync(d => d.TrangThai != HUY && d.GioBatDau <= bay && d.GioKetThuc > bay);

            // ----- 4. Đơn trong kỳ và kỳ trước (lọc theo ngày tạo đơn, giống trang Thống kê) -----
            var tuLay = tuBdTruoc < tuTruoc ? tuBdTruoc : tuTruoc;
            var dons = await datSans
                .Where(d => d.NgayDat >= tuLay && d.NgayDat < denLoaiTru)
                .Select(d => new DonDash
                {
                    MaDatSan = d.MaDatSan,
                    MaKhachHang = d.MaKhachHang,
                    TrangThai = d.TrangThai,
                    DonGia = d.DonGia,
                    NgayDat = d.NgayDat,
                    GioBatDau = d.GioBatDau
                })
                .ToListAsync();

            var kyNay = dons.Where(d => d.NgayDat >= tuNgay && d.NgayDat < denLoaiTru).ToList();
            var kyTruoc = dons.Where(d => d.NgayDat >= tuTruoc && d.NgayDat < denTruoc.AddDays(1)).ToList();
            var csNay = TinhChiSoDash(kyNay);
            var csTruoc = TinhChiSoDash(kyTruoc);

            vm.DoanhThu = csNay.DoanhThu; vm.DoanhThuTruoc = csTruoc.DoanhThu;
            vm.TongDonKy = csNay.Tong; vm.TongDonTruoc = csTruoc.Tong;
            vm.DonHoanThanhKy = csNay.Xong; vm.DonHoanThanhTruoc = csTruoc.Xong;
            vm.DonChoKy = csNay.Cho; vm.DonDangKy = csNay.Dang;
            vm.DonHuyKy = csNay.Huy; vm.DonHuyTruoc = csTruoc.Huy;
            vm.DonKhacKy = csNay.Khac;
            vm.CoKyTruoc = csTruoc.Tong > 0;
            vm.KhachCoDon = csNay.KhachCoDon; vm.KhachCoDonTruoc = csTruoc.KhachCoDon;
            vm.KhachQuayLai = csNay.KhachQuayLai; vm.KhachQuayLaiTruoc = csTruoc.KhachQuayLai;
            vm.DonKhongHuyKy = csNay.DonKhongHuy;

            // Doanh thu tạm tính (sân + dịch vụ, đơn chưa hủy) - tách riêng khỏi doanh thu thực thu
            var donTien = await LayDonTienAsync(d => d.NgayDat >= tuNgay && d.NgayDat < denLoaiTru && d.TrangThai != HUY);
            vm.DoanhThuTamTinh = donTien.Sum(x => x.TongTien);

            // Khách mới theo ngày đăng ký
            var khachs = _thongKeRepository.LayKhachHangs().AsNoTracking();
            var denTruocLoaiTru = denTruoc.AddDays(1);
            vm.KhachMoi = await khachs.CountAsync(k => k.NgayDangKy >= tuNgay && k.NgayDangKy < denLoaiTru);
            vm.KhachMoiTruoc = await khachs.CountAsync(k => k.NgayDangKy >= tuTruoc && k.NgayDangKy < denTruocLoaiTru);

            // ----- 5. Biểu đồ xu hướng (kỳ này so với cùng độ dài ngay trước đó) -----
            vm.CoSoSanhBieuDo = dons.Any(d => d.NgayDat >= tuTruoc && d.NgayDat < denTruoc.AddDays(1));
            // Thống kê theo giờ chỉ có nghĩa khi NgayDat có lưu giờ. Nếu toàn bộ đơn trong ngày đều là 00:00
            // (cột chỉ lưu ngày) thì chia theo giờ sẽ dồn hết vào 0h gây hiểu nhầm -> dùng cột "ngày" và nói rõ.
            if (donViCh == "giờ" && kyNay.Count > 0 && kyNay.All(d => d.NgayDat.TimeOfDay == TimeSpan.Zero))
            {
                donViCh = "ngày";
                nhanDonViCot = NhanCotCua(donViCh);
                vm.GhiChuBieuDo = "Hệ thống chỉ lưu ngày tạo đơn (không có giờ) nên không thể chia theo giờ.";
            }
            vm.DonViBieuDo = donViCh;
            vm.NhanDonViCot = nhanDonViCot;

            List<(string Ten, DateTime Tu, DateTime Den)> cotBd;
            if (donViCh == "giờ")
                cotBd = Enumerable.Range(0, 24).Select(h => ($"{h}h", tuNgay.AddHours(h), tuNgay.AddHours(h + 1))).ToList();
            else
                cotBd = TaoCotThoiGian(tuNgay, denNgay, donViCh);

            foreach (var c in cotBd)
            {
                var trongCot = dons.Where(d => d.NgayDat >= c.Tu && d.NgayDat < c.Den).ToList();
                var cs = TinhChiSoDash(trongCot);
                decimal? truoc = null;
                if (vm.CoSoSanhBieuDo)
                {
                    var tT = c.Tu.AddDays(-dichKyTruoc);
                    var dT = c.Den.AddDays(-dichKyTruoc);
                    truoc = dons.Where(d => d.TrangThai == HOAN_THANH && d.NgayDat >= tT && d.NgayDat < dT).Sum(d => d.DonGia);
                }
                var cuoi = c.Den.AddDays(-1);
                int soNgayCot = (c.Den - c.Tu).Days;
                string nhanDayDu = donViCh == "giờ" ? $"{c.Tu:dd/MM/yyyy} · {c.Tu:HH}:00 - {c.Den:HH}:00"
                    : donViCh == "ngày" ? c.Tu.ToString("dddd, dd/MM/yyyy", Vi)
                    : $"{c.Tu:dd/MM/yyyy} - {cuoi:dd/MM/yyyy}";
                // Tuần đầu/cuối hoặc tháng đầu/cuối của khoảng tùy chỉnh có thể chưa đủ ngày: ghi rõ số ngày thực tế
                if (donViCh == "tuần" && soNgayCot < 7) nhanDayDu += $" ({soNgayCot} ngày)";
                else if (donViCh == "tháng" && soNgayCot != DateTime.DaysInMonth(c.Tu.Year, c.Tu.Month)) nhanDayDu += $" ({soNgayCot} ngày)";
                vm.BieuDo.Add(new DashCotItem
                {
                    Ten = c.Ten,
                    NhanDayDu = nhanDayDu,
                    DoanhThu = cs.DoanhThu,
                    DoanhThuTruoc = truoc,
                    SoHoanThanh = cs.Xong,
                    SoDang = cs.Dang,
                    SoCho = cs.Cho,
                    SoHuy = cs.Huy,
                    SoKhac = cs.Khac
                });
            }

            // ----- 6. Hiệu suất sân (dùng lại cách tính của tab Hiệu suất ở trang Thống kê) -----
            vm.HieuSuat = await TinhHieuSuatSanAsync(tuNgay, denNgay, tuTruoc, denTruoc, donVi);
            if (soNgay >= 7)
            {
                vm.HieuSuatBanDo = vm.HieuSuat;
                vm.NhanBanDo = nhanKy;
            }
            else
            {
                // Kỳ quá ngắn (hôm nay) thì bản đồ nhiệt gần như trống -> lấy 7 ngày gần nhất và ghi rõ
                vm.HieuSuatBanDo = await TinhHieuSuatSanAsync(tuBd, denNgay, tuBdTruoc, tuBd.AddDays(-1), donVi);
                vm.NhanBanDo = $"7 ngày gần nhất · {tuBd:dd/MM} - {denNgay:dd/MM/yyyy}";
            }
            var hsb = vm.HieuSuatBanDo;
            vm.SanCongSuatThap = hsb.San
                .Where(s => s.GioKhaDung > 0 && s.TyLe < NGUONG_SAN_THAP)
                .OrderBy(s => s.TyLe).ThenBy(s => s.Ten).Take(3).ToList();

            // Khung giờ đông nhất / vắng nhất (gộp mọi thứ trong tuần), chỉ nêu khi chênh lệch đủ lớn
            var theoGio = new List<(int Gio, double Dat, double Cap)>();
            for (int g = 0; g < hsb.GioDong - hsb.GioMo; g++)
            {
                double dat = hsb.Heatmap.Sum(h => h.O.Count > g ? h.O[g].GioDat : 0);
                double cap = hsb.Heatmap.Sum(h => h.O.Count > g ? h.O[g].GioKhaDung : 0);
                if (cap > 0) theoGio.Add((hsb.GioMo + g, dat, cap));
            }
            if (theoGio.Count >= 2)
            {
                var cao = theoGio.OrderByDescending(x => x.Dat / x.Cap).First();
                var thap = theoGio.OrderBy(x => x.Dat / x.Cap).First();
                double tCao = cao.Dat / cao.Cap * 100, tThap = thap.Dat / thap.Cap * 100;
                if (tCao - tThap >= 20)
                {
                    vm.KhungGioDong = $"{cao.Gio:00}:00 - {cao.Gio + 1:00}:00 ({tCao:0}% công suất)";
                    vm.KhungGioVang = $"{thap.Gio:00}:00 - {thap.Gio + 1:00}:00 ({tThap:0}% công suất)";
                }
            }

            // ----- 7. Nhận định tự động: chỉ nêu khi dữ liệu đủ chứng minh -----
            if (csNay.Xong + csTruoc.Xong >= 3 && csTruoc.DoanhThu > 0)
            {
                double pct = (double)((csNay.DoanhThu - csTruoc.DoanhThu) / csTruoc.DoanhThu * 100m);
                if (Math.Abs(pct) >= 1)
                    vm.NhanDinh.Add(new DashNhanDinhItem
                    {
                        Muc = pct > 0 ? "tot" : "canhbao",
                        NoiDung = $"Doanh thu {(pct > 0 ? "tăng" : "giảm")} {Math.Abs(pct).ToString("0.#", Vi)}% so với {nhanSoSanh}."
                    });
            }
            if (csNay.Tong >= 5 && csTruoc.Tong >= 5)
            {
                double dHuy = vm.TyLeHuy - vm.TyLeHuyTruoc;
                if (dHuy >= 5)
                    vm.NhanDinh.Add(new DashNhanDinhItem
                    {
                        Muc = "canhbao",
                        NoiDung = $"Tỷ lệ hủy đơn tăng {dHuy.ToString("0.#", Vi)} điểm % so với {nhanSoSanh} (hiện {vm.TyLeHuy.ToString("0.#", Vi)}%)."
                    });
            }
            var gioBd = kyNay.Where(d => d.TrangThai != HUY).Select(d => d.GioBatDau.Hour).ToList();
            if (gioBd.Count >= 5)
            {
                var top = gioBd.GroupBy(h => h).OrderByDescending(g => g.Count()).First();
                double tl = top.Count() * 100.0 / gioBd.Count;
                if (tl >= 20)
                    vm.NhanDinh.Add(new DashNhanDinhItem
                    {
                        Muc = "info",
                        NoiDung = $"Khung giờ được đặt nhiều nhất là {top.Key:00}:00 - {top.Key + 1:00}:00 ({tl.ToString("0.#", Vi)}% số đơn trong kỳ)."
                    });
            }
            if (vm.SanCongSuatThap.Count > 0)
                vm.NhanDinh.Add(new DashNhanDinhItem
                {
                    Muc = "info",
                    NoiDung = $"{vm.SanCongSuatThap.Count} sân có công suất dưới {NGUONG_SAN_THAP:0}% trong {vm.NhanBanDo.Split('·')[0].Trim().ToLowerInvariant()}."
                });

            // ----- 8. Hoạt động gần đây -----
            vm.DonMoiNhat = await LayDsDonAsync(datSans, 6, false);
            vm.KhachMoiNhat = await khachs
                .OrderByDescending(k => k.NgayDangKy).ThenByDescending(k => k.MaKhachHang)
                .Take(5)
                .Select(k => new DashKhachMoiItem { HoTen = k.HoTen, NgayDangKy = k.NgayDangKy })
                .ToListAsync();

            return View(vm);
        }

        // ================== LỌC THEO KỲ ==================
        // Chỗ DUY NHẤT quyết định thống kê tính theo ngày nào.
        // Đang dùng NgayDat (ngày tạo đơn). Muốn theo ngày đá thực tế thì đổi
        // d.NgayDat thành d.GioBatDau ở dòng Where bên dưới.
        private static IQueryable<DatSan> LocTheoKy(IQueryable<DatSan> q, DateTime tuNgay, DateTime denLoaiTru)
        {
            return q.Where(d => d.NgayDat >= tuNgay && d.NgayDat < denLoaiTru);
        }

        // ================== THỐNG KÊ LINQ ==================
        // kieu: homnay | 7ngay | 30ngay | tuannay | thangnay | thangtruoc | quynay | nam | tuychon
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> ThongKe(string? kieu, int? nam, DateTime? tu, DateTime? den)
        {
            if (!LaAdmin()) return TuChoi();

            // ----- 1. Khoảng thời gian [tuNgay, denNgay] và kỳ trước -----
            var homNay = HomNayVn();
            kieu = (kieu ?? "nam").ToLowerInvariant();

            DateTime tuNgay, denNgay;
            switch (kieu)
            {
                case "homnay":
                    tuNgay = denNgay = homNay;
                    break;
                case "tuannay": // tuần bắt đầu từ thứ Hai
                    int lui = ((int)homNay.DayOfWeek + 6) % 7;
                    tuNgay = homNay.AddDays(-lui);
                    denNgay = tuNgay.AddDays(6);
                    break;
                case "thangnay":
                    tuNgay = new DateTime(homNay.Year, homNay.Month, 1);
                    denNgay = tuNgay.AddMonths(1).AddDays(-1);
                    break;
                case "quynay":
                    int quy = (homNay.Month - 1) / 3;
                    tuNgay = new DateTime(homNay.Year, quy * 3 + 1, 1);
                    denNgay = tuNgay.AddMonths(3).AddDays(-1);
                    break;
                case "7ngay":   // 7 ngày gần nhất (gồm hôm nay)
                    denNgay = homNay;
                    tuNgay = homNay.AddDays(-6);
                    break;
                case "30ngay":  // 30 ngày gần nhất (gồm hôm nay)
                    denNgay = homNay;
                    tuNgay = homNay.AddDays(-29);
                    break;
                case "thangtruoc":
                    tuNgay = new DateTime(homNay.Year, homNay.Month, 1).AddMonths(-1);
                    denNgay = tuNgay.AddMonths(1).AddDays(-1);
                    break;
                case "tuychon":
                    tuNgay = (tu ?? homNay).Date;
                    denNgay = (den ?? tuNgay).Date;
                    if (denNgay < tuNgay) (tuNgay, denNgay) = (denNgay, tuNgay);
                    if ((denNgay - tuNgay).TotalDays > 1830) denNgay = tuNgay.AddDays(1830); // tối đa ~5 năm
                    break;
                // (Khoảng tùy chọn kéo dài sang tương lai cũng được cắt về hôm nay ở bước "cắt kỳ" bên dưới)
                default:
                    kieu = "nam";
                    // Giới hạn năm hợp lệ để URL ?nam=0 hoặc ?nam=99999 không làm DateTime văng lỗi
                    int namChon = Math.Clamp(nam ?? homNay.Year, 2000, 2100);
                    tuNgay = new DateTime(namChon, 1, 1);
                    denNgay = new DateTime(namChon, 12, 31);
                    break;
            }
            // Cắt kỳ về hôm nay: không thống kê ngày chưa diễn ra
            // (VD chọn "Năm 2026" khi hôm nay 10/11 thì kỳ là 01/01 - 10/11, không phải đến 31/12).
            // Kỳ trước được lấy cùng độ dài để so sánh công bằng.
            if (denNgay > homNay) denNgay = homNay;
            if (tuNgay > denNgay) tuNgay = denNgay;

            var denLoaiTru = denNgay.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;

            // Kỳ trước: cùng giai đoạn (cùng số ngày đã trôi qua) của năm / tháng / quý trước
            DateTime tuTruoc, denTruoc;
            if (kieu == "nam")
            {
                tuTruoc = tuNgay.AddYears(-1);
                denTruoc = denNgay.AddYears(-1);
            }
            else if (kieu == "thangnay")
            {
                tuTruoc = tuNgay.AddMonths(-1);
                denTruoc = denNgay.AddMonths(-1);
            }
            else if (kieu == "quynay")
            {
                tuTruoc = tuNgay.AddMonths(-3);
                denTruoc = denNgay.AddMonths(-3);
            }
            else
            {
                denTruoc = tuNgay.AddDays(-1);
                tuTruoc = denTruoc.AddDays(-(soNgay - 1));
            }

            var vm = new ThongKeViewModel
            {
                Kieu = kieu,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TuNgayTruoc = tuTruoc,
                DenNgayTruoc = denTruoc,
                Nam = tuNgay.Year,
                // Kỳ ngắn chia theo ngày, kỳ vừa chia theo tuần, kỳ dài chia theo tháng
                DonViBieuDo = soNgay <= 31 ? "ngày" : (soNgay <= 120 ? "tuần" : "tháng")
            };

            // Danh sách năm có dữ liệu để chọn
            vm.DanhSachNam = await _thongKeRepository.LayDatSans()
                                .Select(d => d.NgayDat.Year)
                                .Distinct()
                                .OrderByDescending(y => y)
                                .ToListAsync();

            if (!vm.DanhSachNam.Contains(vm.Nam)) vm.DanhSachNam.Insert(0, vm.Nam);

            var datTrongKy = LocTheoKy(_thongKeRepository.LayDatSans().AsNoTracking(), tuNgay, denLoaiTru);

            // ----- 2. Kỳ trước (để hiện ▲/▼ so sánh) -----
            var datKyTruoc = LocTheoKy(_thongKeRepository.LayDatSans().AsNoTracking(), tuTruoc, denTruoc.AddDays(1));
            vm.LuotDatKyTruoc = await datKyTruoc.CountAsync();
            vm.DonHoanThanhKyTruoc = await datKyTruoc.CountAsync(d => d.TrangThai == HOAN_THANH);
            vm.DoanhThuKyTruoc = await datKyTruoc
                .Where(d => d.TrangThai == HOAN_THANH)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;

            // ----- 3. Các thống kê trong kỳ -----

            // Số sân theo loại (thông tin hiện có, không phụ thuộc thời gian)
            vm.SanTheoLoai = await _thongKeRepository.LaySanTheThaos().AsNoTracking()
                .GroupBy(s => s.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // Số lượt đặt theo sân (không tính đơn đã hủy) + sân được dùng nhiều nhất
            vm.LuotDatTheoSan = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.TenSan)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.SanNhieuNhat = vm.LuotDatTheoSan.FirstOrDefault();

            // Sân ít khách nhất: tính cả sân chưa có lượt nào trong kỳ (SoLuong = 0)
            var luotTheoTen = vm.LuotDatTheoSan.ToDictionary(x => x.Ten, x => x.SoLuong);
            var tatCaSan = await _thongKeRepository.LaySanTheThaos().AsNoTracking()
                .Select(s => s.TenSan)
                .ToListAsync();

            vm.SanItKhach = tatCaSan.Distinct()
                .Select(ten => new ThongKeItem
                {
                    Ten = ten,
                    SoLuong = luotTheoTen.TryGetValue(ten, out var n) ? n : 0
                })
                .OrderBy(x => x.SoLuong).ThenBy(x => x.Ten)
                .Take(5)
                .ToList();

            // Loại sân được ưa thích nhất
            vm.LuotDatTheoLoai = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.SanTheThao!.LoaiSan!.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();
            vm.LoaiSanUaThich = vm.LuotDatTheoLoai.FirstOrDefault();

            // Doanh thu: chỉ đơn Hoàn thành trong kỳ, cộng DonGia (giống Dashboard)
            var donHoanThanh = await datTrongKy
                .Where(d => d.TrangThai == HOAN_THANH)
                .Select(d => new
                {
                    d.MaDatSan,
                    d.NgayDat,
                    d.GioBatDau,
                    d.DonGia,
                    TenSan = d.SanTheThao!.TenSan,
                    TenLoai = d.SanTheThao!.LoaiSan!.TenLoai
                })
                .ToListAsync();

            // Biểu đồ doanh thu: tự chia theo ngày / tuần / tháng (kỳ nào không có đơn thì cột = 0)
            if (vm.DonViBieuDo == "ngày")
            {
                var theoNgay = donHoanThanh
                    .GroupBy(x => x.NgayDat.Date)
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                vm.DoanhThuTheoThang = Enumerable.Range(0, soNgay)
                    .Select(i => tuNgay.AddDays(i))
                    .Select(d =>
                    {
                        theoNgay.TryGetValue(d, out var v);
                        return new ThongKeItem { Ten = d.ToString("dd/MM"), GiaTri = v.Tien, SoLuong = v.SoDon };
                    }).ToList();
            }
            else if (vm.DonViBieuDo == "tuần")
            {
                // Mỗi cột = 7 ngày liên tiếp tính từ tuNgay; nhãn là ngày bắt đầu cột
                int soCot = (int)Math.Ceiling(soNgay / 7.0);
                var theoTuan = donHoanThanh
                    .GroupBy(x => (x.NgayDat.Date - tuNgay).Days / 7)
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                vm.DoanhThuTheoThang = Enumerable.Range(0, soCot)
                    .Select(i =>
                    {
                        theoTuan.TryGetValue(i, out var v);
                        return new ThongKeItem { Ten = tuNgay.AddDays(i * 7).ToString("dd/MM"), GiaTri = v.Tien, SoLuong = v.SoDon };
                    }).ToList();
            }
            else
            {
                var theoThang = donHoanThanh
                    .GroupBy(x => (x.NgayDat.Year, x.NgayDat.Month))
                    .ToDictionary(g => g.Key, g => (Tien: g.Sum(x => x.DonGia), SoDon: g.Count()));

                var thangDau = new DateTime(tuNgay.Year, tuNgay.Month, 1);
                int soThang = (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;

                vm.DoanhThuTheoThang = Enumerable.Range(0, soThang)
                    .Select(i => thangDau.AddMonths(i))
                    .Select(m =>
                    {
                        theoThang.TryGetValue((m.Year, m.Month), out var v);
                        return new ThongKeItem
                        {
                            Ten = kieu == "nam" ? $"Tháng {m.Month}" : $"T{m.Month}/{m.Year}",
                            GiaTri = v.Tien,
                            SoLuong = v.SoDon
                        };
                    }).ToList();
            }

            vm.DoanhThuTheoLoai = donHoanThanh.GroupBy(x => x.TenLoai)
                .Select(g => new ThongKeItem { Ten = g.Key, GiaTri = g.Sum(x => x.DonGia), SoLuong = g.Count() })
                .OrderByDescending(x => x.GiaTri)
                .ToList();

            vm.TongDoanhThuNam = donHoanThanh.Sum(x => x.DonGia);
            vm.SoDonHoanThanh = donHoanThanh.Count;

            // Doanh thu theo NGÀY SỬ DỤNG sân (giờ bắt đầu): thứ trong tuần và buổi trong ngày.
            // Cùng tập đơn hoàn thành đã lọc ở trên, chỉ khác chiều phân tích nên tổng luôn khớp TongDoanhThuNam.
            string[] tenThuTrongTuan = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
            ThongKeItem GomDoanhThuTheoGio(string nhanNhom, Func<DateTime, bool> thuocNhom)
            {
                var dsNhom = donHoanThanh.Where(x => thuocNhom(x.GioBatDau)).ToList();
                return new ThongKeItem { Ten = nhanNhom, GiaTri = dsNhom.Sum(x => x.DonGia), SoLuong = dsNhom.Count };
            }
            vm.DoanhThuTheoThu = Enumerable.Range(0, 7)
                .Select(thuSo => GomDoanhThuTheoGio(tenThuTrongTuan[thuSo], gio => ((int)gio.DayOfWeek + 6) % 7 == thuSo))
                .ToList();
            vm.DoanhThuTheoBuoi = new List<ThongKeItem>
            {
                GomDoanhThuTheoGio("Sáng (trước 12:00)", gio => gio.Hour < 12),
                GomDoanhThuTheoGio("Chiều (12:00 - 18:00)", gio => gio.Hour >= 12 && gio.Hour < 18),
                GomDoanhThuTheoGio("Tối (từ 18:00)", gio => gio.Hour >= 18)
            };

            // Danh sách đơn hoàn thành tạo nên doanh thu, số tiền lớn nhất trước
            vm.DonHoanThanhDs = donHoanThanh
                .OrderByDescending(x => x.DonGia).ThenByDescending(x => x.NgayDat).ThenBy(x => x.MaDatSan)
                .Take(200)
                .Select(x => new DonDongGopItem
                {
                    MaDatSan = x.MaDatSan,
                    TenSan = x.TenSan,
                    TenLoai = x.TenLoai,
                    NgayDat = x.NgayDat,
                    GioBatDau = x.GioBatDau,
                    DonGia = x.DonGia
                })
                .ToList();

            // Dịch vụ được sử dụng nhiều nhất (Top 10 theo số lượng) trong kỳ
            vm.DichVuPhoBien = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .SelectMany(d => d.ChiTietDatSans)
                .GroupBy(c => c.DichVu!.TenDichVu)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key,
                    SoLuong = g.Sum(c => c.SoLuong),
                    GiaTri = g.Sum(c => c.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToListAsync();

            // Số lượt hủy, tỷ lệ hủy và danh sách đơn hủy (để bấm vào xem chi tiết)
            vm.SoLuotHuy = await datTrongKy.CountAsync(d => d.TrangThai == HUY);
            vm.TongLuotDat = await datTrongKy.CountAsync();   // 0 nghĩa là kỳ này chưa có dữ liệu

            vm.DonHuy = await datTrongKy
                .Where(d => d.TrangThai == HUY)
                .OrderByDescending(d => d.NgayDat).ThenByDescending(d => d.GioBatDau)
                .Take(200)
                .Select(d => new DonHuyItem
                {
                    MaDatSan = d.MaDatSan,
                    TenSan = d.SanTheThao!.TenSan,
                    NgayDat = d.NgayDat,
                    GioBatDau = d.GioBatDau,
                    GioKetThuc = d.GioKetThuc,
                    DonGia = d.DonGia
                })
                .ToListAsync();

            // Tỷ lệ sử dụng theo khung giờ (theo giờ bắt đầu, bỏ đơn hủy)
            var gioBatDau = await datTrongKy
                .Where(d => d.TrangThai != HUY)
                .Select(d => d.GioBatDau)
                .ToListAsync();
            int tongGio = gioBatDau.Count;

            vm.KhungGio = gioBatDau.GroupBy(g => g.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new ThongKeItem
                {
                    Ten = $"{g.Key:00}:00 - {g.Key + 1:00}:00",
                    SoLuong = g.Count(),
                    TyLe = tongGio == 0 ? 0 : Math.Round(g.Count() * 100.0 / tongGio, 1)
                }).ToList();
            vm.KhungGioCaoDiem = vm.KhungGio.OrderByDescending(x => x.SoLuong).FirstOrDefault();

            // Tab "Khách hàng" (xem partial _ThongKeKhachHang.cshtml)
            vm.KhachHang = await TinhThongKeKhachHangAsync(datTrongKy, datKyTruoc, tuNgay, denNgay, tuTruoc, denTruoc, vm.DonViBieuDo);

            // Tab "Hiệu suất sân" (xem partial _ThongKeHieuSuat.cshtml)
            vm.HieuSuat = await TinhHieuSuatSanAsync(tuNgay, denNgay, tuTruoc, denTruoc, vm.DonViBieuDo);

            // Tab "Dịch vụ" (xem partial _ThongKeDichVu.cshtml)
            vm.DichVu = await TinhDichVuAsync(datTrongKy, datKyTruoc, tuNgay, denNgay, tuTruoc, denTruoc, vm.DonViBieuDo);

            return View(vm);
        }

        // ================== HIỆU SUẤT SÂN ==================
        // Giờ kinh doanh áp dụng cho mọi sân. Đổi hai hằng số này nếu cơ sở mở cửa khác giờ.
        // (Nếu sau này có giờ mở cửa riêng cho từng sân thì thay trong SoGioDaQua / TachSlot.)
        private const int GIO_MO_CUA = 6;
        private const int GIO_DONG_CUA = 22;

        // Hiệu suất tính theo NGÀY SỬ DỤNG sân (GioBatDau), không theo ngày tạo đơn, vì công suất là của ngày diễn ra.
        private sealed class DonGio
        {
            public int MaSan { get; set; }
            public DateTime GioBatDau { get; set; }
            public DateTime GioKetThuc { get; set; }
        }

        private readonly record struct SlotDat(int MaSan, DateTime Ngay, int Gio, double SoGio);

        // Khung giờ đã trôi qua hoàn toàn (chưa tính giờ hiện tại và các giờ sắp tới của hôm nay)
        private static bool SlotDaQua(DateTime ngay, int gio, DateTime bay)
            => ngay.Date < bay.Date || (ngay.Date == bay.Date && gio < bay.Hour);

        // Số giờ kinh doanh đã diễn ra của MỘT sân trong một ngày
        private static int SoGioDaQua(DateTime ngay, DateTime bay)
        {
            int dem = 0;
            for (int g = GIO_MO_CUA; g < GIO_DONG_CUA; g++)
                if (SlotDaQua(ngay, g, bay)) dem++;
            return dem;
        }

        private async Task<List<DonGio>> LayDonGioAsync(DateTime tu, DateTime den)
        {
            var denLoaiTru = den.AddDays(1);
            return await _thongKeRepository.LayDatSans().AsNoTracking()
                .Where(d => d.TrangThai != HUY && d.GioBatDau >= tu && d.GioBatDau < denLoaiTru)
                .Select(d => new DonGio { MaSan = d.MaSan, GioBatDau = d.GioBatDau, GioKetThuc = d.GioKetThuc })
                .ToListAsync();
        }

        // Tách mỗi đơn thành các phần theo từng giờ: đơn 17:30-19:00 => 17h (0,5), 18h (1).
        // Chỉ giữ phần nằm trong giờ kinh doanh và đã diễn ra, để tử số luôn khớp với mẫu số.
        private static List<SlotDat> TachSlot(IEnumerable<DonGio> dons, HashSet<int> sanHopLe,
                                              DateTime tu, DateTime den, DateTime bay)
        {
            var kq = new List<SlotDat>();
            foreach (var d in dons)
            {
                if (!sanHopLe.Contains(d.MaSan)) continue;
                var cur = d.GioBatDau;
                int chan = 0;
                while (cur < d.GioKetThuc && chan++ < 72)
                {
                    var dauGio = new DateTime(cur.Year, cur.Month, cur.Day, cur.Hour, 0, 0);
                    var cuoiGio = dauGio.AddHours(1);
                    var diem = d.GioKetThuc < cuoiGio ? d.GioKetThuc : cuoiGio;
                    var ngay = dauGio.Date;
                    if (ngay >= tu.Date && ngay <= den.Date
                        && dauGio.Hour >= GIO_MO_CUA && dauGio.Hour < GIO_DONG_CUA
                        && SlotDaQua(ngay, dauGio.Hour, bay))
                    {
                        kq.Add(new SlotDat(d.MaSan, ngay, dauGio.Hour, (diem - cur).TotalHours));
                    }
                    cur = diem;
                }
            }
            return kq;
        }

        // Chia kỳ thành các cột [Tu, Den) theo ngày / tuần / tháng (giống biểu đồ doanh thu)
        private static List<(string Ten, DateTime Tu, DateTime Den)> TaoCotThoiGian(DateTime tuNgay, DateTime denNgay, string donVi)
        {
            var cot = new List<(string, DateTime, DateTime)>();
            var denLoaiTru = denNgay.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;
            if (donVi == "ngày")
            {
                for (int i = 0; i < soNgay; i++)
                {
                    var bd = tuNgay.AddDays(i);
                    cot.Add((bd.ToString("dd/MM"), bd, bd.AddDays(1)));
                }
            }
            else if (donVi == "tuần")
            {
                int soCot = (int)Math.Ceiling(soNgay / 7.0);
                for (int i = 0; i < soCot; i++)
                {
                    var bd = tuNgay.AddDays(i * 7);
                    var kt = bd.AddDays(7);
                    if (kt > denLoaiTru) kt = denLoaiTru;
                    cot.Add((bd.ToString("dd/MM"), bd, kt));
                }
            }
            else
            {
                var thangDau = new DateTime(tuNgay.Year, tuNgay.Month, 1);
                int soThang = (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;
                bool cungNam = tuNgay.Year == denNgay.Year;
                for (int i = 0; i < soThang; i++)
                {
                    var dauThang = thangDau.AddMonths(i);
                    var cuoiThang = dauThang.AddMonths(1);
                    var bd = dauThang < tuNgay ? tuNgay : dauThang;
                    var kt = cuoiThang > denLoaiTru ? denLoaiTru : cuoiThang;
                    cot.Add((cungNam ? $"T{dauThang.Month}" : $"T{dauThang.Month}/{dauThang.Year}", bd, kt));
                }
            }
            return cot;
        }

        private async Task<HieuSuatSanViewModel> TinhHieuSuatSanAsync(
            DateTime tuNgay, DateTime denNgay, DateTime tuTruoc, DateTime denTruoc, string donVi)
        {
            var bay = DateTime.UtcNow.AddHours(7);
            int soNgay = (denNgay - tuNgay).Days + 1;
            const int soCotGio = GIO_DONG_CUA - GIO_MO_CUA;

            var vm = new HieuSuatSanViewModel { GioMo = GIO_MO_CUA, GioDong = GIO_DONG_CUA, SoNgay = soNgay };

            // Chỉ tính sân đang khả dụng: sân tạm ngưng/bảo trì không được coi là công suất có thể bán
            var sans = await _thongKeRepository.LaySanTheThaos().AsNoTracking()
                .Where(s => s.TrangThai == SAN_KHA_DUNG)
                .Select(s => new { s.MaSan, s.TenSan, TenLoai = s.LoaiSan!.TenLoai })
                .ToListAsync();
            int nSan = sans.Count;
            vm.SoSanKhaDung = nSan;
            var hopLe = sans.Select(s => s.MaSan).ToHashSet();

            // ----- Kỳ này -----
            var donKy = (await LayDonGioAsync(tuNgay, denNgay)).Where(d => hopLe.Contains(d.MaSan)).ToList();
            var slots = TachSlot(donKy, hopLe, tuNgay, denNgay, bay);

            double gioMoiSan = 0;                         // giờ kinh doanh đã diễn ra của 1 sân trong cả kỳ
            for (var n = tuNgay.Date; n <= denNgay.Date; n = n.AddDays(1))
                gioMoiSan += SoGioDaQua(n, bay);

            vm.GioKhaDung = gioMoiSan * nSan;
            vm.GioDaDat = slots.Sum(s => s.SoGio);
            vm.TyLeKhaiThac = vm.GioKhaDung > 0 ? Math.Round(Math.Min(vm.GioDaDat / vm.GioKhaDung, 1) * 100, 2) : 0;
            vm.SoLuotDat = donKy.Count;
            vm.LuotTbSanNgay = nSan == 0 || soNgay == 0 ? 0 : Math.Round(donKy.Count / (double)(nSan * soNgay), 2);

            // ----- Kỳ trước (cùng cách tính để so sánh công bằng) -----
            var donTruoc = (await LayDonGioAsync(tuTruoc, denTruoc)).Where(d => hopLe.Contains(d.MaSan)).ToList();
            var slotsTruoc = TachSlot(donTruoc, hopLe, tuTruoc, denTruoc, bay);
            double gioMoiSanTruoc = 0;
            for (var n = tuTruoc.Date; n <= denTruoc.Date; n = n.AddDays(1))
                gioMoiSanTruoc += SoGioDaQua(n, bay);
            double capTruoc = gioMoiSanTruoc * nSan;
            vm.GioDaDatTruoc = slotsTruoc.Sum(s => s.SoGio);
            vm.CoKyTruoc = capTruoc > 0 && vm.GioDaDatTruoc > 0;
            vm.TyLeKyTruoc = capTruoc > 0 ? Math.Round(Math.Min(vm.GioDaDatTruoc / capTruoc, 1) * 100, 2) : 0;

            // ----- Xu hướng theo cột thời gian (kỳ này so với cột tương ứng của kỳ trước) -----
            var datNgay = slots.GroupBy(s => s.Ngay).ToDictionary(g => g.Key, g => g.Sum(x => x.SoGio));
            var datNgayTruoc = slotsTruoc.GroupBy(s => s.Ngay).ToDictionary(g => g.Key, g => g.Sum(x => x.SoGio));

            (double Dat, double Cap) TongCot(DateTime tu, DateTime denLoaiTruCot, Dictionary<DateTime, double> theoNgay)
            {
                double dat = 0, cap = 0;
                for (var n = tu.Date; n < denLoaiTruCot.Date; n = n.AddDays(1))
                {
                    cap += SoGioDaQua(n, bay) * (double)nSan;
                    dat += theoNgay.GetValueOrDefault(n);
                }
                return (dat, cap);
            }

            var cotNay = TaoCotThoiGian(tuNgay, denNgay, donVi);
            var cotTruoc = TaoCotThoiGian(tuTruoc, denTruoc, donVi);
            for (int i = 0; i < cotNay.Count; i++)
            {
                var (dat, cap) = TongCot(cotNay[i].Tu, cotNay[i].Den, datNgay);
                double? tyLeTruoc = null;
                if (i < cotTruoc.Count)
                {
                    var (datT, capT) = TongCot(cotTruoc[i].Tu, cotTruoc[i].Den, datNgayTruoc);
                    if (capT > 0) tyLeTruoc = Math.Round(Math.Min(datT / capT, 1) * 100, 2);
                }
                vm.XuHuong.Add(new HieuSuatCotItem
                {
                    Ten = cotNay[i].Ten,
                    GioDat = Math.Round(dat, 1),
                    GioKhaDung = cap,
                    TyLe = cap > 0 ? Math.Round(Math.Min(dat / cap, 1) * 100, 2) : null,
                    TyLeTruoc = tyLeTruoc
                });
            }

            // ----- Bản đồ nhiệt: thứ trong tuần x giờ -----
            var capO = new double[7, soCotGio];
            var datO = new double[7, soCotGio];
            var luotO = new int[7, soCotGio];
            for (var n = tuNgay.Date; n <= denNgay.Date; n = n.AddDays(1))
            {
                int thu = ((int)n.DayOfWeek + 6) % 7;
                for (int g = GIO_MO_CUA; g < GIO_DONG_CUA; g++)
                    if (SlotDaQua(n, g, bay)) capO[thu, g - GIO_MO_CUA] += nSan;
            }
            foreach (var s in slots)
                datO[((int)s.Ngay.DayOfWeek + 6) % 7, s.Gio - GIO_MO_CUA] += s.SoGio;
            foreach (var d in donKy)
            {
                if (d.GioBatDau.Hour < GIO_MO_CUA || d.GioBatDau.Hour >= GIO_DONG_CUA) continue;
                luotO[((int)d.GioBatDau.DayOfWeek + 6) % 7, d.GioBatDau.Hour - GIO_MO_CUA]++;
            }
            string[] tenThu = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
            for (int t = 0; t < 7; t++)
            {
                var hang = new HeatHangItem { Ten = tenThu[t] };
                for (int g = 0; g < soCotGio; g++)
                {
                    hang.O.Add(new HeatOItem
                    {
                        GioDat = Math.Round(datO[t, g], 1),
                        GioKhaDung = capO[t, g],
                        Luot = luotO[t, g],
                        TyLe = capO[t, g] > 0 ? Math.Round(Math.Min(datO[t, g] / capO[t, g], 1) * 100, 2) : null
                    });
                }
                vm.Heatmap.Add(hang);
            }

            // ----- Từng sân -----
            var gioTheoSan = slots.GroupBy(s => s.MaSan).ToDictionary(g => g.Key, g => g.Sum(x => x.SoGio));
            var luotTheoSan = donKy.GroupBy(d => d.MaSan).ToDictionary(g => g.Key, g => g.Count());
            vm.San = sans.Select(s =>
            {
                double dat = gioTheoSan.GetValueOrDefault(s.MaSan);
                return new HieuSuatSanItem
                {
                    MaSan = s.MaSan,
                    Ten = s.TenSan,
                    TenLoai = s.TenLoai,
                    GioDat = Math.Round(dat, 1),
                    GioKhaDung = gioMoiSan,
                    TyLe = gioMoiSan > 0 ? Math.Round(Math.Min(dat / gioMoiSan, 1) * 100, 2) : 0,
                    Luot = luotTheoSan.GetValueOrDefault(s.MaSan)
                };
            })
            .OrderByDescending(x => x.TyLe).ThenByDescending(x => x.GioDat).ThenBy(x => x.Ten)
            .ToList();
            vm.SoSanCoHoatDong = vm.San.Count(x => x.Luot > 0);

            // ----- Theo loại sân (so sánh công bằng: giờ đặt / giờ khả dụng của tất cả sân trong loại) -----
            vm.Loai = vm.San.GroupBy(s => s.TenLoai)
                .Select(g =>
                {
                    double dat = g.Sum(x => x.GioDat);
                    double cap = g.Sum(x => x.GioKhaDung);
                    return new HieuSuatLoaiItem
                    {
                        Ten = g.Key,
                        SoSan = g.Count(),
                        GioDat = Math.Round(dat, 1),
                        GioKhaDung = cap,
                        TyLe = cap > 0 ? Math.Round(Math.Min(dat / cap, 1) * 100, 2) : 0,
                        Luot = g.Sum(x => x.Luot)
                    };
                })
                .OrderByDescending(x => x.TyLe).ThenByDescending(x => x.GioDat).ThenBy(x => x.Ten)
                .ToList();

            // ----- Ba mốc thời gian: lũy kế kỳ đang xem / tháng hiện tại / 7 ngày gần nhất -----
            // Luôn tính độc lập với bộ lọc để chủ sân thấy cả bức tranh tổng thể lẫn tình hình gần đây.
            var homNayVn = bay.Date;
            var cacMoc = new (string Nhan, DateTime Tu, DateTime Den)[]
            {
                ("Lũy kế kỳ đang xem", tuNgay, denNgay),
                ($"Tháng {homNayVn.Month} (từ đầu tháng)", new DateTime(homNayVn.Year, homNayVn.Month, 1), homNayVn),
                ("7 ngày gần nhất", homNayVn.AddDays(-6), homNayVn)
            };
            var tuRong = cacMoc.Min(m => m.Tu);
            var denRong = cacMoc.Max(m => m.Den);
            var donRong = (await LayDonGioAsync(tuRong, denRong)).Where(d => hopLe.Contains(d.MaSan)).ToList();
            foreach (var moc in cacMoc)
            {
                double gioMoiSanMoc = 0;
                for (var n = moc.Tu.Date; n <= moc.Den.Date; n = n.AddDays(1))
                    gioMoiSanMoc += SoGioDaQua(n, bay);
                double capMoc = gioMoiSanMoc * nSan;
                double datMoc = TachSlot(donRong, hopLe, moc.Tu, moc.Den, bay).Sum(x => x.SoGio);
                vm.Moc.Add(new HieuSuatMocItem
                {
                    Nhan = moc.Nhan,
                    Tu = moc.Tu,
                    Den = moc.Den,
                    GioDat = Math.Round(datMoc, 1),
                    GioKhaDung = capMoc,
                    CoCongSuat = capMoc > 0,
                    TyLe = capMoc > 0 ? Math.Round(Math.Min(datMoc / capMoc, 1) * 100, 2) : 0
                });
            }

            return vm;
        }

        // ================== THỐNG KÊ DỊCH VỤ ==================
        // Một dòng chi tiết dịch vụ của đơn KHÔNG hủy (dùng chung cho kỳ này và kỳ trước).
        private sealed class DongDichVu
        {
            public int MaDatSan { get; set; }
            public DateTime NgayDat { get; set; }
            public bool HoanThanh { get; set; }
            public string Ten { get; set; } = "";
            public int SoLuong { get; set; }
            public decimal ThanhTien { get; set; }
        }

        private static Task<List<DongDichVu>> LayDongDichVuAsync(IQueryable<DatSan> q)
        {
            return q.Where(d => d.TrangThai != HUY)
                .SelectMany(d => d.ChiTietDatSans.Select(c => new DongDichVu
                {
                    MaDatSan = d.MaDatSan,
                    NgayDat = d.NgayDat,
                    HoanThanh = d.TrangThai == HOAN_THANH,
                    Ten = c.DichVu!.TenDichVu,
                    SoLuong = c.SoLuong,
                    ThanhTien = c.ThanhTien
                }))
                .ToListAsync();
        }

        // Cùng bộ lọc kỳ và cùng quy tắc loại đơn hủy với các tab khác.
        // "Số đơn" luôn đếm đơn đặt sân riêng biệt, không đếm số dòng chi tiết.
        private async Task<DichVuViewModel> TinhDichVuAsync(
            IQueryable<DatSan> datTrongKy, IQueryable<DatSan> datKyTruoc,
            DateTime tuNgay, DateTime denNgay, DateTime tuTruoc, DateTime denTruoc, string donVi)
        {
            var vm = new DichVuViewModel();

            var dongNay = await LayDongDichVuAsync(datTrongKy);
            var dongTruoc = await LayDongDichVuAsync(datKyTruoc);

            vm.TongDonHopLe = await datTrongKy.CountAsync(d => d.TrangThai != HUY);
            vm.DonCoDichVu = await datTrongKy.CountAsync(d => d.TrangThai != HUY && d.ChiTietDatSans.Any());
            vm.TongDonHopLeTruoc = await datKyTruoc.CountAsync(d => d.TrangThai != HUY);
            vm.DonCoDichVuTruoc = await datKyTruoc.CountAsync(d => d.TrangThai != HUY && d.ChiTietDatSans.Any());

            vm.DoanhThu = dongNay.Sum(x => x.ThanhTien);
            vm.SoLuong = dongNay.Sum(x => x.SoLuong);
            vm.DoanhThuTruoc = dongTruoc.Sum(x => x.ThanhTien);
            vm.SoLuongTruoc = dongTruoc.Sum(x => x.SoLuong);
            vm.CoKyTruoc = dongTruoc.Count > 0;

            // Tách rõ: giá trị dịch vụ trên đơn không hủy (DoanhThu) và phần đã hoàn thành (ghi nhận doanh thu)
            vm.DoanhThuHoanThanh = dongNay.Where(x => x.HoanThanh).Sum(x => x.ThanhTien);
            // Mẫu số của tỷ trọng: tiền sân của cùng tập đơn không hủy, cùng kỳ (cộng DonGia như tab Doanh thu)
            vm.DoanhThuSanKhongHuy = await datTrongKy.Where(d => d.TrangThai != HUY)
                .SumAsync(d => (decimal?)d.DonGia) ?? 0m;

            // ----- Từng dịch vụ (kỳ này + kỳ trước) -----
            var nay = dongNay.GroupBy(x => x.Ten).ToDictionary(
                g => g.Key,
                g => (SoLuong: g.Sum(x => x.SoLuong), SoDon: g.Select(x => x.MaDatSan).Distinct().Count(), Tien: g.Sum(x => x.ThanhTien)));
            var truoc = dongTruoc.GroupBy(x => x.Ten).ToDictionary(
                g => g.Key,
                g => (SoLuong: g.Sum(x => x.SoLuong), Tien: g.Sum(x => x.ThanhTien)));

            vm.Items = nay.Keys.Union(truoc.Keys)
                .Select(ten =>
                {
                    var n = nay.GetValueOrDefault(ten);
                    var t = truoc.GetValueOrDefault(ten);
                    return new DichVuItem
                    {
                        Ten = ten,
                        SoLuong = n.SoLuong,
                        SoDon = n.SoDon,
                        DoanhThu = n.Tien,
                        SoLuongTruoc = t.SoLuong,
                        DoanhThuTruoc = t.Tien
                    };
                })
                .OrderByDescending(x => x.DoanhThu).ThenByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
                .ToList();

            // ----- Xu hướng doanh thu dịch vụ theo cột thời gian (cùng cách chia với biểu đồ doanh thu) -----
            var cotNay = TaoCotThoiGian(tuNgay, denNgay, donVi);
            var cotTruoc = TaoCotThoiGian(tuTruoc, denTruoc, donVi);
            for (int i = 0; i < cotNay.Count; i++)
            {
                var c = cotNay[i];
                var trongCot = dongNay.Where(x => x.NgayDat >= c.Tu && x.NgayDat < c.Den).ToList();
                decimal? tienTruoc = null;
                if (i < cotTruoc.Count)
                {
                    var ct = cotTruoc[i];
                    tienTruoc = dongTruoc.Where(x => x.NgayDat >= ct.Tu && x.NgayDat < ct.Den).Sum(x => x.ThanhTien);
                }
                vm.XuHuong.Add(new DichVuCotItem
                {
                    Ten = c.Ten,
                    DoanhThu = trongCot.Sum(x => x.ThanhTien),
                    DoanhThuTruoc = tienTruoc,
                    SoLuong = trongCot.Sum(x => x.SoLuong),
                    SoDon = trongCot.Select(x => x.MaDatSan).Distinct().Count()
                });
            }

            return vm;
        }

        // ================== THỐNG KÊ KHÁCH HÀNG ==================
        // Hạng thành viên suy ra từ điểm tích lũy (100.000đ = 10 điểm => 1 triệu đồng = 100 điểm)
        private static readonly (string Ten, string MoTa, string Css, int TuDiem)[] CacHang =
        {
            ("Thành viên", "0 - 99 điểm",      "tv",   0),
            ("Bạc",        "100 - 499 điểm",   "bac",  100),
            ("Vàng",       "500 - 1.499 điểm", "vang", 500),
            ("Kim cương",  "từ 1.500 điểm",    "kc",   1500)
        };

        private static string XepHang(int diem)
        {
            string ten = CacHang[0].Ten;
            foreach (var h in CacHang)
                if (diem >= h.TuDiem) ten = h.Ten;
            return ten;
        }

        // Một đơn đặt sân rút gọn (mọi trạng thái) dùng cho thống kê khách hàng
        internal sealed class DonKhach
        {
            public int MaDatSan { get; set; }
            public int MaKhachHang { get; set; }
            public string TrangThai { get; set; } = "";
            public decimal DonGia { get; set; }
            public DateTime NgayDat { get; set; }
            public DateTime GioKetThuc { get; set; }
            public decimal TienCoc { get; set; }
        }

        private static IQueryable<DonKhach> ChieuDon(IQueryable<DatSan> q)
        {
            return q.Select(d => new DonKhach
            {
                MaDatSan = d.MaDatSan,
                MaKhachHang = d.MaKhachHang,
                TrangThai = d.TrangThai,
                DonGia = d.DonGia,
                NgayDat = d.NgayDat,
                GioKetThuc = d.GioKetThuc,
                TienCoc = d.TienCoc
            });
        }

        // Chỉ số cốt lõi của một kỳ (dùng cho cả kỳ này và kỳ trước để so sánh)
        internal sealed record ChiSoKy(int KhachCoDon, int KhachQuayLai, double TyLeQuayLai,
                                      int TongDon, int SoHuy, double TyLeHuy);

        private static ChiSoKy TinhChiSoKy(List<DonKhach> don)
        {
            var luotMoiKhach = don
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.MaKhachHang)
                .Select(g => g.Count())
                .ToList();

            int khach = luotMoiKhach.Count;
            int quayLai = luotMoiKhach.Count(n => n >= 2);
            int soHuy = don.Count(d => d.TrangThai == HUY);

            return new ChiSoKy(
                khach,
                quayLai,
                khach == 0 ? 0 : Math.Round(quayLai * 100.0 / khach, 1),
                don.Count,
                soHuy,
                don.Count == 0 ? 0 : Math.Round(soHuy * 100.0 / don.Count, 1));
        }

        private async Task<ThongKeKhachHangViewModel> TinhThongKeKhachHangAsync(
            IQueryable<DatSan> datTrongKy, IQueryable<DatSan> datKyTruoc,
            DateTime tuNgay, DateTime denNgay, DateTime tuTruoc, DateTime denTruoc, string donVi)
        {
            var denLoaiTru = denNgay.AddDays(1);
            var denTruocLoaiTru = denTruoc.AddDays(1);
            int soNgay = (denNgay - tuNgay).Days + 1;

            var vm = new ThongKeKhachHangViewModel
            {
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TuNgayTruoc = tuTruoc,
                DenNgayTruoc = denTruoc,
                DonVi = donVi
            };

            // Khách hàng: chỉ lấy các cột cần dùng (hệ thống quản lý sân nên số lượng khách nhỏ)
            var khachHangs = await _thongKeRepository.LayKhachHangs().AsNoTracking()
                .Select(k => new
                {
                    k.MaKhachHang,
                    k.HoTen,
                    k.SoDienThoai,
                    k.DiemTichLuy,
                    k.NgayDangKy
                })
                .ToListAsync();
            var thongTin = khachHangs.ToDictionary(k => k.MaKhachHang);
            vm.TongKhachHang = khachHangs.Count;

            // Các đơn trong kỳ và kỳ trước (mọi trạng thái) - gom theo khách ở bộ nhớ cho đơn giản, dễ kiểm chứng
            var don = await ChieuDon(datTrongKy).ToListAsync();
            var donTruoc = await ChieuDon(datKyTruoc).ToListAsync();

            // ----- 0. Kỳ trước (để hiện ▲/▼ trên thẻ KPI) -----
            var csTruoc = TinhChiSoKy(donTruoc);
            vm.KhachCoDonTruoc = csTruoc.KhachCoDon;
            vm.TyLeQuayLaiTruoc = csTruoc.TyLeQuayLai;
            vm.TongDonTruoc = csTruoc.TongDon;
            vm.TyLeHuyTruoc = csTruoc.TyLeHuy;
            vm.KhachMoiTruoc = khachHangs.Count(k => k.NgayDangKy >= tuTruoc && k.NgayDangKy < denTruocLoaiTru);
            vm.CoDuLieuKyTruoc = csTruoc.TongDon > 0 || vm.KhachMoiTruoc > 0;

            // ----- 1. Top khách hàng theo doanh thu / số lượt đặt (bỏ đơn hủy) -----
            var theoKhach = don
                .Where(d => d.TrangThai != HUY)
                .GroupBy(d => d.MaKhachHang)
                .Select(g => new
                {
                    Ma = g.Key,
                    Luot = g.Count(),
                    DoanhThu = g.Where(x => x.TrangThai == HOAN_THANH).Sum(x => x.DonGia)
                })
                .ToList();

            KhachVipItem TaoVip(int ma, int luot, decimal doanhThu)
            {
                if (thongTin.TryGetValue(ma, out var k))
                    return new KhachVipItem
                    {
                        MaKhachHang = ma,
                        HoTen = k.HoTen,
                        SoDienThoai = k.SoDienThoai,
                        SoLuot = luot,
                        DoanhThu = doanhThu,
                        DiemTichLuy = k.DiemTichLuy,
                        Hang = XepHang(k.DiemTichLuy)
                    };
                return new KhachVipItem
                {
                    MaKhachHang = ma,
                    HoTen = $"Khách #{ma}",
                    SoLuot = luot,
                    DoanhThu = doanhThu,
                    Hang = XepHang(0)
                };
            }

            vm.TopDoanhThu = theoKhach
                .Where(x => x.DoanhThu > 0)
                .OrderByDescending(x => x.DoanhThu).ThenByDescending(x => x.Luot)
                .Take(10)
                .Select(x => TaoVip(x.Ma, x.Luot, x.DoanhThu))
                .ToList();

            vm.TopLuotDat = theoKhach
                .OrderByDescending(x => x.Luot).ThenByDescending(x => x.DoanhThu)
                .Take(10)
                .Select(x => TaoVip(x.Ma, x.Luot, x.DoanhThu))
                .ToList();

            vm.KhachCoDon = theoKhach.Count;
            vm.TongDoanhThu = theoKhach.Sum(x => x.DoanhThu);
            vm.TyTrongDoanhThuTop = vm.TongDoanhThu == 0
                ? 0
                : Math.Round((double)(vm.TopDoanhThu.Sum(x => x.DoanhThu) / vm.TongDoanhThu) * 100.0, 1);

            // ----- 2. Khách quay lại (>= 2 đơn không hủy trong kỳ) so với khách đặt 1 lần -----
            var quayLai = theoKhach.Where(x => x.Luot >= 2).ToList();
            var motLan = theoKhach.Where(x => x.Luot == 1).ToList();

            vm.KhachQuayLai = quayLai.Count;
            vm.TyLeQuayLai = vm.KhachCoDon == 0
                ? 0
                : Math.Round(vm.KhachQuayLai * 100.0 / vm.KhachCoDon, 1);
            vm.DoanhThuQuayLai = quayLai.Sum(x => x.DoanhThu);
            vm.TyTrongDoanhThuQuayLai = vm.TongDoanhThu == 0
                ? 0
                : Math.Round((double)(vm.DoanhThuQuayLai / vm.TongDoanhThu) * 100.0, 1);
            vm.KhachMotLan = motLan.Count;
            vm.DoanhThuMotLan = motLan.Sum(x => x.DoanhThu);

            // ----- 3. Hạng khách hàng: số khách là tích lũy hiện tại, doanh thu là trong kỳ -----
            var demHang = khachHangs
                .GroupBy(k => XepHang(k.DiemTichLuy))
                .ToDictionary(g => g.Key, g => g.Count());

            var doanhThuHang = new Dictionary<string, decimal>();
            var khachChiTieuHang = new Dictionary<string, int>();
            foreach (var it in theoKhach.Where(x => x.DoanhThu > 0))
            {
                string hangCuaKhach = thongTin.TryGetValue(it.Ma, out var kh)
                    ? XepHang(kh.DiemTichLuy)
                    : XepHang(0);
                doanhThuHang[hangCuaKhach] = doanhThuHang.GetValueOrDefault(hangCuaKhach) + it.DoanhThu;
                khachChiTieuHang[hangCuaKhach] = khachChiTieuHang.GetValueOrDefault(hangCuaKhach) + 1;
            }

            vm.Hang = CacHang.Select(h =>
            {
                demHang.TryGetValue(h.Ten, out var soLuong);
                decimal dt = doanhThuHang.GetValueOrDefault(h.Ten);
                int khachCoChiTieu = khachChiTieuHang.GetValueOrDefault(h.Ten);
                return new HangKhachItem
                {
                    Ten = h.Ten,
                    MoTa = h.MoTa,
                    Css = h.Css,
                    SoLuong = soLuong,
                    TyLe = vm.TongKhachHang == 0 ? 0 : Math.Round(soLuong * 100.0 / vm.TongKhachHang, 1),
                    KhachCoChiTieu = khachCoChiTieu,
                    DoanhThu = dt,
                    ChiTieuTrungBinh = khachCoChiTieu == 0 ? 0 : Math.Round(dt / khachCoChiTieu, 0),
                    TyTrongDoanhThu = vm.TongDoanhThu == 0 ? 0 : Math.Round((double)(dt / vm.TongDoanhThu) * 100.0, 1)
                };
            }).ToList();

            // ----- 4. Các cột thời gian dùng chung cho biểu đồ (ngày / tuần / tháng) -----
            // Mỗi cột = [Tu, Den) đã được cắt trong phạm vi kỳ đang lọc
            var cot = new List<(string Ten, DateTime Tu, DateTime Den)>();
            if (donVi == "ngày")
            {
                for (int i = 0; i < soNgay; i++)
                {
                    var batDau = tuNgay.AddDays(i);
                    cot.Add((batDau.ToString("dd/MM"), batDau, batDau.AddDays(1)));
                }
            }
            else if (donVi == "tuần")
            {
                int soCot = (int)Math.Ceiling(soNgay / 7.0);
                for (int i = 0; i < soCot; i++)
                {
                    var batDau = tuNgay.AddDays(i * 7);
                    var ketThuc = batDau.AddDays(7);
                    if (ketThuc > denLoaiTru) ketThuc = denLoaiTru;
                    cot.Add((batDau.ToString("dd/MM"), batDau, ketThuc));
                }
            }
            else
            {
                var thangDau = new DateTime(tuNgay.Year, tuNgay.Month, 1);
                int soThang = (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;
                bool cungNam = tuNgay.Year == denNgay.Year;
                for (int i = 0; i < soThang; i++)
                {
                    var dauThang = thangDau.AddMonths(i);
                    var cuoiThang = dauThang.AddMonths(1);
                    var batDau = dauThang < tuNgay ? tuNgay : dauThang;
                    var ketThuc = cuoiThang > denLoaiTru ? denLoaiTru : cuoiThang;
                    string ten = cungNam ? $"T{dauThang.Month}" : $"T{dauThang.Month}/{dauThang.Year}";
                    cot.Add((ten, batDau, ketThuc));
                }
            }

            // ----- 5. Khách mới (NgayDangKy) và khách đặt lại theo từng cột -----
            var ngayDangKy = khachHangs
                .Where(k => k.NgayDangKy >= tuNgay && k.NgayDangKy < denLoaiTru)
                .Select(k => k.NgayDangKy)
                .ToList();
            vm.TongKhachMoi = ngayDangKy.Count;

            vm.KhachMoi = cot.Select(c => new ThongKeItem
            {
                Ten = c.Ten,
                SoLuong = ngayDangKy.Count(x => x >= c.Tu && x < c.Den)
            }).ToList();

            // Đơn ĐẦU TIÊN của mỗi khách trong toàn bộ lịch sử (không tính đơn hủy), xác định theo (NgayDat, MaDatSan).
            // Khách "đặt lại" trong một cột = có đơn trong cột đó KHÔNG phải đơn đầu tiên của họ.
            // Cách này tính cả khách đặt lần 2 ngay trong cùng cột (cùng tháng), nên khớp hơn với thẻ KPI "tái đặt"
            // (trước đây chỉ tính khi đơn đầu nằm trước ngày bắt đầu cột nên bỏ sót).
            var lichSuDat = await _thongKeRepository.LayDatSans().AsNoTracking()
                .Where(d => d.TrangThai != HUY && d.NgayDat < denLoaiTru)
                .Select(d => new { d.MaDatSan, d.MaKhachHang, d.NgayDat, d.TrangThai, d.DonGia })
                .ToListAsync();
            var donTheoKhach = lichSuDat
                .GroupBy(x => x.MaKhachHang)
                .ToDictionary(g => g.Key, g => g.ToList());
            var donDauTien = donTheoKhach
                .ToDictionary(kv => kv.Key, kv => kv.Value.OrderBy(x => x.NgayDat).ThenBy(x => x.MaDatSan).First().MaDatSan);

            // ----- 5b. Khách mới thực sự, chuyển đổi, nguy cơ rời bỏ, cohort (đều tính đến cuối kỳ) -----
            var ngayDauTien = donTheoKhach.ToDictionary(kv => kv.Key, kv => kv.Value.Min(x => x.NgayDat).Date);

            // Khách mới thực sự = đơn không hủy đầu tiên trong lịch sử rơi vào kỳ
            vm.KhachMoiThucSu = ngayDauTien.Count(kv => kv.Value >= tuNgay && kv.Value < denLoaiTru);
            vm.KhachMoiThucSuTruoc = ngayDauTien.Count(kv => kv.Value >= tuTruoc && kv.Value < denTruocLoaiTru);
            vm.KhachQuayLaiTuKyTruoc = theoKhach.Count(x => ngayDauTien.TryGetValue(x.Ma, out var ngayDau0) && ngayDau0 < tuNgay);
            vm.DoanhThuTruoc = donTruoc.Where(d => d.TrangThai == HOAN_THANH).Sum(d => d.DonGia);

            // Tỷ lệ chuyển đổi đăng ký -> có đơn trong 30 ngày.
            // Chỉ tính tài khoản đã đủ 30 ngày quan sát (tránh coi tài khoản mới đăng ký hôm qua là "chưa chuyển đổi").
            var dangKyDuThoiGian = khachHangs
                .Select(x => new { x.MaKhachHang, Ngay = Convert.ToDateTime(x.NgayDangKy).Date })
                .Where(x => x.Ngay >= tuNgay && x.Ngay.AddDays(CUA_SO_CHUYEN_DOI) <= denNgay.Date)
                .ToList();
            vm.SoDangKyDuThoiGian = dangKyDuThoiGian.Count;
            vm.SoDangKyChuyenDoi = dangKyDuThoiGian.Count(x =>
                donTheoKhach.TryGetValue(x.MaKhachHang, out var dsDonDk)
                && dsDonDk.Any(o => o.NgayDat >= x.Ngay && o.NgayDat < x.Ngay.AddDays(CUA_SO_CHUYEN_DOI)));
            vm.TyLeChuyenDoi = vm.SoDangKyDuThoiGian == 0
                ? 0
                : Math.Round(vm.SoDangKyChuyenDoi * 100.0 / vm.SoDangKyDuThoiGian, 1);

            // Nguy cơ rời bỏ: từng đặt >= 2 lần nhưng đã >= NGAY_NGUY_CO ngày chưa quay lại (tính đến cuối kỳ)
            vm.NguongNgayNguyCo = NGAY_NGUY_CO;
            var dsNguyCo = new List<KhachNguyCoItem>();
            int motLanLau = 0;
            foreach (var kv in donTheoKhach)
            {
                var lanCuoi = kv.Value.Max(x => x.NgayDat).Date;
                int ngayChua = (denNgay.Date - lanCuoi).Days;
                if (ngayChua < NGAY_NGUY_CO) continue;
                if (kv.Value.Count == 1) { motLanLau++; continue; }

                decimal chiTieu = kv.Value.Where(x => x.TrangThai == HOAN_THANH).Sum(x => x.DonGia);
                string hoTenNc = $"Khách #{kv.Key}", sdtNc = "", hangNc = XepHang(0);
                if (thongTin.TryGetValue(kv.Key, out var khNc))
                {
                    hoTenNc = khNc.HoTen;
                    sdtNc = khNc.SoDienThoai;
                    hangNc = XepHang(khNc.DiemTichLuy);
                }
                dsNguyCo.Add(new KhachNguyCoItem
                {
                    MaKhachHang = kv.Key,
                    HoTen = hoTenNc,
                    SoDienThoai = sdtNc,
                    Hang = hangNc,
                    SoDon = kv.Value.Count,
                    TongChiTieu = chiTieu,
                    LanDatGanNhat = lanCuoi,
                    SoNgayChuaQuayLai = ngayChua
                });
            }
            vm.SoKhachNguyCo = dsNguyCo.Count;
            vm.ChiTieuKhachNguyCo = dsNguyCo.Sum(x => x.TongChiTieu);
            vm.SoKhachMotLanLauChuaQuayLai = motLanLau;
            vm.KhachNguyCo = dsNguyCo
                .OrderByDescending(x => x.TongChiTieu).ThenByDescending(x => x.SoNgayChuaQuayLai)
                .Take(10)
                .ToList();

            // Cohort: nhóm khách theo THÁNG có đơn đầu tiên, 6 tháng gần nhất tính đến cuối kỳ.
            // Ô ở tháng chưa kết thúc là null (chưa đủ dữ liệu), không phải 0%.
            static int ChiSoThang(DateTime d) => d.Year * 12 + d.Month;
            var thangCuoi = new DateTime(denNgay.Year, denNgay.Month, 1);
            var thangCuaKhach = donTheoKhach.ToDictionary(
                kv => kv.Key, kv => kv.Value.Select(x => ChiSoThang(x.NgayDat)).ToHashSet());
            var nhomDau = ngayDauTien
                .GroupBy(kv => ChiSoThang(kv.Value))
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            for (int ci = 5; ci >= 0; ci--)
            {
                var dauThangC = thangCuoi.AddMonths(-ci);
                if (!nhomDau.TryGetValue(ChiSoThang(dauThangC), out var dsKhachC) || dsKhachC.Count == 0) continue;

                var hang = new CohortItem { Ten = $"T{dauThangC.Month}/{dauThangC.Year}", SoKhach = dsKhachC.Count };
                for (int kk = 0; kk <= 5; kk++)
                {
                    var thangK = dauThangC.AddMonths(kk);
                    if (kk == 0) { hang.TyLe.Add(100); continue; }
                    if (thangK.AddMonths(1) > denLoaiTru) { hang.TyLe.Add(null); continue; }  // tháng chưa kết thúc

                    int idxK = ChiSoThang(thangK);
                    int conLai = dsKhachC.Count(maKh => thangCuaKhach[maKh].Contains(idxK));
                    hang.TyLe.Add(Math.Round(conLai * 100.0 / dsKhachC.Count, 1));
                }
                vm.Cohort.Add(hang);
            }

            var donKhongHuy = don.Where(d => d.TrangThai != HUY).ToList();

            vm.KhachDatLai = cot.Select(c => new ThongKeItem
            {
                Ten = c.Ten,
                SoLuong = donKhongHuy
                    .Where(d => d.NgayDat >= c.Tu && d.NgayDat < c.Den
                                && donDauTien.TryGetValue(d.MaKhachHang, out var maDonDau) && maDonDau != d.MaDatSan)
                    .Select(d => d.MaKhachHang)
                    .Distinct()
                    .Count()
            }).ToList();

            // ----- 6. Hủy đơn: tổng quan, theo cột thời gian, và khách hủy nhiều lần -----
            vm.TongDon = don.Count;
            vm.SoDonHuy = don.Count(d => d.TrangThai == HUY);
            vm.TyLeHuy = vm.TongDon == 0 ? 0 : Math.Round(vm.SoDonHuy * 100.0 / vm.TongDon, 1);

            // Đơn hủy đã có tiền cọc (rủi ro mất/hoàn cọc) - dùng TienCoc có sẵn trong bảng DatSan
            var donHuyList = don.Where(d => d.TrangThai == HUY).ToList();
            vm.SoDonHuyCoCoc = donHuyList.Count(d => d.TienCoc > 0);
            vm.TienCocDonHuy = donHuyList.Sum(d => d.TienCoc);

            // Đơn đã quá giờ kết thúc nhưng vẫn "Chờ xử lý"/"Đang xử lý": có thể khách không đến hoặc nhân viên quên cập nhật.
            // Tách riêng khỏi đơn hủy vì đây là hai vấn đề vận hành khác nhau.
            var bayGio = DateTime.UtcNow.AddHours(7);
            vm.SoDonQuaGioChuaXuLy = don.Count(d =>
                (d.TrangThai == CHO_XU_LY || d.TrangThai == DANG_XU_LY) && d.GioKetThuc < bayGio);

            vm.HuyTheoKy = cot.Select(c =>
            {
                var donTrongCot = don.Where(d => d.NgayDat >= c.Tu && d.NgayDat < c.Den).ToList();
                int soHuyCot = donTrongCot.Count(d => d.TrangThai == HUY);
                return new HuyKyItem
                {
                    Ten = c.Ten,
                    TongDon = donTrongCot.Count,
                    SoHuy = soHuyCot,
                    TyLe = donTrongCot.Count == 0 ? 0 : Math.Round(soHuyCot * 100.0 / donTrongCot.Count, 1)
                };
            }).ToList();

            var huyTheoKhach = don
                .GroupBy(d => d.MaKhachHang)
                .Select(g => new
                {
                    Ma = g.Key,
                    Tong = g.Count(),
                    Huy = g.Count(x => x.TrangThai == HUY)
                })
                .Where(x => x.Huy > 0)
                .ToList();

            vm.SoKhachHuy = huyTheoKhach.Count;
            var lapLai = huyTheoKhach.Where(x => x.Huy >= 2).ToList();
            vm.SoKhachHuyLapLai = lapLai.Count;
            vm.TyLeHuyDoKhachLapLai = vm.SoDonHuy == 0
                ? 0
                : Math.Round(lapLai.Sum(x => x.Huy) * 100.0 / vm.SoDonHuy, 1);

            // Chỉ liệt kê khách hủy từ 2 đơn trở lên (khách hủy 1 đơn không đủ cơ sở để xếp hạng)
            vm.KhachHayHuy = lapLai
                .OrderByDescending(x => x.Huy)
                .ThenByDescending(x => (double)x.Huy / x.Tong)
                .Take(10)
                .Select(x =>
                {
                    double tyLe = Math.Round(x.Huy * 100.0 / x.Tong, 1);
                    string hoTen = $"Khách #{x.Ma}";
                    string sdt = "";
                    if (thongTin.TryGetValue(x.Ma, out var k))
                    {
                        hoTen = k.HoTen;
                        sdt = k.SoDienThoai;
                    }
                    return new KhachHuyItem
                    {
                        MaKhachHang = x.Ma,
                        HoTen = hoTen,
                        SoDienThoai = sdt,
                        SoDon = x.Tong,
                        SoHuy = x.Huy,
                        TyLeHuy = tyLe,
                        CanLuuY = tyLe >= 50
                    };
                })
                .ToList();

            return vm;
        }

        // ================== HÀM HỖ TRỢ ==================
        // Chỉ dùng cho "Tạm tính hôm nay" ở Dashboard:
        // TongTien = tiền sân (đơn giá x số giờ) + tiền dịch vụ
        private async Task<List<DonTien>> LayDonTienAsync(Expression<Func<DatSan, bool>> dieuKien)
        {
            var raw = await _thongKeRepository.LayDatSans().AsNoTracking()
                .Where(dieuKien)
                .Select(d => new
                {
                    d.MaDatSan,
                    d.NgayDat,
                    d.GioBatDau,
                    d.GioKetThuc,
                    d.DonGia,
                    TenLoai = d.SanTheThao!.LoaiSan!.TenLoai,
                    TienDichVu = d.ChiTietDatSans.Sum(c => (decimal?)c.ThanhTien) ?? 0m
                })
                .ToListAsync();

            return raw.Select(x => new DonTien
            {
                MaDatSan = x.MaDatSan,
                NgayDat = x.NgayDat,
                TenLoai = x.TenLoai,
                TongTien = TienSan(x.DonGia, x.GioBatDau, x.GioKetThuc) + x.TienDichVu
            }).ToList();
        }
    }
}