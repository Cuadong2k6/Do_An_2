using DAL;
using Microsoft.Extensions.Logging;
using Model;
using System.Text.Json;

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
        /// Tạo phiếu mượn sách. copyIds là danh sách bản sao cần mượn.
        /// </summary>
        public async Task<ResponseModel> taophieumuon(Guid readerId, List<Guid> copyIds, DateTime dueDate)
        {
            if (copyIds == null || copyIds.Count == 0)
                return ResponseModel.Fail("Danh sách sách mượn không được rỗng.");

            var reader = await _readerRepo.laychitietchibandoc(readerId);
            if (reader == null)          return ResponseModel.Fail("Không tìm thấy thẻ bạn đọc.");
            if (reader.trangthai != 0)   return ResponseModel.Fail("Thẻ bạn đọc không còn hiệu lực.");
            if (reader.ngayhethan < DateTime.Now) return ResponseModel.Fail("Thẻ bạn đọc đã hết hạn.");
            if (copyIds.Count > reader.somughin)  return ResponseModel.Fail($"Vượt giới hạn số sách mượn. Tối đa: {reader.somughin} cuốn.");

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
