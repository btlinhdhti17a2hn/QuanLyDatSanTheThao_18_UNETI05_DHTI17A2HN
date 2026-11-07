using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface ITiepNhanDatSanRepository
    {
        Task<(List<DatSan> DanhSach, int TongSoBanGhi)> TimKiemDatSanAsync(
            string? tenKhach,
            string? tenSan,
            int? maLoaiSan,
            string? trangThai,
            DateTime? tuNgay,
            DateTime? denNgay,
            string sapXep,
            int trang,
            int kichThuocTrang);

        Task<Dictionary<string, int>> ThongKeTheoTrangThaiAsync();

        Task<List<LoaiSan>> LayDanhSachLoaiSanAsync();

        Task<DatSan?> LayChiTietDatSanAsync(int maDatSan);

        Task<List<DichVu>> LayDanhSachDichVuAsync(List<int> maDichVuDaCo);

        Task<DatSan?> LayDatSanKemChiTietDichVuAsync(int maDatSan);

        Task<Dictionary<int, DichVu>> LayDichVuTheoDanhSachMaAsync(List<int> danhSachMa);

        void XoaChiTietDatSan(ChiTietDatSan chiTiet);

        Task<DatSan?> LayDatSanXuLyAsync(int maDatSan);

        Task<bool> CoDonTrungLichAsync(
            int maDatSan,
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc,
            string trangThaiHuy);

        Task<List<DatSan>> LayDanhSachDonTrungLichAsync(
            int maDatSan,
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc,
            string trangThaiHuy);

        Task<decimal> TinhTongTienDichVuAsync(int maDatSan);

        Task<int> DemLuotDatTrongNgayAsync(
            int maDatSan,
            int maSan,
            DateTime dauNgay,
            DateTime cuoiNgay,
            string trangThaiHuy);

        // Mã giao dịch ngân hàng này đã được dùng để xác nhận cho đơn khác chưa
        Task<bool> MaGiaoDichDaDuocDungAsync(string maGiaoDich, int maDatSanHienTai);

        Task LuuThayDoiAsync();
    }
}