using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace Opilochka.Core
{
    public class EmailService : IEmailService
    {
        private static readonly string SUBJECT_MESSAGE = "Доступ к системе";
        private static readonly string LOGIN_TOPIC = "login: ";
        private static readonly string PASSWORD_TOPIC = "password: ";

        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string recipientAddress, string password, CancellationToken token = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Opilochka", "opilochkatopsecret@gmail.com"));
            message.To.Add(new MailboxAddress("", recipientAddress));
            message.Subject = SUBJECT_MESSAGE;
            message.Body = new TextPart("plain")
            {
                Text = $"{LOGIN_TOPIC}{recipientAddress}\n{PASSWORD_TOPIC}{password}"
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 секунд
                    var timeoutToken = timeoutTokenSource.Token;
                    var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken).Token;

                    _logger.LogInformation("Начата отправка email на {Recipient}", recipientAddress);

                    await client.ConnectAsync("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.StartTls, combinedToken);
                    _logger.LogDebug("Подключились к SMTP");

                    await client.AuthenticateAsync("opilochkatopsecret@gmail.com", "txej awpt rqsa fwce", combinedToken);
                    _logger.LogDebug("Аутентифицировались");

                    await client.SendAsync(message, combinedToken);
                    _logger.LogDebug("Письмо отправлено");

                    await client.DisconnectAsync(true, combinedToken);
                    _logger.LogInformation("Email успешно отправлен на {Recipient}", recipientAddress);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError("Операция отменена: {Message}", ex.Message);
                    throw new TimeoutException("SMTP: Превышено время ожидания");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке email на {Recipient}: {Message}", recipientAddress, ex.Message);
                    throw;
                }
            }
        }
    }
}