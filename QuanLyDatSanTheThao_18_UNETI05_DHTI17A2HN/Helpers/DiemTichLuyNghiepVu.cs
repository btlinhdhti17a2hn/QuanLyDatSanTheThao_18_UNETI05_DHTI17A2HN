// Họ và tên: Vũ Ánh Ngọc
// Mã sinh viên: 23103100106
// Nội dung thực hiện: Module 4 - Nghiệp vụ tính điểm tích lũy khi hoàn thành đơn đặt sân
//                     (Quy đổi 1.000 điểm = 10.000 VNĐ, giảm tối đa 30% tổng đơn, cộng 10 điểm mỗi 100.000 VNĐ thực chi)

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Helpers
{
    public static class DiemTichLuyNghiepVu
    {
        // 1.000 điểm = 10.000 VNĐ => 1 điểm = 10 VNĐ
        public const decimal GiaTriMotDiem = 10m;

        // 100.000 VNĐ thực chi = 10 điểm => 10.000 VNĐ = 1 điểm
        public const decimal SoTienMoiDiem = 10000m;

        // Tỷ lệ giảm tối đa cho phép từ điểm: 30% tổng đơn
        public const decimal TyLeGiamToiDa = 0.3m;

        // Tính số tiền giảm tối đa (30% tổng đơn)
        public static decimal TinhTienGiamToiDa(decimal tongTien)
        {
            if (tongTien <= 0) return 0;
            return Math.Round(tongTien * TyLeGiamToiDa, 0);
        }

        // Tính số điểm tối đa được phép dùng cho đơn này (không vượt quá điểm khách có và không quá 30% tổng đơn)
        public static int TinhDiemToiDaDuocDung(decimal tongTien, int diemHienCo)
        {
            if (tongTien <= 0 || diemHienCo <= 0) return 0;

            decimal tienGiamToiDa = TinhTienGiamToiDa(tongTien);
            int diemToiDaTheoTien = (int)Math.Floor(tienGiamToiDa / GiaTriMotDiem);

            return Math.Max(0, Math.Min(diemHienCo, diemToiDaTheoTien));
        }

        // Kiểm tra tính hợp lệ của số điểm khách muốn dùng
        public static string? KiemTraDiemSuDung(int diemSuDung, decimal tongTien, int diemHienCo)
        {
            if (diemSuDung < 0)
            {
                return "Số điểm sử dụng không được là số âm.";
            }

            if (diemSuDung > diemHienCo)
            {
                return $"Khách chỉ có {diemHienCo:N0} điểm, không đủ để dùng {diemSuDung:N0} điểm.";
            }

            int diemToiDa = TinhDiemToiDaDuocDung(tongTien, diemHienCo);
            if (diemSuDung > diemToiDa)
            {
                return $"Chỉ được dùng tối đa {diemToiDa:N0} điểm cho đơn này (tối đa 30% tổng đơn).";
            }

            return null;
        }

        // Tính số tiền giảm tương ứng với số điểm sử dụng
        public static decimal TinhTienGiam(int diemSuDung)
        {
            if (diemSuDung <= 0) return 0;
            return diemSuDung * GiaTriMotDiem;
        }

        // Tính số điểm thưởng được cộng cho khách dựa trên số tiền thực tế khách chi trả (100.000đ = 10 điểm)
        public static int TinhDiemCong(decimal tienThucChi)
        {
            if (tienThucChi <= 0) return 0;
            return (int)Math.Floor(tienThucChi / SoTienMoiDiem);
        }
    }
}
