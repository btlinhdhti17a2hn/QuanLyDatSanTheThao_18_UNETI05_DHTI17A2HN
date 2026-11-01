// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: ViewModel hồ sơ khách hàng - Hiển thị và chỉnh sửa thông tin cá nhân kèm Validation

using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class HoSoKhachHangViewModel
    {
        public int MaKhachHang { get; set; }
        public int? MaTaiKhoan { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string? TenDangNhap { get; set; }

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Họ và tên bắt buộc nhập")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
        public string HoTen { get; set; } = null!;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Số điện thoại bắt buộc nhập")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0")]
        public string SoDienThoai { get; set; } = null!;

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? DiaChi { get; set; }

        [Display(Name = "Ngày đăng ký")]
        public DateTime NgayDangKy { get; set; }

        [Display(Name = "Điểm tích lũy")]
        public int DiemTichLuy { get; set; }

        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
    }
}
