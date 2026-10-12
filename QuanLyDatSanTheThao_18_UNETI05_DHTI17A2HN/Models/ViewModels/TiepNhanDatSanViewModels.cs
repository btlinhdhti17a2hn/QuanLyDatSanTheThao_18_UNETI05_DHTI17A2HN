// Họ và tên: Vũ Ánh Ngọc
// Mã sinh viên: 23103100106
// Nội dung thực hiện: Module 4 - ViewModel Tiếp nhận đặt sân (danh sách xử lý có tìm kiếm/lọc/sắp xếp/phân trang,
//                     chi tiết đơn kèm kết quả kiểm tra điều kiện trước khi xử lý).

using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    // ==========================================================
    // 1. VIEWMODEL CHO TRANG DANH SÁCH CẦN XỬ LÝ
    // ==========================================================
    public class TiepNhanDatSanListViewModel
    {
        // Dữ liệu của trang hiện tại (đã phân trang bằng Skip/Take)
        public List<DatSan> DanhSach { get; set; } = new();

        // Dữ liệu nền cho ô lọc "Loại sân" (lấy từ Database)
        public List<LoaiSan> DanhSachLoaiSan { get; set; } = new();

        // Các điều kiện tìm kiếm - lọc - sắp xếp (giữ lại để hiển thị lại trên form và khi chuyển trang)
        public string? TenKhach { get; set; }
        public string? TenSan { get; set; }
        public int? MaLoaiSan { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string SapXep { get; set; } = "ngaydat_moi";

        // Thông tin phân trang
        public int TrangHienTai { get; set; } = 1;
        public int TongSoTrang { get; set; } = 1;
        public int TongSoBanGhi { get; set; }

        // Thống kê nhanh theo trạng thái (toàn bộ đơn, không phụ thuộc bộ lọc)
        public int SoChoXuLy { get; set; }
        public int SoDangXuLy { get; set; }
        public int SoHoanThanh { get; set; }
        public int SoDaHuy { get; set; }

        // Thông báo khi bộ lọc được tự điều chỉnh (ví dụ: từ ngày > đến ngày)
        public string? CanhBaoLoc { get; set; }
    }

    // ==========================================================
    // 2. KẾT QUẢ KIỂM TRA ĐIỀU KIỆN TRƯỚC KHI XÁC NHẬN ĐƠN (Mục 8.3, 8.5)
    // ==========================================================
    public class KetQuaKiemTraDonDat
    {
        // Lỗi BẮT BUỘC phải xử lý - còn lỗi thì KHÔNG cho xác nhận
        public List<string> Loi { get; set; } = new();

        // Cảnh báo/thông tin cho nhân viên tham khảo
        public List<string> CanhBao { get; set; } = new();

        // Các đơn đang trùng khung giờ với đơn này (cùng sân)
        public List<DatSan> DonTrungLich { get; set; } = new();

        // Số lượt đặt khác của sân trong ngày (không tính đơn đã hủy)
        public int SoLuotTrongNgay { get; set; }
    }

    // ==========================================================
    // 3. VIEWMODEL CHO TRANG CHI TIẾT / XỬ LÝ MỘT ĐƠN
    // ==========================================================
    public class TiepNhanDatSanChiTietViewModel
    {
        public DatSan DonDat { get; set; } = null!;

        public double SoGio { get; set; }
        public decimal TienSan { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TongTien { get; set; }

        public KetQuaKiemTraDonDat KetQua { get; set; } = new();
        public int GioiHanLuotMoiNgay { get; set; }

        // Nút thao tác nào được phép hiển thị/thực hiện theo trạng thái hiện tại
        public bool CoTheXacNhan { get; set; }
        public bool CoTheHoanThanh { get; set; }
        public bool CoTheHuy { get; set; }
    }
}