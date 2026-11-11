// Họ và tên: Tạ Đình Kim 
// Mã sinh viên: 23103100315
// Nội dung thực hiện: ViewModel cho Dashboard quản trị và trang Thống kê LINQ.

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    public class DashboardViewModel
    {
        // ----- Quy mô hệ thống (tích lũy, không theo kỳ) -----
        public int TongLoaiSan { get; set; }
        public int TongSan { get; set; }
        public int SanKhaDung { get; set; }
        public int TongKhachHang { get; set; }
        public int SanBaoTri => Math.Max(0, TongSan - SanKhaDung);

        // ----- Phạm vi thời gian đang xem -----
        public string Kieu { get; set; } = "homnay";      // homnay | 7ngay | 30ngay | tuychon
        public DateTime CapNhat { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime TuNgayTruoc { get; set; }
        public DateTime DenNgayTruoc { get; set; }
        public int SoNgay { get; set; }
        public string NhanKy { get; set; } = "";           // vd. "Hôm nay · 11/11/2026"
        public string NhanSoSanh { get; set; } = "kỳ trước"; // vd. "hôm qua"

        // ----- 01. Cần xử lý ngay (thời điểm hiện tại, không phụ thuộc kỳ) -----
        public int DonChoXuLy { get; set; }                // mọi ngày, đang chờ xác nhận
        public int DonQuaHan { get; set; }                 // chờ xử lý nhưng đã tới giờ chơi
        public List<DashDonItem> DsQuaHan { get; set; } = new();
        public int PhutSapDienRa { get; set; } = 60;
        public int DonSapDienRa { get; set; }              // chờ/đang xử lý, bắt đầu trong PhutSapDienRa phút tới
        public List<DashDonItem> DsSapDienRa { get; set; } = new();
        public List<DashDonItem> DsChoXacNhan { get; set; } = new();   // chờ xác nhận nhưng chưa tới giờ chơi (bổ sung cho Dashboard một màn hình)
        // Đơn cần xử lý tách làm 2 nhóm không trùng nhau theo ngày chơi (xem ThongKeController.Index)
        public int DonTonChoXacNhan { get; set; }          // Chờ xử lý, ngày chơi trước hôm nay
        public int DonTonQuaGio { get; set; }              // Đang xử lý đã quá giờ kết thúc, ngày chơi trước hôm nay
        public int DonNayChoXacNhan { get; set; }          // Chờ xử lý, ngày chơi từ hôm nay trở đi
        public int DonNayQuaGio { get; set; }              // Đang xử lý quá giờ kết thúc, ngày chơi hôm nay
        public int DonTon => DonTonChoXacNhan + DonTonQuaGio;
        public int DonCanXuLyNay => DonNayChoXacNhan + DonNayQuaGio;
        public List<DashDonItem> DsTon { get; set; } = new();          // cũ nhất trước
        public List<DashDonItem> DsCanXuLyNay { get; set; } = new();   // cũ nhất trước
        public int DonQuaGioChuaChot { get; set; }         // đang xử lý nhưng đã quá giờ kết thúc
        public List<DashDonItem> DsQuaGioChuaChot { get; set; } = new();
        public int DonDangDienRa { get; set; }
        public int SanDangCoKhach { get; set; }

        // ----- 02. Kinh doanh (theo kỳ, tính theo ngày tạo đơn) -----
        public decimal DoanhThu { get; set; }              // đơn Hoàn thành, cộng DonGia (khớp trang Thống kê)
        public decimal DoanhThuTruoc { get; set; }
        public decimal DoanhThuTamTinh { get; set; }       // sân + dịch vụ, đơn chưa hủy
        public int DonHoanThanhKy { get; set; }
        public int DonHoanThanhTruoc { get; set; }
        public int TongDonKy { get; set; }
        public int TongDonTruoc { get; set; }
        public int DonChoKy { get; set; }
        public int DonDangKy { get; set; }
        public int DonHuyKy { get; set; }
        public int DonHuyTruoc { get; set; }
        public int DonKhacKy { get; set; }                 // trạng thái ngoài 4 trạng thái chuẩn
        public bool CoKyTruoc { get; set; }                // kỳ trước có phát sinh đơn để so sánh

        public decimal GiaTriTb => DonHoanThanhKy == 0 ? 0m : Math.Round(DoanhThu / DonHoanThanhKy, 0);
        public decimal GiaTriTbTruoc => DonHoanThanhTruoc == 0 ? 0m : Math.Round(DoanhThuTruoc / DonHoanThanhTruoc, 0);
        public double TyLeHuy => TongDonKy == 0 ? 0 : Math.Round(DonHuyKy * 100.0 / TongDonKy, 1);
        public double TyLeHuyTruoc => TongDonTruoc == 0 ? 0 : Math.Round(DonHuyTruoc * 100.0 / TongDonTruoc, 1);

        // ----- Biểu đồ xu hướng (tối thiểu 7 ngày kết thúc ở cuối kỳ để không chỉ có 1 cột) -----
        public string DonViBieuDo { get; set; } = "ngày";
        public string NhanBieuDo { get; set; } = "";
        public string NhanDonViCot { get; set; } = "1 ngày";   // mỗi cột biểu đồ đại diện: "1 giờ" | "1 ngày" | "7 ngày" | "1 tháng"
        public string GhiChuBieuDo { get; set; } = "";         // ghi chú khi đơn vị nhóm khác dự kiến (vd. không có giờ tạo đơn)
        public bool CoSoSanhBieuDo { get; set; }
        public List<DashCotItem> BieuDo { get; set; } = new();
        public List<DashNhanDinhItem> NhanDinh { get; set; } = new();

        // ----- 04. Hiệu suất sân -----
        public HieuSuatSanViewModel HieuSuat { get; set; } = new();       // đúng kỳ đang xem
        public HieuSuatSanViewModel HieuSuatBanDo { get; set; } = new();  // dùng cho bản đồ nhiệt + xếp hạng sân (kỳ ngắn thì lấy 7 ngày)
        public string NhanBanDo { get; set; } = "";
        public List<HieuSuatSanItem> SanCongSuatThap { get; set; } = new();
        public string? KhungGioVang { get; set; }
        public string? KhungGioDong { get; set; }

        // ----- 05. Khách hàng -----
        public int KhachMoi { get; set; }
        public int KhachMoiTruoc { get; set; }
        public int KhachCoDon { get; set; }
        public int KhachCoDonTruoc { get; set; }
        public int KhachQuayLai { get; set; }              // >= 2 đơn không hủy trong kỳ
        public int KhachQuayLaiTruoc { get; set; }
        public int DonKhongHuyKy { get; set; }
        public double TyLeQuayLai => KhachCoDon == 0 ? 0 : Math.Round(KhachQuayLai * 100.0 / KhachCoDon, 1);
        public double TyLeQuayLaiTruoc => KhachCoDonTruoc == 0 ? 0 : Math.Round(KhachQuayLaiTruoc * 100.0 / KhachCoDonTruoc, 1);
        public double DonTbMoiKhach => KhachCoDon == 0 ? 0 : Math.Round(DonKhongHuyKy / (double)KhachCoDon, 1);

        // ----- 06. Hoạt động gần đây -----
        public List<DashDonItem> DonMoiNhat { get; set; } = new();
        public List<DashKhachMoiItem> KhachMoiNhat { get; set; } = new();
    }

    // Một đơn rút gọn để hiện trong danh sách việc cần làm / hoạt động gần đây
    public class DashDonItem
    {
        public int MaDatSan { get; set; }
        public string TenSan { get; set; } = "";
        public string TenKhach { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public DateTime NgayDat { get; set; }
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }
        public decimal DonGia { get; set; }
    }

    // Một cột trên biểu đồ xu hướng (ngày / tuần / tháng)
    public class DashCotItem
    {
        public string Ten { get; set; } = "";
        public string NhanDayDu { get; set; } = "";
        public decimal DoanhThu { get; set; }
        public decimal? DoanhThuTruoc { get; set; }   // null = kỳ trước không có dữ liệu
        public int SoHoanThanh { get; set; }
        public int SoDang { get; set; }
        public int SoCho { get; set; }
        public int SoHuy { get; set; }
        public int SoKhac { get; set; }
        public int TongDon => SoHoanThanh + SoDang + SoCho + SoHuy + SoKhac;
    }

    // Nhận định tự động: chỉ tạo khi dữ liệu đủ chứng minh
    public class DashNhanDinhItem
    {
        public string Muc { get; set; } = "info";     // tot | canhbao | info
        public string NoiDung { get; set; } = "";
    }

    public class DashKhachMoiItem
    {
        public string HoTen { get; set; } = "";
        public DateTime? NgayDangKy { get; set; }
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

        // Tab "Hiệu suất sân" (tỷ lệ khai thác theo giờ sân)
        public HieuSuatSanViewModel HieuSuat { get; set; } = new();

        // Tab "Dịch vụ" (doanh thu, lượt dùng, xu hướng, hiệu quả từng dịch vụ)
        public DichVuViewModel DichVu { get; set; } = new();
    }

    // ================== TAB DỊCH VỤ ==================

    // Một dịch vụ trong kỳ (kèm số liệu kỳ trước để phát hiện dịch vụ giảm / dừng phát sinh)
    public class DichVuItem
    {
        public string Ten { get; set; } = "";
        public int SoLuong { get; set; }             // tổng số lượng đã dùng trong kỳ (đơn không hủy)
        public int SoDon { get; set; }               // số đơn đặt sân RIÊNG BIỆT có dịch vụ này
        public decimal DoanhThu { get; set; }        // cộng ThanhTien của các dòng chi tiết
        public int SoLuongTruoc { get; set; }
        public decimal DoanhThuTruoc { get; set; }
    }

    // Một cột trên biểu đồ xu hướng doanh thu dịch vụ (ngày / tuần / tháng)
    public class DichVuCotItem
    {
        public string Ten { get; set; } = "";
        public decimal DoanhThu { get; set; }
        public decimal? DoanhThuTruoc { get; set; }  // cột tương ứng của kỳ trước (null = không có)
        public int SoLuong { get; set; }
        public int SoDon { get; set; }               // số đơn đặt sân riêng biệt có dịch vụ trong cột
    }

    public class DichVuViewModel
    {
        public decimal DoanhThu { get; set; }        // tổng tiền dịch vụ của đơn không hủy trong kỳ
        public decimal DoanhThuTruoc { get; set; }
        public int SoLuong { get; set; }             // tổng số lượng dịch vụ đã dùng
        public int SoLuongTruoc { get; set; }
        public int DonCoDichVu { get; set; }         // số đơn không hủy có ít nhất một dịch vụ
        public int DonCoDichVuTruoc { get; set; }
        public int TongDonHopLe { get; set; }        // số đơn không hủy trong kỳ
        public int TongDonHopLeTruoc { get; set; }
        public bool CoKyTruoc { get; set; }          // kỳ trước có phát sinh dịch vụ để so sánh

        // Phạm vi tính: đơn KHÔNG hủy (gồm cả chờ xử lý / đang xử lý / hoàn thành), dùng chung cho cả 4 KPI
        public decimal DoanhThuHoanThanh { get; set; }   // phần dịch vụ của riêng đơn "Hoàn thành" (đã ghi nhận doanh thu)
        public decimal DoanhThuSanKhongHuy { get; set; } // tiền sân (cộng DonGia) của đơn không hủy trong kỳ

        // Tỷ trọng = dịch vụ / (sân + dịch vụ), cùng kỳ và cùng phạm vi đơn không hủy
        public double TyTrongDoanhThu => (DoanhThu + DoanhThuSanKhongHuy) <= 0 ? 0 : Math.Round((double)(DoanhThu * 100m / (DoanhThu + DoanhThuSanKhongHuy)), 1);
        public bool CoTyTrong => DoanhThu > 0 && (DoanhThu + DoanhThuSanKhongHuy) > 0;

        public int DonChuaDichVu => Math.Max(0, TongDonHopLe - DonCoDichVu);
        public decimal GiaTriTbMoiDon => DonCoDichVu == 0 ? 0m : Math.Round(DoanhThu / DonCoDichVu, 0);
        public decimal GiaTriTbMoiDonTruoc => DonCoDichVuTruoc == 0 ? 0m : Math.Round(DoanhThuTruoc / DonCoDichVuTruoc, 0);

        public double TyLeDonCoDichVu => TongDonHopLe == 0 ? 0 : Math.Round(DonCoDichVu * 100.0 / TongDonHopLe, 1);
        public double TyLeDonCoDichVuTruoc => TongDonHopLeTruoc == 0 ? 0 : Math.Round(DonCoDichVuTruoc * 100.0 / TongDonHopLeTruoc, 1);

        public List<DichVuItem> Items { get; set; } = new();       // gồm cả dịch vụ chỉ có ở kỳ trước (SoLuong = 0)
        public List<DichVuCotItem> XuHuong { get; set; } = new();
    }

    // ================== TAB HIỆU SUẤT SÂN ==================

    // Một điểm trên biểu đồ xu hướng khai thác (ngày / tuần / tháng)
    public class HieuSuatCotItem
    {
        public string Ten { get; set; } = "";
        public double? TyLe { get; set; }            // null = cột chưa có giờ nào đã diễn ra
        public double? TyLeTruoc { get; set; }       // cột tương ứng của kỳ trước (null = không có)
        public double GioDat { get; set; }
        public double GioKhaDung { get; set; }
    }

    // Một ô của bản đồ nhiệt (thứ x giờ)
    public class HeatOItem
    {
        public double GioDat { get; set; }
        public double GioKhaDung { get; set; }       // 0 = ngoài giờ kinh doanh hoặc chưa diễn ra
        public int Luot { get; set; }                // số đơn bắt đầu trong khung giờ này
        public double? TyLe { get; set; }            // null khi GioKhaDung = 0
    }

    public class HeatHangItem
    {
        public string Ten { get; set; } = "";        // Thứ 2 ... Chủ nhật
        public List<HeatOItem> O { get; set; } = new();   // từ GioMo đến GioDong - 1
    }

    public class HieuSuatLoaiItem
    {
        public string Ten { get; set; } = "";
        public int SoSan { get; set; }
        public double GioDat { get; set; }
        public double GioKhaDung { get; set; }
        public double TyLe { get; set; }
        public int Luot { get; set; }
    }

    public class HieuSuatSanItem
    {
        public int MaSan { get; set; }
        public string Ten { get; set; } = "";
        public string TenLoai { get; set; } = "";
        public double GioDat { get; set; }
        public double GioKhaDung { get; set; }
        public double TyLe { get; set; }
        public int Luot { get; set; }
    }

    // Một mốc thời gian so sánh (lũy kế kỳ đang xem / tháng này / 7 ngày gần nhất)
    public class HieuSuatMocItem
    {
        public string Nhan { get; set; } = "";
        public DateTime Tu { get; set; }
        public DateTime Den { get; set; }
        public double GioDat { get; set; }
        public double GioKhaDung { get; set; }
        public bool CoCongSuat { get; set; }         // false = chưa có giờ nào đã diễn ra
        public double TyLe { get; set; }
    }

    public class HieuSuatSanViewModel
    {
        public int GioMo { get; set; }
        public int GioDong { get; set; }
        public int SoNgay { get; set; }
        public int SoSanKhaDung { get; set; }        // chỉ sân đang ở trạng thái khả dụng
        public int SoSanCoHoatDong { get; set; }     // sân có ít nhất 1 lượt đặt trong kỳ

        public double GioKhaDung { get; set; }       // tổng giờ có thể kinh doanh (đã diễn ra) của mọi sân
        public double GioDaDat { get; set; }         // tổng giờ sân đã đặt (không tính đơn hủy)
        public double TyLeKhaiThac { get; set; }     // GioDaDat / GioKhaDung (%)
        public double GioTrong => Math.Max(0, GioKhaDung - GioDaDat);

        public bool CoKyTruoc { get; set; }          // kỳ trước có giờ đặt để so sánh
        public double TyLeKyTruoc { get; set; }
        public double GioDaDatTruoc { get; set; }

        public int SoLuotDat { get; set; }           // số đơn không hủy bắt đầu trong kỳ
        public double LuotTbSanNgay { get; set; }    // SoLuotDat / (số sân x số ngày)

        public List<HieuSuatMocItem> Moc { get; set; } = new();
        public List<HieuSuatCotItem> XuHuong { get; set; } = new();
        public List<HeatHangItem> Heatmap { get; set; } = new();
        public List<HieuSuatLoaiItem> Loai { get; set; } = new();
        public List<HieuSuatSanItem> San { get; set; } = new();
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
