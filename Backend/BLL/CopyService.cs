using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class CopyService
    {
        private readonly CopyRepository _copyRepo;
        private readonly ILogger<CopyService> _logger;

        public CopyService(CopyRepository copyRepo, ILogger<CopyService> logger)
        {
            _copyRepo = copyRepo;
            _logger   = logger;
        }

        public async Task<ResponseModel> danhsachbansao(Guid? bookId, int? status, int page, int pageSize)
        {
            var (items, total) = await _copyRepo.danhsachbansao(bookId, status, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> laychitietbansao(Guid copyId)
        {
            var copy = await _copyRepo.laychitietbansao(copyId);
            if (copy == null) return ResponseModel.Fail("Không tìm thấy bản sao.");
            return ResponseModel.Ok(copy);
        }

        public async Task<ResponseModel> thembansao(CopyModel model)
        {
            if (model.book_id == Guid.Empty)        return ResponseModel.Fail("Thiếu mã sách.");
            if (string.IsNullOrWhiteSpace(model.mabancao)) return ResponseModel.Fail("Mã bản sao không được để trống.");
            if (model.status < 0 || model.status > 2) return ResponseModel.Fail("Trạng thái bản sao không hợp lệ.");

            model.copy_id = Guid.NewGuid();
            try
            {
                await _copyRepo.thembansao(model);
                _logger.LogInformation("Thêm bản sao {CopyId} - {MaBanSao}", model.copy_id, model.mabancao);
                return ResponseModel.Ok(model.copy_id, "Thêm bản sao thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm bản sao {MaBanSao}", model.mabancao);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> capnhatbansao(Guid copyId, CopyModel model)
        {
            if (model.status < 0 || model.status > 2)
                return ResponseModel.Fail("Trạng thái bản sao không hợp lệ.");

            model.copy_id = copyId;
            try
            {
                await _copyRepo.capnhatbansao(model);
                _logger.LogInformation("Cập nhật bản sao {CopyId}", copyId);
                return ResponseModel.Ok(null, "Cập nhật bản sao thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật bản sao {CopyId}", copyId);
                return ResponseModel.Fail(ex.Message);
            }
        }
    }
}
