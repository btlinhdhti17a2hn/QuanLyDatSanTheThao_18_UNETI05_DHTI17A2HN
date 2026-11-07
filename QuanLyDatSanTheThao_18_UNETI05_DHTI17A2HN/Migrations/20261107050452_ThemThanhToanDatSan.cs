using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemThanhToanDatSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiemCong",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "DiemSuDung",
                table: "DatSan");

            migrationBuilder.RenameColumn(
                name: "TienGiamDiem",
                table: "DatSan",
                newName: "SoTienDaThu");

            migrationBuilder.AddColumn<bool>(
                name: "DaThanhToan",
                table: "DatSan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDichNganHang",
                table: "DatSan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayThanhToan",
                table: "DatSan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiXacNhanThanhToan",
                table: "DatSan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 1,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 2,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 3,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 4,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 5,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 6,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 7,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 8,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 9,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 10,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 11,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 12,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 13,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 14,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 15,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 16,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 17,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 18,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 19,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 20,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 21,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 22,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 23,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 24,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 25,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 26,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 27,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 28,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 29,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 30,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 31,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 32,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 33,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 34,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 35,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 36,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 37,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 38,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 39,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 40,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 41,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 42,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 43,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 44,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 45,
                columns: new[] { "DaThanhToan", "MaGiaoDichNganHang", "NgayThanhToan", "NguoiXacNhanThanhToan" },
                values: new object[] { false, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaThanhToan",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "MaGiaoDichNganHang",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "NgayThanhToan",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "NguoiXacNhanThanhToan",
                table: "DatSan");

            migrationBuilder.RenameColumn(
                name: "SoTienDaThu",
                table: "DatSan",
                newName: "TienGiamDiem");

            migrationBuilder.AddColumn<int>(
                name: "DiemCong",
                table: "DatSan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiemSuDung",
                table: "DatSan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 1,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 2,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 3,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 4,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 5,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 6,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 7,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 8,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 9,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 10,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 11,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 12,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 13,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 14,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 15,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 16,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 17,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 18,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 19,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 20,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 21,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 22,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 23,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 24,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 25,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 26,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 27,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 28,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 29,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 30,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 31,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 32,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 33,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 34,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 35,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 36,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 37,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 38,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 39,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 40,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 41,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 42,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 43,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 44,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "DatSan",
                keyColumn: "MaDatSan",
                keyValue: 45,
                columns: new[] { "DiemCong", "DiemSuDung" },
                values: new object[] { 0, 0 });
        }
    }
}
