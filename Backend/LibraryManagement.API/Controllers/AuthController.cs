using BLL;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Đăng nhập Admin / Thủ thư</summary>
        [HttpPost("login-admin")]
        public async Task<IActionResult> dangnhapquantri([FromBody] LoginDto req)
        {
            var result = await _authService.dangnhapquantri(req.taikhoan, req.matkhau);
            return result.success ? Ok(result) : Unauthorized(result);
        }

        /// <summary>Đăng nhập Bạn đọc</summary>
        [HttpPost("login-reader")]
        public async Task<IActionResult> dangnhapbandoc([FromBody] ReaderLoginDto req)
        {
            var result = await _authService.dangnhapbandoc(req.email, req.matkhau);
            return result.success ? Ok(result) : Unauthorized(result);
        }
    }

    public class LoginDto
    {
        public string taikhoan { get; set; } = "";
        public string matkhau  { get; set; } = "";
    }

    public class ReaderLoginDto
    {
        public string email   { get; set; } = "";
        public string matkhau { get; set; } = "";
    }
}
