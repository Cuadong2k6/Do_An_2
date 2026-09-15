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
            model.book_id = Guid.NewGuid();
            await _bookRepo.themmoisach(model);
            _logger.LogInformation("Thêm sách mới: {BookId} - {Title}", model.book_id, model.title);
            return ResponseModel.Ok(model.book_id, "Thêm sách thành công.");
        }
    }
}
