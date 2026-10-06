// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Entity DichVu - Quản lý dịch vụ

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        [StringLength(100)]
        public string TenDichVu { get; set; } = null!;

        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Đơn giá bắt buộc nhập")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        public decimal DonGia { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();
    }
}
