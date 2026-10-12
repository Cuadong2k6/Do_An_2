// QR Scan result model
namespace LibraryManagement.API.Models
{
    public class QRScanResult
    {
        public bool Success { get; set; }
        public string ScannedContent { get; set; }
        public string ErrorMessage { get; set; }
        public string ScanType { get; set; }
        public decimal? Amount { get; set; }
        public string Recipient { get; set; }
        public string TransactionId { get; set; }
        public DateTime ScanTime { get; set; } = DateTime.UtcNow;

        public static QRScanResult Success(string scannedContent, string scanType, decimal? amount = null, string recipient = null, string transactionId = null)
        {
            return new QRScanResult
            {
                Success = true,
                ScannedContent = scannedContent,
                ScanType = scanType,
                Amount = amount,
                Recipient = recipient,
                TransactionId = transactionId
            };
        }

        public static QRScanResult Error(string errorMessage)
        {
            return new QRScanResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
