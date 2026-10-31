// Họ và tên: Vũ Ánh Ngọc
// Mã sinh viên: 23103100106
// Nội dung thực hiện: Module 4 - Tiếp nhận đặt sân: danh sách cần xử lý (tìm kiếm, lọc, sắp xếp, phân trang),
//                     xem chi tiết, xác nhận tiền cọc, duyệt / từ chối / hoàn thành đơn, kiểm tra trùng lịch, giờ mở cửa,
//                     tiền cọc, giới hạn lượt đặt bằng LINQ và kiểm soát luồng trạng thái tách riêng đặt sân & thanh toán.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class TiepNhanDatSanController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Số đơn hiển thị trên 1 trang
        private const int KichThuocTrang = 10;

        // Số lượng tối đa của 1 dịch vụ trong 1 đơn
        private const int SoLuongDichVuToiDa = 99;

        // Mục 8.5: Giới hạn số lượt đặt (chưa hủy) của 1 sân trong 1 ngày
        private const int GioiHanLuotMoiNgay = 8;

        // Các trạng thái của đơn đặt sân (hỗ trợ cả chuẩn mới Chờ duyệt/Đã duyệt/Từ chối và cũ Chờ xử lý/Đang xử lý/Đã hủy)
        private const string ChoXuLy = "Chờ xử lý";
        private const string ChoDuyet = "Chờ duyệt";
        private const string DangXuLy = "Đang xử lý";
        private const string DaDuyet = "Đã duyệt";
        private const string HoanThanh = "Hoàn thành";
        private const string DaHuy = "Đã hủy";
        private const string TT_TuChoi = "Từ chối";

        private static readonly string[] TatCaTrangThai = { ChoXuLy, ChoDuyet, DangXuLy, DaDuyet, HoanThanh, DaHuy, TT_TuChoi };

        // Mục 8.4: LUỒNG TRẠNG THÁI HỢP LỆ
        // Chờ duyệt / Chờ xử lý -> Đã duyệt / Đang xử lý -> Hoàn thành; nhánh Hủy/Từ chối.
        // Hoàn thành, Đã hủy, Từ chối là trạng thái cuối, không được chuyển tiếp.
        private static readonly Dictionary<string, string[]> LuongTrangThai = new()
        {
            [ChoXuLy] = new[] { DangXuLy, DaDuyet, DaHuy, TT_TuChoi },
            [ChoDuyet] = new[] { DangXuLy, DaDuyet, DaHuy, TT_TuChoi },
            [DangXuLy] = new[] { HoanThanh, DaHuy, TT_TuChoi },
            [DaDuyet] = new[] { HoanThanh, DaHuy, TT_TuChoi },
            [HoanThanh] = Array.Empty<string>(),
            [DaHuy] = Array.Empty<string>(),
            [TT_TuChoi] = Array.Empty<string>()
        };

        public TiepNhanDatSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // PHÂN QUYỀN TẠI CONTROLLER
        // Chỉ Admin và Nhân viên quản lý được tiếp nhận/xử lý đặt sân
        // ==========================================================
        private bool CoQuyenQuanLy()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin" || vaiTro == "NhanVien";
        }

        private IActionResult? KiemTraQuyen()
        {
            if (HttpContext.Session.GetInt32("MaTaiKhoan") == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để sử dụng chức năng tiếp nhận đặt sân!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (!CoQuyenQuanLy())
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng tiếp nhận đặt sân!";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        // ==========================================================
        // 1. DANH SÁCH ĐƠN CẦN XỬ LÝ (Mục 8.1)
        // Tìm theo khách hàng / sân; lọc theo loại sân, trạng thái đặt sân, trạng thái thanh toán, khoảng ngày; sắp xếp; phân trang
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? tenKhach,
            string? tenSan,
            int? maLoaiSan,
            string? trangThai,
            string? trangThaiThanhToan,
            DateTime? tuNgay,
            DateTime? denNgay,
            string? sapXep,
            int trang = 1)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var vm = new TiepNhanDatSanListViewModel();

            // Chuẩn hóa điều kiện lọc
            tenKhach = tenKhach?.Trim();
            tenSan = tenSan?.Trim();
            if (!string.IsNullOrEmpty(trangThai) && !TatCaTrangThai.Contains(trangThai)) trangThai = null;

            if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value.Date > denNgay.Value.Date)
            {
                (tuNgay, denNgay) = (denNgay, tuNgay);
                vm.CanhBaoLoc = "Khoảng ngày bị nhập ngược nên hệ thống đã tự đảo lại cho đúng.";
            }

            // LINQ: truy vấn gốc, kèm các bảng liên quan để hiển thị tên thay cho mã khóa ngoại
            IQueryable<DatSan> query = _context.DatSans
                .AsNoTracking()
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.ThanhToans);

            // Tìm theo người dùng/khách hàng (họ tên hoặc số điện thoại)
            if (!string.IsNullOrEmpty(tenKhach))
            {
                query = query.Where(d => d.KhachHang.HoTen.Contains(tenKhach)
                                      || d.KhachHang.SoDienThoai.Contains(tenKhach));
            }

            // Tìm theo đối tượng chính (sân thể thao)
            if (!string.IsNullOrEmpty(tenSan))
            {
                query = query.Where(d => d.SanTheThao.TenSan.Contains(tenSan));
            }

            // Lọc theo dữ liệu nền (loại sân)
            if (maLoaiSan.HasValue)
            {
                query = query.Where(d => d.SanTheThao.MaLoaiSan == maLoaiSan.Value);
            }

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

            // Lọc theo khoảng ngày sử dụng sân
            if (tuNgay.HasValue)
            {
                var tu = tuNgay.Value.Date;
                query = query.Where(d => d.GioBatDau >= tu);
            }
            if (denNgay.HasValue)
            {
                var den = denNgay.Value.Date.AddDays(1);
                query = query.Where(d => d.GioBatDau < den);
            }

            // Sắp xếp theo ngày hoặc tên
            sapXep = string.IsNullOrEmpty(sapXep) ? "ngaydat_moi" : sapXep;
            query = sapXep switch
            {
                "ngaydat_cu" => query.OrderBy(d => d.NgayDat).ThenBy(d => d.MaDatSan),
                "ngaysd_moi" => query.OrderByDescending(d => d.GioBatDau).ThenByDescending(d => d.MaDatSan),
                "ngaysd_cu" => query.OrderBy(d => d.GioBatDau).ThenBy(d => d.MaDatSan),
                "khach_az" => query.OrderBy(d => d.KhachHang.HoTen).ThenBy(d => d.MaDatSan),
                "khach_za" => query.OrderByDescending(d => d.KhachHang.HoTen).ThenBy(d => d.MaDatSan),
                "san_az" => query.OrderBy(d => d.SanTheThao.TenSan).ThenBy(d => d.MaDatSan),
                "san_za" => query.OrderByDescending(d => d.SanTheThao.TenSan).ThenBy(d => d.MaDatSan),
                _ => query.OrderByDescending(d => d.NgayDat).ThenByDescending(d => d.MaDatSan)
            };

            // Phân trang trên truy vấn bằng Skip() và Take(), không tải toàn bộ dữ liệu
            int tongSoBanGhi = await query.CountAsync();
            int tongSoTrang = Math.Max(1, (int)Math.Ceiling((double)tongSoBanGhi / KichThuocTrang));
            trang = Math.Max(1, Math.Min(trang, tongSoTrang));

            vm.DanhSach = await query
                .Skip((trang - 1) * KichThuocTrang)
                .Take(KichThuocTrang)
                .ToListAsync();

            // Thống kê nhanh số đơn theo từng trạng thái bằng GroupBy + Count
            var thongKe = await _context.DatSans
                .GroupBy(d => d.TrangThai)
                .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                .ToDictionaryAsync(x => x.TrangThai, x => x.SoLuong);

            vm.SoChoXuLy = thongKe.GetValueOrDefault(ChoXuLy) + thongKe.GetValueOrDefault(ChoDuyet);
            vm.SoDangXuLy = thongKe.GetValueOrDefault(DangXuLy) + thongKe.GetValueOrDefault(DaDuyet);
            vm.SoHoanThanh = thongKe.GetValueOrDefault(HoanThanh);
            vm.SoDaHuy = thongKe.GetValueOrDefault(DaHuy) + thongKe.GetValueOrDefault(TT_TuChoi);

            // Thống kê trạng thái thanh toán
            var thongKeTt = await _context.ThanhToans
                .GroupBy(t => t.TrangThai)
                .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                .ToDictionaryAsync(x => x.TrangThai, x => x.SoLuong);

            vm.SoChuaThanhToan = thongKeTt.GetValueOrDefault("Chưa thanh toán");
            vm.SoChoXacNhan = thongKeTt.GetValueOrDefault("Chờ xác nhận");
            vm.SoDaThanhToan = thongKeTt.GetValueOrDefault("Đã thanh toán");

            // Dữ liệu nền cho select lọc loại sân
            vm.DanhSachLoaiSan = await _context.LoaiSans
                .AsNoTracking()
                .OrderBy(l => l.TenLoai)
                .ToListAsync();

            // Giữ lại điều kiện để View hiển thị lại
            vm.TenKhach = tenKhach;
            vm.TenSan = tenSan;
            vm.MaLoaiSan = maLoaiSan;
            vm.TrangThai = trangThai;
            vm.TrangThaiThanhToan = trangThaiThanhToan;
            vm.TuNgay = tuNgay;
            vm.DenNgay = denNgay;
            vm.SapXep = sapXep;
            vm.TrangHienTai = trang;
            vm.TongSoTrang = tongSoTrang;
            vm.TongSoBanGhi = tongSoBanGhi;

            return View(vm);
        }

        // ==========================================================
        // 2. XEM CHI TIẾT ĐƠN VÀ KẾT QUẢ KIỂM TRA ĐIỀU KIỆN (Mục 8.1, 8.2, 8.3)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var don = await _context.DatSans
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.ChiTietDatSans)
                    .ThenInclude(ct => ct.DichVu)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (don == null) return NotFound();

            var now = DateTime.Now;
            var vm = new TiepNhanDatSanChiTietViewModel
            {
                DonDat = don,
                GioiHanLuotMoiNgay = GioiHanLuotMoiNgay,
                SoGio = (don.GioKetThuc - don.GioBatDau).TotalHours
            };

            vm.TienSan = Math.Round((decimal)vm.SoGio * don.DonGia, 0);
            vm.TienDichVu = don.ChiTietDatSans.Sum(ct => ct.ThanhTien);
            vm.TongTien = vm.TienSan + vm.TienDichVu;

            // Tổng tiền khách còn phải thanh toán (đã trừ tiền cọc)
            vm.ConPhaiThanhToan = Math.Max(vm.TongTien - don.TienCoc, 0);

            // Thêm / sửa dịch vụ chỉ khi đơn chưa kết thúc
            vm.CoTheSuaDichVu = don.TrangThai == ChoXuLy || don.TrangThai == ChoDuyet || don.TrangThai == DangXuLy || don.TrangThai == DaDuyet;
            if (vm.CoTheSuaDichVu)
            {
                var daCo = don.ChiTietDatSans.Select(ct => ct.MaDichVu).ToList();
                vm.DanhSachDichVu = await _context.DichVus
                    .AsNoTracking()
                    .Where(dv => dv.TrangThai || daCo.Contains(dv.MaDichVu))
                    .OrderBy(dv => dv.TenDichVu)
                    .ToListAsync();
            }

            // Chỉ phân tích điều kiện khi đơn còn đang chờ duyệt
            if (don.TrangThai == ChoXuLy || don.TrangThai == ChoDuyet)
            {
                vm.KetQua = await PhanTichDonAsync(don);
            }

            // Nút thao tác phải phù hợp với trạng thái hiện tại
            vm.CoTheXacNhan = (don.TrangThai == ChoXuLy || don.TrangThai == ChoDuyet) && !vm.KetQua.Loi.Any();
            vm.CoTheHoanThanh = (don.TrangThai == DangXuLy || don.TrangThai == DaDuyet) && don.GioBatDau <= now;

            // Đơn Chờ duyệt/Đang xử lý đều có thể hủy / từ chối
            vm.CoTheHuy = don.TrangThai == ChoXuLy || don.TrangThai == ChoDuyet || don.TrangThai == DangXuLy || don.TrangThai == DaDuyet;

            return View(vm);
        }

        // ==========================================================
        // 3. XÁC NHẬN TIỀN CỌC: Chuyển Trạng thái thanh toán sang 'Đã thanh toán'
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanCoc(int id, string? quayLai)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var don = await _context.DatSans
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (don == null) return NotFound();

            var thanhToan = don.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (thanhToan == null)
            {
                thanhToan = new ThanhToan
                {
                    MaDatSan = don.MaDatSan,
                    SoTien = don.TienCoc,
                    PhuongThuc = "Chuyển khoản QR",
                    GhiChu = $"DATSAN{don.MaDatSan}"
                };
                _context.ThanhToans.Add(thanhToan);
            }

            thanhToan.TrangThai = "Đã thanh toán";
            thanhToan.NgayThanhToan = DateTime.Now;
            if (string.IsNullOrEmpty(thanhToan.MaGiaoDich))
            {
                thanhToan.MaGiaoDich = $"XN{DateTime.Now:yyyyMMddHHmmss}";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận tiền đặt cọc ({thanhToan.SoTien:N0} VNĐ) cho đơn #{id}! Trạng thái thanh toán: 'Đã thanh toán'. Bạn có thể tiến hành duyệt đơn.";

            if (!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai))
                return Redirect(quayLai);

            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        // ==========================================================
        // 4. DUYỆT ĐƠN ĐẶT SÂN: Chuyển Trạng thái đặt sân sang 'Đã duyệt'
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> DuyetDon(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, DaDuyet, quayLai,
                "Đã duyệt đơn đặt sân #{0}. Trạng thái đặt sân chuyển sang 'Đã duyệt'.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> XacNhan(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, DaDuyet, quayLai,
                "Đã duyệt đơn đặt sân #{0}. Trạng thái đặt sân chuyển sang 'Đã duyệt'.");
        }

        // ==========================================================
        // 5. HOÀN THÀNH ĐƠN: Đang xử lý / Đã duyệt -> Hoàn thành
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> HoanThanhDon(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, HoanThanh, quayLai,
                "Đơn đặt sân #{0} đã được cập nhật sang trạng thái 'Hoàn thành'.");
        }

        // ==========================================================
        // 6. TỪ CHỐI / HỦY ĐƠN: Chờ duyệt hoặc Đã duyệt -> Từ chối
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> TuChoi(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, TT_TuChoi, quayLai,
                "Đã từ chối đơn đặt sân #{0}. Khung giờ này được trả lại cho khách khác đặt.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> HuyDon(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, TT_TuChoi, quayLai,
                "Đã từ chối/hủy đơn đặt sân #{0}. Khung giờ này được trả lại cho khách khác đặt.");
        }

        // ==========================================================
        // 7. TẠM LƯU DỊCH VỤ
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuDichVu(int id, List<DichVuChonInput>? dichVu)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var don = await _context.DatSans
                .Include(d => d.ChiTietDatSans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (don == null) return NotFound();

            if (don.TrangThai != ChoXuLy && don.TrangThai != ChoDuyet && don.TrangThai != DangXuLy && don.TrangThai != DaDuyet)
            {
                TempData["Error"] = $"Đơn #{id} đang ở trạng thái '{don.TrangThai}' nên không thể thay đổi dịch vụ.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            dichVu ??= new List<DichVuChonInput>();

            if (dichVu.Any(x => x.SoLuong < 0 || x.SoLuong > SoLuongDichVuToiDa))
            {
                TempData["Error"] = $"Số lượng mỗi dịch vụ phải từ 0 đến {SoLuongDichVuToiDa}.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            // Gộp các dòng trùng dịch vụ, bỏ dòng có số lượng 0
            var gop = dichVu
                .Where(x => x.SoLuong > 0)
                .GroupBy(x => x.MaDichVu)
                .Select(g => new { MaDichVu = g.Key, SoLuong = g.Sum(x => x.SoLuong) })
                .ToList();

            var dsMaDichVu = gop.Select(x => x.MaDichVu).ToList();
            var dsDichVu = await _context.DichVus
                .Where(dv => dsMaDichVu.Contains(dv.MaDichVu))
                .ToDictionaryAsync(dv => dv.MaDichVu);

            _context.ChiTietDatSans.RemoveRange(don.ChiTietDatSans);

            int daThem = 0;
            foreach (var item in gop)
            {
                if (!dsDichVu.TryGetValue(item.MaDichVu, out var dv) || !dv.TrangThai)
                    continue;

                int sl = Math.Min(item.SoLuong, SoLuongDichVuToiDa);
                don.ChiTietDatSans.Add(new ChiTietDatSan
                {
                    MaDatSan = don.MaDatSan,
                    MaDichVu = dv.MaDichVu,
                    SoLuong = sl,
                    DonGia = dv.DonGia,
                    ThanhTien = dv.DonGia * sl,
                    TrangThai = "Đã thêm"
                });
                daThem++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật dịch vụ cho đơn #{id} ({daThem} dịch vụ).";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        // ==========================================================
        // CÁC HÀM TIỆN ÍCH DÙNG CHUNG TRONG CONTROLLER
        // ==========================================================
        private async Task<IActionResult> DoiTrangThaiAsync(int id, string trangThaiMoi, string? quayLai, string mauThongBao)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var don = await _context.DatSans
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                .Include(d => d.ChiTietDatSans)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (don == null) return NotFound();

            string? loi = await KiemTraChuyenTrangThaiAsync(don, trangThaiMoi);
            if (loi != null)
            {
                TempData["Error"] = loi;
                return QuayLaiTrang(quayLai, id);
            }

            don.TrangThai = trangThaiMoi;
            await _context.SaveChangesAsync();

            TempData["Success"] = string.Format(mauThongBao, don.MaDatSan);
            return QuayLaiTrang(quayLai, id);
        }

        private async Task<string?> KiemTraChuyenTrangThaiAsync(DatSan don, string trangThaiMoi)
        {
            if (!LuongTrangThai.TryGetValue(don.TrangThai, out var duocPhep) || !duocPhep.Contains(trangThaiMoi))
            {
                return $"Không thể chuyển đơn #{don.MaDatSan} từ '{don.TrangThai}' sang '{trangThaiMoi}'. " +
                       "Đơn phải đi theo luồng: Chờ duyệt → Đã duyệt → Hoàn thành (hoặc Từ chối).";
            }

            var now = DateTime.Now;

            switch (trangThaiMoi)
            {
                case DangXuLy:
                case DaDuyet:
                    var ketQua = await PhanTichDonAsync(don);
                    if (ketQua.Loi.Any())
                    {
                        return $"Không thể duyệt đơn #{don.MaDatSan}: " + string.Join(" ", ketQua.Loi);
                    }
                    break;

                case HoanThanh:
                    if (don.GioBatDau > now)
                    {
                        return $"Không thể hoàn thành đơn #{don.MaDatSan} vì chưa đến giờ sử dụng sân " +
                               $"({don.GioBatDau:dd/MM/yyyy HH:mm}).";
                    }
                    break;

                case DaHuy:
                case TT_TuChoi:
                    break;
            }

            return null;
        }

        private async Task<KetQuaKiemTraDonDat> PhanTichDonAsync(DatSan don)
        {
            var kq = new KetQuaKiemTraDonDat();
            var now = DateTime.Now;

            // 1. Khách hàng
            if (don.KhachHang == null || !don.KhachHang.TrangThai)
            {
                kq.Loi.Add("Khách hàng không tồn tại hoặc đang bị khóa.");
            }

            // 2. Sân thể thao
            var san = don.SanTheThao;
            if (san == null || !san.TrangThai)
            {
                kq.Loi.Add("Sân thể thao không tồn tại hoặc đã ngừng hoạt động.");
            }
            else
            {
                if (san.NgayBaoTri.HasValue && san.NgayBaoTri.Value.Date == don.GioBatDau.Date)
                {
                    kq.Loi.Add($"Sân có lịch bảo trì vào ngày {don.GioBatDau:dd/MM/yyyy}.");
                }

                // 3. Giờ mở cửa / đóng cửa
                if (don.GioBatDau.TimeOfDay < san.GioMoCua || don.GioKetThuc.TimeOfDay > san.GioDongCua)
                {
                    kq.Loi.Add($"Khung giờ đặt nằm ngoài giờ hoạt động của sân ({san.GioMoCua:hh\\:mm} - {san.GioDongCua:hh\\:mm}).");
                }
            }

            // 4. Thời gian hợp lệ
            if (don.GioKetThuc <= don.GioBatDau)
            {
                kq.Loi.Add("Giờ kết thúc phải sau giờ bắt đầu.");
            }
            else if (don.GioKetThuc.Date != don.GioBatDau.Date)
            {
                kq.Loi.Add("Lịch đặt phải nằm trong cùng một ngày.");
            }

            if (don.GioKetThuc < now)
            {
                kq.Loi.Add("Đơn đã quá thời gian sử dụng sân, chỉ có thể hủy/từ chối đơn.");
            }

            // 5. Trùng lịch: loại trừ chính đơn này và các đơn đã hủy/từ chối
            var donTrung = _context.DatSans
                .Where(d => d.MaDatSan != don.MaDatSan
                         && d.MaSan == don.MaSan
                         && d.TrangThai != DaHuy
                         && d.TrangThai != TT_TuChoi
                         && d.GioBatDau < don.GioKetThuc
                         && d.GioKetThuc > don.GioBatDau);

            if (await donTrung.AnyAsync())
            {
                kq.DonTrungLich = await donTrung
                    .Include(d => d.KhachHang)
                    .OrderBy(d => d.GioBatDau)
                    .Take(5)
                    .ToListAsync();

                kq.Loi.Add("Sân đã có lịch khác trùng khoảng thời gian này.");
            }

            // 6. Tiền cọc không vượt tổng tiền dự kiến
            double soGio = (don.GioKetThuc - don.GioBatDau).TotalHours;
            decimal tienDichVu = await _context.ChiTietDatSans
                .Where(ct => ct.MaDatSan == don.MaDatSan)
                .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0;
            decimal tongDuKien = Math.Round((decimal)Math.Max(soGio, 0) * don.DonGia, 0) + tienDichVu;

            if (don.TienCoc < 0)
            {
                kq.Loi.Add("Tiền cọc không được âm.");
            }
            else if (don.TienCoc > tongDuKien)
            {
                kq.Loi.Add($"Tiền cọc ({don.TienCoc:N0} VNĐ) vượt quá tổng tiền dự kiến ({tongDuKien:N0} VNĐ).");
            }

            // Kiểm tra trạng thái đặt cọc
            var tt = don.ThanhToans.OrderByDescending(t => t.MaThanhToan).FirstOrDefault();
            if (tt == null || tt.TrangThai != "Đã thanh toán")
            {
                kq.CanhBao.Add("Khách hàng chưa thanh toán hoặc đang 'Chờ xác nhận' tiền cọc. Vui lòng bấm 'Xác nhận đã đặt cọc' sau khi kiểm tra tiền về.");
            }

            // 7. Kiểm soát khả năng đáp ứng: tối đa 8 đơn/ngày
            var dauNgay = don.GioBatDau.Date;
            var cuoiNgay = dauNgay.AddDays(1);
            kq.SoLuotTrongNgay = await _context.DatSans.CountAsync(d =>
                d.MaDatSan != don.MaDatSan
                && d.MaSan == don.MaSan
                && d.TrangThai != DaHuy
                && d.TrangThai != TT_TuChoi
                && d.GioBatDau >= dauNgay
                && d.GioBatDau < cuoiNgay);

            if (kq.SoLuotTrongNgay + 1 > GioiHanLuotMoiNgay)
            {
                kq.Loi.Add($"Sân đã đạt giới hạn {GioiHanLuotMoiNgay} lượt đặt trong ngày {dauNgay:dd/MM/yyyy}.");
            }
            else if (kq.SoLuotTrongNgay + 1 == GioiHanLuotMoiNgay)
            {
                kq.CanhBao.Add($"Đây là lượt cuối cùng còn lại của sân trong ngày ({GioiHanLuotMoiNgay}/{GioiHanLuotMoiNgay} lượt).");
            }

            return kq;
        }

        private IActionResult QuayLaiTrang(string? quayLai, int maDatSan)
        {
            if (!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai))
                return Redirect(quayLai);

            return RedirectToAction(nameof(ChiTiet), new { id = maDatSan });
        }
    }
}
