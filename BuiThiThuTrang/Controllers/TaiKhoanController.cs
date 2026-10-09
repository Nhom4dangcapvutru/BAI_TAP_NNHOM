// Họ và tên: Bùi Thị Thu Trang
// Mã sinh viên: 23103100130
// Nội dung thực hiện: Quản lý tài khoản - CRUD, khóa/mở khóa, kiểm tra trùng tên đăng nhập

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Data;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Constants;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Entities;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    public class TaiKhoanController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public TaiKhoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /TaiKhoan
        public async Task<IActionResult> Index(string searchString, string vaiTro, string trangThai, int page = 1)
        {
            int pageSize = 5;
            var query = _context.TaiKhoans.AsQueryable();

            // Tìm kiếm theo tên đăng nhập / họ tên / email
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.TenDangNhap.Contains(searchString)
                                      || t.HoTen.Contains(searchString)
                                      || t.Email.Contains(searchString));
            }

            // Lọc theo vai trò
            if (!string.IsNullOrEmpty(vaiTro))
                query = query.Where(t => t.VaiTro == vaiTro);

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(t => t.TrangThai == trangThai);

            // Sắp xếp + Phân trang
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(t => t.MaTaiKhoan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;
            ViewBag.VaiTro = vaiTro;
            ViewBag.TrangThai = trangThai;

            return View(items);
        }

        // GET: /TaiKhoan/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == id);

            if (taiKhoan == null) return NotFound();
            return View(taiKhoan);
        }

        // GET: /TaiKhoan/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /TaiKhoan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoan taiKhoan)
        {
            // Kiểm tra trùng tên đăng nhập
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == taiKhoan.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra trùng email
            if (await _context.TaiKhoans.AnyAsync(t => t.Email == taiKhoan.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(taiKhoan);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm tài khoản thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(taiKhoan);
        }

        // GET: /TaiKhoan/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null) return NotFound();
            return View(taiKhoan);
        }

        // POST: /TaiKhoan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.MaTaiKhoan) return NotFound();

            // Kiểm tra trùng tên đăng nhập (trừ chính nó)
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == taiKhoan.TenDangNhap && t.MaTaiKhoan != id))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra trùng email (trừ chính nó)
            if (await _context.TaiKhoans.AnyAsync(t => t.Email == taiKhoan.Email && t.MaTaiKhoan != id))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taiKhoan);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật tài khoản thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TaiKhoans.Any(t => t.MaTaiKhoan == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(taiKhoan);
        }

        // GET: /TaiKhoan/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
            if (taiKhoan == null) return NotFound();
            return View(taiKhoan);
        }

        // POST: /TaiKhoan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .Include(t => t.ChiTietSuaChuas)
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == id);

            if (taiKhoan == null) return NotFound();

            // Không xóa nếu đã phát sinh lịch sử
            if (taiKhoan.ChiTietSuaChuas.Any())
            {
                TempData["Error"] = "Không thể xóa tài khoản đã có lịch sử sửa chữa. Hãy khóa thay vì xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu là khách hàng -> xóa luôn khách hàng
            if (taiKhoan.KhachHang != null)
            {
                _context.KhachHangs.Remove(taiKhoan.KhachHang);
            }

            _context.TaiKhoans.Remove(taiKhoan);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /TaiKhoan/ToggleStatus/5 - Khóa / Mở khóa
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null) return NotFound();

            taiKhoan.TrangThai = taiKhoan.TrangThai == TrangThaiTaiKhoan.HoatDong
                ? TrangThaiTaiKhoan.BiKhoa
                : TrangThaiTaiKhoan.HoatDong;

            _context.Update(taiKhoan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(taiKhoan.TrangThai == TrangThaiTaiKhoan.BiKhoa ? "khóa" : "mở khóa")} tài khoản {taiKhoan.TenDangNhap}.";
            return RedirectToAction(nameof(Index));
        }
    }
}