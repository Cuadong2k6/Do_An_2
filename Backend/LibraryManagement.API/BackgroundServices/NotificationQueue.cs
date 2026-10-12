// Background services cho các tác vụ lâu
namespace LibraryManagement.API.BackgroundServices
{
    public interface INotificationQueue
    {
        void EnqueueNotification(NotificationMessage message);
        NotificationMessage DequeueNotification();
        int GetPendingCount();
    }

    public class NotificationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } // 'email', 'push', 'sms'
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Recipient { get; set; } // email address or device token
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
    }
}
