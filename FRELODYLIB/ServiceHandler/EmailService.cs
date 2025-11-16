using FRELODYAPP.Dtos.AuthDtos;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace FRELODYLIB.ServiceHandler
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(EmailDto emailDto, CancellationToken cancellationToken = default)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(emailDto);
            Console.WriteLine($"EmailService.SendEmailAsync called with: {json}");
            if (emailDto == null)
                return ServiceResult<bool>.Failure(new ArgumentNullException(nameof(emailDto)));

            if (string.IsNullOrWhiteSpace(emailDto.ToEmail))
                return ServiceResult<bool>.Failure(new ArgumentException("Recipient email is required.", nameof(emailDto.ToEmail)));

            if (string.IsNullOrWhiteSpace(emailDto.emailSenderAccount))
                return ServiceResult<bool>.Failure(new ArgumentException("Sender account is required.", nameof(emailDto.emailSenderAccount)));

            if (string.IsNullOrWhiteSpace(emailDto.emailSenderSecret))
                return ServiceResult<bool>.Failure(new ArgumentException("Sender secret is required.", nameof(emailDto.emailSenderSecret)));

            var smtpSection = _configuration.GetSection("SmtpCredentials:Default");
            var host = smtpSection["Host"] ?? "plesk7400.is.cc";
            var port = smtpSection.GetValue<int>("Port", 587);
            var enableSsl = smtpSection.GetValue<bool>("EnableSsl", false);
            var timeoutMs = smtpSection.GetValue<int>("TimeoutMs", 30_000);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailDto.CompanyName ?? "No Reply", emailDto.emailSenderAccount));
            email.To.Add(MailboxAddress.Parse(emailDto.ToEmail));

            if (!string.IsNullOrWhiteSpace(emailDto.ReplyToEmail))
            {
                email.ReplyTo.Add(MailboxAddress.Parse(emailDto.ReplyToEmail));
            }

            email.Subject = emailDto.Subject ?? "(No Subject)";
            email.Body = new TextPart(TextFormat.Html) { Text = emailDto.Body ?? "" };

            using var smtp = new SmtpClient();
            try
            {
                smtp.Timeout = timeoutMs;
                _logger.LogInformation("Connecting to SMTP {Host}:{Port}...", host, port);

                await smtp.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

                if (!string.IsNullOrWhiteSpace(emailDto.emailSenderAccount) &&
                    !string.IsNullOrWhiteSpace(emailDto.emailSenderSecret) &&
                    !(host == "localhost" && port == 1025))
                {
                    await smtp.AuthenticateAsync(emailDto.emailSenderAccount, emailDto.emailSenderSecret, cancellationToken);
                }

                await smtp.SendAsync(email, cancellationToken);
                _logger.LogInformation("Email sent successfully to {ToEmail}", emailDto.ToEmail);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex) when (ex is SmtpCommandException or MailKit.Security.AuthenticationException or TimeoutException)
            {
                _logger.LogError(ex, "SMTP failed: {Message}. To: {ToEmail}, From: {From}", ex.Message, emailDto.ToEmail, emailDto.emailSenderAccount);
                return ServiceResult<bool>.Failure(new InvalidOperationException($"Failed to send email to {emailDto.ToEmail}. Check SMTP settings or network.", ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {ToEmail}", emailDto.ToEmail);
                return ServiceResult<bool>.Failure(ex);
            }
            finally
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
