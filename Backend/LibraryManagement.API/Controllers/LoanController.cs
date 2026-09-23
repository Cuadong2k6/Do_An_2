using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanController : ControllerBase
    {
        private readonly LoanService _loanService;

        public LoanController(LoanService loanService)
        {
            _loanService = loanService;
        }

        /// <summary>Tạo phiếu mượn sách</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> taophieumuon([FromBody] LoanRequestDto req)
        {
            var result = await _loanService.taophieumuon(req.reader_id, req.copy_ids, req.due_date);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Trả sách (tự động tính phạt nếu trễ)</summary>
        [HttpPut("{loanId:guid}/return")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> trasach(Guid loanId)
        {
            var result = await _loanService.trasach(loanId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Danh sách sách quá hạn (tìm kiếm + phân trang)</summary>
        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachsachquahan(
            [FromQuery] string? keyword,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10)
        {
            var result = await _loanService.danhsachsachquahan(keyword, page, pageSize);
            return Ok(result);
        }

        /// <summary>Lịch sử mượn của bạn đọc</summary>
        [HttpGet("history/{readerId}")]
        [Authorize]
        public async Task<IActionResult> lichsumuon(Guid readerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _loanService.lichsumuon(readerId, page, pageSize);
            return Ok(result);
        }

        /// <summary>Gia hạn phiếu mượn</summary>
        [HttpPut("{loanId:guid}/renew")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> giathanphieu(Guid loanId, [FromQuery] int themngay = 7)
        {
            var result = await _loanService.giathanphieu(loanId, themngay);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Danh sách phiếu đang mượn (tìm kiếm + phân trang)</summary>
        [HttpGet("active")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> danhsachdangmuon(
            [FromQuery] string? keyword,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10)
        {
            var result = await _loanService.danhsachdangmuon(keyword, page, pageSize);
            return Ok(result);
        }
    }

    public class LoanRequestDto
    {
        public Guid       reader_id { get; set; }
        public List<Guid> copy_ids  { get; set; } = new();
        public DateTime   due_date  { get; set; }
    }
}
