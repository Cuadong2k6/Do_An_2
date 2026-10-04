using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    /// <summary>Nhật ký thay đổi sách / bạn đọc — chỉ đọc, không có nút khôi phục.
    /// Chỉ Admin/ThuThu được xem vì nội dung là dữ liệu nội bộ của nhân viên.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NhatkyController : ControllerBase
    {
        private readonly NhatkyService _nhatkyService;

        public NhatkyController(NhatkyService nhatkyService)
        {
            _nhatkyService = nhatkyService;
        }

        /// <summary>Danh sách nhật ký (lọc theo bảng / hành động, có phân trang)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachnhatky(
            [FromQuery] string? keyword,
            [FromQuery] string? bang,
            [FromQuery] string? hanhdong,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20)
        {
            var result = await _nhatkyService.danhsachnhatky(keyword, bang, hanhdong, page, pageSize);
            return Ok(result);
        }
    }
}