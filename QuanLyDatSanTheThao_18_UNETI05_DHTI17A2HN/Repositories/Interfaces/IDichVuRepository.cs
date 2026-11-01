
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface IDichVuRepository
    {
        Task<List<DichVu>> GetAllAsync();
        Task<DichVu?> GetByIdAsync(int id);
        Task<bool> TenDichVuExistsAsync(string tenDichVu, int? id = null);
        Task<int> DemSoLuongDaSuDungAsync(int id);
        Task<bool> DaDuocSuDungAsync(int id);
        Task AddAsync(DichVu dichVu);
        Task UpdateAsync(DichVu dichVu);
        Task DeleteAsync(DichVu dichVu);
        Task SaveChangesAsync();
    }
}
