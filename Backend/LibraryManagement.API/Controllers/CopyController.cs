using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CopyController : ControllerBase
    {
        private readonly CopyService _copyService;

        public CopyController(CopyService copyService)
        {
            _copyService = copyService;
        }

        /// <summary>Danh sách bản sao (lọc theo sách/trạng thái + phân trang)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachbansao(
            [FromQuery] Guid?  bookId,
            [FromQuery] int?   status,
            [FromQuery] int    page     = 1,
            [FromQuery] int    pageSize = 10)
        {
            var result = await _copyService.danhsachbansao(bookId, status, page, pageSize);
            return Ok(result);
        }

        /// <summary>Chi tiết bản sao</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> chitietchibansao(Guid id)
        {
            var result = await _copyService.laychitietbansao(id);
            return result.success ? Ok(result) : NotFound(result);
        }

        /// <summary>Thêm bản sao mới</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> themmoibansao([FromBody] CopyModel model)
        {
            var result = await _copyService.thembansao(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật bản sao (kệ, trạng thái)</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> capnhatbansao(Guid id, [FromBody] CopyModel model)
        {
            var result = await _copyService.capnhatbansao(id, model);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
