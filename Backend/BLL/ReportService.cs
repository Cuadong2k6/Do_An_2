using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class ReportService
    {
        private readonly ReportRepository _reportRepo;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ReportRepository reportRepo, ILogger<ReportService> logger)
        {
            _reportRepo = reportRepo;
            _logger     = logger;
        }

        public async Task<ResponseModel> sachmuonnhieu(int topN)
        {
            if (topN <= 0) topN = 10;
            var items = await _reportRepo.laysachmuonnhieu(topN);
            return ResponseModel.Ok(items, totalItems: items.Count());
        }

        public async Task<ResponseModel> tonkhotheoloai()
        {
            var items = await _reportRepo.laytonkhotheoloai();
            return ResponseModel.Ok(items, totalItems: items.Count());
        }

        public async Task<ResponseModel> baocaoquahan()
        {
            var items = await _reportRepo.laybaocaoquahan();
            _logger.LogInformation("Báo cáo quá hạn: {Count} phiếu", items.Count());
            return ResponseModel.Ok(items, totalItems: items.Count());
        }
    }
}
