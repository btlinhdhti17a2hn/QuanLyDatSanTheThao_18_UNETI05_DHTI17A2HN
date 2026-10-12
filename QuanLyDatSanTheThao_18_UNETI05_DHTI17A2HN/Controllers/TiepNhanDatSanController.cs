// Họ và tên: Vũ Ánh Ngọc
// Mã sinh viên: 23103100106
// Nội dung thực hiện: Module 4 - Tiếp nhận đặt sân: danh sách cần xử lý (tìm kiếm, lọc, sắp xếp, phân trang),
//                     xem chi tiết, xác nhận / hoàn thành / hủy đơn, kiểm tra trùng lịch, giờ mở cửa,
//                     tiền cọc, giới hạn lượt đặt bằng LINQ và kiểm soát luồng trạng thái.

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

        // Mục 8.5: Giới hạn số lượt đặt (chưa hủy) của 1 sân trong 1 ngày
        private const int GioiHanLuotMoiNgay = 8;

        // Các trạng thái của đơn đặt sân (khớp với dữ liệu mẫu và Module 3)
        private const string ChoXuLy = "Chờ xử lý";
        private const string DangXuLy = "Đang xử lý";
        private const string HoanThanh = "Hoàn thành";
        private const string DaHuy = "Đã hủy";

        private static readonly string[] TatCaTrangThai = { ChoXuLy, DangXuLy, HoanThanh, DaHuy };

        // Mục 8.4: LUỒNG TRẠNG THÁI HỢP LỆ
        // Chờ xử lý -> Đang xử lý -> Hoàn thành; nhánh Hủy từ Chờ xử lý hoặc Đang xử lý.
        // Hoàn thành và Đã hủy là trạng thái cuối, không được chuyển tiếp.
        private static readonly Dictionary<string, string[]> LuongTrangThai = new()
        {
            [ChoXuLy] = new[] { DangXuLy, DaHuy },
            [DangXuLy] = new[] { HoanThanh, DaHuy },
            [HoanThanh] = Array.Empty<string>(),
            [DaHuy] = Array.Empty<string>()
        };

        public TiepNhanDatSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // PHÂN QUYỀN TẠI CONTROLLER (không chỉ ẩn menu trên View)
        // Chỉ Admin và Nhân viên quản lý được tiếp nhận/xử lý đặt sân
        // ==========================================================
        private bool CoQuyenQuanLy()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin" || vaiTro == "NhanVien";
        }

        // Trả về null nếu hợp lệ, ngược lại trả về kết quả chuyển hướng
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
        // Tìm theo khách hàng / sân; lọc theo loại sân, trạng thái, khoảng ngày; sắp xếp; phân trang
        // Tất cả điều kiện kết hợp được với nhau và được giữ nguyên khi chuyển trang
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? tenKhach,
            string? tenSan,
            int? maLoaiSan,
            string? trangThai,
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

            // Nếu người dùng chọn từ ngày > đến ngày thì hoán đổi lại và báo cho người dùng biết
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
                    .ThenInclude(s => s.LoaiSan);

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

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
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

            vm.SoChoXuLy = thongKe.GetValueOrDefault(ChoXuLy);
            vm.SoDangXuLy = thongKe.GetValueOrDefault(DangXuLy);
            vm.SoHoanThanh = thongKe.GetValueOrDefault(HoanThanh);
            vm.SoDaHuy = thongKe.GetValueOrDefault(DaHuy);

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

            // Chỉ phân tích điều kiện khi đơn còn đang chờ xác nhận
            if (don.TrangThai == ChoXuLy)
            {
                vm.KetQua = await PhanTichDonAsync(don);
            }

            // Nút thao tác phải phù hợp với trạng thái hiện tại
            vm.CoTheXacNhan = don.TrangThai == ChoXuLy && !vm.KetQua.Loi.Any();
            vm.CoTheHoanThanh = don.TrangThai == DangXuLy && don.GioBatDau <= now;
            vm.CoTheHuy = don.TrangThai == ChoXuLy
                          || (don.TrangThai == DangXuLy && don.GioBatDau > now);

            return View(vm);
        }

        // ==========================================================
        // 3. XÁC NHẬN ĐƠN: Chờ xử lý -> Đang xử lý
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> XacNhan(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, DangXuLy, quayLai,
                "Đã xác nhận đơn đặt sân #{0}. Đơn chuyển sang trạng thái 'Đang xử lý'.");
        }

        // ==========================================================
        // 4. HOÀN THÀNH ĐƠN: Đang xử lý -> Hoàn thành
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> HoanThanhDon(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, HoanThanh, quayLai,
                "Đơn đặt sân #{0} đã được cập nhật sang trạng thái 'Hoàn thành'.");
        }

        // ==========================================================
        // 5. HỦY / TỪ CHỐI ĐƠN: Chờ xử lý hoặc Đang xử lý -> Đã hủy
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> HuyDon(int id, string? quayLai)
        {
            return DoiTrangThaiAsync(id, DaHuy, quayLai,
                "Đã hủy đơn đặt sân #{0}. Khung giờ này được trả lại cho khách khác đặt.");
        }

        // ==========================================================
        // HÀM DÙNG CHUNG: CẬP NHẬT TRẠNG THÁI THEO ĐÚNG LUỒNG
        // ==========================================================
        private async Task<IActionResult> DoiTrangThaiAsync(int id, string trangThaiMoi, string? quayLai, string mauThongBao)
        {
            var chan = KiemTraQuyen();
            if (chan != null) return chan;

            var don = await _context.DatSans
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                .Include(d => d.ChiTietDatSans)
                .FirstOrDefaultAsync(d => d.MaDatSan == id);

            if (don == null) return NotFound();

            // Kiểm tra luồng trạng thái + các điều kiện nghiệp vụ trước khi cho phép chuyển
            string? loi = await KiemTraChuyenTrangThaiAsync(don, trangThaiMoi);
            if (loi != null)
            {
                TempData["Error"] = loi;
                return QuayLaiTrang(quayLai, id);
            }

            // Chỉ cập nhật đúng trường trạng thái, không xóa bất kỳ dữ liệu lịch sử nào
            don.TrangThai = trangThaiMoi;
            await _context.SaveChangesAsync();

            TempData["Success"] = string.Format(mauThongBao, don.MaDatSan);
            return QuayLaiTrang(quayLai, id);
        }

        // Kiểm tra việc chuyển trạng thái có hợp lệ hay không. Trả về null nếu hợp lệ.
        private async Task<string?> KiemTraChuyenTrangThaiAsync(DatSan don, string trangThaiMoi)
        {
            // 1. Đúng luồng trạng thái (Mục 8.4)
            if (!LuongTrangThai.TryGetValue(don.TrangThai, out var duocPhep) || !duocPhep.Contains(trangThaiMoi))
            {
                return $"Không thể chuyển đơn #{don.MaDatSan} từ '{don.TrangThai}' sang '{trangThaiMoi}'. " +
                       "Đơn phải đi theo luồng: Chờ xử lý → Đang xử lý → Hoàn thành (hoặc Hủy).";
            }

            var now = DateTime.Now;

            switch (trangThaiMoi)
            {
                case DangXuLy:
                    // 2. Phải đạt toàn bộ điều kiện trước khi xác nhận (Mục 8.3, 8.5)
                    var ketQua = await PhanTichDonAsync(don);
                    if (ketQua.Loi.Any())
                    {
                        return $"Không thể xác nhận đơn #{don.MaDatSan}: " + string.Join(" ", ketQua.Loi);
                    }
                    break;

                case HoanThanh:
                    if (don.GioBatDau > now)
                    {
                        return $"Đơn #{don.MaDatSan} chưa đến giờ sử dụng sân ({don.GioBatDau:dd/MM/yyyy HH:mm}) nên chưa thể hoàn thành.";
                    }
                    break;

                case DaHuy:
                    // Đơn đã xác nhận mà đã đến/quá giờ sử dụng thì không được hủy tùy ý
                    if (don.TrangThai == DangXuLy && don.GioBatDau <= now)
                    {
                        return $"Đơn #{don.MaDatSan} đã đến hoặc quá giờ sử dụng sân nên không thể hủy. Hãy cập nhật sang 'Hoàn thành'.";
                    }
                    break;
            }

            return null;
        }

        // ==========================================================
        // PHÂN TÍCH ĐIỀU KIỆN XÁC NHẬN BẰNG LINQ (Mục 8.3, 8.5)
        //  - Khách hàng tồn tại và đang hoạt động
        //  - Sân tồn tại, đang hoạt động, không bảo trì vào ngày sử dụng
        //  - Chỉ được đặt trong giờ mở cửa
        //  - Giờ kết thúc phải sau giờ bắt đầu
        //  - Không trùng lịch với đơn khác của cùng sân
        //  - Tiền cọc không vượt tổng tiền dự kiến
        //  - Không vượt giới hạn số lượt đặt của sân trong ngày
        // ==========================================================
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
                kq.Loi.Add("Đơn đã quá thời gian sử dụng sân, chỉ có thể hủy đơn.");
            }

            // 5. Trùng lịch: LINQ Any/ToList, loại trừ chính đơn này và các đơn đã hủy
            var donTrung = _context.DatSans
                .Where(d => d.MaDatSan != don.MaDatSan
                         && d.MaSan == don.MaSan
                         && d.TrangThai != DaHuy
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

            // 6. Tiền cọc không vượt tổng tiền dự kiến (tiền sân + dịch vụ)
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

            // 7. Kiểm soát khả năng đáp ứng: đếm số lượt đặt khác của sân trong ngày
            var dauNgay = don.GioBatDau.Date;
            var cuoiNgay = dauNgay.AddDays(1);
            kq.SoLuotTrongNgay = await _context.DatSans.CountAsync(d =>
                d.MaDatSan != don.MaDatSan
                && d.MaSan == don.MaSan
                && d.TrangThai != DaHuy
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

        // Quay về trang người dùng vừa thao tác (chỉ chấp nhận đường dẫn nội bộ để tránh open redirect)
        private IActionResult QuayLaiTrang(string? quayLai, int id)
        {
            if (!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai))
            {
                return LocalRedirect(quayLai);
            }
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
    }
}