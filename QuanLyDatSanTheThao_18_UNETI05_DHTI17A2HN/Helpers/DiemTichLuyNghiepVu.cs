//Họ và tên: Vũ Ánh Ngọc
// Mã sinh viên: 23103100106
// Nội dung thực hiện: Quy tắc tích điểm / dùng điểm của khách hàng
//   - Chi tiêu 100.000đ  -> cộng 10 điểm (1 điểm / 10.000đ, làm tròn xuống)
//   - 1.000 điểm         -> giảm 10.000đ (1 điểm = 10đ)
//   - Số tiền giảm từ điểm không vượt quá 30% tổng giá trị đơn

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Helpers
{
    public static class DiemTichLuyNghiepVu
    {
        // ===== CẤU HÌNH: muốn đổi quy tắc chỉ cần sửa 3 hằng số này =====
        public const decimal SoTienMoiDiem = 10000m;   // cứ 10.000đ chi tiêu được 1 điểm (100.000đ = 10 điểm)
        public const decimal GiaTriMotDiem = 10m;      // 1 điểm giảm 10đ (1.000 điểm = 10.000đ)
        public const decimal TyLeGiamToiDa = 0.30m;    // giảm tối đa 30% tổng đơn

        // Điểm được cộng cho số tiền khách thực chi (đã trừ phần giảm từ điểm)
        public static int TinhDiemCong(decimal soTienThucChi)
        {
            if (soTienThucChi <= 0) return 0;
            return (int)Math.Floor(soTienThucChi / SoTienMoiDiem);
        }

        // Số tiền được giảm tương ứng với số điểm sử dụng
        public static decimal TinhTienGiam(int diemSuDung) => diemSuDung <= 0 ? 0 : diemSuDung * GiaTriMotDiem;

        // Số tiền giảm tối đa cho một đơn (30% tổng đơn)
        public static decimal TinhTienGiamToiDa(decimal tongDon) =>
            tongDon <= 0 ? 0 : Math.Floor(tongDon * TyLeGiamToiDa);

        // Số điểm tối đa được dùng = min(điểm khách đang có, điểm tương ứng mức giảm tối đa 30%)
        public static int TinhDiemToiDaDuocDung(decimal tongDon, int diemHienCo)
        {
            if (tongDon <= 0 || diemHienCo <= 0) return 0;
            int diemTheoTran = (int)Math.Floor(TinhTienGiamToiDa(tongDon) / GiaTriMotDiem);
            return Math.Min(diemHienCo, diemTheoTran);
        }

        // Trả về null nếu hợp lệ, ngược lại trả về lý do không hợp lệ
        public static string? KiemTraDiemSuDung(int diemSuDung, decimal tongDon, int diemHienCo)
        {
            if (diemSuDung < 0)
                return "Số điểm sử dụng không được âm.";

            if (diemSuDung == 0)
                return null;

            if (diemSuDung > diemHienCo)
                return $"Khách hàng chỉ có {diemHienCo:N0} điểm, không đủ để dùng {diemSuDung:N0} điểm.";

            int toiDa = TinhDiemToiDaDuocDung(tongDon, diemHienCo);
            if (diemSuDung > toiDa)
                return $"Chỉ được dùng tối đa {toiDa:N0} điểm cho đơn này " +
                       $"(giảm không quá {TyLeGiamToiDa:P0} tổng đơn = {TinhTienGiamToiDa(tongDon):N0} VNĐ).";

            return null;
        }
    }
}