using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class ReaderService
    {
        private readonly ReaderRepository _readerRepo;
        private readonly ILogger<ReaderService> _logger;

        public ReaderService(ReaderRepository readerRepo, ILogger<ReaderService> logger)
        {
            _readerRepo = readerRepo;
            _logger     = logger;
        }

        public async Task<ResponseModel> danhsachbandoc(string? keyword, int? trangthai, int page, int pageSize)
        {
            var (items, total) = await _readerRepo.danhsachbandoc(keyword, trangthai, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> chitietchibandoc(Guid readerId)
        {
            var reader = await _readerRepo.laychitietchibandoc(readerId);
            if (reader == null) return ResponseModel.Fail("Không tìm thấy bạn đọc.");
            return ResponseModel.Ok(reader);
        }

        public async Task<ResponseModel> dangkymoi(ReaderModel model)
        {
            if (string.IsNullOrWhiteSpace(model.hoten)) return ResponseModel.Fail("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(model.email)) return ResponseModel.Fail("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(model.so_the)) return ResponseModel.Fail("Số thẻ không được để trống.");

            model.reader_id  = Guid.NewGuid();
            model.ngaycap    = DateTime.Now;
            model.ngayhethan = DateTime.Now.AddYears(1);
            model.trangthai  = 0;
            model.somughin   = 3;

            await _readerRepo.dangkythebandoc(model);
            _logger.LogInformation("Đăng ký bạn đọc mới: {ReaderId} - {HoTen}", model.reader_id, model.hoten);
            return ResponseModel.Ok(model.reader_id, "Đăng ký thẻ bạn đọc thành công.");
        }

        public async Task<ResponseModel> capnhatthongtin(ReaderModel model)
        {
            await _readerRepo.capnhatthongtinbandoc(model);
            return ResponseModel.Ok((object?)null, "Cập nhật thành công.");
        }
    }
}
