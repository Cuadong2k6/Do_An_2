using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model.Shared;

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

        /// <summary>Đăng nhập (Admin / Thủ thư / Bạn đọc)</summary>
        [HttpPost("login")]
        public async Task<IActionResult> dangnhap([FromBody] LoginDto req)
        {
            var result = await _authService.dangnhap(req.tendangnhap, req.matkhau);
            return result.success ? Ok(result) : Unauthorized(result);
        }
    }

    public class LoginDto
    {
        /// <summary>Tên tài khoản (Admin / Thủ thư) hoặc email (Bạn đọc)</summary>
        public string tendangnhap { get; set; } = "";
        public string matkhau     { get; set; } = "";
    }
}
