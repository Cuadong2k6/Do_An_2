using LibraryManagement.API.BackgroundServices;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestNotificationController : ControllerBase
    {
        private readonly INotificationQueue _notificationQueue;
        private readonly ILogger<TestNotificationController> _logger;

        public TestNotificationController(
            INotificationQueue notificationQueue,
            ILogger<TestNotificationController> logger)
        {
            _notificationQueue = notificationQueue;
            _logger = logger;
        }

        [HttpPost("send-test-email")]
        public IActionResult SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var message = new NotificationMessage
                {
                    Type = "email",
                    Subject = request.Subject ?? "Test Email từ Library Management System",
                    Body = request.Body ?? "<h1>Đây là email test</h1><p>Hệ thống notification hoạt động bình thường.</p>",
                    Recipient = request.Recipient
                };

                _notificationQueue.EnqueueNotification(message);
                
                _logger.LogInformation("Đã thêm email test vào queue (Recipient: {Recipient})", request.Recipient);
                
                return Ok(new
                {
                    success = true,
                    messageId = message.Id,
                    queueSize = _notificationQueue.GetPendingCount()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm email test vào queue");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("queue-status")]
        public IActionResult GetQueueStatus()
        {
            var pendingCount = _notificationQueue.GetPendingCount();
            return Ok(new
            {
                pendingMessages = pendingCount,
                timestamp = DateTime.UtcNow
            });
        }
    }

    public class TestEmailRequest
    {
        public string Recipient { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
