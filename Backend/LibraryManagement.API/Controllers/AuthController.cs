using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model.Shared;
using LibraryManagement.API.Services;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IOtpService _otpService;
        private readonly GoogleTokenValidator _googleTokenValidator;
        private readonly IConfiguration _configuration;

        public AuthController(AuthService authService, IOtpService otpService, GoogleTokenValidator googleTokenValidator, IConfiguration configuration)
        {
            _authService = authService;
            _otpService = otpService;
            _googleTokenValidator = googleTokenValidator;
            _configuration = configuration;
        }

        [HttpGet("google-config")]
        [AllowAnonymous]
        public IActionResult LayCauHinhGoogle()
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID")
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ResponseModel.Fail("Đăng nhập Google chưa được cấu hình trên máy chủ."));

            return Ok(new { client_id = clientId });
        }

        /// <summary>Đăng nhập hoặc đăng ký bạn đọc bằng Google ID token.</summary>
        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> DangNhapGoogle([FromBody] GoogleLoginDto? req)
        {
            if (string.IsNullOrWhiteSpace(req?.id_token))
                return BadRequest(ResponseModel.Fail("Google ID token không được để trống."));

            try
            {
                var payload = await _googleTokenValidator.XacThucAsync(req.id_token);
                var result = await _authService.DangNhapGoogleAsync(payload.Subject, payload.Email, payload.Name ?? payload.Email);
                return result.success ? Ok(result) : Unauthorized(result);
            }
            catch (Google.Apis.Auth.InvalidJwtException)
            {
                return Unauthorized(ResponseModel.Fail("Google ID token không hợp lệ hoặc đã hết hạn."));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("chưa cấu hình", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ResponseModel.Fail("Đăng nhập Google chưa được cấu hình trên máy chủ."));
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(ResponseModel.Fail("Google ID token thiếu thông tin hợp lệ."));
            }
        }

        /// <summary>Đăng nhập (Admin / Thủ thư / Bạn đọc)</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> dangnhap([FromBody] LoginDto req)
        {
            var result = await _authService.dangnhap(req.tendangnhap, req.matkhau);
            return result.success ? Ok(result) : Unauthorized(result);
        }

        /// <summary>Yêu cầu đặt lại mật khẩu. Phản hồi generic, không lộ tài khoản.</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> yeucaudatlaimatkhau([FromBody] YeuCauDatLaiMatKhauDto? req)
        {
            var result = await _authService.yeucaudatlaimatkhau(req?.email);
            return Ok(result);
        }

        /// <summary>Hoàn tất đặt lại mật khẩu bằng token một lần.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> hoanthanhdatlaimatkhau([FromBody] HoanThanhDatLaiMatKhauDto? req)
        {
var result = await _authService.hoanthanhdatlaimatkhau(req?.token, req?.matkhau);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Gửi mã OTP qua email.</summary>
        [HttpPost("otp/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestDto? req)
        {
            var result = await _otpService.YeuCauOtpAsync(req?.email);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Xác thực mã OTP, trả proof trong ResponseModel.data.</summary>
        [HttpPost("otp/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto? req)
        {
            var result = await _otpService.XacThucOtpAsync(req?.email, req?.otp);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }

    public class LoginDto
    {
        /// <summary>Tên tài khoản (Admin / Thủ thư) hoặc email (Bạn đọc)</summary>
        public string tendangnhap { get; set; } = "";
        public string matkhau     { get; set; } = "";
    }

    public class GoogleLoginDto
    {
        public string id_token { get; set; } = "";
    }

    public class YeuCauDatLaiMatKhauDto
    {
        public string email { get; set; } = "";
    }

    public class HoanThanhDatLaiMatKhauDto
    {
        public string token { get; set; } = "";
        public string matkhau { get; set; } = "";
    }

    public class OtpRequestDto
    {
        public string email { get; set; } = "";
    }

    public class OtpVerifyDto
    {
        public string email { get; set; } = "";
        public string otp { get; set; } = "";
    }
}
