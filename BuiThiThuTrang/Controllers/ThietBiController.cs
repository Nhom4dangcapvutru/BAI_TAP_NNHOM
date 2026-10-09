// Họ và tên: Nguyễn Văn Tú
// Mã sinh viên: 23103100077
// Nội dung thực hiện: Module 2 - Quản lý thiết bị: CRUD, Tìm kiếm, Lọc kết hợp, Sắp xếp, Phân trang LINQ

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Data;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Common;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Constants;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Entities;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.ViewModels;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    public class ThietBiController : Controller
    {
        private const int PageSize = 6; // Số lượng bản ghi trên 1 trang
        private readonly ApplicationDbContext _context;

        public ThietBiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        // ==========================================
        public async Task<IActionResult> Index(
            string? searchString,
            int? maLoai,
            string? hangSanXuat,
            string? trangThaiBaoHanh,
            string? trangThaiThietBi,
            decimal? giaTu,
            decimal? giaDen,
            string? sortOrder,
            int pageIndex = 1)
        {
            // Truy vấn gốc (dùng Include để lấy thông tin bảng quan hệ)
            var query = _context.ThietBis
                .Include(t => t.LoaiThietBi)
                .Include(t => t.KhachHang)
                .AsNoTracking()
                .AsQueryable();

            // A. TÌM KIẾM (Tên thiết bị, Serial, Tên khách hàng)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var search = searchString.Trim();
                query = query.Where(t => t.TenThietBi.Contains(search)
                                      || t.SerialNumber.Contains(search)
                                      || t.KhachHang!.HoTen.Contains(search));
            }

            // B. LỌC KẾT HỢP
            if (maLoai.HasValue && maLoai.Value > 0)
            {
                query = query.Where(t => t.MaLoai == maLoai.Value);
            }

            if (!string.IsNullOrWhiteSpace(hangSanXuat))
            {
                query = query.Where(t => t.LoaiThietBi!.HangSanXuat == hangSanXuat);
            }

            if (trangThaiBaoHanh == "con")
            {
                query = query.Where(t => t.HanBaoHanh.Date >= DateTime.Now.Date);
            }
            else if (trangThaiBaoHanh == "het")
            {
                query = query.Where(t => t.HanBaoHanh.Date < DateTime.Now.Date);
            }

            if (!string.IsNullOrWhiteSpace(trangThaiThietBi))
            {
                query = query.Where(t => t.TrangThai == trangThaiThietBi);
            }

            if (giaTu.HasValue)
            {
                query = query.Where(t => t.GiaTri >= giaTu.Value);
            }

            if (giaDen.HasValue)
            {
                query = query.Where(t => t.GiaTri <= giaDen.Value);
            }

            // C. SẮP XẾP BẰNG LINQ
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["BuyDateSortParam"] = sortOrder == "date_buy_asc" ? "date_buy_desc" : "date_buy_asc";
            ViewData["WarrantySortParam"] = sortOrder == "warranty_asc" ? "warranty_desc" : "warranty_asc";
            ViewData["PriceSortParam"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(t => t.TenThietBi),
                "date_buy_asc" => query.OrderBy(t => t.NgayMua),
                "date_buy_desc" => query.OrderByDescending(t => t.NgayMua),
                "warranty_asc" => query.OrderBy(t => t.HanBaoHanh),
                "warranty_desc" => query.OrderByDescending(t => t.HanBaoHanh),
                "price_asc" => query.OrderBy(t => t.GiaTri),
                "price_desc" => query.OrderByDescending(t => t.GiaTri),
                _ => query.OrderBy(t => t.TenThietBi) // Mặc định: Tên A -> Z
            };

            // D. PHÂN TRANG (Dùng Skip/Take qua PaginatedList)
            var paginatedItems = await PaginatedList<ThietBi>.CreateAsync(query, pageIndex, PageSize);

            // E. LẤY DỮ LIỆU CHO CÁC DROPDOWN
            var dsLoai = await _context.LoaiThietBis.AsNoTracking().ToListAsync();
            var dsHang = dsLoai.Select(l => l.HangSanXuat).Distinct().ToList();
            var dsTrangThai = new List<string>
            {
                TrangThaiThietBi.HoatDong,
                TrangThaiThietBi.DangBaoHanh,
                TrangThaiThietBi.DangSuaChua,
                TrangThaiThietBi.Hong
            };

            var viewModel = new ThietBiIndexViewModel
            {
                DanhSachThietBi = paginatedItems,
                SearchString = searchString,
                MaLoai = maLoai,
                HangSanXuat = hangSanXuat,
                TrangThaiBaoHanh = trangThaiBaoHanh,
                TrangThaiThietBi = trangThaiThietBi,
                GiaTu = giaTu,
                GiaDen = giaDen,
                SortOrder = sortOrder,
                LoaiThietBiList = new SelectList(dsLoai, "MaLoai", "TenLoai", maLoai),
                HangSanXuatList = new SelectList(dsHang, hangSanXuat),
                TrangThaiList = new SelectList(dsTrangThai, trangThaiThietBi)
            };

            return View(viewModel);
        }

        // ==========================================
        // 2. CHI TIẾT THIẾT BỊ
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var thietBi = await _context.ThietBis
                .Include(t => t.LoaiThietBi)
                .Include(t => t.KhachHang)
                .Include(t => t.PhieuSuaChuas) // Xem lịch sử sửa chữa
                .FirstOrDefaultAsync(m => m.MaThietBi == id);

            if (thietBi == null) return NotFound();

            return View(thietBi);
        }

        // ==========================================
        // 3. THÊM MỚI THIẾT BỊ (CREATE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownDataAsync();
            var model = new ThietBi
            {
                NgayMua = DateTime.Now.Date,
                HanBaoHanh = DateTime.Now.Date.AddYears(1)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThietBi thietBi)
        {
            // Kiểm tra trùng SerialNumber
            if (await _context.ThietBis.AnyAsync(t => t.SerialNumber == thietBi.SerialNumber))
            {
                ModelState.AddModelError("SerialNumber", "Số Serial (S/N) này đã tồn tại trong hệ thống.");
            }

            // Kiểm tra nghiệp vụ ngày
            if (thietBi.HanBaoHanh < thietBi.NgayMua)
            {
                ModelState.AddModelError("HanBaoHanh", "Hạn bảo hành không được trước ngày mua thiết bị.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(thietBi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mới thiết bị thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownDataAsync(thietBi.MaLoai, thietBi.MaKhachHang);
            return View(thietBi);
        }

        // ==========================================
        // 4. CHỈNH SỬA THIẾT BỊ (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thietBi = await _context.ThietBis.FindAsync(id);
            if (thietBi == null) return NotFound();

            await LoadDropdownDataAsync(thietBi.MaLoai, thietBi.MaKhachHang);
            return View(thietBi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThietBi thietBi)
        {
            if (id != thietBi.MaThietBi) return NotFound();

            // Kiểm tra trùng SerialNumber với thiết bị khác
            if (await _context.ThietBis.AnyAsync(t => t.SerialNumber == thietBi.SerialNumber && t.MaThietBi != id))
            {
                ModelState.AddModelError("SerialNumber", "Số Serial này đang trùng với thiết bị khác.");
            }

            if (thietBi.HanBaoHanh < thietBi.NgayMua)
            {
                ModelState.AddModelError("HanBaoHanh", "Hạn bảo hành không được trước ngày mua thiết bị.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thietBi);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thiết bị thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.ThietBis.AnyAsync(e => e.MaThietBi == thietBi.MaThietBi))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownDataAsync(thietBi.MaLoai, thietBi.MaKhachHang);
            return View(thietBi);
        }

        // ==========================================
        // 5. XÓA THIẾT BỊ (DELETE - Kiểm tra ràng buộc lịch sử)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var thietBi = await _context.ThietBis
                .Include(t => t.LoaiThietBi)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(m => m.MaThietBi == id);

            if (thietBi == null) return NotFound();

            // Kiểm tra nghiệp vụ: đã từng có phiếu sửa chữa hay chưa
            var countPhieu = await _context.PhieuSuaChuas.CountAsync(p => p.MaThietBi == id);
            ViewBag.HasHistory = countPhieu > 0;
            ViewBag.HistoryCount = countPhieu;

            return View(thietBi);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Nghiệp vụ bắt buộc (Mục 6.2): Không xóa đối tượng đã phát sinh lịch sử
            var hasHistory = await _context.PhieuSuaChuas.AnyAsync(p => p.MaThietBi == id);
            if (hasHistory)
            {
                TempData["ErrorMessage"] = "Không thể xóa! Thiết bị này đã có lịch sử sửa chữa/bảo hành trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var thietBi = await _context.ThietBis.FindAsync(id);
            if (thietBi != null)
            {
                _context.ThietBis.Remove(thietBi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thiết bị thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Hàm tiện ích load Dropdown lấy trực tiếp từ Database
        private async Task LoadDropdownDataAsync(int? selectedLoai = null, int? selectedKhachHang = null)
        {
            ViewBag.MaLoai = new SelectList(await _context.LoaiThietBis.AsNoTracking().ToListAsync(), "MaLoai", "TenLoai", selectedLoai);
            ViewBag.MaKhachHang = new SelectList(await _context.KhachHangs.AsNoTracking().ToListAsync(), "MaKhachHang", "HoTen", selectedKhachHang);
        }
    }
}