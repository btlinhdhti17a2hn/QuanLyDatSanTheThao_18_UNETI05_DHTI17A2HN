// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Entity SanTheThao - Quản lý sân thể thao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("SanTheThao")]
    public class SanTheThao
    {
        [Key]
        public int MaSan { get; set; }

        [Required(ErrorMessage = "Tên sân bắt buộc nhập")]
        [StringLength(100)]
        public string TenSan { get; set; } = null!;

        [Required]
        public int MaLoaiSan { get; set; }
        [ForeignKey("MaLoaiSan")]
        public virtual LoaiSan LoaiSan { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ bắt buộc nhập")]
        [StringLength(255)]
        public string DiaChi { get; set; } = null!;

        public string? TienIch { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá >= 0")]
        public decimal DonGia { get; set; }

        public bool TrangThai { get; set; } = true;

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan GioMoCua { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan GioDongCua { get; set; }

        public string? GhiChu { get; set; }
        public DateTime? NgayBaoTri { get; set; }

        public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
    }
}
