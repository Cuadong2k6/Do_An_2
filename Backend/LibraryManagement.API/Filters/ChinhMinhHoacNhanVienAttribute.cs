using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace LibraryManagement.API.Filters
{
    /// <summary>
    /// Chỉ cho phép truy cập khi là Admin/ThuThu, hoặc khi giá trị trong
    /// tham số tuyến chính là ID của chính người đang đăng nhập.
    ///
    /// Dùng cho các endpoint có định dạng: /api/xxx/{id} và /api/xxx/{readerId}
    ///   [ChinhMinhHoacNhanVien("id")]
    ///   [ChinhMinhHoacNhanVien("readerId")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ChinhMinhHoacNhanVienAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _tenThamSoTuyenDung;

        /// <param name="tenThamSoTuyenDung">Tên tham số trên URL chứa ID bạn đọc (vd: "id", "readerId").</param>
        public ChinhMinhHoacNhanVienAttribute(string tenThamSoTuyenDung)
        {
            _tenThamSoTuyenDung = tenThamSoTuyenDung;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Chưa đăng nhập → 401
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Nhân viên (Admin / ThuThu) được xem tất cả
            if (user.IsInRole("Admin") || user.IsInRole("ThuThu"))
            {
                return;
            }

            // Bạn đọc chỉ được xem dữ liệu của chính mình
            var idCuaToi = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var idTrenUrl = context.HttpContext.Request.RouteValues[_tenThamSoTuyenDung]?.ToString();

            if (!string.IsNullOrWhiteSpace(idCuaToi)
                && Guid.TryParse(idTrenUrl, out var idYeuCau)
                && Guid.TryParse(idCuaToi, out var idToi)
                && idYeuCau == idToi)
            {
                return;
            }

            // Còn lại → 403
            context.Result = new ForbidResult();
        }
    }
}