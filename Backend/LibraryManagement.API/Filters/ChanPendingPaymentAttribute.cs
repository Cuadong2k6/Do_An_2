using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace LibraryManagement.API.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ChanPendingPaymentAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var onboardingStatus = user.FindFirstValue("onboarding_status");

        if (user.IsInRole("BanDoc")
            && (onboardingStatus == "1" || onboardingStatus == "2" || onboardingStatus == "4"))
        {
            var thongDiep = onboardingStatus switch
            {
                "1" => "Tài khoản đang chờ thanh toán, chưa thể thực hiện nghiệp vụ thư viện.",
                "2" => "Tài khoản đang chờ duyệt kích hoạt, chưa thể thực hiện nghiệp vụ thư viện.",
                "4" => "Yêu cầu kích hoạt đã bị từ chối. Vui lòng liên hệ thư viện.",
                _ => "Tài khoản chưa thể thực hiện nghiệp vụ thư viện."
            };

            context.Result = new ObjectResult(Model.Shared.ResponseModel.Fail(
                thongDiep))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
