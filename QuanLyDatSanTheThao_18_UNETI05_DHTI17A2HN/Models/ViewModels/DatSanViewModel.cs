// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Module 3 - ViewModel Đặt sân thể thao (Kết hợp dữ liệu sân, ngày giờ đặt, tiền cọc và validation)

using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class DatSanViewModel
    {
        [Display(Name = "Sân thể thao")]
        [Required(ErrorMessage = "Vui lòng chọn sân thể thao muốn đặt")]
        public int MaSan { get; set; }

        [Display(Name = "Ngày sử dụng")]
        [Required(ErrorMessage = "Vui lòng chọn ngày sử dụng sân")]
        [DataType(DataType.Date)]
        public DateTime NgaySuDung { get; set; } = DateTime.Today;

        [Display(Name = "Giờ bắt đầu")]
        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan GioBatDau { get; set; }

        [Display(Name = "Giờ kết thúc")]
        [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan GioKetThuc { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
        public string? GhiChu { get; set; }

        // Các thuộc tính phụ phục vụ hiển thị và tính toán
        public string? TenSan { get; set; }
        public string? TenLoaiSan { get; set; }
        public decimal DonGiaTheoGio { get; set; }
        public decimal TongTienDuKien { get; set; }
        public decimal TienCocDuKien { get; set; }
    }
}
