// Họ và tên: Bùi Thị Thu Trang
// Mã sinh viên:23103100103
// Nội dung thực hiện: HomeController - Trang chủ PUBLIC cho khách vãng lai
//                     Kế thừa BaseController nhưng được whitelist là PUBLIC

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Data;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models;
using System.Diagnostics;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    /// <summary>
    /// HomeController - Xử lý trang chủ, giới thiệu, dịch vụ, liên hệ.
    /// Đây là controller PUBLIC: khách vãng lai (chưa đăng nhập) vẫn xem được.
    /// BaseController đã whitelist "Home" trong danh sách PublicControllers.
    /// </summary>
    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // GET: /  hoặc  /Home/Index
        // Trang chủ PUBLIC — Hiển thị thống kê + thiết bị mới + CTA
        // ==============================================================
        public async Task<IActionResult> Index()
        {
            // ---- Thống kê nổi bật cho khách vãng lai xem ----
            ViewBag.SoLoaiThietBi = await _context.LoaiThietBis.CountAsync();
            ViewBag.SoThietBi = await _context.ThietBis.CountAsync();
            ViewBag.SoKhachHang = await _context.KhachHangs.CountAsync();
            ViewBag.SoPhieuHoanThanh = await _context.PhieuSuaChuas
                .CountAsync(p => p.TrangThai == "Hoàn thành");

            // ---- Top 6 thiết bị mới nhất để trưng bày ----
            ViewBag.ThietBiMoi = await _context.ThietBis
                .Include(t => t.LoaiThietBi)
                .OrderByDescending(t => t.MaThietBi)
                .Take(6)
                .ToListAsync();

            return View();
        }

        // ==============================================================
        // GET: /Home/About
        // Trang giới thiệu PUBLIC
        // ==============================================================
        public IActionResult About()
        {
            ViewData["Title"] = "Giới thiệu";
            return View();
        }

        // ==============================================================
        // GET: /Home/Services
        // Trang dịch vụ PUBLIC
        // ==============================================================
        public IActionResult Services()
        {
            ViewData["Title"] = "Dịch vụ";
            return View();
        }

        // ==============================================================
        // GET: /Home/Contact
        // Trang liên hệ PUBLIC
        // ==============================================================
        public IActionResult Contact()
        {
            ViewData["Title"] = "Liên hệ";
            return View();
        }

        // ==============================================================
        // GET: /Home/Privacy
        // Trang chính sách bảo mật PUBLIC
        // ==============================================================
        public IActionResult Privacy()
        {
            ViewData["Title"] = "Chính sách bảo mật";
            return View();
        }

        // ==============================================================
        // GET: /Home/Error
        // Trang lỗi hệ thống
        // ==============================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}