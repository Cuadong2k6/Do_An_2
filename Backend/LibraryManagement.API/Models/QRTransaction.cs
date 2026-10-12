// QR Transaction model
namespace LibraryManagement.API.Models
{
    public enum QRTransactionType
    {
        Payment,
        LoanPayment,
        FinePayment,
        Reservation
    }

    public enum QRTransactionStatus
    {
        Generated,
        Active,
        Used,
        Expired,
        Failed,
        Cancelled
    }

    public class QRTransaction
    {
        public Guid TransactionId { get; set; }
        public string QRContent { get; set; }
        public QRTransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string Recipient { get; set; }
        public QRTransactionStatus Status { get; set; } = QRTransactionStatus.Generated;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid ScannedBy { get; set; }
        public Guid RelatedEntityId { get; set; } // loanId, paymentId, fineId, etc.
        public string Metadata { get; set; } // JSON for additional data
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class QRTransactionResult
    {
        public bool Success { get; set; }
        public Guid? TransactionId { get; set; }
        public string Message { get; set; }
        public string QRCodeBase64 { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
