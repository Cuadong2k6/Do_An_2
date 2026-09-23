using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReaderController : ControllerBase
    {
        private readonly ReaderService _readerService;

        public ReaderController(ReaderService readerService)
        {
            _readerService = readerService;
        }

        /// <summary>Danh sách bạn đọc (có tìm kiếm + phân trang)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachbandoc(
            [FromQuery] string? keyword,
            [FromQuery] int?    trangthai,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10)
        {
            var result = await _readerService.danhsachbandoc(keyword, trangthai, page, pageSize);
            return Ok(result);
        }

        /// <summary>Chi tiết bạn đọc</summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> chitietchibandoc(Guid id)
        {
            var result = await _readerService.chitietchibandoc(id);
            return result.success ? Ok(result) : NotFound(result);
        }

        /// <summary>Đăng ký thẻ bạn đọc mới</summary>
        [HttpPost("register")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> dangkymoi([FromBody] ReaderModel model)
        {
            var result = await _readerService.dangkymoi(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật thông tin bạn đọc</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> capnhatthongtin(Guid id, [FromBody] ReaderModel model)
        {
            model.reader_id = id;
            var result = await _readerService.capnhatthongtin(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Gia hạn thẻ bạn đọc</summary>
        [HttpPut("{id:guid}/renew")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> giathanthu(Guid id, [FromBody] RenewCardDto req)
        {
            var result = await _readerService.giathanthu(id, req.ngayhethan);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Xuất thẻ bạn đọc file PDF</summary>
        [HttpGet("{id:guid}/card")]
        [Authorize]
        public async Task<IActionResult> xuatthebandoc(Guid id)
        {
            var pdf = await _readerService.xuatthebandoc(id);
            if (pdf == null) return NotFound(ResponseModel.Fail("Không tìm thấy bạn đọc."));
            return File(pdf, "application/pdf", $"the-thu-vien-{id}.pdf");
        }

        /// <summary>Xoá độc giả (chỉ khi chưa có lịch sử mượn / đặt chỗ)</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> xoadocgia(Guid id)
        {
            var result = await _readerService.xoadocgia(id);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }

    public class RenewCardDto
    {
        public DateTime ngayhethan { get; set; }
    }
}
