// Họ và tên: Bùi Thùy Linh
// Mã sinh viên: 23103100108
// Nội dung thực hiện: Module 2 - Controller SanTheThao: CRUD sân thể thao, cập nhật trạng thái,
//                     tìm kiếm, lọc, sắp xếp, phân trang bằng EF Core/LINQ và phân quyền tại Controller.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class SanTheThaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Số sân hiển thị trên mỗi trang
        private const int KichThuocTrang = 6;

        // Các trạng thái đặt sân KHÔNG còn chiếm lịch của sân
        private static readonly string[] TrangThaiKhongChiem = { "Đã hủy", "Từ chối" };

        public SanTheThaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // PHÂN QUYỀN (kiểm tra tại Controller, không chỉ ẩn menu)
        // =====================================================================
        private bool LaQuanLy()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin" || vaiTro == "NhanVien";
        }

        // Trả về null nếu hợp lệ; ngược lại trả về trang cần chuyển hướng
        private IActionResult? ChanNeuKhongPhaiQuanLy()
        {
            if (HttpContext.Session.GetInt32("MaTaiKhoan") == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để sử dụng chức năng này!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            if (!LaQuanLy())
            {
                TempData["Error"] = "Chỉ Admin hoặc Nhân viên mới được quản lý sân thể thao!";
                return RedirectToAction(nameof(Index));
            }
            return null;
        }

        // Quay lại đúng trang (giữ nguyên điều kiện lọc) sau khi xóa / đổi trạng thái
        private IActionResult QuayLai(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // 1. DANH SÁCH: TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG (ai cũng xem được)
        // =====================================================================
        public async Task<IActionResult> Index(
            string? tuKhoa, int? maLoaiSan, string? trangThai,
            decimal? giaTu, decimal? giaDen,
            DateTime? ngaySuDung, TimeSpan? gioBatDau, TimeSpan? gioKetThuc,
            bool conTrong = false, string sapXep = "ten_asc", int trang = 1)
        {
            var vm = new SanTheThaoIndexViewModel();

            // --- Chuẩn hóa điều kiện đầu vào ---
            if (giaTu.HasValue && giaDen.HasValue && giaTu > giaDen)
            {
                (giaTu, giaDen) = (giaDen, giaTu);
                vm.CanhBao = "Khoảng giá nhập ngược nên hệ thống đã tự đảo lại giúp bạn.";
            }

            bool coKhungGio = gioBatDau.HasValue && gioKetThuc.HasValue;
            if (coKhungGio && gioKetThuc <= gioBatDau)
            {
                coKhungGio = false;
                vm.CanhBao = "Giờ kết thúc phải sau giờ bắt đầu nên khung giờ đã được bỏ qua.";
            }

            // --- Câu truy vấn gốc (chưa chạy xuống DB) ---
            var query = _context.SanTheThaos
                .AsNoTracking()
                .Include(s => s.LoaiSan)
                .AsQueryable();

            // --- TÌM KIẾM: tên sân / tên loại sân / địa chỉ ---
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(s => s.TenSan.Contains(kw)
                                      || s.LoaiSan.TenLoai.Contains(kw)
                                      || s.DiaChi.Contains(kw));
            }

            // --- LỌC: loại sân ---
            if (maLoaiSan.HasValue)
                query = query.Where(s => s.MaLoaiSan == maLoaiSan.Value);

            // --- LỌC: trạng thái ---
            if (trangThai == "hoatdong") query = query.Where(s => s.TrangThai);
            else if (trangThai == "ngung") query = query.Where(s => !s.TrangThai);

            // --- LỌC: khoảng giá ---
            if (giaTu.HasValue) query = query.Where(s => s.DonGia >= giaTu.Value);
            if (giaDen.HasValue) query = query.Where(s => s.DonGia <= giaDen.Value);

            // --- LỌC: ngày sử dụng + sân còn trống ---
            if (ngaySuDung.HasValue || conTrong)
            {
                var ngay = (ngaySuDung ?? DateTime.Today).Date;

                // Sân phải đang hoạt động và không bảo trì đúng ngày đó
                query = query.Where(s => s.TrangThai
                                      && !(s.NgayBaoTri.HasValue && s.NgayBaoTri.Value.Date == ngay));

                // Nếu có khung giờ: sân phải mở cửa đủ khung giờ đó
                if (coKhungGio)
                {
                    var bd = gioBatDau!.Value;
                    var kt = gioKetThuc!.Value;
                    query = query.Where(s => s.GioMoCua <= bd && s.GioDongCua >= kt);
                }

                // Sân còn trống: không có lịch đặt nào (chưa hủy/từ chối) chồng lên khoảng thời gian
                if (conTrong)
                {
                    var khungBatDau = ngay + (coKhungGio ? gioBatDau!.Value : TimeSpan.Zero);
                    var khungKetThuc = coKhungGio ? ngay + gioKetThuc!.Value : ngay.AddDays(1);

                    query = query.Where(s => !s.DatSans.Any(d =>
                        !TrangThaiKhongChiem.Contains(d.TrangThai)
                        && d.GioBatDau < khungKetThuc
                        && d.GioKetThuc > khungBatDau));
                }
            }

            // --- SẮP XẾP (thêm MaSan để thứ tự ổn định khi phân trang) ---
            query = sapXep switch
            {
                "ten_desc" => query.OrderByDescending(s => s.TenSan).ThenBy(s => s.MaSan),
                "gia_asc" => query.OrderBy(s => s.DonGia).ThenBy(s => s.MaSan),
                "gia_desc" => query.OrderByDescending(s => s.DonGia).ThenBy(s => s.MaSan),
                "gio_asc" => query.OrderBy(s => s.GioMoCua).ThenBy(s => s.MaSan),
                "gio_desc" => query.OrderByDescending(s => s.GioMoCua).ThenBy(s => s.MaSan),
                _ => query.OrderBy(s => s.TenSan).ThenBy(s => s.MaSan)
            };

            // --- PHÂN TRANG: đếm tổng rồi Skip/Take ngay trên truy vấn ---
            int tongKetQua = await query.CountAsync();
            int tongTrang = (int)Math.Ceiling(tongKetQua / (double)KichThuocTrang);
            if (trang > tongTrang) trang = tongTrang;
            if (trang < 1) trang = 1;

            vm.DanhSach = await query
                .Skip((trang - 1) * KichThuocTrang)
                .Take(KichThuocTrang)
                .ToListAsync();

            // --- Đổ dữ liệu ra ViewModel để View giữ lại điều kiện ---
            vm.TuKhoa = tuKhoa?.Trim();
            vm.MaLoaiSan = maLoaiSan;
            vm.TrangThai = trangThai;
            vm.GiaTu = giaTu;
            vm.GiaDen = giaDen;
            vm.NgaySuDung = ngaySuDung;
            vm.GioBatDau = gioBatDau;
            vm.GioKetThuc = gioKetThuc;
            vm.ConTrong = conTrong;
            vm.SapXep = sapXep;
            vm.TrangHienTai = trang;
            vm.TongTrang = tongTrang;
            vm.TongKetQua = tongKetQua;
            vm.KichThuocTrang = KichThuocTrang;

            // --- Thống kê nhanh cho khối thẻ phía trên ---
            var homNay = DateTime.Today;
            vm.TongSan = await _context.SanTheThaos.CountAsync();
            vm.SanKhaDung = await _context.SanTheThaos.CountAsync(s =>
                s.TrangThai && !(s.NgayBaoTri.HasValue && s.NgayBaoTri.Value.Date == homNay));
            vm.SanKhongKhaDung = vm.TongSan - vm.SanKhaDung;

            vm.LoaiSans = await _context.LoaiSans.AsNoTracking().OrderBy(l => l.TenLoai).ToListAsync();

            return View(vm);
        }

        // =====================================================================
        // 2. CHI TIẾT (ai cũng xem được)
        // =====================================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var san = await _context.SanTheThaos
                .AsNoTracking()
                .Include(s => s.LoaiSan)
                .FirstOrDefaultAsync(s => s.MaSan == id);
            if (san == null) return NotFound();

            var bayGio = DateTime.Now;

            // Lịch đã đặt sắp tới của sân (để khách biết khung giờ nào đã bị chiếm)
            var lich = await _context.DatSans
                .AsNoTracking()
                .Where(d => d.MaSan == id
                         && d.GioKetThuc >= bayGio
                         && !TrangThaiKhongChiem.Contains(d.TrangThai))
                .OrderBy(d => d.GioBatDau)
                .Take(10)
                .Select(d => new LichDatItem
                {
                    GioBatDau = d.GioBatDau,
                    GioKetThuc = d.GioKetThuc,
                    TrangThai = d.TrangThai,
                    TenKhach = d.KhachHang.HoTen
                })
                .ToListAsync();

            var vm = new SanTheThaoDetailsViewModel
            {
                San = san,
                LichSapToi = lich,
                SoLuotDatHoanThanh = await _context.DatSans
                    .CountAsync(d => d.MaSan == id && d.TrangThai == "Hoàn thành")
            };
            return View(vm);
        }

        // =====================================================================
        // 3. THÊM MỚI (Admin / Nhân viên)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;

            await NapLoaiSan(null);
            return View(new SanTheThao
            {
                TrangThai = true,
                DonGia = 100000,
                GioMoCua = new TimeSpan(6, 0, 0),
                GioDongCua = new TimeSpan(22, 0, 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("TenSan,MaLoaiSan,DiaChi,TienIch,DonGia,TrangThai,GioMoCua,GioDongCua,GhiChu,NgayBaoTri")] SanTheThao san)
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;

            await KiemTraNghiepVu(san, null);

            if (ModelState.IsValid)
            {
                san.TenSan = san.TenSan.Trim();
                _context.SanTheThaos.Add(san);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm sân thể thao thành công!";
                return RedirectToAction(nameof(Index));
            }

            await NapLoaiSan(san.MaLoaiSan);
            return View(san);
        }

        // =====================================================================
        // 4. SỬA (Admin / Nhân viên)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;
            if (id == null) return NotFound();

            var san = await _context.SanTheThaos.FindAsync(id);
            if (san == null) return NotFound();

            await NapLoaiSan(san.MaLoaiSan);
            return View(san);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("MaSan,TenSan,MaLoaiSan,DiaChi,TienIch,DonGia,TrangThai,GioMoCua,GioDongCua,GhiChu,NgayBaoTri")] SanTheThao san)
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;
            if (id != san.MaSan) return NotFound();

            var goc = await _context.SanTheThaos.FindAsync(id);
            if (goc == null) return NotFound();

            await KiemTraNghiepVu(san, goc);

            if (ModelState.IsValid)
            {
                bool vuaTatSan = goc.TrangThai && !san.TrangThai;

                // Chép từng trường từ form sang bản ghi gốc (tránh Over-posting)
                goc.TenSan = san.TenSan.Trim();
                goc.MaLoaiSan = san.MaLoaiSan;
                goc.DiaChi = san.DiaChi;
                goc.TienIch = san.TienIch;
                goc.DonGia = san.DonGia;
                goc.TrangThai = san.TrangThai;
                goc.GioMoCua = san.GioMoCua;
                goc.GioDongCua = san.GioDongCua;
                goc.GhiChu = san.GhiChu;
                goc.NgayBaoTri = san.NgayBaoTri;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sân thể thao thành công!";

                if (vuaTatSan)
                {
                    int soLich = await DemLichSapToi(id);
                    if (soLich > 0)
                        TempData["Warning"] = $"Sân đã chuyển sang ngừng hoạt động nhưng vẫn còn {soLich} lịch đặt sắp tới cần xử lý!";
                }
                return RedirectToAction(nameof(Index));
            }

            await NapLoaiSan(san.MaLoaiSan);
            return View(san);
        }

        // =====================================================================
        // 5. XÓA (không xóa sân đã phát sinh lịch đặt)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;

            var san = await _context.SanTheThaos.FindAsync(id);
            if (san == null) return NotFound();

            // Kiểm tra tham chiếu bằng LINQ trước khi xóa
            bool daCoLichDat = await _context.DatSans.AnyAsync(d => d.MaSan == id);
            if (daCoLichDat)
            {
                TempData["Error"] = "Không thể xóa vì sân này đã phát sinh lịch đặt. Hãy chuyển sang \"Ngừng hoạt động\" để giữ lịch sử!";
                return QuayLai(returnUrl);
            }

            _context.SanTheThaos.Remove(san);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa sân thể thao thành công!";
            return QuayLai(returnUrl);
        }

        // =====================================================================
        // 6. ĐỔI TRẠNG THÁI NHANH (Hoạt động <-> Ngừng hoạt động)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id, string? returnUrl)
        {
            var chan = ChanNeuKhongPhaiQuanLy();
            if (chan != null) return chan;

            var san = await _context.SanTheThaos.FindAsync(id);
            if (san == null) return NotFound();

            san.TrangThai = !san.TrangThai;
            await _context.SaveChangesAsync();

            if (san.TrangThai)
            {
                TempData["Success"] = $"Đã mở lại hoạt động cho \"{san.TenSan}\".";
            }
            else
            {
                TempData["Success"] = $"Đã ngừng hoạt động \"{san.TenSan}\".";
                int soLich = await DemLichSapToi(id);
                if (soLich > 0)
                    TempData["Warning"] = $"Sân này vẫn còn {soLich} lịch đặt sắp tới (chưa hủy) cần được xử lý!";
            }
            return QuayLai(returnUrl);
        }

        // =====================================================================
        // CÁC HÀM DÙNG CHUNG
        // =====================================================================

        // Nạp danh sách Loại sân từ Database cho thẻ <select> (khóa ngoại)
        private async Task NapLoaiSan(int? dangChon)
        {
            var ds = await _context.LoaiSans
                .AsNoTracking()
                .Where(l => l.TrangThai || l.MaLoaiSan == dangChon)
                .OrderBy(l => l.TenLoai)
                .ToListAsync();
            ViewBag.LoaiSanList = new SelectList(ds, "MaLoaiSan", "TenLoai", dangChon);
        }

        // Đếm số lịch đặt sắp tới (chưa hủy/từ chối) của một sân
        private Task<int> DemLichSapToi(int maSan)
        {
            var bayGio = DateTime.Now;
            return _context.DatSans.CountAsync(d => d.MaSan == maSan
                                                 && d.GioKetThuc >= bayGio
                                                 && !TrangThaiKhongChiem.Contains(d.TrangThai));
        }

        // Các quy tắc nghiệp vụ mà Data Annotation không làm được
        // goc = null khi thêm mới; goc != null khi sửa
        private async Task KiemTraNghiepVu(SanTheThao san, SanTheThao? goc)
        {
            // Navigation property không có trong form nên phải bỏ qua lỗi [Required] của nó
            ModelState.Remove(nameof(SanTheThao.LoaiSan));

            // Giờ đóng cửa phải sau giờ mở cửa
            if (san.GioDongCua <= san.GioMoCua)
                ModelState.AddModelError(nameof(SanTheThao.GioDongCua), "Giờ đóng cửa phải sau giờ mở cửa.");

            // Khóa ngoại: loại sân phải tồn tại (và đang hoạt động nếu là loại mới chọn)
            if (san.MaLoaiSan <= 0)
            {
                ModelState.Remove(nameof(SanTheThao.MaLoaiSan));
                ModelState.AddModelError(nameof(SanTheThao.MaLoaiSan), "Vui lòng chọn loại sân.");
            }
            else
            {
                var loai = await _context.LoaiSans.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.MaLoaiSan == san.MaLoaiSan);
                if (loai == null)
                    ModelState.AddModelError(nameof(SanTheThao.MaLoaiSan), "Loại sân không tồn tại.");
                else if (!loai.TrangThai && (goc == null || goc.MaLoaiSan != san.MaLoaiSan))
                    ModelState.AddModelError(nameof(SanTheThao.MaLoaiSan), "Loại sân này đang bị khóa, vui lòng chọn loại khác.");
            }

            // Tên sân không được trùng
            if (!string.IsNullOrWhiteSpace(san.TenSan))
            {
                var ten = san.TenSan.Trim();
                int maHienTai = goc?.MaSan ?? 0;
                bool trung = await _context.SanTheThaos.AnyAsync(s => s.TenSan == ten && s.MaSan != maHienTai);
                if (trung)
                    ModelState.AddModelError(nameof(SanTheThao.TenSan), "Tên sân này đã tồn tại trong hệ thống!");
            }

            // Ngày bảo trì không được ở quá khứ (khi sửa chỉ kiểm tra nếu người dùng đổi ngày)
            if (san.NgayBaoTri.HasValue
                && san.NgayBaoTri.Value.Date < DateTime.Today
                && (goc == null || goc.NgayBaoTri != san.NgayBaoTri))
            {
                ModelState.AddModelError(nameof(SanTheThao.NgayBaoTri), "Ngày bảo trì không được là ngày trong quá khứ.");
            }
        }
    }
}