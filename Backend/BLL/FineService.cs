using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class FineService
    {
        private readonly FineRepository _fineRepo;
        private readonly ILogger<FineService> _logger;

        public FineService(FineRepository fineRepo, ILogger<FineService> logger)
        {
            _fineRepo = fineRepo;
            _logger   = logger;
        }

        public async Task<ResponseModel> laytienphat(Guid loanId)
        {
            var fine = await _fineRepo.laytienphattheomuon(loanId);
            if (fine == null) return ResponseModel.Fail("Không có phiếu phạt cho lần mượn này.");
            return ResponseModel.Ok(fine);
        }

        public async Task<ResponseModel> thutienphattrehan(PaymentModel model)
        {
            if (model.sotien <= 0) return ResponseModel.Fail("Số tiền không hợp lệ.");
            model.pay_id       = Guid.NewGuid();
            model.payment_date = DateTime.Now;
            await _fineRepo.thutienphattrehan(model);
            _logger.LogInformation("Thu tiền phạt fine_id={FineId}, số tiền={SoTien}", model.fine_id, model.sotien);
            return ResponseModel.Ok(model.pay_id, "Thu tiền phạt thành công.");
        }
    }
}
