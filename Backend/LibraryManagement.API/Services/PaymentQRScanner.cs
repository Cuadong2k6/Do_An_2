// Dịch vụ quét và xử lý mã QR thanh toán
namespace LibraryManagement.API.Services
{
    public interface IPaymentQRScanner
    {
        QRScanResult ScanQRCode(byte[] qrImage);
        QRScanResult ScanQRCodeFromFile(string filePath);
        QRScanResult ScanQRCodeFromBase64(string base64String);
        bool ValidateQRFormat(string content);
    }

    public class PaymentQRScanner : IPaymentQRScanner
    {
        private readonly ILogger<PaymentQRScanner> _logger;
        private readonly IQRCodeGenerator _qrCodeGenerator;

        public PaymentQRScanner(
            ILogger<PaymentQRScanner> logger,
            IQRCodeGenerator qrCodeGenerator)
        {
            _logger = logger;
            _qrCodeGenerator = qrCodeGenerator;
        }

        public QRScanResult ScanQRCode(byte[] qrImage)
        {
            try
            {
                if (qrImage == null || qrImage.Length == 0)
                    return QRScanResult.Error("Hình ảnh QR code trống");

                // Mô phỏng xử lý QR (thực tế sẽ dùng thư viện ZXing hoặc similar)
                var result = new QRScanResult
                {
                    Success = true,
                    ScannedContent = "LOAN-2024-001|AMOUNT:50000|DATE:2024-01-15|RECIPIENT:THỦ THƯ",
                    ScanType = "loan_payment",
                    Amount = 50000,
                    Recipient = "THỦ THƯ",
                    TransactionId = "LOAN-2024-001",
                    ScanTime = DateTime.UtcNow
                };

                _logger.LogInformation("Đã quét QR code thành công: {TransactionId}", result.TransactionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quét QR code");
                return QRScanResult.Error($"Quét QR code thất bại: {ex.Message}");
            }
        }

        public QRScanResult ScanQRCodeFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return QRScanResult.Error("File không tồn tại");

                var bytes = File.ReadAllBytes(filePath);
                return ScanQRCode(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc file QR code: {FilePath}", filePath);
                return QRScanResult.Error($"Đọc file QR code thất bại: {ex.Message}");
            }
        }

        public QRScanResult ScanQRCodeFromBase64(string base64String)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64String);
                return ScanQRCode(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi giải mã base64 QR code");
                return QRScanResult.Error($"Giải mã base64 QR code thất bại: {ex.Message}");
            }
        }

        public bool ValidateQRFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Validate QR code format (ví dụ: LOAN-2024-001|AMOUNT:50000)
            var parts = content.Split('|');
            if (parts.Length < 2)
                return false;

            var prefix = parts[0].ToUpper();
            var validPrefixes = new[] { "LOAN", "FINE", "PAYMENT", "RESERVATION" };
            return validPrefixes.Contains(prefix);
        }
    }
}