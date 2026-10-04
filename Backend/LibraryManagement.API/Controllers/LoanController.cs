using BLL;
using LibraryManagement.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model.Shared;
using System.Security.Claims;

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

        /// <summary>Bạn đọc tự mượn sách từ giỏ hàng</summary>
        [HttpPost("mine")]
        [Authorize(Roles = "BanDoc")]
        public async Task<IActionResult> taophieumuongio([FromBody] GioHangMuonDto req)
        {
            // reader_id lấy từ token, KHÔNG nhận từ body — tránh bạn đọc mượn hộ người khác
            var readerIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(readerIdRaw, out var readerId))
                return Unauthorized(ResponseModel.Fail("Token không hợp lệ."));

            var result = await _loanService.taophieumuongio(readerId, req.book_ids, req.due_date);
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
        [ChinhMinhHoacNhanVien("readerId")]
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

    /// <summary>Giỏ hàng của bạn đọc: danh sách SÁCH (book_id), không phải mã bản sao.</summary>
    public class GioHangMuonDto
    {
        public List<Guid> book_ids { get; set; } = new();
        public DateTime   due_date { get; set; }
    }
}
