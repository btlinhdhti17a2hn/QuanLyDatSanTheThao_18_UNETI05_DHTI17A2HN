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
        public bool CotCuoiDangDienRa { get; set; } // true: cột cuối của biểu đồ là kỳ chưa kết thúc (VD tháng hiện tại)
    }

    // Một đơn hoàn thành đóng góp vào doanh thu (hiện ở tab Doanh thu)
    public class DonDongGopItem
    {
        public int MaDatSan { get; set; }
        public string TenSan { get; set; } = "";
        public string TenLoai { get; set; } = "";
        public DateTime NgayDat { get; set; }
        public DateTime GioBatDau { get; set; }
        public decimal DonGia { get; set; }
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
        public int DonHoanThanhKyTruoc { get; set; }   // số đơn "Hoàn thành" của kỳ trước (để so sánh thẻ Đơn hoàn thành)

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
        public List<ThongKeItem> DoanhThuTheoThu { get; set; } = new();    // đơn hoàn thành theo thứ trong tuần (ngày sử dụng sân), Thứ 2 đến Chủ nhật
        public List<ThongKeItem> DoanhThuTheoBuoi { get; set; } = new();   // đơn hoàn thành theo buổi sáng / chiều / tối (giờ bắt đầu)
        public List<DonDongGopItem> DonHoanThanhDs { get; set; } = new();  // đơn hoàn thành, số tiền lớn nhất trước (tối đa 200)
        public decimal TongDoanhThuNam { get; set; }
        public int SoDonHoanThanh { get; set; }
        public List<ThongKeItem> DichVuPhoBien { get; set; } = new();
        public int SoLuotHuy { get; set; }
        public int TongLuotDat { get; set; }
        public double TyLeHuy => TongLuotDat == 0 ? 0 : Math.Round(SoLuotHuy * 100.0 / TongLuotDat, 1);
        public List<DonHuyItem> DonHuy { get; set; } = new();
        public List<ThongKeItem> KhungGio { get; set; } = new();
        public ThongKeItem? KhungGioCaoDiem { get; set; }

        // Tab "Khách hàng"
        public ThongKeKhachHangViewModel KhachHang { get; set; } = new();
    }

    // ================== TAB KHÁCH HÀNG ==================

    // Một dòng trong bảng Top khách hàng
    public class KhachVipItem
    {
        public int MaKhachHang { get; set; }
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public int SoLuot { get; set; }              // số lượt đặt (không tính đơn hủy)
        public decimal DoanhThu { get; set; }        // cộng DonGia các đơn "Hoàn thành"
        public int DiemTichLuy { get; set; }
        public string Hang { get; set; } = "";
    }

    // Một hạng thành viên (suy ra từ điểm tích lũy)
    public class HangKhachItem
    {
        public string Ten { get; set; } = "";
        public string MoTa { get; set; } = "";       // ví dụ "100 - 499 điểm"
        public string Css { get; set; } = "";        // tên class màu: tv | bac | vang | kc
        public int SoLuong { get; set; }             // số khách hiện thuộc hạng này (tích lũy, không theo kỳ)
        public double TyLe { get; set; }             // % trên tổng số khách

        // Phần dưới tính TRONG KỲ lọc
        public int KhachCoChiTieu { get; set; }      // số khách của hạng có doanh thu hoàn thành trong kỳ
        public decimal DoanhThu { get; set; }        // doanh thu hoàn thành của hạng trong kỳ
        public decimal ChiTieuTrungBinh { get; set; } // DoanhThu / KhachCoChiTieu
        public double TyTrongDoanhThu { get; set; }  // % trên tổng doanh thu kỳ
    }

    // Số liệu đơn đặt / hủy theo từng cột thời gian (ngày/tuần/tháng)
    public class HuyKyItem
    {
        public string Ten { get; set; } = "";
        public int TongDon { get; set; }
        public int SoHuy { get; set; }
        public double TyLe { get; set; }             // % hủy trong cột
    }

    // Một dòng trong bảng khách hay hủy đơn
    public class KhachHuyItem
    {
        public int MaKhachHang { get; set; }
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public int SoDon { get; set; }               // tổng đơn trong kỳ (kể cả hủy)
        public int SoHuy { get; set; }
        public double TyLeHuy { get; set; }          // % đơn hủy của riêng khách này
        public bool CanLuuY { get; set; }            // hủy >= 2 lần và tỷ lệ >= 50%
    }

    // Khách có nguy cơ rời bỏ: từng đặt từ 2 lần nhưng đã lâu chưa quay lại
    public class KhachNguyCoItem
    {
        public int MaKhachHang { get; set; }
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string Hang { get; set; } = "";
        public int SoDon { get; set; }               // tổng đơn không hủy toàn thời gian (đến cuối kỳ)
        public decimal TongChiTieu { get; set; }     // doanh thu đơn hoàn thành toàn thời gian (đến cuối kỳ)
        public DateTime LanDatGanNhat { get; set; }
        public int SoNgayChuaQuayLai { get; set; }   // tính từ lần đặt gần nhất đến cuối kỳ
    }

    // Một hàng của bảng giữ chân khách theo nhóm tháng (cohort)
    public class CohortItem
    {
        public string Ten { get; set; } = "";        // nhóm khách có đơn đầu tiên trong tháng này, ví dụ "T9/2026"
        public int SoKhach { get; set; }
        // TyLe[k] = % khách của nhóm có đơn ở tháng thứ k sau tháng đầu (k = 0 là 100%).
        // null = chưa đủ thời gian quan sát (tháng chưa kết thúc), KHÔNG phải 0%.
        public List<double?> TyLe { get; set; } = new();
    }

    public class ThongKeKhachHangViewModel
    {
        // --- Khách mới thực sự và chuyển đổi ---
        public int KhachMoiThucSu { get; set; }          // khách có đơn (không hủy) ĐẦU TIÊN trong lịch sử rơi vào kỳ này
        public int KhachMoiThucSuTruoc { get; set; }
        public int SoDangKyDuThoiGian { get; set; }      // tài khoản đăng ký trong kỳ và đã qua đủ 30 ngày quan sát
        public int SoDangKyChuyenDoi { get; set; }       // trong số đó, có đơn trong 30 ngày kể từ ngày đăng ký
        public double TyLeChuyenDoi { get; set; }        // % (chỉ có ý nghĩa khi SoDangKyDuThoiGian > 0)
        public int KhachQuayLaiTuKyTruoc { get; set; }   // khách có đơn đầu trước kỳ và tiếp tục có đơn trong kỳ
        public decimal DoanhThuTruoc { get; set; }       // doanh thu hoàn thành của kỳ trước

        // --- Nguy cơ rời bỏ (tính đến cuối kỳ) ---
        public int NguongNgayNguyCo { get; set; } = 30;
        public int SoKhachNguyCo { get; set; }           // đặt >= 2 lần, đã >= NguongNgayNguyCo ngày chưa quay lại
        public decimal ChiTieuKhachNguyCo { get; set; }  // tổng chi tiêu toàn thời gian của nhóm này
        public int SoKhachMotLanLauChuaQuayLai { get; set; } // chỉ đặt 1 lần và đã >= NguongNgayNguyCo ngày
        public List<KhachNguyCoItem> KhachNguyCo { get; set; } = new();

        // --- Giữ chân theo nhóm tháng ---
        public List<CohortItem> Cohort { get; set; } = new();

        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime TuNgayTruoc { get; set; }
        public DateTime DenNgayTruoc { get; set; }
        public string DonVi { get; set; } = "tháng"; // ngày | tuần | tháng (cùng đơn vị biểu đồ doanh thu)

        // --- Tổng quan ---
        public int TongKhachHang { get; set; }       // toàn hệ thống
        public int KhachCoDon { get; set; }          // khách có ít nhất 1 đơn (không hủy) trong kỳ
        public int TongKhachMoi { get; set; }        // khách đăng ký trong kỳ

        // --- Kỳ trước (để hiện ▲/▼ trên thẻ KPI) ---
        public bool CoDuLieuKyTruoc { get; set; }    // kỳ trước có đơn hoặc khách mới hay không
        public int KhachCoDonTruoc { get; set; }
        public int KhachMoiTruoc { get; set; }
        public double TyLeQuayLaiTruoc { get; set; }
        public double TyLeHuyTruoc { get; set; }
        public int TongDonTruoc { get; set; }

        // --- Top khách hàng ---
        public List<KhachVipItem> TopDoanhThu { get; set; } = new();
        public List<KhachVipItem> TopLuotDat { get; set; } = new();
        public decimal TongDoanhThu { get; set; }    // tổng doanh thu hoàn thành trong kỳ
        public double TyTrongDoanhThuTop { get; set; } // % doanh thu do nhóm Top doanh thu đóng góp

        // --- Phân hạng ---
        public List<HangKhachItem> Hang { get; set; } = new();

        // --- Xu hướng theo cột thời gian (cùng số cột, cùng thứ tự) ---
        public List<ThongKeItem> KhachMoi { get; set; } = new();     // khách đăng ký mới trong cột
        public List<ThongKeItem> KhachDatLai { get; set; } = new();  // khách có đơn trong cột không phải đơn đầu tiên của họ

        // --- Quay lại ---
        public int KhachQuayLai { get; set; }        // khách đặt >= 2 đơn trong kỳ
        public double TyLeQuayLai { get; set; }      // % trên KhachCoDon
        public double TyTrongDoanhThuQuayLai { get; set; } // % doanh thu do khách quay lại đóng góp
        public decimal DoanhThuQuayLai { get; set; }
        public int KhachMotLan { get; set; }         // khách chỉ đặt đúng 1 đơn trong kỳ
        public decimal DoanhThuMotLan { get; set; }

        // --- Hủy đơn ---
        public int TongDon { get; set; }             // tổng đơn trong kỳ (kể cả hủy)
        public int SoDonHuy { get; set; }
        public double TyLeHuy { get; set; }
        public int SoDonHuyCoCoc { get; set; }       // đơn hủy đã có tiền cọc (TienCoc > 0)
        public decimal TienCocDonHuy { get; set; }   // tổng tiền cọc của các đơn hủy
        public int SoDonQuaGioChuaXuLy { get; set; } // đơn quá giờ kết thúc vẫn "Chờ xử lý"/"Đang xử lý" (nghi không đến hoặc quên cập nhật)
        public int SoKhachHuy { get; set; }          // số khách có >= 1 đơn hủy
        public int SoKhachHuyLapLai { get; set; }    // số khách hủy >= 2 đơn
        public double TyLeHuyDoKhachLapLai { get; set; } // % đơn hủy do nhóm hủy lặp lại gây ra
        public List<KhachHuyItem> KhachHayHuy { get; set; } = new();  // chỉ gồm khách hủy >= 2 đơn
        public List<HuyKyItem> HuyTheoKy { get; set; } = new();       // tỷ lệ hủy theo từng cột thời gian
    }
}
