using BLL;
using LibraryManagement.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model.Reader;
using Model.Shared;
using System.Security.Claims;

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
        [ChinhMinhHoacNhanVien("id")]
        public async Task<IActionResult> chitietchibandoc(Guid id)
        {
            var result = await _readerService.chitietchibandoc(id);
            return result.success ? Ok(result) : NotFound(result);
        }

        [HttpPut("me/profile")]
        [Authorize(Roles = "BanDoc")]
        public async Task<IActionResult> CapNhatHoSo([FromBody] CapNhatHoSoReaderDto req)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var readerId))
                return Unauthorized(ResponseModel.Fail("Token không hợp lệ."));

            var result = await _readerService.CapNhatHoSoAsync(readerId, req.hoten, req.sodienthoai, req.diachi);
            return result.success ? Ok(result) : BadRequest(result);
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
        [ChinhMinhHoacNhanVien("id")]
        [ChanPendingPayment]
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

        /// <summary>Khoá bạn đọc (trangthai = 2). Chỉ Admin/ThuThu.</summary>
        [HttpPut("{id:guid}/lock")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> KhoaBandoc(Guid id)
        {
            var result = await _readerService.KhoaBandocAsync(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Mở khoá bạn đọc (trangthai = 0). Chỉ Admin/ThuThu.</summary>
        [HttpPut("{id:guid}/unlock")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> MoKhoaBandoc(Guid id)
        {
            var result = await _readerService.MoKhoaBandocAsync(id);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("me/activation/request")]
        [Authorize(Roles = "BanDoc")]
        public async Task<IActionResult> YeuCauKichHoat()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var readerId))
                return Unauthorized(ResponseModel.Fail("Token không hợp lệ."));

            var result = await _readerService.yeucaukichhoat(readerId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("activations/pending")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> DanhSachKichHoatChoDuyet()
        {
            var result = await _readerService.danhsachkichhoatchoduyet();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("activations/{id:guid}/approve")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> DuyetKichHoat(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var staffId))
                return Unauthorized(ResponseModel.Fail("Token không hợp lệ."));

            var result = await _readerService.duyetkichhoat(id, staffId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("activations/{id:guid}/reject")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> TuChoiKichHoat(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var staffId))
                return Unauthorized(ResponseModel.Fail("Token không hợp lệ."));

            var result = await _readerService.tuchoikichhoat(id, staffId);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }

    public class RenewCardDto
    {
        public DateTime ngayhethan { get; set; }
    }

    public sealed class CapNhatHoSoReaderDto
    {
        public string hoten { get; set; } = "";
        public string? sodienthoai { get; set; }
        public string? diachi { get; set; }
    }
}
