// Họ và tên: Nguyễn Thị Thảo
// Mã sinh viên: 23103100092
// Nội dung thực hiện: Quản lý khách hàng - CRUD, tìm kiếm, lọc, sắp xếp, phân trang
// Phân quyền: Admin/Kỹ thuật viên được truy cập; Khách hàng bị chặn.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Data;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Helpers;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Constants;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Entities;
using QuanLyThietBi_UNETI04_DHTI17A2HN.ViewModels;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KhachHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // KIỂM TRA PHÂN QUYỀN: chỉ Admin/Kỹ thuật viên được dùng các chức năng
        // ====================================================================
        private bool KiemTraQuyenNhanVien()
        {
            return SessionHelper.IsAdminOrKyThuatVien(HttpContext.Session);
        }

        // ====================================================================
        // 1. INDEX - Danh sách khách hàng + Tìm kiếm + Lọc + Sắp xếp + Phân trang
        // ====================================================================
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string? loaiKhachHang,
            string? trangThai,
            string? sapXep,
            int trang = 1)
        {
            // Phân quyền
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            // Khởi tạo query
            var query = _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .AsQueryable();

            // ----------------------------------------------------------------
            // TÌM KIẾM: theo Họ tên, SĐT, Email, CCCD
            // ----------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                query = query.Where(k =>
                    k.HoTen.Contains(tuKhoa) ||
                    k.SoDienThoai.Contains(tuKhoa) ||
                    k.Email.Contains(tuKhoa) ||
                    (k.CCCD != null && k.CCCD.Contains(tuKhoa)));
            }

            // ----------------------------------------------------------------
            // LỌC: theo loại khách hàng và trạng thái
            // ----------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(loaiKhachHang))
            {
                query = query.Where(k => k.LoaiKhachHang == loaiKhachHang);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(k => k.TrangThai == trangThai);
            }

            // ----------------------------------------------------------------
            // SẮP XẾP
            // ----------------------------------------------------------------
            query = sapXep switch
            {
                "ten_desc" => query.OrderByDescending(k => k.HoTen),
                "ngaytao_asc" => query.OrderBy(k => k.NgayTao),
                "ngaytao_desc" => query.OrderByDescending(k => k.NgayTao),
                _ => query.OrderBy(k => k.HoTen) // Mặc định: tên A → Z
            };

            // ----------------------------------------------------------------
            // PHÂN TRANG
            // ----------------------------------------------------------------
            const int kichThuocTrang = 10;
            int tongSoBanGhi = await query.CountAsync();
            int tongSoTrang = (int)Math.Ceiling(tongSoBanGhi / (double)kichThuocTrang);

            // Đảm bảo trang hợp lệ
            if (trang < 1) trang = 1;
            if (trang > tongSoTrang && tongSoTrang > 0) trang = tongSoTrang;

            var khachHangs = await query
                .Skip((trang - 1) * kichThuocTrang)
                .Take(kichThuocTrang)
                .ToListAsync();

            // ----------------------------------------------------------------
            // TẠO VIEWMODEL
            // ----------------------------------------------------------------
            var viewModel = new KhachHangFilterViewModel
            {
                KhachHangs = khachHangs,
                TuKhoa = tuKhoa,
                LoaiKhachHang = loaiKhachHang,
                TrangThai = trangThai,
                SapXep = sapXep,
                TrangHienTai = trang,
                TongSoTrang = tongSoTrang,
                TongSoBanGhi = tongSoBanGhi,
                KichThuocTrang = kichThuocTrang
            };

            return View(viewModel);
        }

        // ====================================================================
        // 2. DETAILS - Xem chi tiết khách hàng
        // ====================================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.ThietBis)
                .Include(k => k.PhieuSuaChuas)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);

            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            return View(khachHang);
        }

        // ====================================================================
        // 3. CREATE - GET: Hiển thị form thêm khách hàng
        // ====================================================================
        [HttpGet]
        public IActionResult Create()
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // ====================================================================
        // 4. CREATE - POST: Xử lý thêm khách hàng
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HoTen,SoDienThoai,Email,DiaChi,CCCD,LoaiKhachHang,TrangThai,GhiChu,MaSoThue")]
            KhachHang khachHang)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            // ----------------------------------------------------------------
            // KIỂM TRA NGHIỆP VỤ
            // ----------------------------------------------------------------

            // 1. Kiểm tra SĐT không trùng
            bool trungSdt = await _context.KhachHangs
                .AnyAsync(k => k.SoDienThoai == khachHang.SoDienThoai);

            if (trungSdt)
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã được đăng ký cho khách hàng khác.");
            }

            // 2. Kiểm tra Email không trùng
            bool trungEmail = await _context.KhachHangs
                .AnyAsync(k => k.Email == khachHang.Email);

            if (trungEmail)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký cho khách hàng khác.");
            }

            // 3. Kiểm tra CCCD không trùng (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(khachHang.CCCD))
            {
                bool trungCccd = await _context.KhachHangs
                    .AnyAsync(k => k.CCCD == khachHang.CCCD);

                if (trungCccd)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD này đã được đăng ký.");
                }
            }

            // 4. Nếu là Doanh nghiệp → bắt buộc có Mã số thuế
            if (khachHang.LoaiKhachHang == LoaiKhachHangConstant.DoanhNghiep
                && string.IsNullOrWhiteSpace(khachHang.MaSoThue))
            {
                ModelState.AddModelError("MaSoThue", "Khách hàng doanh nghiệp bắt buộc phải có mã số thuế.");
            }

            // ----------------------------------------------------------------
            // LƯU DATABASE
            // ----------------------------------------------------------------
            if (ModelState.IsValid)
            {
                khachHang.NgayTao = DateTime.Now; // Hệ thống tự gán
                _context.Add(khachHang);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã thêm khách hàng '{khachHang.HoTen}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(khachHang);
        }

        // ====================================================================
        // 5. EDIT - GET: Hiển thị form sửa khách hàng
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            return View(khachHang);
        }

        // ====================================================================
        // 6. EDIT - POST: Xử lý sửa khách hàng
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MaKhachHang,MaTaiKhoan,HoTen,SoDienThoai,Email,DiaChi,CCCD,NgayTao,LoaiKhachHang,TrangThai,GhiChu,MaSoThue")]
            KhachHang khachHang)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            if (id != khachHang.MaKhachHang)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // ----------------------------------------------------------------
            // KIỂM TRA NGHIỆP VỤ (loại trừ chính khách hàng này)
            // ----------------------------------------------------------------
            bool trungSdt = await _context.KhachHangs
                .AnyAsync(k => k.SoDienThoai == khachHang.SoDienThoai
                            && k.MaKhachHang != khachHang.MaKhachHang);

            if (trungSdt)
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã được đăng ký cho khách hàng khác.");
            }

            bool trungEmail = await _context.KhachHangs
                .AnyAsync(k => k.Email == khachHang.Email
                            && k.MaKhachHang != khachHang.MaKhachHang);

            if (trungEmail)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký cho khách hàng khác.");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.CCCD))
            {
                bool trungCccd = await _context.KhachHangs
                    .AnyAsync(k => k.CCCD == khachHang.CCCD
                                && k.MaKhachHang != khachHang.MaKhachHang);

                if (trungCccd)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD này đã được đăng ký.");
                }
            }

            if (khachHang.LoaiKhachHang == LoaiKhachHangConstant.DoanhNghiep
                && string.IsNullOrWhiteSpace(khachHang.MaSoThue))
            {
                ModelState.AddModelError("MaSoThue", "Khách hàng doanh nghiệp bắt buộc phải có mã số thuế.");
            }

            // ----------------------------------------------------------------
            // LƯU DATABASE
            // ----------------------------------------------------------------
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khachHang);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã cập nhật khách hàng '{khachHang.HoTen}' thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhachHangExists(khachHang.MaKhachHang))
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(khachHang);
        }

        // ====================================================================
        // 7. DELETE - GET: Hiển thị trang xác nhận xóa
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.ThietBis)
                .Include(k => k.PhieuSuaChuas)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);

            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            return View(khachHang);
        }

        // ====================================================================
        // 8. DELETE - POST: Xử lý xóa khách hàng
        // ====================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!KiemTraQuyenNhanVien())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.ThietBis)
                .Include(k => k.PhieuSuaChuas)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);

            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            // ----------------------------------------------------------------
            // KIỂM TRA NGHIỆP VỤ: Không cho xóa nếu đã có thiết bị hoặc phiếu
            // ----------------------------------------------------------------
            if (khachHang.ThietBis.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng vì đang có thiết bị trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            if (khachHang.PhieuSuaChuas.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng vì đang có phiếu sửa chữa trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            string tenKhachHang = khachHang.HoTen;

            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa khách hàng '{tenKhachHang}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        // HELPER: Kiểm tra khách hàng có tồn tại không
        // ====================================================================
        private bool KhachHangExists(int id)
        {
            return _context.KhachHangs.Any(e => e.MaKhachHang == id);
        }
    }
}