// Worker service cho notification queue
namespace LibraryManagement.API.BackgroundServices
{
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly INotificationQueue _notificationQueue;
        private readonly IServiceProvider _serviceProvider;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            INotificationQueue notificationQueue,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _notificationQueue = notificationQueue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Worker Service đang chạy");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var notification = _notificationQueue.DequeueNotification();
                    if (notification != null)
                    {
                        await ProcessNotificationAsync(notification, stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationWorkerService loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Notification Worker Service đã dừng");
        }

        private async Task ProcessNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            try
            {
                switch (message.Type.ToLower())
                {
                    case "email":
                        await SendEmailNotificationAsync(message, stoppingToken);
                        break;
                    case "push":
                        await SendPushNotificationAsync(message, stoppingToken);
                        break;
                    case "sms":
                        await SendSmsNotificationAsync(message, stoppingToken);
                        break;
                    default:
                        _logger.LogWarning("Không hỗ trợ loại notification: {Type}", message.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification {MessageId}", message.Id);
                HandleNotificationFailure(message);
            }
        }

        private async Task SendEmailNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                await emailService.SendEmailAsync(message.Subject, message.Body, message.Recipient);
                _logger.LogInformation("Đã gửi email thành công tới {Recipient}", message.Recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email tới {Recipient}", message.Recipient);
                HandleNotificationFailure(message);
            }
        }

        private async Task SendPushNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            // Implement push notification logic
            _logger.LogInformation("Đã gửi push notification tới {Recipient}", message.Recipient);
        }

        private async Task SendSmsNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            // Implement SMS notification logic
            _logger.LogInformation("Đã gửi SMS tới {Recipient}", message.Recipient);
        }

        private void HandleNotificationFailure(NotificationMessage message)
        {
            message.RetryCount++;
            message.NextRetryAt = DateTime.UtcNow.Add(TimeSpan.FromMinutes(Math.Pow(2, message.RetryCount)));

            if (message.RetryCount < message.RetryCount)
            {
                _notificationQueue.EnqueueNotification(message);
                _logger.LogWarning("Đặt lại notification {MessageId} để thử lại vào {NextRetry}", message.Id, message.NextRetryAt);
            }
            else
            {
                _logger.Error("Notification {MessageId} đã thử lại hết số lần", message.Id);
            }
        }
    }
}
