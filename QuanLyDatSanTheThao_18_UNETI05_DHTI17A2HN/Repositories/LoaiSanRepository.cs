
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class LoaiSanRepository : ILoaiSanRepository
    {
        private readonly ApplicationDbContext _context;

        public LoaiSanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LoaiSan>> GetAllAsync()
        {
            return await _context.LoaiSans.ToListAsync();
        }

        public async Task<LoaiSan?> GetByIdAsync(int id)
        {
            return await _context.LoaiSans.FindAsync(id);
        }

        public async Task AddAsync(LoaiSan loaiSan)
        {
            _context.LoaiSans.Add(loaiSan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LoaiSan loaiSan)
        {
            _context.Update(loaiSan);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var loaiSan = await _context.LoaiSans.FindAsync(id);

            if (loaiSan != null)
            {
                _context.LoaiSans.Remove(loaiSan);
                await _context.SaveChangesAsync();
            }
        }

        // Kiểm tra tên loại sân có bị trùng hay không
        public async Task<bool> TenLoaiExistsAsync(string tenLoai, int? id = null)
        {
            return await _context.LoaiSans.AnyAsync(x =>
                x.TenLoai.ToLower() == tenLoai.ToLower()
                && (!id.HasValue || x.MaLoaiSan != id.Value));
        }

        // Kiểm tra dữ liệu đã được tham chiếu trong bảng SanTheThao chưa trước khi xóa
        public async Task<bool> LoaiSanDaDuocSuDungAsync(int id)
        {
            return await _context.SanTheThaos.AnyAsync(s => s.MaLoaiSan == id);
        }

        public async Task<bool> LoaiSanExistsAsync(int id)
        {
            return await _context.LoaiSans.AnyAsync(e => e.MaLoaiSan == id);
        }
    }
}
