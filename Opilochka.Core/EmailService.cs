using MailKit.Net.Smtp;
using MimeKit;


namespace Opilochka.Core
{
    public class EmailService
    {
        private static readonly string SUBJECT_MESSAGE = "Доступ к системе";
        private static readonly string LOGIN_TOPIC = "login: ";
        private static readonly string PASSWORD_TOPIC = "password: ";

        /// <summary>
        /// отправка сообщения на почту
        /// </summary>
        /// <param name="recipientAddress"></param> адрес почты
        /// <param name="password"></param> пароль для отправки
        public void SendEmail(string recipientAddress, string password)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Opilochka", "opilochkatopsecret@gmail.com"));
            message.To.Add(new MailboxAddress("", recipientAddress));
            message.Subject = SUBJECT_MESSAGE;
            message.Body = new TextPart("plain")
            {
                Text = LOGIN_TOPIC + recipientAddress + "\n" + PASSWORD_TOPIC + password 
            };
            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("opilochkatopsecret@gmail.com", "tlwa dxrt wzvz htjd");
                //client.Authenticate("opilochka100@gmail.com", "ygzr ljty irfu xaxy");
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}
