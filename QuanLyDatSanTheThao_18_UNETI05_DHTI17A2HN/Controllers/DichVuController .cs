// Họ và tên: Tạ Đình Kim 
// Mã sinh viên: 23103100315
// Nội dung thực hiện: Controller DichVu - CRUD dịch vụ (nước uống, thuê vợt, bóng...) bám sát giao diện thực tế.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    public class DichVuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DichVuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== PHÂN QUYỀN (kiểm tra tại Controller) ==================
        private string? VaiTro => HttpContext.Session.GetString("VaiTro");
        private bool LaAdmin() => VaiTro == "Admin";
        private bool LaQuanLy() => VaiTro == "Admin" || VaiTro == "NhanVien";

        private IActionResult TuChoi()
        {
            if (string.IsNullOrEmpty(VaiTro))
                return RedirectToAction("DangNhap", "TaiKhoan");   // chưa đăng nhập
            TempData["Loi"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction("Index", "Home");
        }

        // ================== DANH SÁCH (Hiển thị toàn bộ theo giao diện) ==================
        public async Task<IActionResult> Index()
        {
            if (!LaQuanLy()) return TuChoi();

            var ds = await _context.DichVus
                .AsNoTracking()
                .OrderBy(d => d.MaDichVu)
                .ToListAsync();

            // Gán lại các ViewBag để View không bị lỗi null khi đọc phân trang/tìm kiếm cũ
            ViewBag.TuKhoa = "";
            ViewBag.TrangThai = null;
            ViewBag.SapXep = "";
            ViewBag.Trang = 1;
            ViewBag.TongTrang = 1;
            ViewBag.TongSo = ds.Count;
            ViewBag.LaAdmin = LaAdmin();

            return View(ds);
        }

        // ================== CHI TIẾT ==================
        public async Task<IActionResult> Details(int? id)
        {
            if (!LaQuanLy()) return TuChoi();
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.AsNoTracking().FirstOrDefaultAsync(d => d.MaDichVu == id);
            if (dichVu == null) return NotFound();

            ViewBag.DaSuDung = await _context.ChiTietDatSans
                .Where(c => c.MaDichVu == id).SumAsync(c => (int?)c.SoLuong) ?? 0;
            return View(dichVu);
        }

        // ================== THÊM (Admin) ==================
        public IActionResult Create()
        {
            if (!LaAdmin()) return TuChoi();
            return View(new DichVu { TrangThai = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDichVu,MoTa,DonGia,TrangThai")] DichVu dichVu)
        {
            if (!LaAdmin()) return TuChoi();

            dichVu.TenDichVu = (dichVu.TenDichVu ?? "").Trim();

            if (await _context.DichVus.AnyAsync(d => d.TenDichVu == dichVu.TenDichVu))
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã tồn tại.");

            if (!ModelState.IsValid) return View(dichVu);

            _context.Add(dichVu);
            await _context.SaveChangesAsync();
            TempData["ThanhCong"] = "Thêm dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== SỬA (Admin) ==================
        public async Task<IActionResult> Edit(int? id)
        {
            if (!LaAdmin()) return TuChoi();
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDichVu,TenDichVu,MoTa,DonGia,TrangThai")] DichVu model)
        {
            if (!LaAdmin()) return TuChoi();
            if (id != model.MaDichVu) return NotFound();

            model.TenDichVu = (model.TenDichVu ?? "").Trim();

            if (await _context.DichVus.AnyAsync(d => d.TenDichVu == model.TenDichVu && d.MaDichVu != id))
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null) return NotFound();

            dichVu.TenDichVu = model.TenDichVu;
            dichVu.DonGia = model.DonGia;
            dichVu.MoTa = model.MoTa;
            dichVu.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            TempData["ThanhCong"] = "Cập nhật dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== ĐỔI TRẠNG THÁI (Admin) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            if (!LaAdmin()) return TuChoi();
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null) return NotFound();

            dichVu.TrangThai = !dichVu.TrangThai;
            await _context.SaveChangesAsync();
            TempData["ThanhCong"] = $"Đã chuyển \"{dichVu.TenDichVu}\" sang trạng thái {(dichVu.TrangThai ? "Hoạt động" : "Ngừng cung cấp")}.";
            return RedirectToAction(nameof(Index));
        }

        // ================== XÓA (Admin) ==================
        public async Task<IActionResult> Delete(int? id)
        {
            if (!LaAdmin()) return TuChoi();
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.AsNoTracking().FirstOrDefaultAsync(d => d.MaDichVu == id);
            if (dichVu == null) return NotFound();

            ViewBag.DaSuDung = await _context.ChiTietDatSans.AnyAsync(c => c.MaDichVu == id);
            return View(dichVu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!LaAdmin()) return TuChoi();

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null) return NotFound();

            if (await _context.ChiTietDatSans.AnyAsync(c => c.MaDichVu == id))
            {
                TempData["Loi"] = "Dịch vụ đã được sử dụng trong đơn đặt sân, không thể xóa. Hãy chuyển sang trạng thái ngừng cung cấp.";
                return RedirectToAction(nameof(Index));
            }

            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã xóa dịch vụ.";
            return RedirectToAction(nameof(Index));
        }
    }
}