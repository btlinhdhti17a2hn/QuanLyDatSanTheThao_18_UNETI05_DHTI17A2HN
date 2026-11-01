
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface ITaiKhoanRepository
    {
        TaiKhoan? GetByDangNhap(string tenDangNhap, string matKhau);
        TaiKhoan? GetByTenDangNhap(string tenDangNhap);
        Task AddTaiKhoanAsync(TaiKhoan taiKhoan);
        Task AddKhachHangAsync(KhachHang khachHang);
        List<TaiKhoan> GetAll();
        Task<TaiKhoan?> GetByIdAsync(int id);
        Task SaveChangesAsync();
    }
}
