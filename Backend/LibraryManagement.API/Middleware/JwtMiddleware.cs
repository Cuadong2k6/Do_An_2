// Bảo mật và middleware cho authentication/authorization
namespace LibraryManagement.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;
        private readonly IJwtService _jwtService;

        public JwtMiddleware(
            RequestDelegate next,
            ILogger<JwtMiddleware> logger,
            IJwtService jwtService)
        {
            _next = next;
            _logger = logger;
            _jwtService = jwtService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                if (_jwtService.ValidateToken(token))
                {
                    context.Items["UserId"] = _jwtService.GetUserIdFromToken(token);
                    context.Items["Email"] = _jwtService.GetEmailFromToken(token);
                    context.Items["Roles"] = _jwtService.GetRolesFromToken(token);
                }
                else
                {
                    _logger.LogWarning("Token JWT không hợp lệ từ {IP}", context.Connection.RemoteIpAddress);
                }
            }

            await _next(context);
        }
    }
}