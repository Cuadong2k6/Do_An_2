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
        if (user.IsInRole("BanDoc")
            && user.FindFirstValue("onboarding_status") == "1")
        {
            context.Result = new ObjectResult(Model.Shared.ResponseModel.Fail(
                "Tài khoản đang chờ thanh toán, chưa thể thực hiện nghiệp vụ thư viện."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
