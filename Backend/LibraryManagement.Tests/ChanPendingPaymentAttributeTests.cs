using FluentAssertions;
using LibraryManagement.API.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Xunit;

namespace LibraryManagement.Tests
{
    public class ChanPendingPaymentAttributeTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("4")]
        public void BanDoc_OnboardingBiChan_PhaiTra403(string onboarding)
        {
            var attr = new ChanPendingPaymentAttribute();
            var context = TaoContext("BanDoc", onboarding);

            attr.OnAuthorization(context);

            context.Result.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)context.Result!;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("3")]
        public void BanDoc_OnboardingHopLe_KhongBiChan(string onboarding)
        {
            var attr = new ChanPendingPaymentAttribute();
            var context = TaoContext("BanDoc", onboarding);

            attr.OnAuthorization(context);

            context.Result.Should().BeNull();
        }

        [Fact]
        public void Staff_KhongBiChanTheoOnboarding()
        {
            var attr = new ChanPendingPaymentAttribute();
            var context = TaoContext("Admin", "2");

            attr.OnAuthorization(context);

            context.Result.Should().BeNull();
        }

        private static AuthorizationFilterContext TaoContext(string role, string onboarding)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim("onboarding_status", onboarding)
            };

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = user };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}
