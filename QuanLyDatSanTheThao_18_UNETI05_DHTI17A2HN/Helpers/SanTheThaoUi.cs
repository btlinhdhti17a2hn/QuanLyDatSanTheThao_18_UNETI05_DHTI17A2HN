// Họ và tên: Bùi Thùy Linh
// Mã sinh viên: 23103100108
// Nội dung thực hiện: Module 2 - Lớp hỗ trợ giao diện cho sân thể thao
//                     (chọn icon/màu theo loại sân, định dạng giờ - tiền, tách tiện ích).

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Helpers
{
    public static class SanTheThaoUi
    {
        // Icon theo tên loại sân
        public static string Icon(string? tenLoai)
        {
            var t = (tenLoai ?? "").ToLower();
            if (t.Contains("bóng đá")) return "⚽";
            if (t.Contains("bóng rổ")) return "🏀";
            if (t.Contains("cầu lông")) return "🏸";
            if (t.Contains("tennis")) return "🎾";
            if (t.Contains("pickleball")) return "🏓";
            if (t.Contains("bóng chuyền")) return "🏐";
            return "🏟️";
        }

        // Tên class màu nền (khai báo trong santhethao.css)
        public static string Theme(string? tenLoai)
        {
            var t = (tenLoai ?? "").ToLower();
            if (t.Contains("bóng đá")) return "th-green";
            if (t.Contains("bóng rổ")) return "th-orange";
            if (t.Contains("cầu lông")) return "th-blue";
            if (t.Contains("tennis")) return "th-purple";
            if (t.Contains("pickleball")) return "th-teal";
            return "th-green";
        }

        // 06:00
        public static string Gio(TimeSpan t) => t.ToString(@"hh\:mm");

        // 300,000 đ
        public static string Tien(decimal v) => v.ToString("N0") + " đ";

        // Tách chuỗi tiện ích "Wifi, Trà đá, Chỗ để xe" thành mảng
        public static string[] TachTienIch(string? tienIch)
        {
            if (string.IsNullOrWhiteSpace(tienIch)) return Array.Empty<string>();
            return tienIch.Split(new[] { ',', ';' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Class màu cho trạng thái đặt sân (dùng ở trang Chi tiết)
        public static string MauTrangThaiDat(string? trangThai) => trangThai switch
        {
            "Chờ xử lý" => "st-cho",
            "Đang xử lý" => "st-dang",
            "Hoàn thành" => "st-xong",
            _ => "st-khac"
        };
    }
}