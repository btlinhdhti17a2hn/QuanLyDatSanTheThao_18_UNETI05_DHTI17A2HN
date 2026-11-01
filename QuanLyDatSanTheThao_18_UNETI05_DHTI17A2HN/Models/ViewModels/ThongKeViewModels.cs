// Họ và tên: Tạ Đình Kim 
// Mã sinh viên: 23103100315
// Nội dung thực hiện: ViewModel cho Dashboard quản trị và trang Thống kê LINQ.

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TongLoaiSan { get; set; }
        public int TongSan { get; set; }
        public int SanKhaDung { get; set; }
        public int TongKhachHang { get; set; }
        public int TongDatSan { get; set; }
        public int DonChoXuLy { get; set; }
        public int DonDangXuLy { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonHomNay { get; set; }
        public decimal DoanhThuHoanThanh { get; set; }      // đơn đã hoàn thành
        public decimal DoanhThuTamTinhHomNay { get; set; }  // đơn hôm nay chưa hủy
    }

    // Một dòng thống kê (tên nhóm, số lượng, giá trị tiền, tỷ lệ %)
    public class ThongKeItem
    {
        public string Ten { get; set; } = "";
        public int SoLuong { get; set; }
        public decimal GiaTri { get; set; }
        public double TyLe { get; set; }
    }

    // Dữ liệu 1 đơn đặt sân đã tính sẵn tiền (dùng cho thống kê doanh thu)
    public class DonTien
    {
        public int MaDatSan { get; set; }
        public DateTime NgayDat { get; set; }
        public string TenLoai { get; set; } = "";
        public decimal TongTien { get; set; }
    }

    // Một đơn bị hủy (hiện trong cửa sổ "Xem danh sách đơn hủy")
    public class DonHuyItem
    {
        public int MaDatSan { get; set; }
        public string TenSan { get; set; } = "";
        public DateTime NgayDat { get; set; }
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }
        public decimal DonGia { get; set; }
    }

    // Model cho partial _BangThongKe và _BieuDoCot
    public class BangThongKe
    {
        public string TieuDe { get; set; } = "";
        public string MoTa { get; set; } = "";      // để trống: tự hiện "Tổng cộng ..."
        public string DonVi { get; set; } = "";
        public bool LaTien { get; set; }
        public int TopN { get; set; } = 5;
        public bool XepHang { get; set; } = true;   // false: giữ nguyên thứ tự (vd. khung giờ)
        public List<ThongKeItem> Items { get; set; } = new();
    }

    public class ThongKeViewModel
    {
        public int Nam { get; set; }
        public List<int> DanhSachNam { get; set; } = new();

        // ----- Bộ lọc thời gian -----
        public string Kieu { get; set; } = "nam";   // homnay | tuannay | thangnay | quynay | nam | tuychon
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        // ----- So sánh với kỳ trước -----
        public DateTime TuNgayTruoc { get; set; }
        public DateTime DenNgayTruoc { get; set; }
        public decimal DoanhThuKyTruoc { get; set; }
        public int LuotDatKyTruoc { get; set; }

        // Mỗi cột biểu đồ đại diện cho: "ngày" | "tuần" | "tháng"
        public string DonViBieuDo { get; set; } = "tháng";

        public List<ThongKeItem> SanTheoLoai { get; set; } = new();
        public List<ThongKeItem> LuotDatTheoSan { get; set; } = new();
        public ThongKeItem? SanNhieuNhat { get; set; }
        public List<ThongKeItem> SanItKhach { get; set; } = new();   // sân ít khách nhất (kể cả sân 0 lượt)
        public List<ThongKeItem> LuotDatTheoLoai { get; set; } = new();
        public ThongKeItem? LoaiSanUaThich { get; set; }
        public List<ThongKeItem> DoanhThuTheoThang { get; set; } = new();
        public List<ThongKeItem> DoanhThuTheoLoai { get; set; } = new();
        public decimal TongDoanhThuNam { get; set; }
        public int SoDonHoanThanh { get; set; }
        public List<ThongKeItem> DichVuPhoBien { get; set; } = new();
        public int SoLuotHuy { get; set; }
        public int TongLuotDat { get; set; }
        public double TyLeHuy => TongLuotDat == 0 ? 0 : Math.Round(SoLuotHuy * 100.0 / TongLuotDat, 1);
        public List<DonHuyItem> DonHuy { get; set; } = new();
        public List<ThongKeItem> KhungGio { get; set; } = new();
        public ThongKeItem? KhungGioCaoDiem { get; set; }
    }
}
