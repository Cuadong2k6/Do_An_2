using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController : ControllerBase
    {
        private readonly ReservationService _resService;

        public ReservationController(ReservationService resService)
        {
            _resService = resService;
        }

        /// <summary>Đặt chỗ sách</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> taodatcho([FromBody] ReservationModel model)
        {
            var result = await _resService.taodatcho(model);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Danh sách đặt chỗ (lọc theo trạng thái + phân trang)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachdatcho(
            [FromQuery] int? trangthai,
            [FromQuery] int  page     = 1,
            [FromQuery] int  pageSize = 10)
        {
            var result = await _resService.danhsachdatcho(trangthai, page, pageSize);
            return Ok(result);
        }

        /// <summary>Lịch sử đặt chỗ của bạn đọc</summary>
        [HttpGet("by-reader/{readerId:guid}")]
        [Authorize]
        public async Task<IActionResult> danhsachdatchocuabandoc(
            Guid readerId,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _resService.danhsachdatchocuabandoc(readerId, page, pageSize);
            return Ok(result);
        }

        /// <summary>Xác nhận bạn đọc đã nhận chỗ</summary>
        [HttpPut("{id:guid}/receive")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> nhancho(Guid id)
        {
            var result = await _resService.nhancho(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Hủy đặt chỗ</summary>
        [HttpPut("{id:guid}/cancel")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> huycho(Guid id)
        {
            var result = await _resService.huycho(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật trạng thái đặt chỗ hết hạn</summary>
        [HttpPost("expire")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> capnhatchohethan()
        {
            var result = await _resService.capnhatchohethan();
            return Ok(result);
        }
    }
}
