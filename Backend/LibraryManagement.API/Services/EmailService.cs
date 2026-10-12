// Email service implementation
namespace LibraryManagement.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task SendEmailAsync(string subject, string body, string recipient)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailConfiguration = _configuration.GetSection("Notification:Email");
            
            var smtpServer = emailConfiguration["SmtpServer"] ?? "localhost";
            var port = int.Parse(emailConfiguration["Port"] ?? "25");
            var username = emailConfiguration["Username"];
            var password = emailConfiguration["Password"];
            var fromAddress = emailConfiguration["FromAddress"] ?? "notifications@library.com";

            using var client = new SmtpClient(smtpServer, port)
                {
                    Credentials = !string.IsNullOrEmpty(username) ? new NetworkCredential(username, password) : null,
                    EnableSsl = port == 465 || port == 587,
                    UseDefaultCredentials = string.IsNullOrEmpty(username)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

message.To.Add(recipient);

                await client.SendMailAsync(message);
                _logger.LogInformation("Đã gửi email thành công tới {Recipient}", recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email tới {Recipient}", recipient);
                throw;
            }
        }
    }
}