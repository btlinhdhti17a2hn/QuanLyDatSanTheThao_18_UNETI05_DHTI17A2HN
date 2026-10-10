// Họ và tên: Bùi Thùy Linh
// Mã sinh viên: 23103100108
// Nội dung thực hiện: Module 2 - ViewModel cho danh sách sân (tìm kiếm, lọc, sắp xếp, phân trang)
//                     và trang chi tiết sân.

using System.Globalization;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class SanTheThaoIndexViewModel
    {
        // ----- Kết quả -----
        public List<SanTheThao> DanhSach { get; set; } = new();
        public List<LoaiSan> LoaiSans { get; set; } = new();   // dữ liệu cho <select> lọc loại sân

        // ----- Điều kiện tìm kiếm / lọc / sắp xếp -----
        public string? TuKhoa { get; set; }
        public int? MaLoaiSan { get; set; }
        public string? TrangThai { get; set; }        // "hoatdong" | "ngung" | null
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        public DateTime? NgaySuDung { get; set; }
        public TimeSpan? GioBatDau { get; set; }
        public TimeSpan? GioKetThuc { get; set; }
        public bool ConTrong { get; set; }
        public string SapXep { get; set; } = "ten_asc";

        // ----- Phân trang -----
        public int TrangHienTai { get; set; } = 1;
        public int TongTrang { get; set; }
        public int TongKetQua { get; set; }
        public int KichThuocTrang { get; set; }

        // ----- Thống kê nhanh (toàn bộ bảng, không phụ thuộc bộ lọc) -----
        public int TongSan { get; set; }
        public int SanKhaDung { get; set; }
        public int SanKhongKhaDung { get; set; }

        public string? CanhBao { get; set; }

        // Có đang áp dụng điều kiện lọc nào không?
        public bool CoLoc =>
            !string.IsNullOrWhiteSpace(TuKhoa) || MaLoaiSan.HasValue || !string.IsNullOrEmpty(TrangThai)
            || GiaTu.HasValue || GiaDen.HasValue || NgaySuDung.HasValue || ConTrong;

        /// <summary>
        /// Tạo bộ tham số URL (giữ nguyên toàn bộ điều kiện) khi chuyển sang trang khác.
        /// Dùng với asp-all-route-data ở nút phân trang.
        /// </summary>
        public Dictionary<string, string> TaoRouteData(int trang)
        {
            var d = new Dictionary<string, string>
            {
                ["trang"] = trang.ToString(),
                ["sapXep"] = SapXep
            };
            if (!string.IsNullOrWhiteSpace(TuKhoa)) d["tuKhoa"] = TuKhoa!;
            if (MaLoaiSan.HasValue) d["maLoaiSan"] = MaLoaiSan.Value.ToString();
            if (!string.IsNullOrEmpty(TrangThai)) d["trangThai"] = TrangThai!;
            if (GiaTu.HasValue) d["giaTu"] = GiaTu.Value.ToString("0", CultureInfo.InvariantCulture);
            if (GiaDen.HasValue) d["giaDen"] = GiaDen.Value.ToString("0", CultureInfo.InvariantCulture);
            if (NgaySuDung.HasValue) d["ngaySuDung"] = NgaySuDung.Value.ToString("yyyy-MM-dd");
            if (GioBatDau.HasValue) d["gioBatDau"] = GioBatDau.Value.ToString(@"hh\:mm");
            if (GioKetThuc.HasValue) d["gioKetThuc"] = GioKetThuc.Value.ToString(@"hh\:mm");
            if (ConTrong) d["conTrong"] = "true";
            return d;
        }
    }

    // Một dòng lịch đặt hiển thị ở trang chi tiết sân
    public class LichDatItem
    {
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }
        public string TrangThai { get; set; } = "";
        public string? TenKhach { get; set; }
    }

    public class SanTheThaoDetailsViewModel
    {
        public SanTheThao San { get; set; } = null!;
        public List<LichDatItem> LichSapToi { get; set; } = new();
        public int SoLuotDatHoanThanh { get; set; }
    }
}