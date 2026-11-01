
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface IKhachHangRepository
    {
        Task<KhachHang?> GetByTaiKhoanAsync(int maTaiKhoan);
        Task<KhachHang?> GetHoSoAsync(int maKhachHang, int maTaiKhoan);
        Task<List<KhachHang>> GetAllAsync(
            string? tuKhoa, string? trangThai,
            int trang, int kichThuocTrang);

        Task<int> DemTongSoAsync(string? tuKhoa, string? trangThai);
        Task<KhachHang?> GetDetailsAsync(int id);
        Task<KhachHang?> GetByIdAsync(int id);
        Task UpdateAsync(KhachHang khachHang);
        Task SaveChangesAsync();
    }
}
