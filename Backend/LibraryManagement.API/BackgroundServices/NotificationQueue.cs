using System.Collections.Concurrent;

namespace LibraryManagement.API.BackgroundServices
{
    public interface INotificationQueue
    {
        void EnqueueNotification(NotificationMessage message);
        NotificationMessage? DequeueNotification();
        int GetPendingCount();
    }

    public class NotificationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
    }

    public class NotificationQueue : INotificationQueue
    {
        private readonly ConcurrentQueue<NotificationMessage> _queue = new();

        public void EnqueueNotification(NotificationMessage message)
        {
            _queue.Enqueue(message);
        }

        public NotificationMessage? DequeueNotification()
        {
            var now = DateTime.UtcNow;

            // Tìm message sẵn sàng trong queue
            NotificationMessage? result = null;
            var tempList = new List<NotificationMessage>();

            while (_queue.TryDequeue(out var message))
            {
                if (message.NextRetryAt.HasValue && message.NextRetryAt.Value > now)
                {
                    // Message chưa sẵn sàng, giữ lại để enqueue lại
                    tempList.Add(message);
                }
                else
                {
                    // Message sẵn sàng, trả về
                    result = message;
                    break;
                }
            }

            // Enqueue lại các message chưa sẵn sàng
            foreach (var msg in tempList)
            {
                _queue.Enqueue(msg);
            }

            return result;
        }

        public int GetPendingCount()
        {
            return _queue.Count;
        }
    }
}
