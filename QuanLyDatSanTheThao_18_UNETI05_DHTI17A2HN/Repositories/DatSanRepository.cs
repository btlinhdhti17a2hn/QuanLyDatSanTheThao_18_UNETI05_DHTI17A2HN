using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class DatSanRepository : IDatSanRepository
    {
        private readonly ApplicationDbContext _context;

        public DatSanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy khách hàng đang đăng nhập theo mã tài khoản
        public async Task<KhachHang?> LayKhachHangTheoTaiKhoanAsync(int maTaiKhoan)
        {
            return await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);
        }

        // Lấy danh sách sân đang hoạt động
        public async Task<List<SanTheThao>> LayDanhSachSanHoatDongAsync()
        {
            return await _context.SanTheThaos
                .Include(s => s.LoaiSan)
                .Where(s => s.TrangThai)
                .OrderBy(s => s.TenSan)
                .ToListAsync();
        }

        // Lấy thông tin sân theo mã, kèm loại sân
        public async Task<SanTheThao?> LaySanTheoMaAsync(int maSan)
        {
            return await _context.SanTheThaos
                .Include(s => s.LoaiSan)
                .FirstOrDefaultAsync(s => s.MaSan == maSan);
        }

        // Kiểm tra sân đã có đơn đặt giao nhau về thời gian hay chưa
        public async Task<bool> KiemTraTrungLichAsync(
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc)
        {
            return await _context.DatSans.AnyAsync(d =>
                d.MaSan == maSan
                && d.TrangThai != "Đã hủy"
                && d.GioBatDau < gioKetThuc
                && d.GioKetThuc > gioBatDau);
        }

        // Thêm đơn đặt sân mới
        public async Task ThemDatSanAsync(DatSan datSan)
        {
            await _context.DatSans.AddAsync(datSan);
            await _context.SaveChangesAsync();
        }

        // Đếm số lượng đơn đặt sân theo khách hàng và trạng thái
        public async Task<int> DemLichSuDatSanAsync(int maKhachHang, string? trangThai)
        {
            var query = _context.DatSans
                .Where(d => d.MaKhachHang == maKhachHang);

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            return await query.CountAsync();
        }

        // Lấy lịch sử đặt sân có phân trang
        public async Task<List<DatSan>> LayLichSuDatSanAsync(
            int maKhachHang,
            string? trangThai,
            int trang,
            int kichThuocTrang)
        {
            var query = _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Where(d => d.MaKhachHang == maKhachHang);

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            return await query
                .OrderByDescending(d => d.NgayDat)
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();
        }

        // Lấy chi tiết đơn đặt sân cùng sân, khách hàng và dịch vụ
        public async Task<DatSan?> LayChiTietDatSanAsync(int maDatSan)
        {
            return await _context.DatSans
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatSans)
                    .ThenInclude(ct => ct.DichVu)
                .FirstOrDefaultAsync(d => d.MaDatSan == maDatSan);
        }

        // Lấy đơn đặt sân theo mã để cập nhật trạng thái
        public async Task<DatSan?> LayDatSanTheoMaAsync(int maDatSan)
        {
            return await _context.DatSans
                .FirstOrDefaultAsync(d => d.MaDatSan == maDatSan);
        }

        // Lưu thay đổi
        public async Task LuuThayDoiAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}