// Họ và tên: Bùi Thị Thu Trang
// Mã sinh viên: 23103100103
// Nội dung thực hiện: Quản lý loại thiết bị - CRUD, kiểm tra trùng tên và ràng buộc xóa

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Data;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Entities;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    public class LoaiThietBiController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public LoaiThietBiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /LoaiThietBi
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 5;
            var query = _context.LoaiThietBis.AsQueryable();

            // Tìm kiếm theo tên loại hoặc hãng sản xuất
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.TenLoai.Contains(searchString)
                                      || l.HangSanXuat.Contains(searchString));
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(l => l.MaLoai)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;

            return View(items);
        }

        // GET: /LoaiThietBi/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var loai = await _context.LoaiThietBis
                .Include(l => l.ThietBis)
                .FirstOrDefaultAsync(l => l.MaLoai == id);

            if (loai == null) return NotFound();
            return View(loai);
        }

        // GET: /LoaiThietBi/Create
        public IActionResult Create() => View();

        // POST: /LoaiThietBi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiThietBi loai)
        {
            // Kiểm tra trùng tên loại thiết bị
            if (await _context.LoaiThietBis.AnyAsync(l => l.TenLoai == loai.TenLoai))
            {
                ModelState.AddModelError("TenLoai", "Tên loại thiết bị đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(loai);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm loại thiết bị thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(loai);
        }

        // GET: /LoaiThietBi/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var loai = await _context.LoaiThietBis.FindAsync(id);
            if (loai == null) return NotFound();
            return View(loai);
        }

        // POST: /LoaiThietBi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LoaiThietBi loai)
        {
            if (id != loai.MaLoai) return NotFound();

            // Kiểm tra trùng tên (trừ chính nó)
            if (await _context.LoaiThietBis.AnyAsync(l => l.TenLoai == loai.TenLoai && l.MaLoai != id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại thiết bị đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loai);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật loại thiết bị thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.LoaiThietBis.Any(l => l.MaLoai == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loai);
        }

        // GET: /LoaiThietBi/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var loai = await _context.LoaiThietBis
                .Include(l => l.ThietBis)
                .FirstOrDefaultAsync(l => l.MaLoai == id);

            if (loai == null) return NotFound();
            return View(loai);
        }

        // POST: /LoaiThietBi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loai = await _context.LoaiThietBis
                .Include(l => l.ThietBis)
                .FirstOrDefaultAsync(l => l.MaLoai == id);

            if (loai == null) return NotFound();

            // Không xóa nếu đã có thiết bị tham chiếu
            if (loai.ThietBis.Any())
            {
                TempData["Error"] = $"Không thể xóa loại '{loai.TenLoai}' vì đang có {loai.ThietBis.Count} thiết bị sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _context.LoaiThietBis.Remove(loai);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa loại thiết bị thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}