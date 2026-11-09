using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task GuiEmailAsync(string denEmail, string tieuDe, string noiDungHtml)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("Chưa cấu hình EmailSettings (SenderEmail, Password).");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName, Encoding.UTF8),
                Subject = tieuDe,
                SubjectEncoding = Encoding.UTF8,
                Body = noiDungHtml,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            message.To.Add(denEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                // Mật khẩu ứng dụng Google hiển thị có dấu cách, bỏ dấu cách đi cho chắc
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password.Replace(" ", ""))
            };

            await client.SendMailAsync(message);
        }
    }
}
