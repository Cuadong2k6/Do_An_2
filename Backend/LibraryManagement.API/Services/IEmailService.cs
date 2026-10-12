// Giao diện service cho email service
namespace LibraryManagement.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string body, string recipient);
        Task SendTemplatedEmailAsync(string template, Dictionary<string, string> templateData, string recipient);
    }
}