// Dịch vụ xử lý giao dịch QR
namespace LibraryManagement.API.Services
{
    public interface IQRTransactionService
    {
        Task<QRTransactionResult> ProcessPaymentQRAsync(string qrContent, Guid? scannedByUserId = null);
        Task<QRTransactionResult> ProcessLoanQRAsync(string qrContent, Guid? scannedByUserId = null);
        Task<QRTransactionResult> ProcessFineQRAsync(string qrContent, Guid? scannedByUserId = null);
        Task<QRTransactionResult> ProcessReservationQRAsync(string qrContent, Guid? scannedByUserId = null);
        Task<QRTransactionResult> CreatePaymentQRAsync(Guid loanId, decimal amount, Guid userId);
        Task<QRTransactionResult> CreateFineQRAsync(Guid fineId, decimal amount, Guid userId);
    }

    public class QRTransactionService : IQRTransactionService
    {
        private readonly ILogger<QRTransactionService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPaymentQRScanner _qrScanner;
        private readonly IQRCodeGenerator _qrGenerator;
        private readonly IDalUnitOfWork _dalUnitOfWork;

        public QRTransactionService(
            ILogger<QRTransactionService> logger,
            IHttpContextAccessor httpContextAccessor,
            IPaymentQRScanner qrScanner,
            IQRCodeGenerator qrGenerator,
            IDalUnitOfWork dalUnitOfWork)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _qrScanner = qrScanner;
            _qrGenerator = qrGenerator;
            _dalUnitOfWork = dalUnitOfWork;
        }

        public async Task<QRTransactionResult> ProcessPaymentQRAsync(string qrContent, Guid? scannedByUserId = null)
        {
            try
            {
                var scanResult = _qrScanner.ScanQRCode(qrContent);
                if (!scanResult.Success)
                    return new QRTransactionResult
                    {
                        Success = false,
                        Message = scanResult.ErrorMessage
                    };

                // Xử lý giao dịch QR
                var transaction = new QRTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    QRContent = qrContent,
                    Amount = scanResult.Amount ?? 0,
                    Recipient = scanResult.Recipient,
                    TransactionType = "payment",
                    Status = "completed",
                    ProcessedAt = DateTime.UtcNow,
                    ScannedBy = scannedByUserId ?? GetCurrentUserId(),
                    RelatedEntityId = scanResult.TransactionId ?? Guid.NewGuid()
                };

                _dalUnitOfWork.QRTransactions.Add(transaction);
                await _dalUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Đã xử lý giao dịch QR: {TransactionId}", transaction.TransactionId);

                return new QRTransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    Message = "Xử lý QR code thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý QR code thanh toán");
                return new QRTransactionResult
                {
                    Success = false,
                    Message = $"Lỗi xử lý QR code thanh toán: {ex.Message}"
                };
            }
        }

        public async Task<QRTransactionResult> ProcessLoanQRAsync(string qrContent, Guid? scannedByUserId = null)
        {
            try
            {
                var scanResult = _qrScanner.ScanQRCode(qrContent);
                if (!scanResult.Success)
                    return new QRTransactionResult
                    {
                        Success = false,
                        Message = scanResult.ErrorMessage
                    };

                // Xử lý loan QR - cập nhật trạng thái loan
                var transaction = new QRTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    QRContent = qrContent,
                    Amount = scanResult.Amount ?? 0,
                    Recipient = scanResult.Recipient,
                    TransactionType = "loan_payment",
                    Status = "completed",
                    ProcessedAt = DateTime.UtcNow,
                    ScannedBy = scannedByUserId ?? GetCurrentUserId(),
                    RelatedEntityId = scanResult.TransactionId ?? Guid.NewGuid()
                };

                _dalUnitOfWork.QRTransactions.Add(transaction);
                await _dalUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Đã xử lý QR code trả sách: {TransactionId}", transaction.TransactionId);

                return new QRTransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    Message = "Xử lý QR code trả sách thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý QR code trả sách");
                return new QRTransactionResult
                {
                    Success = false,
                    Message = $"Lỗi xử lý QR code trả sách: {ex.Message}"
                };
            }
        }

        public async Task<QRTransactionResult> ProcessFineQRAsync(string qrContent, Guid? scannedByUserId = null)
        {
            // Tương tự implementation cho fine QR
            return await ProcessPaymentQRAsync(qrContent, scannedByUserId);
        }

        public async Task<QRTransactionResult> ProcessReservationQRAsync(string qrContent, Guid? scannedByUserId = null)
        {
            // Tương tự implementation cho reservation QR
            return await ProcessPaymentQRAsync(qrContent, scannedByUserId);
        }

        public async Task<QRTransactionResult> CreatePaymentQRAsync(Guid loanId, decimal amount, Guid userId)
        {
            try
            {
                var loan = await _dalUnitOfWork.Loans.GetByIdAsync(loanId);
                if (loan == null)
                    return new QRTransactionResult
                    {
                        Success = false,
                        Message = "Phiếu mượn không tồn tại"
                    };

                var qrContent = $"LOAN-{loanId:D}|AMOUNT:{amount:N0}|DATE:{DateTime.UtcNow:yyyy-MM-dd}|RECIPIENT:{loan.UserName}";
                var qrCode = _qrGenerator.GenerateQRCodeAsBytes(qrContent);

                var transaction = new QRTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    QRContent = qrContent,
                    Amount = amount,
                    Recipient = loan.UserName,
                    TransactionType = "payment",
                    Status = "generated",
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    ScannedBy = userId,
                    RelatedEntityId = loanId
                };

                _dalUnitOfWork.QRTransactions.Add(transaction);
                await _dalUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Đã tạo QR code thanh toán: {TransactionId}", transaction.TransactionId);

                return new QRTransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    QRCodeBase64 = Convert.ToBase64String(qrCode),
                    Message = "Tạo QR code thanh toán thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo QR code thanh toán");
                return new QRTransactionResult
                {
                    Success = false,
                    Message = $"Lỗi tạo QR code thanh toán: {ex.Message}"
                };
            }
        }

        public async Task<QRTransactionResult> CreateFineQRAsync(Guid fineId, decimal amount, Guid userId)
        {
            // Tương tự implementation cho fine QR
            return await CreatePaymentQRAsync(Guid.NewGuid(), amount, userId);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? new Guid(userIdClaim.Value) : Guid.Empty;
        }
    }
}