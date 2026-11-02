
// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - Repository quản lý thông tin khách hàng, xem và cập nhật hồ sơ cá nhân, danh sách khách hàng, tìm kiếm và phân trang

using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class KhachHangRepository : IKhachHangRepository
    {
        private readonly ApplicationDbContext _context;

        public KhachHangRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy thông tin khách hàng liên kết với Tài khoản
        public async Task<KhachHang?> GetByTaiKhoanAsync(int maTaiKhoan)
        {
            return await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);
        }

        // Kiểm tra khách hàng chỉ được cập nhật hồ sơ của chính mình
        public async Task<KhachHang?> GetHoSoAsync(int maKhachHang, int maTaiKhoan)
        {
            return await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k =>
                    k.MaKhachHang == maKhachHang &&
                    k.MaTaiKhoan == maTaiKhoan);
        }

        // Tìm kiếm + Lọc trạng thái + Sắp xếp + Phân trang
        public async Task<List<KhachHang>> GetAllAsync(
            string? tuKhoa, string? trangThai,
            int trang, int kichThuocTrang)
        {
            var query = _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .AsQueryable();

            // 1. Tìm kiếm LINQ theo Họ tên, Số điện thoại hoặc Email
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                query = query.Where(k => k.HoTen.Contains(tuKhoa)
                                      || k.SoDienThoai.Contains(tuKhoa)
                                      || (k.Email != null && k.Email.Contains(tuKhoa)));
            }

            // 2. Lọc LINQ theo Trạng thái hoạt động
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "HoatDong")
                {
                    query = query.Where(k => k.TrangThai == true);
                }
                else if (trangThai == "BiKhoa")
                {
                    query = query.Where(k => k.TrangThai == false);
                }
            }

            // 3. Sắp xếp LINQ: Khách hàng mới nhất lên đầu
            query = query.OrderByDescending(k => k.MaKhachHang);

            // 4. Phân trang bằng Skip() và Take() trên LINQ
            return await query
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();
        }

        // Đếm tổng số bản ghi sau khi tìm kiếm và lọc
        public async Task<int> DemTongSoAsync(string? tuKhoa, string? trangThai)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                query = query.Where(k => k.HoTen.Contains(tuKhoa)
                                      || k.SoDienThoai.Contains(tuKhoa)
                                      || (k.Email != null && k.Email.Contains(tuKhoa)));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "HoatDong")
                    query = query.Where(k => k.TrangThai == true);
                else if (trangThai == "BiKhoa")
                    query = query.Where(k => k.TrangThai == false);
            }

            return await query.CountAsync();
        }

        // Xem chi tiết khách hàng kèm lịch sử đặt sân
        public async Task<KhachHang?> GetDetailsAsync(int id)
        {
            return await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatSans)
                    .ThenInclude(d => d.SanTheThao)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);
        }

        // Lấy khách hàng theo mã
        public async Task<KhachHang?> GetByIdAsync(int id)
        {
            return await _context.KhachHangs.FindAsync(id);
        }

        // Cập nhật khách hàng
        public async Task UpdateAsync(KhachHang khachHang)
        {
            _context.Update(khachHang);
            await _context.SaveChangesAsync();
        }

        // Lưu thay đổi
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
