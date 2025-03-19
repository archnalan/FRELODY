using DocumentFormat.OpenXml.Vml;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SongsWithChords.Dtos.AuthDtos;

namespace SongsWithChords.Data
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void SendEmail(EmailDto emailDto)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailDto.CompanyName, emailDto.emailSenderAccount));
            email.To.Add(MailboxAddress.Parse(emailDto.ToEmail));
            email.ReplyTo.Add(MailboxAddress.Parse(emailDto.ReplyToEmail));


            email.Subject = emailDto.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = emailDto.Body
            };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.Connect("plesk7400.is.cc", 587, MailKit.Security.SecureSocketOptions.None);
            smtp.Authenticate(emailDto.emailSenderAccount, emailDto.emailSenderSecret);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
