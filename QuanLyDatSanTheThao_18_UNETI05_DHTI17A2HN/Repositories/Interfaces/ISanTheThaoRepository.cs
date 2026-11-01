
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface ISanTheThaoRepository
    {
        // Danh sách, tìm kiếm, lọc, sắp xếp và phân trang
        Task<SanTheThaoIndexViewModel> GetAllAsync(
            string? tuKhoa, int? maLoaiSan, string? trangThai,
            decimal? giaTu, decimal? giaDen,
            DateTime? ngaySuDung, TimeSpan? gioBatDau, TimeSpan? gioKetThuc,
            bool conTrong = false, string sapXep = "ten_asc", int trang = 1);

        // Xem chi tiết sân và lịch đặt sắp tới
        Task<SanTheThaoDetailsViewModel?> GetDetailsAsync(int id);

        // Lấy sân theo mã
        Task<SanTheThao?> GetByIdAsync(int id);

        // Thêm, sửa, xóa sân
        Task AddAsync(SanTheThao san);
        Task UpdateAsync(SanTheThao goc, SanTheThao san);
        Task DeleteAsync(SanTheThao san);

        // Lấy danh sách loại sân cho SelectList
        Task<List<LoaiSan>> GetLoaiSanAsync(int? dangChon);

        // Kiểm tra nghiệp vụ
        Task<LoaiSan?> GetLoaiSanByIdAsync(int id);
        Task<bool> TenSanExistsAsync(string tenSan, int maHienTai);
        Task<bool> DaCoLichDatAsync(int id);
        Task<int> DemLichSapToiAsync(int maSan);

        // Cập nhật trạng thái sân
        Task SaveChangesAsync();
    }
}
