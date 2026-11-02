
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface ILoaiSanRepository
    {
        Task<List<LoaiSan>> GetAllAsync();
        Task<LoaiSan?> GetByIdAsync(int id);
        Task AddAsync(LoaiSan loaiSan);
        Task UpdateAsync(LoaiSan loaiSan);
        Task DeleteAsync(int id);
        Task<bool> TenLoaiExistsAsync(string tenLoai, int? id = null);
        Task<bool> LoaiSanDaDuocSuDungAsync(int id);
        Task<bool> LoaiSanExistsAsync(int id);
    }
}
