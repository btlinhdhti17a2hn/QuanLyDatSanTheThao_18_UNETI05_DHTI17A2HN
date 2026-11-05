// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: ViewModel phục vụ Quy trình thanh toán tiền cọc mô phỏng qua mã QR (hỗ trợ quét mã mở trang web trên điện thoại)
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class ThanhToanViewModel
    {
        public int MaDatSan { get; set; }
        public int MaThanhToan { get; set; }

        public string TenSan { get; set; } = "";
        public string TenLoaiSan { get; set; } = "";
        public DateTime NgaySuDung { get; set; }
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }

        public string HoTenKhachHang { get; set; } = "";
        public string SoDienThoai { get; set; } = "";

        public decimal TongTien { get; set; }
        public decimal TienCoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
        [Range(1000, 100000000, ErrorMessage = "Số tiền thanh toán phải lớn hơn 1.000 VNĐ")]
        [Display(Name = "Số tiền thanh toán (VNĐ)")]
        public decimal SoTienThanhToan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức")]
        public string PhuongThuc { get; set; } = "Chuyển khoản QR";

        [Required(ErrorMessage = "Vui lòng nhập nội dung chuyển khoản")]
        [Display(Name = "Nội dung chuyển khoản")]
        public string NoiDungChuyenKhoan { get; set; } = "";

        public string TrangThaiThanhToan { get; set; } = "Chưa thanh toán";
        public string TrangThaiDatSan { get; set; } = "Chờ duyệt";
        public DateTime? NgayThanhToan { get; set; }
        public string? MaGiaoDich { get; set; }

        [Display(Name = "Ghi chú giao dịch")]
        public string? GhiChu { get; set; }

        // Thông tin tài khoản nhận tiền mô phỏng của trung tâm thể thao
        public string TenNganHang { get; set; } = "Ngân hàng Quân Đội (MB Bank)";
        public string SoTaiKhoan { get; set; } = "0987654321";
        public string ChuTaiKhoan { get; set; } = "TRUNG TAM THE THAO UNETI";

        // Đường dẫn URL trang thanh toán di động
        public string MobileUrl { get; set; } = "";

        // Mã QR mã hóa đường link URL trang web di động (để điện thoại quét mở trang thanh toán)
        public string MobileQrCodeUrl =>
            string.IsNullOrEmpty(MobileUrl) ? "" :
            $"https://api.qrserver.com/v1/create-qr-code/?size=260x260&margin=10&data={Uri.EscapeDataString(MobileUrl)}";

        // URL sinh mã QR tài khoản ngân hàng (VietQR)
        public string VietQrCodeUrl =>
            $"https://api.vietqr.io/image/970422-{SoTaiKhoan}-compact2.png?amount={(long)SoTienThanhToan}&addInfo={Uri.EscapeDataString(NoiDungChuyenKhoan)}&accountName={Uri.EscapeDataString(ChuTaiKhoan)}";
    }
}
