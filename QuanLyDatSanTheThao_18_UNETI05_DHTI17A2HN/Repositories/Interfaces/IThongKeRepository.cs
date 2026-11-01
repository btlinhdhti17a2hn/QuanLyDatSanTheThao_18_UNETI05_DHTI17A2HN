using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces
{
    public interface IThongKeRepository
    {
        // Lấy dữ liệu loại sân phục vụ thống kê
        IQueryable<LoaiSan> LayLoaiSans();

        // Lấy dữ liệu sân thể thao phục vụ thống kê
        IQueryable<SanTheThao> LaySanTheThaos();

        // Lấy dữ liệu khách hàng phục vụ thống kê
        IQueryable<KhachHang> LayKhachHangs();

        // Lấy dữ liệu đặt sân phục vụ thống kê
        IQueryable<DatSan> LayDatSans();
    }
}