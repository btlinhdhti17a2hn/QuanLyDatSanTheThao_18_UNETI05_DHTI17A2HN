using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class ThongKeRepository : IThongKeRepository
    {
        private readonly ApplicationDbContext _context;

        public ThongKeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy dữ liệu loại sân phục vụ thống kê
        public IQueryable<LoaiSan> LayLoaiSans()
        {
            return _context.LoaiSans.AsNoTracking();
        }

        // Lấy dữ liệu sân thể thao phục vụ thống kê
        public IQueryable<SanTheThao> LaySanTheThaos()
        {
            return _context.SanTheThaos;
        }

        // Lấy dữ liệu khách hàng phục vụ thống kê
        public IQueryable<KhachHang> LayKhachHangs()
        {
            return _context.KhachHangs;
        }

        // Lấy dữ liệu đặt sân phục vụ thống kê
        public IQueryable<DatSan> LayDatSans()
        {
            return _context.DatSans;
        }
    }
}