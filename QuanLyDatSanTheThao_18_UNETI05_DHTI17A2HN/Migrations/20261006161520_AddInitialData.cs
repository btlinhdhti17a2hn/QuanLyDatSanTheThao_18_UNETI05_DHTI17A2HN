using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DichVu",
                columns: table => new
                {
                    MaDichVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDichVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVu", x => x.MaDichVu);
                });

            migrationBuilder.CreateTable(
                name: "LoaiSan",
                columns: table => new
                {
                    MaLoaiSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoNguoiToiDa = table.Column<int>(type: "int", nullable: false),
                    DonGiaTheoGio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiSan", x => x.MaLoaiSan);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "SanTheThao",
                columns: table => new
                {
                    MaSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaLoaiSan = table.Column<int>(type: "int", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TienIch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    GioMoCua = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioDongCua = table.Column<TimeSpan>(type: "time", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayBaoTri = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanTheThao", x => x.MaSan);
                    table.ForeignKey(
                        name: "FK_SanTheThao_LoaiSan_MaLoaiSan",
                        column: x => x.MaLoaiSan,
                        principalTable: "LoaiSan",
                        principalColumn: "MaLoaiSan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    MaKhachHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiemTichLuy = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.MaKhachHang);
                    table.ForeignKey(
                        name: "FK_KhachHang_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "DatSan",
                columns: table => new
                {
                    MaDatSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhachHang = table.Column<int>(type: "int", nullable: false),
                    MaSan = table.Column<int>(type: "int", nullable: false),
                    NgayDat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienCoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatSan", x => x.MaDatSan);
                    table.ForeignKey(
                        name: "FK_DatSan_KhachHang_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatSan_SanTheThao_MaSan",
                        column: x => x.MaSan,
                        principalTable: "SanTheThao",
                        principalColumn: "MaSan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDatSan",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatSan = table.Column<int>(type: "int", nullable: false),
                    MaDichVu = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatSan", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietDatSan_DatSan_MaDatSan",
                        column: x => x.MaDatSan,
                        principalTable: "DatSan",
                        principalColumn: "MaDatSan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDatSan_DichVu_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "DichVu",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DichVu",
                columns: new[] { "MaDichVu", "DonGia", "MoTa", "TenDichVu", "TrangThai" },
                values: new object[,]
                {
                    { 1, 10000m, null, "Nước suối Aquafina", true },
                    { 2, 15000m, null, "Nước tăng lực Redbull", true },
                    { 3, 30000m, null, "Thuê áo Bib (bộ)", true },
                    { 4, 20000m, null, "Thuê bóng/quả", true },
                    { 5, 15000m, null, "Thuê vợt cầu lông", true }
                });

            migrationBuilder.InsertData(
                table: "LoaiSan",
                columns: new[] { "MaLoaiSan", "DonGiaTheoGio", "MoTa", "SoNguoiToiDa", "TenLoai", "TrangThai" },
                values: new object[,]
                {
                    { 1, 300000m, "Sân cỏ nhân tạo", 14, "Sân Bóng Đá 7 Người", true },
                    { 2, 200000m, "Sân cỏ nhân tạo", 10, "Sân Bóng Đá 5 Người", true },
                    { 3, 80000m, "Sân thảm PVC", 4, "Sân Cầu Lông", true },
                    { 4, 150000m, "Sân đất nện", 4, "Sân Tennis", true },
                    { 5, 120000m, "Sân bê tông", 10, "Sân Bóng Rổ", true }
                });

            migrationBuilder.InsertData(
                table: "TaiKhoan",
                columns: new[] { "MaTaiKhoan", "Email", "HoTen", "MatKhau", "TenDangNhap", "TrangThai", "VaiTro" },
                values: new object[,]
                {
                    { 1, "admin1@uneti.edu.vn", "Quản trị viên 1", "123456", "admin1", true, "Admin" },
                    { 2, "admin2@uneti.edu.vn", "Quản trị viên 2", "123456", "admin2", true, "Admin" },
                    { 3, "nv1@uneti.edu.vn", "Nhân viên 1", "123456", "nhanvien1", true, "NhanVien" },
                    { 4, "nv2@uneti.edu.vn", "Nhân viên 2", "123456", "nhanvien2", true, "NhanVien" },
                    { 5, "nv3@uneti.edu.vn", "Nhân viên 3", "123456", "nhanvien3", true, "NhanVien" },
                    { 6, "kh1@gmail.com", "Khách hàng 1", "123456", "khachhang1", true, "KhachHang" },
                    { 7, "kh2@gmail.com", "Khách hàng 2", "123456", "khachhang2", true, "KhachHang" },
                    { 8, "kh3@gmail.com", "Khách hàng 3", "123456", "khachhang3", true, "KhachHang" },
                    { 9, "kh4@gmail.com", "Khách hàng 4", "123456", "khachhang4", true, "KhachHang" },
                    { 10, "kh5@gmail.com", "Khách hàng 5", "123456", "khachhang5", true, "KhachHang" },
                    { 11, "kh6@gmail.com", "Khách hàng 6", "123456", "khachhang6", true, "KhachHang" },
                    { 12, "kh7@gmail.com", "Khách hàng 7", "123456", "khachhang7", true, "KhachHang" },
                    { 13, "kh8@gmail.com", "Khách hàng 8", "123456", "khachhang8", true, "KhachHang" },
                    { 14, "kh9@gmail.com", "Khách hàng 9", "123456", "khachhang9", true, "KhachHang" },
                    { 15, "kh10@gmail.com", "Khách hàng 10", "123456", "khachhang10", true, "KhachHang" },
                    { 16, "kh11@gmail.com", "Khách hàng 11", "123456", "khachhang11", true, "KhachHang" },
                    { 17, "kh12@gmail.com", "Khách hàng 12", "123456", "khachhang12", true, "KhachHang" },
                    { 18, "kh13@gmail.com", "Khách hàng 13", "123456", "khachhang13", true, "KhachHang" },
                    { 19, "kh14@gmail.com", "Khách hàng 14", "123456", "khachhang14", true, "KhachHang" },
                    { 20, "kh15@gmail.com", "Khách hàng 15", "123456", "khachhang15", true, "KhachHang" },
                    { 21, "kh16@gmail.com", "Khách hàng 16", "123456", "khachhang16", true, "KhachHang" },
                    { 22, "kh17@gmail.com", "Khách hàng 17", "123456", "khachhang17", true, "KhachHang" },
                    { 23, "kh18@gmail.com", "Khách hàng 18", "123456", "khachhang18", true, "KhachHang" },
                    { 24, "kh19@gmail.com", "Khách hàng 19", "123456", "khachhang19", true, "KhachHang" },
                    { 25, "kh20@gmail.com", "Khách hàng 20", "123456", "khachhang20", true, "KhachHang" },
                    { 26, "kh21@gmail.com", "Khách hàng 21", "123456", "khachhang21", true, "KhachHang" },
                    { 27, "kh22@gmail.com", "Khách hàng 22", "123456", "khachhang22", true, "KhachHang" },
                    { 28, "kh23@gmail.com", "Khách hàng 23", "123456", "khachhang23", true, "KhachHang" },
                    { 29, "kh24@gmail.com", "Khách hàng 24", "123456", "khachhang24", true, "KhachHang" },
                    { 30, "kh25@gmail.com", "Khách hàng 25", "123456", "khachhang25", true, "KhachHang" },
                    { 31, "kh26@gmail.com", "Khách hàng 26", "123456", "khachhang26", true, "KhachHang" },
                    { 32, "kh27@gmail.com", "Khách hàng 27", "123456", "khachhang27", true, "KhachHang" },
                    { 33, "kh28@gmail.com", "Khách hàng 28", "123456", "khachhang28", true, "KhachHang" },
                    { 34, "kh29@gmail.com", "Khách hàng 29", "123456", "khachhang29", true, "KhachHang" },
                    { 35, "kh30@gmail.com", "Khách hàng 30", "123456", "khachhang30", true, "KhachHang" }
                });

            migrationBuilder.InsertData(
                table: "KhachHang",
                columns: new[] { "MaKhachHang", "DiaChi", "DiemTichLuy", "Email", "GhiChu", "GioiTinh", "HoTen", "MaTaiKhoan", "NgayDangKy", "NgaySinh", "SoDienThoai", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Hà Nội", 10, "kh1@gmail.com", null, "Nữ", "Khách hàng 1", 6, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 1, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654001", true },
                    { 2, "Hà Nội", 20, "kh2@gmail.com", null, "Nam", "Khách hàng 2", 7, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 1, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654002", true },
                    { 3, "Hà Nội", 30, "kh3@gmail.com", null, "Nữ", "Khách hàng 3", 8, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654003", true },
                    { 4, "Hà Nội", 40, "kh4@gmail.com", null, "Nam", "Khách hàng 4", 9, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654004", true },
                    { 5, "Hà Nội", 50, "kh5@gmail.com", null, "Nữ", "Khách hàng 5", 10, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 3, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654005", true },
                    { 6, "Hà Nội", 60, "kh6@gmail.com", null, "Nam", "Khách hàng 6", 11, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 3, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654006", true },
                    { 7, "Hà Nội", 70, "kh7@gmail.com", null, "Nữ", "Khách hàng 7", 12, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654007", true },
                    { 8, "Hà Nội", 80, "kh8@gmail.com", null, "Nam", "Khách hàng 8", 13, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654008", true },
                    { 9, "Hà Nội", 90, "kh9@gmail.com", null, "Nữ", "Khách hàng 9", 14, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654009", true },
                    { 10, "Hà Nội", 100, "kh10@gmail.com", null, "Nam", "Khách hàng 10", 15, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654010", true },
                    { 11, "Hà Nội", 110, "kh11@gmail.com", null, "Nữ", "Khách hàng 11", 16, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 6, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654011", true },
                    { 12, "Hà Nội", 120, "kh12@gmail.com", null, "Nam", "Khách hàng 12", 17, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654012", true },
                    { 13, "Hà Nội", 130, "kh13@gmail.com", null, "Nữ", "Khách hàng 13", 18, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654013", true },
                    { 14, "Hà Nội", 140, "kh14@gmail.com", null, "Nam", "Khách hàng 14", 19, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 7, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654014", true },
                    { 15, "Hà Nội", 150, "kh15@gmail.com", null, "Nữ", "Khách hàng 15", 20, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654015", true },
                    { 16, "Hà Nội", 160, "kh16@gmail.com", null, "Nam", "Khách hàng 16", 21, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654016", true },
                    { 17, "Hà Nội", 170, "kh17@gmail.com", null, "Nữ", "Khách hàng 17", 22, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 9, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654017", true },
                    { 18, "Hà Nội", 180, "kh18@gmail.com", null, "Nam", "Khách hàng 18", 23, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 9, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654018", true },
                    { 19, "Hà Nội", 190, "kh19@gmail.com", null, "Nữ", "Khách hàng 19", 24, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654019", true },
                    { 20, "Hà Nội", 200, "kh20@gmail.com", null, "Nam", "Khách hàng 20", 25, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 10, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654020", true },
                    { 21, "Hà Nội", 210, "kh21@gmail.com", null, "Nữ", "Khách hàng 21", 26, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 11, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654021", true },
                    { 22, "Hà Nội", 220, "kh22@gmail.com", null, "Nam", "Khách hàng 22", 27, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 11, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654022", true },
                    { 23, "Hà Nội", 230, "kh23@gmail.com", null, "Nữ", "Khách hàng 23", 28, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 12, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654023", true },
                    { 24, "Hà Nội", 240, "kh24@gmail.com", null, "Nam", "Khách hàng 24", 29, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654024", true },
                    { 25, "Hà Nội", 250, "kh25@gmail.com", null, "Nữ", "Khách hàng 25", 30, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654025", true },
                    { 26, "Hà Nội", 260, "kh26@gmail.com", null, "Nam", "Khách hàng 26", 31, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654026", true },
                    { 27, "Hà Nội", 270, "kh27@gmail.com", null, "Nữ", "Khách hàng 27", 32, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 2, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654027", true },
                    { 28, "Hà Nội", 280, "kh28@gmail.com", null, "Nam", "Khách hàng 28", 33, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 2, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654028", true },
                    { 29, "Hà Nội", 290, "kh29@gmail.com", null, "Nữ", "Khách hàng 29", 34, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654029", true },
                    { 30, "Hà Nội", 300, "kh30@gmail.com", null, "Nam", "Khách hàng 30", 35, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2001, 3, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "0987654030", true }
                });

            migrationBuilder.InsertData(
                table: "SanTheThao",
                columns: new[] { "MaSan", "DiaChi", "DonGia", "GhiChu", "GioDongCua", "GioMoCua", "MaLoaiSan", "NgayBaoTri", "TenSan", "TienIch", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 200000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 2, null, "Sân số 1 - Sân Bóng Đá 5 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 2, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 80000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 3, null, "Sân số 2 - Sân Cầu Lông", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 3, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 150000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 4, null, "Sân số 3 - Sân Tennis", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 4, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 120000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 5, null, "Sân số 4 - Sân Bóng Rổ", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 5, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 300000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 1, null, "Sân số 5 - Sân Bóng Đá 7 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 6, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 200000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 2, null, "Sân số 6 - Sân Bóng Đá 5 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 7, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 80000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 3, null, "Sân số 7 - Sân Cầu Lông", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 8, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 150000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 4, null, "Sân số 8 - Sân Tennis", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 9, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 120000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 5, null, "Sân số 9 - Sân Bóng Rổ", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 10, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 300000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 1, null, "Sân số 10 - Sân Bóng Đá 7 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 11, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 200000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 2, null, "Sân số 11 - Sân Bóng Đá 5 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 12, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 80000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 3, null, "Sân số 12 - Sân Cầu Lông", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 13, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 150000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 4, null, "Sân số 13 - Sân Tennis", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 14, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 120000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 5, null, "Sân số 14 - Sân Bóng Rổ", "Wifi, Trà đá miễn phí, Chỗ để xe", true },
                    { 15, "Khu thể thao UNETI - Lĩnh Nam, Hoàng Mai", 300000m, null, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), 1, null, "Sân số 15 - Sân Bóng Đá 7 Người", "Wifi, Trà đá miễn phí, Chỗ để xe", true }
                });

            migrationBuilder.InsertData(
                table: "DatSan",
                columns: new[] { "MaDatSan", "DonGia", "GioBatDau", "GioKetThuc", "MaKhachHang", "MaSan", "NgayDat", "TienCoc", "TrangThai" },
                values: new object[,]
                {
                    { 1, 160000m, new DateTime(2026, 10, 2, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 2, 19, 0, 0, 0, DateTimeKind.Unspecified), 2, 2, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 2, 300000m, new DateTime(2026, 10, 3, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 3, 19, 0, 0, 0, DateTimeKind.Unspecified), 3, 3, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 3, 240000m, new DateTime(2026, 10, 4, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 4, 19, 0, 0, 0, DateTimeKind.Unspecified), 4, 4, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 4, 600000m, new DateTime(2026, 10, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 5, 19, 0, 0, 0, DateTimeKind.Unspecified), 5, 5, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 5, 400000m, new DateTime(2026, 10, 6, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 6, 19, 0, 0, 0, DateTimeKind.Unspecified), 6, 6, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 6, 160000m, new DateTime(2026, 10, 7, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 7, 19, 0, 0, 0, DateTimeKind.Unspecified), 7, 7, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 7, 300000m, new DateTime(2026, 10, 8, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 8, 19, 0, 0, 0, DateTimeKind.Unspecified), 8, 8, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 8, 240000m, new DateTime(2026, 10, 9, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 9, 19, 0, 0, 0, DateTimeKind.Unspecified), 9, 9, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 9, 600000m, new DateTime(2026, 10, 10, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 10, 19, 0, 0, 0, DateTimeKind.Unspecified), 10, 10, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 10, 400000m, new DateTime(2026, 10, 11, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 11, 19, 0, 0, 0, DateTimeKind.Unspecified), 11, 11, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 11, 160000m, new DateTime(2026, 10, 12, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 12, 19, 0, 0, 0, DateTimeKind.Unspecified), 12, 12, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 12, 300000m, new DateTime(2026, 10, 13, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 13, 19, 0, 0, 0, DateTimeKind.Unspecified), 13, 13, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 13, 240000m, new DateTime(2026, 10, 14, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 14, 19, 0, 0, 0, DateTimeKind.Unspecified), 14, 14, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 14, 600000m, new DateTime(2026, 10, 15, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 15, 19, 0, 0, 0, DateTimeKind.Unspecified), 15, 15, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 15, 400000m, new DateTime(2026, 10, 16, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 16, 19, 0, 0, 0, DateTimeKind.Unspecified), 16, 1, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 16, 160000m, new DateTime(2026, 10, 17, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 17, 19, 0, 0, 0, DateTimeKind.Unspecified), 17, 2, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 17, 300000m, new DateTime(2026, 10, 18, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 18, 19, 0, 0, 0, DateTimeKind.Unspecified), 18, 3, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 18, 240000m, new DateTime(2026, 10, 19, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 19, 19, 0, 0, 0, DateTimeKind.Unspecified), 19, 4, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 19, 600000m, new DateTime(2026, 10, 20, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 20, 19, 0, 0, 0, DateTimeKind.Unspecified), 20, 5, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 20, 400000m, new DateTime(2026, 10, 21, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 21, 19, 0, 0, 0, DateTimeKind.Unspecified), 21, 6, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 21, 160000m, new DateTime(2026, 10, 22, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 22, 19, 0, 0, 0, DateTimeKind.Unspecified), 22, 7, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 22, 300000m, new DateTime(2026, 10, 23, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 23, 19, 0, 0, 0, DateTimeKind.Unspecified), 23, 8, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 23, 240000m, new DateTime(2026, 10, 24, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 24, 19, 0, 0, 0, DateTimeKind.Unspecified), 24, 9, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 24, 600000m, new DateTime(2026, 10, 25, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 25, 19, 0, 0, 0, DateTimeKind.Unspecified), 25, 10, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 25, 400000m, new DateTime(2026, 10, 26, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 26, 19, 0, 0, 0, DateTimeKind.Unspecified), 26, 11, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 26, 160000m, new DateTime(2026, 10, 27, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 27, 19, 0, 0, 0, DateTimeKind.Unspecified), 27, 12, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 27, 300000m, new DateTime(2026, 10, 28, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 28, 19, 0, 0, 0, DateTimeKind.Unspecified), 28, 13, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 28, 240000m, new DateTime(2026, 10, 1, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 1, 19, 0, 0, 0, DateTimeKind.Unspecified), 29, 14, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 29, 600000m, new DateTime(2026, 10, 2, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 2, 19, 0, 0, 0, DateTimeKind.Unspecified), 30, 15, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 30, 400000m, new DateTime(2026, 10, 3, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 3, 19, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 31, 160000m, new DateTime(2026, 10, 4, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 4, 19, 0, 0, 0, DateTimeKind.Unspecified), 2, 2, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 32, 300000m, new DateTime(2026, 10, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 5, 19, 0, 0, 0, DateTimeKind.Unspecified), 3, 3, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 33, 240000m, new DateTime(2026, 10, 6, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 6, 19, 0, 0, 0, DateTimeKind.Unspecified), 4, 4, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 34, 600000m, new DateTime(2026, 10, 7, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 7, 19, 0, 0, 0, DateTimeKind.Unspecified), 5, 5, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 35, 400000m, new DateTime(2026, 10, 8, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 8, 19, 0, 0, 0, DateTimeKind.Unspecified), 6, 6, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 36, 160000m, new DateTime(2026, 10, 9, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 9, 19, 0, 0, 0, DateTimeKind.Unspecified), 7, 7, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 37, 300000m, new DateTime(2026, 10, 10, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 10, 19, 0, 0, 0, DateTimeKind.Unspecified), 8, 8, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 38, 240000m, new DateTime(2026, 10, 11, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 11, 19, 0, 0, 0, DateTimeKind.Unspecified), 9, 9, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 39, 600000m, new DateTime(2026, 10, 12, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 12, 19, 0, 0, 0, DateTimeKind.Unspecified), 10, 10, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 40, 400000m, new DateTime(2026, 10, 13, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 13, 19, 0, 0, 0, DateTimeKind.Unspecified), 11, 11, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 41, 160000m, new DateTime(2026, 10, 14, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 14, 19, 0, 0, 0, DateTimeKind.Unspecified), 12, 12, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" },
                    { 42, 300000m, new DateTime(2026, 10, 15, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 15, 19, 0, 0, 0, DateTimeKind.Unspecified), 13, 13, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Hoàn thành" },
                    { 43, 240000m, new DateTime(2026, 10, 16, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 16, 19, 0, 0, 0, DateTimeKind.Unspecified), 14, 14, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đã hủy" },
                    { 44, 600000m, new DateTime(2026, 10, 17, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 17, 19, 0, 0, 0, DateTimeKind.Unspecified), 15, 15, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Chờ xử lý" },
                    { 45, 400000m, new DateTime(2026, 10, 18, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 18, 19, 0, 0, 0, DateTimeKind.Unspecified), 16, 1, new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 100000m, "Đang xử lý" }
                });

            migrationBuilder.InsertData(
                table: "ChiTietDatSan",
                columns: new[] { "MaChiTiet", "DonGia", "GhiChu", "MaDatSan", "MaDichVu", "SoLuong", "ThanhTien", "TrangThai" },
                values: new object[,]
                {
                    { 1, 15000m, "Cung cấp đầy đủ", 1, 2, 2, 30000m, "Hoàn tất" },
                    { 2, 30000m, "Cung cấp đầy đủ", 2, 3, 3, 90000m, "Hoàn tất" },
                    { 3, 20000m, "Cung cấp đầy đủ", 3, 4, 1, 20000m, "Hoàn tất" },
                    { 4, 15000m, "Cung cấp đầy đủ", 4, 5, 2, 30000m, "Hoàn tất" },
                    { 5, 10000m, "Cung cấp đầy đủ", 5, 1, 3, 30000m, "Hoàn tất" },
                    { 6, 15000m, "Cung cấp đầy đủ", 6, 2, 1, 15000m, "Hoàn tất" },
                    { 7, 30000m, "Cung cấp đầy đủ", 7, 3, 2, 60000m, "Hoàn tất" },
                    { 8, 20000m, "Cung cấp đầy đủ", 8, 4, 3, 60000m, "Hoàn tất" },
                    { 9, 15000m, "Cung cấp đầy đủ", 9, 5, 1, 15000m, "Hoàn tất" },
                    { 10, 10000m, "Cung cấp đầy đủ", 10, 1, 2, 20000m, "Hoàn tất" },
                    { 11, 15000m, "Cung cấp đầy đủ", 11, 2, 3, 45000m, "Hoàn tất" },
                    { 12, 30000m, "Cung cấp đầy đủ", 12, 3, 1, 30000m, "Hoàn tất" },
                    { 13, 20000m, "Cung cấp đầy đủ", 13, 4, 2, 40000m, "Hoàn tất" },
                    { 14, 15000m, "Cung cấp đầy đủ", 14, 5, 3, 45000m, "Hoàn tất" },
                    { 15, 10000m, "Cung cấp đầy đủ", 15, 1, 1, 10000m, "Hoàn tất" },
                    { 16, 15000m, "Cung cấp đầy đủ", 16, 2, 2, 30000m, "Hoàn tất" },
                    { 17, 30000m, "Cung cấp đầy đủ", 17, 3, 3, 90000m, "Hoàn tất" },
                    { 18, 20000m, "Cung cấp đầy đủ", 18, 4, 1, 20000m, "Hoàn tất" },
                    { 19, 15000m, "Cung cấp đầy đủ", 19, 5, 2, 30000m, "Hoàn tất" },
                    { 20, 10000m, "Cung cấp đầy đủ", 20, 1, 3, 30000m, "Hoàn tất" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatSan_MaDatSan",
                table: "ChiTietDatSan",
                column: "MaDatSan");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatSan_MaDichVu",
                table: "ChiTietDatSan",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_DatSan_MaKhachHang",
                table: "DatSan",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_DatSan_MaSan",
                table: "DatSan",
                column: "MaSan");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_MaTaiKhoan",
                table: "KhachHang",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SanTheThao_MaLoaiSan",
                table: "SanTheThao",
                column: "MaLoaiSan");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDatSan");

            migrationBuilder.DropTable(
                name: "DatSan");

            migrationBuilder.DropTable(
                name: "DichVu");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "SanTheThao");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "LoaiSan");
        }
    }
}
