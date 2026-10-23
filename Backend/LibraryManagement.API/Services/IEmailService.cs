namespace LibraryManagement.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string body, string recipient);
    }
}