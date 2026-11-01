
// Họ và tên: Lê Bích Ngọc
// Mã sinh viên: 23103100111
// Nội dung thực hiện: Controller LoaiSan - Quản lý dữ liệu nền (CRUD, kiểm tra trùng tên, kiểm tra tham chiếu)

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Filters;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Models.Entities;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Repositories.Interfaces;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Controllers
{
    [AuthorizeRole("Admin")]
    public class LoaiSanController : Controller
    {
        private readonly ILoaiSanRepository _repository;

        public LoaiSanController(ILoaiSanRepository repository)
        {
            _repository = repository;
        }

        // Kiểm tra quyền Admin tại Controller (Yêu cầu bắt buộc của đề bài)
        private bool KiemTraQuyenAdmin()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin";
        }

        // 1. Xem danh sách loại sân
        public async Task<IActionResult> Index()
        {
            if (!KiemTraQuyenAdmin())
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var dsLoaiSan = await _repository.GetAllAsync();
            return View(dsLoaiSan);
        }

        // 2. Thêm mới - GET
        [HttpGet]
        public IActionResult Create()
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            return View();
        }

        // Thêm mới - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiSan loaiSan)
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            // Kiểm tra tên loại sân bắt buộc và không được trùng (Dùng LINQ)
            if (await _repository.TenLoaiExistsAsync(loaiSan.TenLoai))
            {
                ModelState.AddModelError("TenLoai", "Tên loại sân này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid)
            {
                await _repository.AddAsync(loaiSan);
                TempData["Success"] = "Thêm mới loại sân thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(loaiSan);
        }

        // 3. Sửa - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            if (id == null) return NotFound();

            var loaiSan = await _repository.GetByIdAsync(id.Value);
            if (loaiSan == null) return NotFound();

            return View(loaiSan);
        }

        // Sửa - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LoaiSan loaiSan)
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            if (id != loaiSan.MaLoaiSan) return NotFound();

            // Kiểm tra trùng tên (trừ chính nó)
            if (await _repository.TenLoaiExistsAsync(loaiSan.TenLoai, id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại sân này đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(loaiSan);
                    TempData["Success"] = "Cập nhật loại sân thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _repository.LoaiSanExistsAsync(loaiSan.MaLoaiSan)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loaiSan);
        }

        // 4. Xóa loại sân (Có kiểm tra dữ liệu đã được tham chiếu hay chưa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var loaiSan = await _repository.GetByIdAsync(id);
            if (loaiSan == null) return NotFound();

            // QUAN TRỌNG: Kiểm tra dữ liệu đã được tham chiếu trong bảng SanTheThao chưa trước khi xóa
            bool daThamChieu = await _repository.LoaiSanDaDuocSuDungAsync(id);
            if (daThamChieu)
            {
                TempData["Error"] = "Không thể xóa vì đã có sân thể thao thuộc loại sân này!";
                return RedirectToAction(nameof(Index));
            }

            await _repository.DeleteAsync(id);
            TempData["Success"] = "Xóa loại sân thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
