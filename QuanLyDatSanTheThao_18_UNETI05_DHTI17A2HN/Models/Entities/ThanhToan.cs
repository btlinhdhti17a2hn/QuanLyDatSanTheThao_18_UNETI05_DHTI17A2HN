// Họ và tên: Nguyễn Thị Lan Nhi
// Mã sinh viên: 23103100104
// Nội dung thực hiện: Entity ThanhToan - Quản lý trạng thái thanh toán và mô phỏng giao dịch đặt cọc
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("ThanhToan")]
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        [Required]
        public int MaDatSan { get; set; }
        [ForeignKey("MaDatSan")]
        public virtual DatSan DatSan { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal SoTien { get; set; }

        [Required]
        [StringLength(50)]
        public string PhuongThuc { get; set; } = "Chuyển khoản QR";

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Chưa thanh toán"; // "Chưa thanh toán" | "Chờ xác nhận" | "Đã thanh toán"

        public DateTime? NgayThanhToan { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }

        [StringLength(100)]
        public string? MaGiaoDich { get; set; }
    }
}
