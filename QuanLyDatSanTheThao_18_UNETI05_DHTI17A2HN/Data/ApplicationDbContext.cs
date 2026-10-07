// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Cấu hình ApplicationDbContext, ánh xạ cơ sở dữ liệu (DbSet) và khởi tạo dữ liệu mẫu (Seed Data).
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<LoaiSan> LoaiSans { get; set; }
        public DbSet<SanTheThao> SanTheThaos { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<DatSan> DatSans { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<ChiTietDatSan> ChiTietDatSans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ràng buộc Unique cho Tên đăng nhập
            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(t => t.TenDangNhap)
                .IsUnique();

            // ----------------------------------------------------------------------
            // 1. DỮ LIỆU MẪU: LOẠI SÂN (05 BẢN GHI)
            // ----------------------------------------------------------------------
            var loaiSans = new List<LoaiSan>{
                new LoaiSan { MaLoaiSan = 1, TenLoai = "Sân Bóng Đá 7 Người", MoTa = "Sân cỏ nhân tạo", SoNguoiToiDa = 14, DonGiaTheoGio = 300000, TrangThai = true },
                new LoaiSan { MaLoaiSan = 2, TenLoai = "Sân Bóng Đá 5 Người", MoTa = "Sân cỏ nhân tạo", SoNguoiToiDa = 10, DonGiaTheoGio = 200000, TrangThai = true },
                new LoaiSan { MaLoaiSan = 3, TenLoai = "Sân Cầu Lông", MoTa = "Sân thảm PVC", SoNguoiToiDa = 4, DonGiaTheoGio = 80000, TrangThai = true },
                new LoaiSan { MaLoaiSan = 4, TenLoai = "Sân Tennis", MoTa = "Sân đất nện", SoNguoiToiDa = 4, DonGiaTheoGio = 150000, TrangThai = true },
                new LoaiSan { MaLoaiSan = 5, TenLoai = "Sân Bóng Rổ", MoTa = "Sân bê tông", SoNguoiToiDa = 10, DonGiaTheoGio = 120000, TrangThai = true }
            };
            modelBuilder.Entity<LoaiSan>().HasData(loaiSans);

            // ----------------------------------------------------------------------
            // 2. DỮ LIỆU MẪU: DỊCH VỤ (05 BẢN GHI)
            // ----------------------------------------------------------------------
            var dichVus = new List<DichVu>{
                new DichVu { MaDichVu = 1, TenDichVu = "Nước suối Aquafina", DonGia = 10000, TrangThai = true },
                new DichVu { MaDichVu = 2, TenDichVu = "Nước tăng lực Redbull", DonGia = 15000, TrangThai = true },
                new DichVu { MaDichVu = 3, TenDichVu = "Thuê áo Bib (bộ)", DonGia = 30000, TrangThai = true },
                new DichVu { MaDichVu = 4, TenDichVu = "Thuê bóng/quả", DonGia = 20000, TrangThai = true },
                new DichVu { MaDichVu = 5, TenDichVu = "Thuê vợt cầu lông", DonGia = 15000, TrangThai = true }
            };
            modelBuilder.Entity<DichVu>().HasData(dichVus);

            // ----------------------------------------------------------------------
            // 3. DỮ LIỆU MẪU: SÂN THỂ THAO (15 BẢN GHI)
            // ----------------------------------------------------------------------
            var sanTheThaos = new List<SanTheThao>();
            for (int i = 1; i <= 15; i++)
            {
                int loaiSanId = (i % 5) + 1; // Phân bổ đều cho 5 loại sân
                sanTheThaos.Add(new SanTheThao
                {
                    MaSan = i,
                    TenSan = $"Sân số {i} - {loaiSans[loaiSanId - 1].TenLoai}",
                    MaLoaiSan = loaiSanId,
                    DiaChi = $"Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai",
                    TienIch = "Wifi, Trà đá miễn phí, Chỗ để xe",
                    DonGia = loaiSans[loaiSanId - 1].DonGiaTheoGio,
                    TrangThai = true,
                    GioMoCua = new TimeSpan(6, 0, 0), // 6h sáng
                    GioDongCua = new TimeSpan(22, 0, 0) // 10h tối
                });
            }
            modelBuilder.Entity<SanTheThao>().HasData(sanTheThaos);

            // ----------------------------------------------------------------------
            // 4. DỮ LIỆU MẪU: TÀI KHOẢN (02 ADMIN, 03 NHÂN VIÊN, 30 KHÁCH HÀNG)
            // ----------------------------------------------------------------------
            var taiKhoans = new List<TaiKhoan>();

            // 02 Tài khoản Admin
            taiKhoans.Add(new TaiKhoan { MaTaiKhoan = 1, TenDangNhap = "admin1", MatKhau = "123456", HoTen = "Quản trị viên 1", Email = "admin1@uneti.edu.vn", VaiTro = "Admin", TrangThai = true });
            taiKhoans.Add(new TaiKhoan { MaTaiKhoan = 2, TenDangNhap = "admin2", MatKhau = "123456", HoTen = "Quản trị viên 2", Email = "admin2@uneti.edu.vn", VaiTro = "Admin", TrangThai = true });

            // 03 Tài khoản Nhân viên
            taiKhoans.Add(new TaiKhoan { MaTaiKhoan = 3, TenDangNhap = "nhanvien1", MatKhau = "123456", HoTen = "Nhân viên 1", Email = "nv1@uneti.edu.vn", VaiTro = "NhanVien", TrangThai = true });
            taiKhoans.Add(new TaiKhoan { MaTaiKhoan = 4, TenDangNhap = "nhanvien2", MatKhau = "123456", HoTen = "Nhân viên 2", Email = "nv2@uneti.edu.vn", VaiTro = "NhanVien", TrangThai = true });
            taiKhoans.Add(new TaiKhoan { MaTaiKhoan = 5, TenDangNhap = "nhanvien3", MatKhau = "123456", HoTen = "Nhân viên 3", Email = "nv3@uneti.edu.vn", VaiTro = "NhanVien", TrangThai = true });

            // 30 Tài khoản Khách hàng (ID từ 6 đến 35)
            for (int i = 1; i <= 30; i++)
            {
                taiKhoans.Add(new TaiKhoan
                {
                    MaTaiKhoan = i + 5,
                    TenDangNhap = $"khachhang{i}",
                    MatKhau = "123456",
                    HoTen = $"Khách hàng {i}",
                    Email = $"kh{i}@gmail.com",
                    VaiTro = "KhachHang",
                    TrangThai = true
                });
            }
            modelBuilder.Entity<TaiKhoan>().HasData(taiKhoans);

            // ----------------------------------------------------------------------
            // 5. DỮ LIỆU MẪU: KHÁCH HÀNG (30 BẢN GHI - LIÊN KẾT TÀI KHOẢN TỪ 6-35)
            // ----------------------------------------------------------------------
            var khachHangs = new List<KhachHang>();
            for (int i = 1; i <= 30; i++)
            {
                khachHangs.Add(new KhachHang
                {
                    MaKhachHang = i,
                    MaTaiKhoan = i + 5,
                    HoTen = $"Khách hàng {i}",
                    NgaySinh = new DateTime(2000, 1, 1).AddDays(i * 15),
                    GioiTinh = (i % 2 == 0) ? "Nam" : "Nữ",
                    SoDienThoai = $"0987654{i:D3}", // Format 0987654001, 0987654002...
                    Email = $"kh{i}@gmail.com",
                    DiaChi = "Hà Nội",
                    NgayDangKy = new DateTime(2026, 9, 1),
                    DiemTichLuy = i * 10,
                    TrangThai = true
                });
            }
            modelBuilder.Entity<KhachHang>().HasData(khachHangs);

            // ----------------------------------------------------------------------
            // 6. DỮ LIỆU MẪU: ĐẶT SÂN (45 BẢN GHI ĐA DẠNG TRẠNG THÁI)
            // ----------------------------------------------------------------------
            var datSans = new List<DatSan>();
            string[] cacTrangThai = { "Chờ xử lý", "Đang xử lý", "Hoàn thành", "Đã hủy" }; // 4 trạng thái
            for (int i = 1; i <= 45; i++)
            {
                int khId = (i % 30) + 1; // Random khách hàng 1-30
                int sanId = (i % 15) + 1; // Random sân 1-15

                // Set lịch đá bóng vào 17h chiều các ngày trong tháng 10/2026
                DateTime gioBatDau = new DateTime(2026, 10, (i % 28) + 1, 17, 0, 0);

                datSans.Add(new DatSan
                {
                    MaDatSan = i,
                    MaKhachHang = khId,
                    MaSan = sanId,
                    NgayDat = new DateTime(2026, 9, 25), // Ngày lên hệ thống book
                    GioBatDau = gioBatDau,
                    GioKetThuc = gioBatDau.AddHours(2), // Đá 2 tiếng
                    DonGia = sanTheThaos[sanId - 1].DonGia * 2, // Tiền 2 tiếng
                    TienCoc = 100000,
                    TrangThai = cacTrangThai[i % 4] // Chia đều các trạng thái
                });
            }
            modelBuilder.Entity<DatSan>().HasData(datSans);

            // ----------------------------------------------------------------------
            // 7. DỮ LIỆU MẪU: CHI TIẾT ĐẶT SÂN (20 BẢN GHI DỊCH VỤ)
            // ----------------------------------------------------------------------
            var chiTietDatSans = new List<ChiTietDatSan>();
            for (int i = 1; i <= 20; i++)
            {
                int dvId = (i % 5) + 1; // Random dịch vụ 1-5
                int sl = (i % 3) + 1; // Số lượng random từ 1-3
                decimal donGiaDv = dichVus[dvId - 1].DonGia;

                chiTietDatSans.Add(new ChiTietDatSan
                {
                    MaChiTiet = i,
                    MaDatSan = i, // Gắn dịch vụ vào 20 đơn đặt sân đầu tiên
                    MaDichVu = dvId,
                    SoLuong = sl,
                    DonGia = donGiaDv,
                    ThanhTien = donGiaDv * sl,
                    GhiChu = "Cung cấp đầy đủ",
                    TrangThai = "Hoàn tất"
                });
            }
            modelBuilder.Entity<ChiTietDatSan>().HasData(chiTietDatSans);
        }
    }
}
