// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Entity DatSan - Xử lý đặt sân
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities
{
    [Table("DatSan")]
    public class DatSan
    {
        [Key]
        public int MaDatSan { get; set; }

        [Required]
        public int MaKhachHang { get; set; }
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang KhachHang { get; set; } = null!;

        [Required]
        public int MaSan { get; set; }
        [ForeignKey("MaSan")]
        public virtual SanTheThao SanTheThao { get; set; } = null!;

        [Required]
        public DateTime NgayDat { get; set; } = DateTime.Now;

        [Required]
        public DateTime GioBatDau { get; set; }

        [Required]
        public DateTime GioKetThuc { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DonGia { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TienCoc { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xử lý";

        // ===== ĐIỂM TÍCH LŨY (ghi nhận khi đơn được Hoàn thành - không map vào bảng CSDL) =====
        // Số điểm khách đã dùng để giảm giá cho đơn này
        [NotMapped]
        [Range(0, int.MaxValue)]
        public int DiemSuDung { get; set; } = 0;

        // Số tiền được giảm từ điểm (1.000 điểm = 10.000đ)
        [NotMapped]
        [Range(0, double.MaxValue)]
        public decimal TienGiamDiem { get; set; } = 0;

        // Số điểm được cộng thêm cho khách từ đơn này (100.000đ = 10 điểm)
        [NotMapped]
        [Range(0, int.MaxValue)]
        public int DiemCong { get; set; } = 0;

        // ===== ĐIỀU KIỆN HOÀN THÀNH ĐƠN (không lưu vào database) =====
        // Chỉ được hoàn thành khi khách đã sử dụng sân ít nhất 80% tổng thời gian đã đặt
        public const double TyLeThoiGianToiThieu = 0.8;

        // Thời điểm sớm nhất được phép hoàn thành = giờ bắt đầu + 80% thời lượng đặt
        [NotMapped]
        public DateTime MocDuocHoanThanh
        {
            get
            {
                var thoiLuong = GioKetThuc - GioBatDau;
                if (thoiLuong < TimeSpan.Zero) thoiLuong = TimeSpan.Zero;
                return GioBatDau + TimeSpan.FromTicks((long)(thoiLuong.Ticks * TyLeThoiGianToiThieu));
            }
        }

        public bool DuThoiGianHoanThanh(DateTime now) => now >= MocDuocHoanThanh;

        public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();
    }
}