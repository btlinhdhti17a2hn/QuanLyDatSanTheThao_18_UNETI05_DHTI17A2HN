// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Entity SanTheThao - Quản lý sân thể thao
//
// Họ và tên: Bùi Thùy Linh
// Mã sinh viên: 23103100108
// Nội dung thực hiện: Module 2 - Bổ sung [Display], thông báo Validation tiếng Việt
//                     và thuộc tính tính toán trạng thái khả dụng của sân.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("SanTheThao")]
    public class SanTheThao
    {
        [Key]
        public int MaSan { get; set; }

        [Display(Name = "Tên sân")]
        [Required(ErrorMessage = "Tên sân bắt buộc nhập")]
        [StringLength(100, ErrorMessage = "Tên sân tối đa 100 ký tự")]
        public string TenSan { get; set; } = null!;

        [Display(Name = "Loại sân")]
        [Required(ErrorMessage = "Vui lòng chọn loại sân")]
        public int MaLoaiSan { get; set; }
        [ForeignKey("MaLoaiSan")]
        public virtual LoaiSan LoaiSan { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "Địa chỉ bắt buộc nhập")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string DiaChi { get; set; } = null!;

        [Display(Name = "Tiện ích")]
        public string? TienIch { get; set; }

        [Display(Name = "Đơn giá")]
        [Required(ErrorMessage = "Đơn giá bắt buộc nhập")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        public decimal DonGia { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        [Display(Name = "Giờ mở cửa")]
        [Required(ErrorMessage = "Giờ mở cửa bắt buộc nhập")]
        [DataType(DataType.Time)]
        public TimeSpan GioMoCua { get; set; }

        [Display(Name = "Giờ đóng cửa")]
        [Required(ErrorMessage = "Giờ đóng cửa bắt buộc nhập")]
        [DataType(DataType.Time)]
        public TimeSpan GioDongCua { get; set; }

        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        [Display(Name = "Ngày bảo trì")]
        public DateTime? NgayBaoTri { get; set; }

        public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();

        // ------------------------------------------------------------------
        // CÁC THUỘC TÍNH TÍNH TOÁN (không lưu vào CSDL => không cần Migration)
        // ------------------------------------------------------------------

        /// <summary>Sân đang bảo trì khi ngày bảo trì trùng với hôm nay.</summary>
        [NotMapped]
        public bool DangBaoTri => NgayBaoTri.HasValue && NgayBaoTri.Value.Date == DateTime.Today;

        /// <summary>Sân khả dụng = đang hoạt động và không đúng ngày bảo trì.</summary>
        [NotMapped]
        public bool DangKhaDung => TrangThai && !DangBaoTri;

        /// <summary>Chữ hiển thị trạng thái cho người dùng.</summary>
        [NotMapped]
        public string TinhTrang => !TrangThai ? "Ngừng hoạt động" : (DangBaoTri ? "Đang bảo trì" : "Còn khả dụng");
    }
}