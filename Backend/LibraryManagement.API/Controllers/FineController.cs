using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FineController : ControllerBase
    {
        private readonly FineService _fineService;

        public FineController(FineService fineService)
        {
            _fineService = fineService;
        }

        /// <summary>Lấy thông tin phiếu phạt theo phiếu mượn</summary>
        [HttpGet("{loanId}")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> laytienphat(Guid loanId)
        {
            var result = await _fineService.laytienphat(loanId);
            return result.success ? Ok(result) : NotFound(result);
        }

        /// <summary>Thu tiền phạt</summary>
        [HttpPost("payment")]
        [Authorize(Roles = "Admin,ThuThu")]
        public async Task<IActionResult> thutienphattrehan([FromBody] PaymentModel model)
        {
            var result = await _fineService.thutienphattrehan(model);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
