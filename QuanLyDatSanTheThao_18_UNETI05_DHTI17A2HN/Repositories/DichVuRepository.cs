
// Họ và tên: Tạ Đình Kim
// Mã sinh viên: 23103100315
// Nội dung thực hiện: Repository DichVu - CRUD dịch vụ (nước uống, thuê vợt, bóng...) bám sát giao diện thực tế.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class DichVuRepository : IDichVuRepository
    {
        private readonly ApplicationDbContext _context;

        public DichVuRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách dịch vụ
        public async Task<List<DichVu>> GetAllAsync()
        {
            return await _context.DichVus
                .AsNoTracking()
                .OrderBy(d => d.MaDichVu)
                .ToListAsync();
        }

        // Lấy dịch vụ theo mã
        public async Task<DichVu?> GetByIdAsync(int id)
        {
            return await _context.DichVus.FindAsync(id);
        }

        // Kiểm tra tên dịch vụ đã tồn tại hay chưa
        public async Task<bool> TenDichVuExistsAsync(string tenDichVu, int? id = null)
        {
            return await _context.DichVus.AnyAsync(d =>
                d.TenDichVu == tenDichVu
                && (!id.HasValue || d.MaDichVu != id.Value));
        }

        // Đếm số lượng dịch vụ đã sử dụng
        public async Task<int> DemSoLuongDaSuDungAsync(int id)
        {
            return await _context.ChiTietDatSans
                .Where(c => c.MaDichVu == id)
                .SumAsync(c => (int?)c.SoLuong) ?? 0;
        }

        // Kiểm tra dịch vụ đã được sử dụng trong đơn đặt sân hay chưa
        public async Task<bool> DaDuocSuDungAsync(int id)
        {
            return await _context.ChiTietDatSans
                .AnyAsync(c => c.MaDichVu == id);
        }

        // Thêm dịch vụ
        public async Task AddAsync(DichVu dichVu)
        {
            _context.DichVus.Add(dichVu);
            await _context.SaveChangesAsync();
        }

        // Cập nhật dịch vụ
        public async Task UpdateAsync(DichVu dichVu)
        {
            _context.Update(dichVu);
            await _context.SaveChangesAsync();
        }

        // Xóa dịch vụ
        public async Task DeleteAsync(DichVu dichVu)
        {
            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();
        }

        // Lưu thay đổi
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
