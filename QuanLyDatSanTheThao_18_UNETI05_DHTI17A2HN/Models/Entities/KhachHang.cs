// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Entity KhachHang - Quản lý khách hàng

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int MaKhachHang { get; set; }

        public int? MaTaiKhoan { get; set; }
        [ForeignKey("MaTaiKhoan")]
        public virtual TaiKhoan? TaiKhoan { get; set; }

        [Required(ErrorMessage = "Họ tên bắt buộc nhập")]
        [StringLength(100)]
        public string HoTen { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        public string? GioiTinh { get; set; }

        [Required(ErrorMessage = "Số điện thoại bắt buộc nhập")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        public string SoDienThoai { get; set; } = null!;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        [Range(0, int.MaxValue)]
        public int DiemTichLuy { get; set; } = 0;

        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }

        public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
    }
}
