
// Họ và tên: Bùi Thùy Linh
// Mã sinh viên: 23103100108
// Nội dung thực hiện: Module 2 - Repository SanTheThao: CRUD sân thể thao, tìm kiếm,
//                     lọc, sắp xếp, phân trang, thống kê và kiểm tra nghiệp vụ.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class SanTheThaoRepository : ISanTheThaoRepository
    {
        private readonly ApplicationDbContext _context;

        // Số sân hiển thị trên mỗi trang
        private const int KichThuocTrang = 6;

        // Các trạng thái đặt sân KHÔNG còn chiếm lịch của sân
        private static readonly string[] TrangThaiKhongChiem = { "Đã hủy", "Từ chối" };

        public SanTheThaoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // 1. DANH SÁCH: TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        // =====================================================================
        public async Task<SanTheThaoIndexViewModel> GetAllAsync(
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

            return vm;
        }

        // =====================================================================
        // 2. CHI TIẾT
        // =====================================================================
        public async Task<SanTheThaoDetailsViewModel?> GetDetailsAsync(int id)
        {
            var san = await _context.SanTheThaos
                .AsNoTracking()
                .Include(s => s.LoaiSan)
                .FirstOrDefaultAsync(s => s.MaSan == id);

            if (san == null) return null;

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

            return vm;
        }

        // =====================================================================
        // 3. LẤY DỮ LIỆU SÂN
        // =====================================================================
        public async Task<SanTheThao?> GetByIdAsync(int id)
        {
            return await _context.SanTheThaos.FindAsync(id);
        }

        // =====================================================================
        // 4. THÊM MỚI
        // =====================================================================
        public async Task AddAsync(SanTheThao san)
        {
            _context.SanTheThaos.Add(san);
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // 5. SỬA
        // =====================================================================
        public async Task UpdateAsync(SanTheThao goc, SanTheThao san)
        {
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
        }

        // =====================================================================
        // 6. XÓA
        // =====================================================================
        public async Task DeleteAsync(SanTheThao san)
        {
            _context.SanTheThaos.Remove(san);
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // 7. LẤY DANH SÁCH LOẠI SÂN
        // =====================================================================
        public async Task<List<LoaiSan>> GetLoaiSanAsync(int? dangChon)
        {
            return await _context.LoaiSans
                .AsNoTracking()
                .Where(l => l.TrangThai || l.MaLoaiSan == dangChon)
                .OrderBy(l => l.TenLoai)
                .ToListAsync();
        }

        public async Task<LoaiSan?> GetLoaiSanByIdAsync(int id)
        {
            return await _context.LoaiSans.AsNoTracking()
                .FirstOrDefaultAsync(l => l.MaLoaiSan == id);
        }

        // =====================================================================
        // 8. KIỂM TRA NGHIỆP VỤ
        // =====================================================================

        // Kiểm tra tên sân không được trùng
        public async Task<bool> TenSanExistsAsync(string tenSan, int maHienTai)
        {
            return await _context.SanTheThaos.AnyAsync(s =>
                s.TenSan == tenSan && s.MaSan != maHienTai);
        }

        // Kiểm tra tham chiếu bằng LINQ trước khi xóa
        public async Task<bool> DaCoLichDatAsync(int id)
        {
            return await _context.DatSans.AnyAsync(d => d.MaSan == id);
        }

        // Đếm số lịch đặt sắp tới (chưa hủy/từ chối) của một sân
        public Task<int> DemLichSapToiAsync(int maSan)
        {
            var bayGio = DateTime.Now;
            return _context.DatSans.CountAsync(d => d.MaSan == maSan
                                                 && d.GioKetThuc >= bayGio
                                                 && !TrangThaiKhongChiem.Contains(d.TrangThai));
        }

        // Cập nhật dữ liệu
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
