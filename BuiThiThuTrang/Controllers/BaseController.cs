// Họ và tên: Bùi Thị Thu Trang
// Mã sinh viên: 23103100103
// Nội dung thực hiện: BaseController - Kiểm tra đăng nhập + phân quyền
// ĐÃ SỬA: Cho phép PUBLIC controller (Home, Account) chạy không cần login
//         Redirect login kèm returnUrl để quay lại đúng trang

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyThietBi_UNETI04_DHTI17A2HN.Models.Constants;

namespace QuanLyThietBi_UNETI04_DHTI17A2HN.Controllers
{
    public class BaseController : Controller
    {
        // ✅ DANH SÁCH CONTROLLER PUBLIC - Không cần đăng nhập
        private static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Home",      // Trang chủ, giới thiệu, dịch vụ
            "Account"    // Login, Logout, Register, AccessDenied
        };

        // ✅ CÁC CONTROLLER CHỈ ADMIN - Bắt buộc login + role Admin
        private static readonly HashSet<string> AdminOnlyControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "TaiKhoan",
            "LoaiThietBi"
        };

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";

            // 1. PUBLIC CONTROLLER → cho qua, không cần login
            if (PublicControllers.Contains(controller))
            {
                base.OnActionExecuting(context);
                return;
            }

            // 2. Chưa đăng nhập → chuyển về Login kèm returnUrl để quay lại đúng trang
            if (string.IsNullOrEmpty(vaiTro))
            {
                var returnUrl = context.HttpContext.Request.Path
                              + context.HttpContext.Request.QueryString;
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục.";

                context.Result = new RedirectToActionResult(
                    "Login", "Account", new { returnUrl = returnUrl.ToString() });
                return;
            }

            // 3. Phân quyền ADMIN-ONLY
            if (AdminOnlyControllers.Contains(controller) && vaiTro != VaiTroConstant.Admin)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này.";
                context.Result = RedirectToAction("AccessDenied", "Account");
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}