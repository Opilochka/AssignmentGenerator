namespace Opilochka.Core
{
    public interface IEmailService
    {
        void SendEmail(string recipientAddress, string password);
    }
}