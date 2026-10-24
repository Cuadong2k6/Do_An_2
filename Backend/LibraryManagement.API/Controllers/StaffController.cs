using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model.Staff;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class StaffController : ControllerBase
    {
        private readonly StaffService _staffService;

        public StaffController(StaffService staffService)
        {
            _staffService = staffService;
        }

        /// <summary>Danh sách thủ thư có phân trang.</summary>
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _staffService.DanhSachThuThuAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>Chi tiết thủ thư theo user_id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(Guid id)
        {
            var result = await _staffService.ChiTietThuThuAsync(id);
            return result.success ? Ok(result) : NotFound(result);
        }

        /// <summary>Tạo tài khoản thủ thư mới.</summary>
        [HttpPost]
        public async Task<IActionResult> Tao([FromBody] TaoThuThuRequest req)
        {
            var result = await _staffService.TaoThuThuAsync(req);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cập nhật thông tin thủ thư.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> CapNhat(Guid id, [FromBody] CapNhatThuThuRequest req)
        {
            var result = await _staffService.CapNhatThuThuAsync(id, req);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Xóa thủ thư.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(Guid id)
        {
            var result = await _staffService.XoaThuThuAsync(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Khóa tài khoản thủ thư.</summary>
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> Khoa(Guid id)
        {
            var result = await _staffService.KhoaThuThuAsync(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Mở khóa tài khoản thủ thư.</summary>
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> MoKhoa(Guid id)
        {
            var result = await _staffService.MoKhoaThuThuAsync(id);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
