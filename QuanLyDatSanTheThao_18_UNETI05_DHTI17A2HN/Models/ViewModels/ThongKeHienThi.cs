using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.ViewModels
{
    /// <summary>Các phép tính dùng chung của trang Thống kê (trước đây nằm trong khối @{ } đầu ThongKe.cshtml).</summary>
    public class ThongKeHienThi
    {
        public ThongKeViewModel Data { get; }

        public CultureInfo Inv { get; }
        public Func<DateTime, string> F { get; }
        public string HuyMau { get; }
        public string HuyNhan { get; }
        public string HuyLop { get; }
        public string HuyIcon { get; }
        public string HuyRong { get; }
        public List<int> DanhSachNam { get; }
        public string Kieu { get; }
        public DateTime Tu { get; }
        public DateTime Den { get; }
        public string Nhan { get; }
        public List<Preset> Presets { get; }
        public bool CoDuLieu { get; }
        public Func<decimal, decimal, string[]> Delta { get; }
        public string KyTruocNhan { get; }
        public string KyTruocKhoang { get; }
        public decimal TienHuy { get; }
        public bool TienHuyTruncated { get; }
        public decimal TongDichVu { get; }
        public string[] DtDelta { get; }
        public string[] LuotDelta { get; }
        public string[] DonDelta { get; }
        public string[] TbDelta { get; }
        public bool CotCuoiDangDienRa { get; }
        public List<string> NhanDinh { get; }
        public decimal TrungBinh { get; }
        public string MoTaBieuDo { get; }
        public ThongKeItem KgThap { get; }
        public List<LoaiRow> LoaiRows { get; }
        public decimal TongTienLoai { get; }
        public string[] Mau { get; }
        public List<(string Mau, string Day, string Lech, int Idx)> Donut { get; }
        public Func<decimal, string> Gon { get; }
        public Dictionary<int, ThongKeItem> GioMap { get; }
        public int GioDau { get; }
        public int GioCuoi { get; }
        public int GioMax { get; }
        public int GioCaoDiem { get; }
        public BuoiRow[] BuoiRows { get; }
        public int BuoiMax { get; }
        public int BuoiTong { get; }
        public int TongLuotSan { get; }
        public Func<int, int, string> TyLeStr { get; }
        public int TongSan { get; }
        public int SanCoDat { get; }
        public double LuotTbSan { get; }
        public List<SanLoaiRow> SanLoaiRows { get; }
        public double SanLoaiMax { get; }
        public string TieuDeBaoCao { get; }
        public string KyBaoCao { get; }
        public string TieuDeTrang { get; }
        public Func<decimal, string> Tien { get; }
        public List<string> PhanTich { get; }

        public ThongKeHienThi(ThongKeViewModel model)
        {
            Data = model;
            var inv = CultureInfo.InvariantCulture;
            Func<DateTime, string> f = d => d.ToString("dd/MM/yyyy", inv);

            // ===== Tỷ lệ hủy =====
            double tyLeHuy = Convert.ToDouble(model.TyLeHuy, inv);
            string huyMau = tyLeHuy < 10 ? "#16a34a" : (tyLeHuy < 20 ? "#d97706" : "#dc2626");
            string huyNhan = tyLeHuy < 10 ? "Ở mức tốt" : (tyLeHuy < 20 ? "Cần theo dõi" : "Tỷ lệ cao");
            string huyLop = tyLeHuy < 10 ? "good" : (tyLeHuy < 20 ? "warn" : "bad");
            string huyIcon = tyLeHuy < 10 ? "✔" : "⚠";
            string huyRong = Math.Min(100, tyLeHuy).ToString("0.##", inv);

            // ===== Bộ lọc thời gian =====
            // Danh sách năm cho dropdown: gộp năm có dữ liệu + dải năm liên tục (từ 5 năm trước đến 2 năm sau hiện tại)
            int namHienTai = DateTime.Today.Year;
            var danhSachNam = new List<int>();
            if (model.DanhSachNam != null) { danhSachNam.AddRange(model.DanhSachNam); }
            for (int y = namHienTai - 5; y <= namHienTai + 2; y++) { danhSachNam.Add(y); }
            danhSachNam.Add(model.Nam);
            danhSachNam = danhSachNam.Distinct().OrderByDescending(y => y).ToList();

            string kieu = string.IsNullOrEmpty(model.Kieu) ? "nam" : model.Kieu;
            DateTime tu = model.TuNgay == default(DateTime) ? new DateTime(model.Nam, 1, 1) : model.TuNgay.Date;
            DateTime den = model.DenNgay == default(DateTime) ? new DateTime(model.Nam, 12, 31) : model.DenNgay.Date;

            string nhan;
            switch (kieu)
            {
                case "homnay": nhan = "hôm nay (" + f(tu) + ")"; break;
                case "tuannay": nhan = "tuần này (" + f(tu) + " - " + f(den) + ")"; break;
                case "thangnay": nhan = "tháng " + tu.ToString("MM/yyyy", inv); break;
                case "quynay": nhan = "quý " + ((tu.Month - 1) / 3 + 1) + "/" + tu.Year; break;
                case "tuychon": nhan = f(tu) + " - " + f(den); break;
                default: nhan = "năm " + tu.Year; break;
            }

            var presets = new List<Preset>
            {
                new Preset { Key = "homnay",   Text = "Hôm nay" },
                new Preset { Key = "tuannay",  Text = "Tuần này" },
                new Preset { Key = "thangnay", Text = "Tháng này" },
                new Preset { Key = "quynay",   Text = "Quý này" },
                new Preset { Key = "nam",      Text = "Năm" },
                new Preset { Key = "tuychon",  Text = "Tùy chọn" }
            };

            bool coDuLieu = model.TongLuotDat > 0;

            // ===== So sánh với kỳ trước: trả về [lớp CSS, chữ hiển thị] =====
            Func<decimal, decimal, string[]> delta = (cur, prev) =>
            {
                if (prev == 0) return cur == 0 ? new[] { "flat", "–" } : new[] { "new", "Chưa có dữ liệu kỳ trước" };
                decimal pct = Math.Round((cur - prev) / prev * 100m, 1);
                if (pct == 0) return new[] { "flat", "= 0%" };
                return new[] { pct > 0 ? "up" : "down", (pct > 0 ? "▲ " : "▼ ") + Math.Abs(pct).ToString("0.#") + "%" };
            };
            string kyTruocTip = "So với " + f(model.TuNgayTruoc) + " - " + f(model.DenNgayTruoc);

            // Nhãn kỳ trước theo bộ lọc đang chọn (hôm qua / tuần trước / tháng trước / quý trước / năm trước)
            string kyTruocNhan;
            switch (kieu)
            {
                case "homnay": kyTruocNhan = "so với hôm qua"; break;
                case "tuannay": kyTruocNhan = "so với tuần trước"; break;
                case "thangnay": kyTruocNhan = "so với tháng trước"; break;
                case "quynay": kyTruocNhan = "so với quý trước"; break;
                case "nam": kyTruocNhan = "so với năm trước"; break;
                default: kyTruocNhan = "so với kỳ trước"; break;
            }
            string kyTruocKhoang = f(model.TuNgayTruoc) + " - " + f(model.DenNgayTruoc);

            // Tiền thất thoát do đơn hủy (cộng từ danh sách đơn hủy; nếu danh sách bị cắt thì chỉ là mức tối thiểu)
            decimal tienHuy = model.DonHuy.Sum(x => x.DonGia);
            bool tienHuyTruncated = model.SoLuotHuy > model.DonHuy.Count;

            // Tổng doanh thu dịch vụ để tính tỷ trọng đóng góp
            decimal tongDichVu = model.DichVuPhoBien.Sum(x => x.GiaTri);

            var dtDelta = delta(model.TongDoanhThuNam, model.DoanhThuKyTruoc);
            var luotDelta = delta(model.TongLuotDat, model.LuotDatKyTruoc);
            var donDelta = delta(model.SoDonHoanThanh, model.DonHoanThanhKyTruoc);
            decimal trungBinh = model.SoDonHoanThanh > 0 ? Math.Round(model.TongDoanhThuNam / model.SoDonHoanThanh, 0) : 0;

            // So sánh giá trị trung bình/đơn với kỳ trước (chỉ có ý nghĩa khi kỳ trước có đơn hoàn thành)
            decimal tbKyTruoc = model.DonHoanThanhKyTruoc > 0 ? Math.Round(model.DoanhThuKyTruoc / model.DonHoanThanhKyTruoc, 0) : 0;
            var tbDelta = delta(trungBinh, tbKyTruoc);

            // Cột cuối của biểu đồ có phải kỳ chưa kết thúc không (VD tháng 11 mới đến ngày 11)
            DateTime ngayHienTaiVn = DateTime.UtcNow.AddHours(7).Date;
            bool cotCuoiDangDienRa = false;
            if (den >= ngayHienTaiVn && model.DoanhThuTheoThang.Count > 0)
            {
                if (model.DonViBieuDo == "ngày") cotCuoiDangDienRa = true;
                else if (model.DonViBieuDo == "tuần") cotCuoiDangDienRa = tu.AddDays(7 * (model.DoanhThuTheoThang.Count - 1) + 6) > den;
                else cotCuoiDangDienRa = new DateTime(den.Year, den.Month, 1).AddMonths(1).AddDays(-1) > den;
            }

            string moTaBieuDo = model.DonViBieuDo == "tuần"
                ? "Mỗi cột = 1 tuần (nhãn là ngày bắt đầu). Cột đậm là mốc cao nhất."
                : "Mỗi cột = 1 " + model.DonViBieuDo + ". Cột đậm là mốc cao nhất.";

            // ===== Nhận định khung giờ =====
            var kg = model.KhungGio;
            var kgCao = model.KhungGioCaoDiem;
            var kgThap = kg.Count > 1 ? kg.OrderBy(x => x.SoLuong).First() : null;

            // ===== Doanh thu theo loại sân: gộp lượt đặt + doanh thu, dùng cho biểu đồ tròn và bảng =====
            var tienTheoLoai = model.DoanhThuTheoLoai.ToDictionary(x => x.Ten, x => x.GiaTri);
            var donHoanThanhTheoLoai = model.DoanhThuTheoLoai.ToDictionary(x => x.Ten, x => x.SoLuong);   // số đơn hoàn thành mỗi loại sân
            var loaiRows = model.LuotDatTheoLoai
                .Select(x => new LoaiRow { Ten = x.Ten, Luot = x.SoLuong, DonHoanThanh = donHoanThanhTheoLoai.GetValueOrDefault(x.Ten), Tien = tienTheoLoai.GetValueOrDefault(x.Ten) })
                .OrderByDescending(x => x.Tien).ThenByDescending(x => x.Luot).ThenBy(x => x.Ten)
                .ToList();
            decimal tongTienLoai = loaiRows.Sum(x => x.Tien);
            string[] mau = { "#14532d", "#16a34a", "#4ade80", "#0ea5e9", "#f59e0b", "#94a3b8" };

            const double chuVi = 2 * Math.PI * 44;   // bán kính vòng tròn = 44
            int soPhanCoTien = loaiRows.Count(x => x.Tien > 0);
            var donut = new List<(string Mau, string Day, string Lech, int Idx)>();
            double daDung = 0;
            for (int idx = 0; idx < loaiRows.Count; idx++)
            {
                if (tongTienLoai <= 0 || loaiRows[idx].Tien <= 0) continue;
                double dai = (double)(loaiRows[idx].Tien / tongTienLoai) * chuVi;
                double hien = soPhanCoTien > 1 ? Math.Max(dai - 1.5, 0.5) : dai;   // chừa khe nhỏ giữa các phần
                donut.Add((mau[idx % mau.Length],
                           hien.ToString("0.###", inv) + " " + (chuVi - hien).ToString("0.###", inv),
                           (-daDung).ToString("0.###", inv), idx));
                daDung += dai;
            }

            // Số tiền gọn cho giữa biểu đồ tròn
            Func<decimal, string> gon = v => v >= 1000000000m ? (v / 1000000000m).ToString("0.##") + " tỷ"
                                           : v >= 1000000m ? (v / 1000000m).ToString("0.##") + " tr"
                                           : v.ToString("N0") + " đ";

            // ===== Biểu đồ cột theo giờ (mặc định 6h - 22h, tự mở rộng nếu có đơn ngoài khoảng này) =====
            var gioMap = new Dictionary<int, ThongKeItem>();
            foreach (var mucGio in model.KhungGio)
            {
                if (mucGio.Ten.Length >= 2 && int.TryParse(mucGio.Ten.Substring(0, 2), out var gioSo)) gioMap[gioSo] = mucGio;
            }
            int gioDau = gioMap.Count > 0 ? Math.Min(6, gioMap.Keys.Min()) : 6;
            int gioCuoi = gioMap.Count > 0 ? Math.Max(22, gioMap.Keys.Max()) : 22;
            int gioMax = gioMap.Count > 0 ? gioMap.Values.Max(x => x.SoLuong) : 0;
            int gioCaoDiem = -1;
            if (kgCao != null && kgCao.Ten.Length >= 2 && int.TryParse(kgCao.Ten.Substring(0, 2), out var gioCao)) gioCaoDiem = gioCao;

            // Thu hẹp trục giờ về vùng có dữ liệu (±2 giờ), tối thiểu 8 cột, để không lãng phí chỗ khi đơn dồn vào vài giờ
            if (gioMap.Count > 0)
            {
                int gA = Math.Max(0, gioMap.Keys.Min() - 2);
                int gB = Math.Min(23, gioMap.Keys.Max() + 2);
                while (gB - gA + 1 < 8 && (gA > 0 || gB < 23))
                {
                    if (gA > 0) gA--;
                    if (gB - gA + 1 < 8 && gB < 23) gB++;
                }
                gioDau = gA;
                gioCuoi = gB;
            }

            // Gom khung giờ theo buổi: Sáng (< 12h), Chiều (12h - 18h), Tối (từ 18h)
            var buoiRows = new BuoiRow[]
            {
                new BuoiRow { Ten = "Sáng",  Mo = "trước 12:00",  Luot = gioMap.Where(x => x.Key < 12).Sum(x => x.Value.SoLuong) },
                new BuoiRow { Ten = "Chiều", Mo = "12:00 - 18:00", Luot = gioMap.Where(x => x.Key >= 12 && x.Key < 18).Sum(x => x.Value.SoLuong) },
                new BuoiRow { Ten = "Tối",   Mo = "từ 18:00",      Luot = gioMap.Where(x => x.Key >= 18).Sum(x => x.Value.SoLuong) }
            };
            int buoiMax = buoiRows.Max(x => x.Luot);
            int buoiTong = buoiRows.Sum(x => x.Luot);

            // ===== Xếp hạng sân =====
            int tongLuotSan = model.LuotDatTheoSan.Sum(x => x.SoLuong);
            var topSan = model.LuotDatTheoSan.Take(5).ToList();
            Func<int, int> hangSan = so => 1 + model.LuotDatTheoSan.Count(x => x.SoLuong > so);   // đồng hạng cùng thứ tự
            Func<int, int, string> tyLeStr = (so, tong) => tong == 0 ? "0%" : (so * 100.0 / tong).ToString("0.#") + "%";
            var sanIt = model.SanItKhach.Take(5).ToList();

            // Xếp hạng sân: cùng số lượt thì sắp theo tên để thứ tự cố định, và gắn nhãn "Đồng hạng"
            var topSanRows = model.LuotDatTheoSan.OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten).Take(5).ToList();

            // Chỉ số khai thác sân (ước lượng từ lượt đặt, chưa cần giờ mở cửa)
            var luotTheoLoai = model.LuotDatTheoLoai.GroupBy(x => x.Ten).ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuong));
            int tongSan = model.SanTheoLoai.Sum(x => x.SoLuong);
            int sanCoDat = model.LuotDatTheoSan.Count(x => x.SoLuong > 0);
            double luotTbSan = tongSan > 0 ? (double)tongLuotSan / tongSan : 0;
            var sanLoaiRows = model.SanTheoLoai
                .Select(x => new SanLoaiRow { Ten = x.Ten, SoSan = x.SoLuong, Luot = luotTheoLoai.GetValueOrDefault(x.Ten) })
                .ToList();
            double sanLoaiMax = sanLoaiRows.Count == 0 ? 0 : sanLoaiRows.Max(x => x.TbSan);

            // ===== Báo cáo khi in / lưu PDF: tiêu đề theo kỳ đang xem + phân tích tự động =====
            string loaiBaoCao;
            switch (kieu)
            {
                case "homnay": loaiBaoCao = "ngày"; break;
                case "tuannay": loaiBaoCao = "tuần"; break;
                case "thangnay": loaiBaoCao = "tháng"; break;
                case "quynay": loaiBaoCao = "quý"; break;
                case "nam": loaiBaoCao = "năm"; break;
                default: loaiBaoCao = null; break;
            }
            string tieuDeBaoCao = loaiBaoCao != null ? "Báo cáo " + loaiBaoCao : "Báo cáo thống kê";
            string kyBaoCao = kieu == "homnay" ? f(tu) : f(tu) + " - " + f(den);
            string tieuDeTrang = tieuDeBaoCao + " " + kyBaoCao.Replace("/", "-");   // thành tên file khi "Lưu thành PDF"
            Func<decimal, string> tien = v => v.ToString("N0") + " đ";

            var phanTich = new List<string>();
            if (model.DoanhThuKyTruoc > 0)
            {
                decimal pctDt = Math.Round((model.TongDoanhThuNam - model.DoanhThuKyTruoc) / model.DoanhThuKyTruoc * 100m, 1);
                phanTich.Add("Doanh thu đạt " + tien(model.TongDoanhThuNam) + ", " + (pctDt >= 0 ? "tăng " : "giảm ") + Math.Abs(pctDt).ToString("0.#") + "% " + kyTruocNhan + " (" + tien(model.DoanhThuKyTruoc) + ").");
            }
            else
            {
                phanTich.Add("Doanh thu đạt " + tien(model.TongDoanhThuNam) + "; kỳ trước (" + kyTruocKhoang + ") chưa có dữ liệu để so sánh.");
            }
            phanTich.Add("Có " + model.TongLuotDat + " lượt đặt, trong đó " + model.SoDonHoanThanh + " đơn hoàn thành; giá trị trung bình " + tien(trungBinh) + " mỗi đơn.");
            phanTich.Add("Tỷ lệ hủy đơn " + model.TyLeHuy + "% (" + model.SoLuotHuy + "/" + model.TongLuotDat + " đơn) - " + huyNhan.ToLower()
                         + (model.SoLuotHuy > 0 ? ", giá trị đơn hủy " + (tienHuyTruncated ? "tối thiểu " : "") + tien(tienHuy) + "." : "."));
            var loaiTop = loaiRows.FirstOrDefault();
            if (loaiTop != null && loaiTop.Tien > 0 && tongTienLoai > 0)
            {
                phanTich.Add(loaiTop.Ten + " mang lại doanh thu cao nhất: " + tien(loaiTop.Tien) + " (" + (loaiTop.Tien / tongTienLoai * 100m).ToString("0.#") + "% doanh thu theo loại sân).");
            }
            if (kgCao != null)
            {
                phanTich.Add("Khung giờ cao điểm là " + kgCao.Ten + " với " + kgCao.SoLuong + " lượt (" + kgCao.TyLe.ToString("0.#") + "% tổng lượt đặt).");
            }
            if (buoiTong > 0)
            {
                var buoiTop = buoiRows.OrderByDescending(x => x.Luot).First();
                phanTich.Add("Buổi " + buoiTop.Ten.ToLower() + " có nhiều lượt đặt nhất (" + tyLeStr(buoiTop.Luot, buoiTong) + ").");
            }
            if (kgThap != null && kgCao != null && kgThap.Ten != kgCao.Ten)
            {
                phanTich.Add("Khung giờ vắng nhất là " + kgThap.Ten + " (" + kgThap.SoLuong + " lượt); có thể cân nhắc ưu đãi để kích cầu.");
            }
            if (topSanRows.Count > 0)
            {
                var sanTop = topSanRows[0];
                int cungLuotDau = model.LuotDatTheoSan.Count(x => x.SoLuong == sanTop.SoLuong);
                phanTich.Add(cungLuotDau > 1
                    ? cungLuotDau + " sân cùng dẫn đầu với " + sanTop.SoLuong + " lượt đặt mỗi sân."
                    : sanTop.Ten + " được đặt nhiều nhất với " + sanTop.SoLuong + " lượt.");
            }
            if (tongSan > 0 && sanCoDat < tongSan)
            {
                phanTich.Add((tongSan - sanCoDat) + "/" + tongSan + " sân chưa có lượt đặt nào trong kỳ.");
            }
            if (model.DichVuPhoBien.Count > 0)
            {
                var dvTop = model.DichVuPhoBien[0];
                phanTich.Add("Dịch vụ bán chạy nhất là " + dvTop.Ten + " (" + dvTop.SoLuong + " lượt sử dụng, " + tien(dvTop.GiaTri) + ").");
            }
            if (tyLeHuy >= 20)
            {
                phanTich.Add("Tỷ lệ hủy đang ở mức cao, nên rà soát nguyên nhân hủy (chính sách đặt cọc, nhắc lịch) để giảm số đơn hủy.");
            }

            // ===== Nhận định cho tab Doanh thu: chỉ dựa trên số liệu đã có, không suy diễn thêm =====
            var nhanDinh = new List<string>();
            if (model.TongDoanhThuNam > 0)
            {
                var ndCotCao = model.DoanhThuTheoThang.OrderByDescending(x => x.GiaTri).FirstOrDefault();
                int ndSoCotCoTien = model.DoanhThuTheoThang.Count(x => x.GiaTri > 0);
                if (ndCotCao != null && ndCotCao.GiaTri > 0)
                {
                    string ndTenCot = model.DonViBieuDo == "tuần" ? "tuần bắt đầu " + ndCotCao.Ten
                                    : (model.DonViBieuDo == "ngày" ? "ngày " + ndCotCao.Ten : ndCotCao.Ten);
                    if (ndSoCotCoTien == 1 && model.DoanhThuTheoThang.Count > 1)
                    {
                        nhanDinh.Add("Toàn bộ doanh thu của kỳ tập trung ở " + ndTenCot + "; "
                            + (model.DoanhThuTheoThang.Count - 1) + " " + model.DonViBieuDo + " còn lại chưa có đơn hoàn thành.");
                    }
                    else if (ndSoCotCoTien > 1)
                    {
                        nhanDinh.Add(char.ToUpper(ndTenCot[0]) + ndTenCot.Substring(1) + " có doanh thu cao nhất: " + tien(ndCotCao.GiaTri)
                            + " (" + (ndCotCao.GiaTri / model.TongDoanhThuNam * 100m).ToString("0.#") + "% doanh thu kỳ).");
                    }
                }

                var ndLoaiDau = loaiRows.FirstOrDefault();
                if (ndLoaiDau != null && ndLoaiDau.Tien > 0 && tongTienLoai > 0)
                {
                    string ndCauLoai = ndLoaiDau.Ten + " đóng góp nhiều nhất (" + (ndLoaiDau.Tien / tongTienLoai * 100m).ToString("0.#") + "% doanh thu)";
                    var ndLoaiTb = loaiRows.Where(x => x.DonHoanThanh > 0).OrderByDescending(x => x.Tien / x.DonHoanThanh).FirstOrDefault();
                    if (ndLoaiTb != null && loaiRows.Count(x => x.DonHoanThanh > 0) > 1)
                    {
                        ndCauLoai += ndLoaiTb.Ten == ndLoaiDau.Ten
                            ? " và cũng có giá trị TB/đơn cao nhất."
                            : "; " + ndLoaiTb.Ten + " có giá trị TB/đơn cao nhất (" + tien(Math.Round(ndLoaiTb.Tien / ndLoaiTb.DonHoanThanh, 0)) + ").";
                    }
                    else
                    {
                        ndCauLoai += ".";
                    }
                    nhanDinh.Add(ndCauLoai);
                }

                if (model.SoLuotHuy > 0 && tienHuy > 0)
                {
                    nhanDinh.Add("Giá trị các đơn hủy " + (tienHuyTruncated ? "(tối thiểu) " : "") + tien(tienHuy) + " tương đương "
                        + (tienHuy / model.TongDoanhThuNam * 100m).ToString("0.#") + "% doanh thu hoàn thành. Đây là giá trị đặt chỗ bị hủy, chưa phải khoản mất chắc chắn vì sân có thể được đặt lại.");
                }
            }
            int ndChuaXong = model.TongLuotDat - model.SoDonHoanThanh - model.SoLuotHuy;
            if (ndChuaXong > 0)
            {
                nhanDinh.Add(ndChuaXong + " đơn chưa hoàn thành và chưa hủy (đang chờ hoặc đang xử lý) nên chưa được tính vào doanh thu.");
            }

            Inv = inv;
            F = f;
            HuyMau = huyMau;
            HuyNhan = huyNhan;
            HuyLop = huyLop;
            HuyIcon = huyIcon;
            HuyRong = huyRong;
            DanhSachNam = danhSachNam;
            Kieu = kieu;
            Tu = tu;
            Den = den;
            Nhan = nhan;
            Presets = presets;
            CoDuLieu = coDuLieu;
            Delta = delta;
            KyTruocNhan = kyTruocNhan;
            KyTruocKhoang = kyTruocKhoang;
            TienHuy = tienHuy;
            TienHuyTruncated = tienHuyTruncated;
            TongDichVu = tongDichVu;
            DtDelta = dtDelta;
            LuotDelta = luotDelta;
            DonDelta = donDelta;
            TrungBinh = trungBinh;
            MoTaBieuDo = moTaBieuDo;
            KgThap = kgThap;
            LoaiRows = loaiRows;
            TongTienLoai = tongTienLoai;
            Mau = mau;
            Donut = donut;
            Gon = gon;
            GioMap = gioMap;
            GioDau = gioDau;
            GioCuoi = gioCuoi;
            GioMax = gioMax;
            GioCaoDiem = gioCaoDiem;
            BuoiRows = buoiRows;
            BuoiMax = buoiMax;
            BuoiTong = buoiTong;
            TongLuotSan = tongLuotSan;
            TyLeStr = tyLeStr;
            TongSan = tongSan;
            SanCoDat = sanCoDat;
            LuotTbSan = luotTbSan;
            SanLoaiRows = sanLoaiRows;
            SanLoaiMax = sanLoaiMax;
            TieuDeBaoCao = tieuDeBaoCao;
            KyBaoCao = kyBaoCao;
            TieuDeTrang = tieuDeTrang;
            Tien = tien;
            PhanTich = phanTich;
            TbDelta = tbDelta;
            CotCuoiDangDienRa = cotCuoiDangDienRa;
            NhanDinh = nhanDinh;
        }
    }

    public class Preset { public string Key { get; set; } public string Text { get; set; } }
    public class LoaiRow { public string Ten { get; set; } public int Luot { get; set; } public int DonHoanThanh { get; set; } public decimal Tien { get; set; } }
    public class BuoiRow { public string Ten { get; set; } public string Mo { get; set; } public int Luot { get; set; } }
    public class SanLoaiRow
    {
        public string Ten { get; set; }
        public int SoSan { get; set; }
        public int Luot { get; set; }
        public double TbSan => SoSan > 0 ? (double)Luot / SoSan : 0.0;
    }
}
