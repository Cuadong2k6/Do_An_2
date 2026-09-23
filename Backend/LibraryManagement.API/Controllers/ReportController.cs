using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>Báo cáo sách mượn nhiều nhất</summary>
        [HttpGet("top-borrowed-books")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> sachmuonnhieu([FromQuery] int topN = 10)
        {
            var result = await _reportService.sachmuonnhieu(topN);
            return Ok(result);
        }

        /// <summary>Báo cáo tồn kho theo thể loại</summary>
        [HttpGet("inventory-by-category")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> tonkhotheoloai()
        {
            var result = await _reportService.tonkhotheoloai();
            return Ok(result);
        }

        /// <summary>Báo cáo sách quá hạn chưa trả</summary>
        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> baocaoquahan()
        {
            var result = await _reportService.baocaoquahan();
            return Ok(result);
        }
    }
}
