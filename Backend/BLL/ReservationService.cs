using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class ReservationService
    {
        private readonly ReservationRepository _resRepo;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(ReservationRepository resRepo, ILogger<ReservationService> logger)
        {
            _resRepo = resRepo;
            _logger  = logger;
        }

        public async Task<ResponseModel> taodatcho(ReservationModel model)
        {
            if (model.book_id == Guid.Empty)   return ResponseModel.Fail("Thiếu mã sách.");
            if (model.reader_id == Guid.Empty) return ResponseModel.Fail("Thiếu mã bạn đọc.");

            // Mặc định giữ chỗ 3 ngày nếu không truyền ngày hết hạn
            if (model.expiry_date <= DateTime.Now)
                model.expiry_date = DateTime.Now.AddDays(3);

            model.res_id    = Guid.NewGuid();
            model.res_date  = DateTime.Now;
            model.trangthai = 0;

            try
            {
                await _resRepo.taodatcho(model);
                _logger.LogInformation("Đặt chỗ {ResId}: sách {BookId} - bạn đọc {ReaderId}",
                    model.res_id, model.book_id, model.reader_id);
                return ResponseModel.Ok(model.res_id, "Đặt chỗ thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đặt chỗ sách {BookId}", model.book_id);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> danhsachdatcho(int? trangthai, int page, int pageSize)
        {
            var (items, total) = await _resRepo.danhsachdatcho(trangthai, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> danhsachdatchocuabandoc(Guid readerId, int page, int pageSize)
        {
            var (items, total) = await _resRepo.danhsachdatchocuabandoc(readerId, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> nhancho(Guid resId)
        {
            try
            {
                await _resRepo.nhancho(resId);
                _logger.LogInformation("Nhận chỗ {ResId}", resId);
                return ResponseModel.Ok(null, "Nhận chỗ thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nhận chỗ {ResId}", resId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> huycho(Guid resId)
        {
            try
            {
                await _resRepo.huycho(resId);
                _logger.LogInformation("Hủy chỗ {ResId}", resId);
                return ResponseModel.Ok(null, "Hủy đặt chỗ thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy chỗ {ResId}", resId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> capnhatchohethan()
        {
            await _resRepo.capnhatchohethan();
            return ResponseModel.Ok(null, "Cập nhật đặt chỗ hết hạn thành công.");
        }
    }
}
