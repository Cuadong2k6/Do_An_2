using LibraryManagement.API.Services;

namespace LibraryManagement.API.BackgroundServices
{
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly INotificationQueue _notificationQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            INotificationQueue notificationQueue,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _notificationQueue = notificationQueue;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
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

            await emailService.SendEmailAsync(message.Subject, message.Body, message.Recipient);
            _logger.LogInformation("Đã gửi email thành công tới {Recipient}", message.Recipient);
        }

        private Task SendPushNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            _logger.LogWarning("Push notification chưa được hỗ trợ (Recipient: {Recipient})", message.Recipient);
            return Task.CompletedTask;
        }

        private Task SendSmsNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
        {
            _logger.LogWarning("SMS notification chưa được hỗ trợ (Recipient: {Recipient})", message.Recipient);
            return Task.CompletedTask;
        }

        private void HandleNotificationFailure(NotificationMessage message)
        {
            var retryConfig = _configuration.GetSection("Notification:Queue:RetryPolicy");
            var maxRetryCount = int.Parse(retryConfig["MaxRetryCount"] ?? "5");
            var initialDelaySeconds = int.Parse(retryConfig["InitialDelaySeconds"] ?? "30");
            var maxDelaySeconds = int.Parse(retryConfig["MaxDelaySeconds"] ?? "3600");

            message.RetryCount++;

            var delaySeconds = Math.Min(initialDelaySeconds * Math.Pow(2, message.RetryCount - 1), maxDelaySeconds);
            message.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);

            if (message.RetryCount <= maxRetryCount)
            {
                _notificationQueue.EnqueueNotification(message);
                _logger.LogWarning("Đặt lại notification {MessageId} để thử lại vào {NextRetry} (lần {RetryCount}/{MaxRetry})", 
                    message.Id, message.NextRetryAt, message.RetryCount, maxRetryCount);
            }
            else
            {
                _logger.LogError("Notification {MessageId} đã thử lại hết số lần ({MaxRetry})", message.Id, maxRetryCount);
            }
        }
    }
}
