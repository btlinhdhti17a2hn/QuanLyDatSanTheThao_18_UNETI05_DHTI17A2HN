using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface IDatSanRepository
    {
        Task<KhachHang?> LayKhachHangTheoTaiKhoanAsync(int maTaiKhoan);

        Task<List<SanTheThao>> LayDanhSachSanHoatDongAsync();
        Task<SanTheThao?> LaySanTheoMaAsync(int maSan);

        Task<bool> KiemTraTrungLichAsync(
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc);

        Task ThemDatSanAsync(DatSan datSan);

        Task<int> DemLichSuDatSanAsync(int maKhachHang, string? trangThai);
        Task<List<DatSan>> LayLichSuDatSanAsync(
            int maKhachHang,
            string? trangThai,
            int trang,
            int kichThuocTrang);

        Task<DatSan?> LayChiTietDatSanAsync(int maDatSan);
        Task<DatSan?> LayDatSanTheoMaAsync(int maDatSan);

        Task LuuThayDoiAsync();
    }
}