using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class TiepNhanDatSanRepository : ITiepNhanDatSanRepository
    {
        private readonly ApplicationDbContext _context;

        public TiepNhanDatSanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tìm kiếm, lọc, sắp xếp và phân trang danh sách đơn đặt sân
        public async Task<(List<DatSan> DanhSach, int TongSoBanGhi)> TimKiemDatSanAsync(
            string? tenKhach,
            string? tenSan,
            int? maLoaiSan,
            string? trangThai,
            DateTime? tuNgay,
            DateTime? denNgay,
            string sapXep,
            int trang,
            int kichThuocTrang)
        {
            IQueryable<DatSan> query = _context.DatSans
                .AsNoTracking()
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan);

            // Tìm theo họ tên hoặc số điện thoại khách hàng
            if (!string.IsNullOrEmpty(tenKhach))
            {
                query = query.Where(d =>
                    d.KhachHang.HoTen.Contains(tenKhach)
                    || d.KhachHang.SoDienThoai.Contains(tenKhach));
            }

            // Tìm theo tên sân
            if (!string.IsNullOrEmpty(tenSan))
            {
                query = query.Where(d => d.SanTheThao.TenSan.Contains(tenSan));
            }

            // Lọc theo loại sân
            if (maLoaiSan.HasValue)
            {
                query = query.Where(d => d.SanTheThao.MaLoaiSan == maLoaiSan.Value);
            }

            // Lọc theo trạng thái đơn
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            // Lọc theo ngày sử dụng sân
            if (tuNgay.HasValue)
            {
                var tu = tuNgay.Value.Date;
                query = query.Where(d => d.GioBatDau >= tu);
            }

            if (denNgay.HasValue)
            {
                var den = denNgay.Value.Date.AddDays(1);
                query = query.Where(d => d.GioBatDau < den);
            }

            // Sắp xếp danh sách
            query = sapXep switch
            {
                "ngaydat_cu" => query.OrderBy(d => d.NgayDat).ThenBy(d => d.MaDatSan),
                "ngaysd_moi" => query.OrderByDescending(d => d.GioBatDau).ThenByDescending(d => d.MaDatSan),
                "ngaysd_cu" => query.OrderBy(d => d.GioBatDau).ThenBy(d => d.MaDatSan),
                "khach_az" => query.OrderBy(d => d.KhachHang.HoTen).ThenBy(d => d.MaDatSan),
                "khach_za" => query.OrderByDescending(d => d.KhachHang.HoTen).ThenBy(d => d.MaDatSan),
                "san_az" => query.OrderBy(d => d.SanTheThao.TenSan).ThenBy(d => d.MaDatSan),
                "san_za" => query.OrderByDescending(d => d.SanTheThao.TenSan).ThenBy(d => d.MaDatSan),
                _ => query.OrderByDescending(d => d.NgayDat).ThenByDescending(d => d.MaDatSan)
            };

            int tongSoBanGhi = await query.CountAsync();

            var danhSach = await query
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();

            return (danhSach, tongSoBanGhi);
        }

        // Thống kê số đơn theo từng trạng thái
        public async Task<Dictionary<string, int>> ThongKeTheoTrangThaiAsync()
        {
            return await _context.DatSans
                .GroupBy(d => d.TrangThai)
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToDictionaryAsync(x => x.TrangThai, x => x.SoLuong);
        }

        // Lấy danh sách loại sân phục vụ bộ lọc
        public async Task<List<LoaiSan>> LayDanhSachLoaiSanAsync()
        {
            return await _context.LoaiSans
                .AsNoTracking()
                .OrderBy(l => l.TenLoai)
                .ToListAsync();
        }

        // Lấy chi tiết đơn đặt sân cùng khách hàng, sân và dịch vụ
        public async Task<DatSan?> LayChiTietDatSanAsync(int maDatSan)
        {
            return await _context.DatSans
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                    .ThenInclude(s => s.LoaiSan)
                .Include(d => d.ChiTietDatSans)
                    .ThenInclude(ct => ct.DichVu)
                .FirstOrDefaultAsync(d => d.MaDatSan == maDatSan);
        }

        // Lấy dịch vụ đang kinh doanh và những dịch vụ đã có trong đơn
        public async Task<List<DichVu>> LayDanhSachDichVuAsync(List<int> maDichVuDaCo)
        {
            return await _context.DichVus
                .AsNoTracking()
                .Where(dv => dv.TrangThai || maDichVuDaCo.Contains(dv.MaDichVu))
                .OrderBy(dv => dv.TenDichVu)
                .ToListAsync();
        }

        // Lấy đơn đặt sân kèm các dòng chi tiết dịch vụ để cập nhật
        public async Task<DatSan?> LayDatSanKemChiTietDichVuAsync(int maDatSan)
        {
            return await _context.DatSans
                .Include(d => d.ChiTietDatSans)
                .FirstOrDefaultAsync(d => d.MaDatSan == maDatSan);
        }

        // Lấy các dịch vụ theo danh sách mã
        public async Task<Dictionary<int, DichVu>> LayDichVuTheoDanhSachMaAsync(List<int> danhSachMa)
        {
            return await _context.DichVus
                .Where(dv => danhSachMa.Contains(dv.MaDichVu))
                .ToDictionaryAsync(dv => dv.MaDichVu);
        }

        // Xóa một dòng dịch vụ khỏi đơn đặt sân
        public void XoaChiTietDatSan(ChiTietDatSan chiTiet)
        {
            _context.ChiTietDatSans.Remove(chiTiet);
        }

        // Lấy đơn đặt sân cùng dữ liệu cần thiết để kiểm tra và chuyển trạng thái
        public async Task<DatSan?> LayDatSanXuLyAsync(int maDatSan)
        {
            return await _context.DatSans
                .Include(d => d.KhachHang)
                .Include(d => d.SanTheThao)
                .Include(d => d.ChiTietDatSans)
                .FirstOrDefaultAsync(d => d.MaDatSan == maDatSan);
        }

        // Kiểm tra có đơn khác bị trùng lịch hay không
        public async Task<bool> CoDonTrungLichAsync(
            int maDatSan,
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc,
            string trangThaiHuy)
        {
            return await _context.DatSans.AnyAsync(d =>
                d.MaDatSan != maDatSan
                && d.MaSan == maSan
                && d.TrangThai != trangThaiHuy
                && d.GioBatDau < gioKetThuc
                && d.GioKetThuc > gioBatDau);
        }

        // Lấy danh sách tối đa 5 đơn bị trùng lịch để hiển thị cảnh báo
        public async Task<List<DatSan>> LayDanhSachDonTrungLichAsync(
            int maDatSan,
            int maSan,
            DateTime gioBatDau,
            DateTime gioKetThuc,
            string trangThaiHuy)
        {
            return await _context.DatSans
                .Where(d =>
                    d.MaDatSan != maDatSan
                    && d.MaSan == maSan
                    && d.TrangThai != trangThaiHuy
                    && d.GioBatDau < gioKetThuc
                    && d.GioKetThuc > gioBatDau)
                .Include(d => d.KhachHang)
                .OrderBy(d => d.GioBatDau)
                .Take(5)
                .ToListAsync();
        }

        // Tính tổng tiền dịch vụ trong đơn
        public async Task<decimal> TinhTongTienDichVuAsync(int maDatSan)
        {
            return await _context.ChiTietDatSans
                .Where(ct => ct.MaDatSan == maDatSan)
                .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0;
        }

        // Đếm số lượt đặt sân trong ngày, không tính đơn đã hủy và đơn hiện tại
        public async Task<int> DemLuotDatTrongNgayAsync(
            int maDatSan,
            int maSan,
            DateTime dauNgay,
            DateTime cuoiNgay,
            string trangThaiHuy)
        {
            return await _context.DatSans.CountAsync(d =>
                d.MaDatSan != maDatSan
                && d.MaSan == maSan
                && d.TrangThai != trangThaiHuy
                && d.GioBatDau >= dauNgay
                && d.GioBatDau < cuoiNgay);
        }

        // Kiểm tra mã giao dịch ngân hàng đã được dùng xác nhận thanh toán cho đơn khác hay chưa
        public async Task<bool> MaGiaoDichDaDuocDungAsync(string maGiaoDich, int maDatSanHienTai)
        {
            return await _context.DatSans.AnyAsync(d =>
                d.MaDatSan != maDatSanHienTai
                && d.DaThanhToan
                && d.MaGiaoDichNganHang == maGiaoDich);
        }

        // Lưu tất cả thay đổi
        public async Task LuuThayDoiAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}