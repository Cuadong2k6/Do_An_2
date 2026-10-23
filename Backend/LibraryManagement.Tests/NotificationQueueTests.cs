using LibraryManagement.API.BackgroundServices;
using Xunit;

namespace LibraryManagement.Tests
{
    public class NotificationQueueTests
    {
        [Fact]
        public void DequeueNotification_SkipNotReadyMessage_ReturnNull()
        {
            // Arrange
            var queue = new NotificationQueue();
            var futureMessage = new NotificationMessage
            {
                Type = "Email",
                Subject = "Test Future",
                Body = "Body",
                Recipient = "test@example.com",
                NextRetryAt = DateTime.UtcNow.AddMinutes(10)
            };
            queue.EnqueueNotification(futureMessage);

            // Act
            var result = queue.DequeueNotification();

            // Assert
            Assert.Null(result);
            Assert.Equal(1, queue.GetPendingCount());
        }

        [Fact]
        public void DequeueNotification_ReadyMessage_ReturnMessage()
        {
            // Arrange
            var queue = new NotificationQueue();
            var readyMessage = new NotificationMessage
            {
                Type = "Email",
                Subject = "Test Ready",
                Body = "Body",
                Recipient = "test@example.com",
                NextRetryAt = DateTime.UtcNow.AddMinutes(-1)
            };
            queue.EnqueueNotification(readyMessage);

            // Act
            var result = queue.DequeueNotification();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Ready", result.Subject);
            Assert.Equal(0, queue.GetPendingCount());
        }

        [Fact]
        public void DequeueNotification_NoNextRetryAt_ReturnMessage()
        {
            // Arrange
            var queue = new NotificationQueue();
            var message = new NotificationMessage
            {
                Type = "Email",
                Subject = "Test No Retry",
                Body = "Body",
                Recipient = "test@example.com",
                NextRetryAt = null
            };
            queue.EnqueueNotification(message);

            // Act
            var result = queue.DequeueNotification();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test No Retry", result.Subject);
            Assert.Equal(0, queue.GetPendingCount());
        }

        [Fact]
        public void DequeueNotification_NotReadyBlocksReadyMessage_FIXED()
        {
            // Arrange
            var queue = new NotificationQueue();
            
            // Message 1: chưa sẵn sàng
            var notReadyMessage = new NotificationMessage
            {
                Type = "Email",
                Subject = "Not Ready",
                Body = "Body 1",
                Recipient = "test1@example.com",
                NextRetryAt = DateTime.UtcNow.AddMinutes(10)
            };
            
            // Message 2: sẵn sàng
            var readyMessage = new NotificationMessage
            {
                Type = "Email",
                Subject = "Ready",
                Body = "Body 2",
                Recipient = "test2@example.com",
                NextRetryAt = null
            };
            
            queue.EnqueueNotification(notReadyMessage);
            queue.EnqueueNotification(readyMessage);

            // Act
            var result1 = queue.DequeueNotification();
            var result2 = queue.DequeueNotification();

            // Assert
            // FIXED: skip message chưa sẵn sàng, lấy message sẵn sàng phía sau
            Assert.NotNull(result1);
            Assert.Equal("Ready", result1.Subject); // Lấy được readyMessage
            
            // Lần dequeue thứ 2: không có message sẵn sàng nào nữa
            Assert.Null(result2);
            
            // Message chưa sẵn sàng vẫn còn trong queue
            Assert.Equal(1, queue.GetPendingCount());
        }

        [Fact]
        public void GetPendingCount_MultipleMessages_ReturnCorrectCount()
        {
            // Arrange
            var queue = new NotificationQueue();
            queue.EnqueueNotification(new NotificationMessage
            {
                Type = "Email",
                Subject = "Test 1",
                Recipient = "test1@example.com"
            });
            queue.EnqueueNotification(new NotificationMessage
            {
                Type = "Email",
                Subject = "Test 2",
                Recipient = "test2@example.com"
            });

            // Assert
            Assert.Equal(2, queue.GetPendingCount());
        }
    }
}
