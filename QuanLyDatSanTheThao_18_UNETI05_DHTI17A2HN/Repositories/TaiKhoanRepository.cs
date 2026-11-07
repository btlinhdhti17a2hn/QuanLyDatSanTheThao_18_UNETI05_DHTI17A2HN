
// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Repository TaiKhoan - Xử lý truy vấn đăng nhập, đăng ký và quản lý tài khoản

using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories
{
    public class TaiKhoanRepository : ITaiKhoanRepository
    {
        private readonly ApplicationDbContext _context;

        public TaiKhoanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Kiểm tra thông tin đăng nhập
        public TaiKhoan? GetByDangNhap(string tenDangNhap, string matKhau)
        {
            return _context.TaiKhoans.FirstOrDefault(x =>
                x.TenDangNhap == tenDangNhap &&
                x.MatKhau == matKhau);
        }

        // Kiểm tra tên đăng nhập đã tồn tại hay chưa
        public TaiKhoan? GetByTenDangNhap(string tenDangNhap)
        {
            return _context.TaiKhoans.FirstOrDefault(x =>
                x.TenDangNhap == tenDangNhap);
        }
        // Lấy thông tin khách hàng gắn với tài khoản (dùng để xác minh khi quên mật khẩu)
        public KhachHang? GetKhachHangByMaTaiKhoan(int maTaiKhoan)
        {
            return _context.KhachHangs.FirstOrDefault(k => k.MaTaiKhoan == maTaiKhoan);
        }

        // Thêm tài khoản mới
        public async Task AddTaiKhoanAsync(TaiKhoan taiKhoan)
        {
            _context.TaiKhoans.Add(taiKhoan);
            await _context.SaveChangesAsync();
        }

        // Thêm khách hàng liên kết với tài khoản
        public async Task AddKhachHangAsync(KhachHang khachHang)
        {
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();
        }

        // Lấy danh sách tài khoản
        public List<TaiKhoan> GetAll()
        {
            return _context.TaiKhoans
                .OrderByDescending(t => t.MaTaiKhoan)
                .ToList();
        }

        // Lấy tài khoản theo mã
        public async Task<TaiKhoan?> GetByIdAsync(int id)
        {
            return await _context.TaiKhoans.FindAsync(id);
        }

        // Lưu thay đổi
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
