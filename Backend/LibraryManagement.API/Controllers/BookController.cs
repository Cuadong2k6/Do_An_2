using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly BookService _bookService;

        public BookController(BookService bookService)
        {
            _bookService = bookService;
        }

        /// <summary>Tìm kiếm sách nâng cao có phân trang</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> timkiemsach(
            [FromQuery] string? keyword,
            [FromQuery] string? theloai,
            [FromQuery] string? tacgia,
            [FromQuery] int?    namxuatban,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10)
        {
            var result = await _bookService.timkiemsach(keyword, theloai, tacgia, namxuatban, page, pageSize);
            return Ok(result);
        }

        /// <summary>Lấy chi tiết sách theo ID</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> chitietsach(Guid id)
        {
            var result = await _bookService.laychitietsach(id);
            return result.success ? Ok(result) : NotFound(result);
        }

        /// <summary>Thêm sách mới (Admin/Thủ thư)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> themmoisach([FromBody] BookModel model)
        {
            var result = await _bookService.themmoisach(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật sách</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> capnhatsach(Guid id, [FromBody] BookModel model)
        {
            model.book_id = id;
            var result = await _bookService.capnhatsach(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Xoá sách (chỉ khi chưa có bản sao)</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> xoasach(Guid id)
        {
            var result = await _bookService.xoasach(id);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
