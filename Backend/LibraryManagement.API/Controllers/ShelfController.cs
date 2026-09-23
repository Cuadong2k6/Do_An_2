using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelfController : ControllerBase
    {
        private readonly ShelfService _shelfService;

        public ShelfController(ShelfService shelfService)
        {
            _shelfService = shelfService;
        }

        /// <summary>Danh sách kệ sách (tìm kiếm + phân trang)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachke(
            [FromQuery] string? keyword,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10)
        {
            var result = await _shelfService.danhsachke(keyword, page, pageSize);
            return Ok(result);
        }

        /// <summary>Thêm kệ sách mới</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> themke([FromBody] ShelfModel model)
        {
            var result = await _shelfService.themke(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật kệ sách</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> capnhatke(int id, [FromBody] ShelfModel model)
        {
            var result = await _shelfService.capnhatke(id, model);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
