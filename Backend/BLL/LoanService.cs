using DAL.Helper;
using DAL;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Model.Loan;
using Model.Reader;
using Model.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace BLL
{
    public class LoanService
    {
        private readonly LoanRepository  _loanRepo;
        private readonly FineRepository  _fineRepo;
        private readonly ReaderRepository _readerRepo;
        private readonly ILogger<LoanService> _logger;

        public LoanService(LoanRepository loanRepo, FineRepository fineRepo, ReaderRepository readerRepo, ILogger<LoanService> logger)
        {
            _loanRepo   = loanRepo;
            _fineRepo   = fineRepo;
            _readerRepo = readerRepo;
            _logger     = logger;
        }

        /// <summary>
        /// Kiểm tra thẻ bạn đọc còn hiệu lực.
        /// Trả về (thông báo lỗi, thẻ bạn đọc) — loi null nghĩa là hợp lệ.
        /// </summary>
        private async Task<(string? loi, ReaderModel? the)> khthebanDoc(Guid readerId)
        {
            var the = await _readerRepo.laychitietchibandoc(readerId);
            if (the == null)                    return ("Không tìm thấy thẻ bạn đọc.", null);
            if (the.trangthai != 0)             return ("Thẻ bạn đọc không còn hiệu lực.", null);
            if (the.ngayhethan < DateTime.Now)  return ("Thẻ bạn đọc đã hết hạn.", null);
            return (null, the);
        }

        /// <summary>
        /// Tạo phiếu mượn sách. copyIds là danh sách bản sao cần mượn.
        /// </summary>
        public async Task<ResponseModel> taophieumuon(Guid readerId, List<Guid> copyIds, DateTime dueDate)
        {
            if (copyIds == null || copyIds.Count == 0)
                return ResponseModel.Fail("Danh sách sách mượn không được rỗng.");

            var (loiThe, reader) = await khthebanDoc(readerId);
            if (loiThe != null) return ResponseModel.Fail(loiThe);
            if (copyIds.Count > reader!.somughin)  return ResponseModel.Fail($"Vượt giới hạn số sách mượn. Tối đa: {reader.somughin} cuốn.");

            // Đóng gói danh sách copy_id thành JSON trước khi gửi xuống SP
            var listjsonChitiet = JsonSerializer.Serialize(copyIds.Select(id => new { copy_id = id }));

            var loan = new LoanModel
            {
                loan_id          = Guid.NewGuid(),
                reader_id        = readerId,
                loan_date        = DateTime.Now,
                due_date         = dueDate,
                trangthai        = 0,
                listjson_chitiet = listjsonChitiet
            };

            try
            {
                await _loanRepo.taomoimuontra(loan);
                _logger.LogInformation("Tạo phiếu mượn {LoanId} cho bạn đọc {ReaderID}", loan.loan_id, readerId);
                return ResponseModel.Ok(loan.loan_id, "Tạo phiếu mượn thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo phiếu mượn cho bạn đọc {ReaderID}", readerId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Bạn đọc tự mượn từ giỏ hàng. bookIds là danh sách SÁCH (book_id) cần mượn —
        /// hệ thống tự gán bản sao đang rảnh, bạn đọc không cần biết mã bản sao.
        /// Toàn bộ giỏ gộp thành 1 phiếu; nếu 1 cuốn không còn bản sao rảnh thì hủy cả phiếu.
        /// Giới hạn thẻ tính CẢ sách đang mượn: đang mượn + số cuốn trong giỏ phải <= somughin.
        /// </summary>
        public async Task<ResponseModel> taophieumuongio(Guid readerId, List<Guid> bookIds, DateTime dueDate)
        {
            if (bookIds == null || bookIds.Count == 0)
                return ResponseModel.Fail("Giỏ hàng không có sách nào để mượn.");

            var (loiThe, reader) = await khthebanDoc(readerId);
            if (loiThe != null) return ResponseModel.Fail(loiThe);

            // Giới hạn mượn của thẻ là tổng số cuốn đang giữ, không phải số cuốn trong 1 phiếu
            var soDangMuon  = await _readerRepo.sodangmuon(readerId);
            var conDuocMuan = reader!.somughin - soDangMuon;
            if (bookIds.Count > conDuocMuan)
                return ResponseModel.Fail(conDuocMuan <= 0
                    ? $"Bạn đang mượn {soDangMuon} cuốn, đã đạt giới hạn {reader.somughin} cuốn của thẻ. Hãy trả sách trước khi mượn tiếp."
                    : $"Vượt giới hạn mượn của thẻ. Bạn đang mượn {soDangMuon}/{reader.somughin} cuốn, chỉ mượn thêm tối đa {conDuocMuan} cuốn.");

            // Đóng gói danh sách book_id thành JSON trước khi gửi xuống SP
            var listjsonChitiet = JsonSerializer.Serialize(bookIds.Select(id => new { book_id = id }));

            var loan = new LoanModel
            {
                loan_id          = Guid.NewGuid(),
                reader_id        = readerId,
                loan_date        = DateTime.Now,
                due_date         = dueDate,
                trangthai        = 0,
                listjson_chitiet = listjsonChitiet
            };

            try
            {
                await _loanRepo.taophieumuongio(loan);
                _logger.LogInformation("Bạn đọc {ReaderID} mượn {SoSach} cuốn qua giỏ hàng, phiếu {LoanId}",
                    readerId, bookIds.Count, loan.loan_id);
                return ResponseModel.Ok(new { loan_id = loan.loan_id, so_sach = bookIds.Count },
                    $"Mượn thành công {bookIds.Count} cuốn.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo phiếu mượn từ giỏ hàng của bạn đọc {ReaderID}", readerId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Trả sách và tự động tính phạt nếu quá hạn.
        /// </summary>
        public async Task<ResponseModel> trasach(Guid loanId)
        {
            try
            {
                await _loanRepo.trasach(loanId);

                // Tự động tính phạt nếu quá hạn
                await _fineRepo.tinhtoantienphat(loanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trả sách phiếu mượn {LoanId}", loanId);
                return ResponseModel.Fail(ex.Message);
            }

            var fine = await _fineRepo.laytienphattheomuon(loanId);
            if (fine != null && !fine.is_paid)
            {
                _logger.LogInformation("Phiếu mượn {LoanId} trễ {SoNgay} ngày, phạt {Amount}đ", loanId, fine.songaytre, fine.amount);
                return ResponseModel.Ok(fine, $"Trả sách thành công. Tiền phạt: {fine.amount:N0} đ");
            }

            return ResponseModel.Ok(null, "Trả sách thành công, không có phạt.");
        }

        /// <summary>Danh sách sách quá hạn (tìm kiếm + phân trang)</summary>
        public async Task<ResponseModel> danhsachsachquahan(string? keyword, int page, int pageSize)
        {
            var (items, total) = await _loanRepo.laydanhsachsachquahan(keyword, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> lichsumuon(Guid readerId, int page, int pageSize)
        {
            var (items, total) = await _loanRepo.laydanhsachmuontratheobandoc(readerId, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        /// <summary>
        /// Gia hạn phiếu mượn thêm số ngày.
        /// </summary>
        public async Task<ResponseModel> giathanphieu(Guid loanId, int themngay)
        {
            if (themngay <= 0) return ResponseModel.Fail("Số ngày gia hạn phải lớn hơn 0.");
            try
            {
                await _loanRepo.giathanphieu(loanId, themngay);
                _logger.LogInformation("Gia hạn phiếu mượn {LoanId} thêm {ThemNgay} ngày", loanId, themngay);
                return ResponseModel.Ok(null, $"Gia hạn thêm {themngay} ngày thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gia hạn phiếu mượn {LoanId}", loanId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Danh sách phiếu đang mượn (tìm kiếm + phân trang).
        /// </summary>
        public async Task<ResponseModel> danhsachdangmuon(string? keyword, int page, int pageSize)
        {
            var (items, total) = await _loanRepo.laydanhsachdangmuon(keyword, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }
    }
}
