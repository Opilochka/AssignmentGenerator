namespace Opilochka.Core
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipientAddress, string password, CancellationToken token = default);
    }
}