using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class BookService
    {
        private readonly BookRepository _bookRepo;
        private readonly ILogger<BookService> _logger;

        public BookService(BookRepository bookRepo, ILogger<BookService> logger)
        {
            _bookRepo = bookRepo;
            _logger   = logger;
        }

        public async Task<ResponseModel> timkiemsach(string? keyword, string? theloai, string? tacgia, int? namxuatban, int page, int pageSize)
        {
            _logger.LogInformation("Tìm kiếm sách: keyword={Keyword}", keyword);
            var (items, total) = await _bookRepo.timkiemsachnangcao(keyword, theloai, tacgia, namxuatban, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> laychitietsach(Guid bookId)
        {
            var book = await _bookRepo.laychitietsach(bookId);
            if (book == null) return ResponseModel.Fail("Không tìm thấy sách.");
            return ResponseModel.Ok(book);
        }

        public async Task<ResponseModel> themmoisach(BookModel model)
        {
            if (string.IsNullOrWhiteSpace(model.title)) return ResponseModel.Fail("Tên sách không được để trống.");
            if (string.IsNullOrWhiteSpace(model.isbn))  return ResponseModel.Fail("ISBN không được để trống.");
            if (model.tongsobancao < 0)                 return ResponseModel.Fail("Tổng số bản sao phải lớn hơn hoặc bằng 0.");

            model.book_id = Guid.NewGuid();
            try
            {
                await _bookRepo.themmoisach(model);
                _logger.LogInformation("Thêm sách mới: {BookId} - {Title} - {SoBanSao} bản sao", model.book_id, model.title, model.tongsobancao ?? 0);
                return ResponseModel.Ok(model.book_id, "Thêm sách thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm sách ISBN {Isbn}", model.isbn);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> capnhatsach(BookModel model)
        {
            if (model.book_id == Guid.Empty)            return ResponseModel.Fail("Thiếu mã sách.");
            if (string.IsNullOrWhiteSpace(model.title)) return ResponseModel.Fail("Tên sách không được để trống.");
            if (string.IsNullOrWhiteSpace(model.isbn))  return ResponseModel.Fail("ISBN không được để trống.");
            if (model.tongsobancao < 0)                 return ResponseModel.Fail("Tổng số bản sao phải lớn hơn hoặc bằng 0.");

            try
            {
                await _bookRepo.capnhatsach(model);
                _logger.LogInformation("Cập nhật sách: {BookId} - {Title} - {SoBanSao} bản sao", model.book_id, model.title, model.tongsobancao);
                return ResponseModel.Ok(null, "Cập nhật sách thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật sách {BookId}", model.book_id);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> xoasach(Guid bookId)
        {
            try
            {
                await _bookRepo.xoasach(bookId);
                _logger.LogInformation("Xoá sách: {BookId}", bookId);
                return ResponseModel.Ok(null, "Xoá sách thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xoá sách {BookId}", bookId);
                return ResponseModel.Fail(ex.Message);
            }
        }
    }
}
