using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FRELODYLIB.ServiceHandler
{
    public class SmtpSenderService : ISmtpSenderService
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<SmtpSenderService> _logger;

        public SmtpSenderService(IEmailService emailService, IConfiguration configuration, UserManager<User> userManager, ILogger<SmtpSenderService> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<bool>> SendMailAsync(EmailDto emailDto)
        {
            ValidateEmailDto(emailDto);

            try
            {
                // Pull defaults from config if not provided
                emailDto.emailSenderAccount ??= _configuration["SmtpCredentials:Default:EmailSenderAccount"] ?? throw new InvalidOperationException("Sender account not configured.");
                emailDto.emailSenderSecret ??= _configuration["SmtpCredentials:Default:EmailSenderSecret"] ?? throw new InvalidOperationException("Sender secret not configured.");
                emailDto.ToEmail ??= _configuration["SmtpCredentials:Default:SupportEmail"];

                var result = await _emailService.SendEmailAsync(emailDto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Email sent successfully to {ToEmail}", emailDto.ToEmail);
                    _logger.LogInformation("Email sent successfully with body: {Body}", emailDto.Body);
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogError(result.Error, "Failed to send email to {ToEmail}", emailDto.ToEmail);
                    return ServiceResult<bool>.Failure(result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", emailDto.ToEmail);
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> SendDeveloperNotificationEmailAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be empty", nameof(message));

            try
            {
                var supportEmail = _configuration["SmtpCredentials:Default:DeveloperNotificationEmail"] ?? throw new InvalidOperationException("Developer notification email not configured.");
                var emailDto = new EmailDto(true)
                {
                    ToEmail = supportEmail,
                    Body = message,
                    Subject = "Error in your app. Please check it out"
                };

                return await SendMailAsync(emailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send developer notification");
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> SendPasswordResetEmailAsync(string userEmail, string requestorUri)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                return ServiceResult<bool>.Failure(new ArgumentException("User email cannot be empty", nameof(userEmail)));

            if (string.IsNullOrWhiteSpace(requestorUri))
                return ServiceResult<bool>.Failure(new ArgumentException("Requestor URI cannot be empty", nameof(requestorUri)));

            var appuser = await _userManager.FindByEmailAsync(userEmail);
            if (appuser == null)
            {
                _logger.LogWarning("User with email {UserEmail} not found for password reset", userEmail);
                return ServiceResult<bool>.Failure(new KeyNotFoundException("User not found"));
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(appuser);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // requestorUri is the Origin header from the frontend (always the correct client URL).
            // Only fall back to config if Origin was not provided.
            var baseUrl = !string.IsNullOrWhiteSpace(requestorUri)
                ? requestorUri.TrimEnd('/')
                : _configuration["AppSettings:FrontendUrl"]
                    ?? _configuration["AppSettings:BaseUrl"]
                    ?? throw new InvalidOperationException("No frontend URL available for password reset link.");
            var encodedEmail = Uri.EscapeDataString(appuser.Email!);
            var callbackUrl = $"{baseUrl}/resetpassword/{code}?email={encodedEmail}";

            var emailDto = new EmailDto(true)
            {
                Subject = "Reset Your Password – FRELODY",
                ToEmail = appuser.Email,
                Body = BuildPasswordResetEmailHtml(callbackUrl)
            };
            Console.WriteLine($"callbackUrl: + {callbackUrl}");
            return await SendMailAsync(emailDto);
        }

        private static string BuildPasswordResetEmailHtml(string callbackUrl)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background:#f4f4f7;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f4f7;"">
<tr><td align=""center"" style=""padding:40px 20px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.07);"">

  <!-- Wordmark -->
  <tr>
    <td style=""padding:28px 32px 0;text-align:center;"">
      <span style=""font-size:13px;font-weight:700;letter-spacing:0.12em;text-transform:uppercase;color:#667eea;"">FRELODY</span>
    </td>
  </tr>

  <!-- Body -->
  <tr>
    <td style=""padding:24px 32px 28px;"">
      <p style=""margin:0 0 6px;font-size:18px;font-weight:600;color:#111827;"">Reset your password</p>
      <p style=""margin:0 0 24px;font-size:14px;color:#6b7280;line-height:1.6;"">
        We received a request to reset your password. Click the button below to choose a new one.
      </p>

      <!-- CTA -->
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:24px;"">
        <tr>
          <td style=""border-radius:6px;background:#667eea;"">
            <a href=""{callbackUrl}""
               style=""display:inline-block;padding:12px 28px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:6px;"">
              Reset password
            </a>
          </td>
        </tr>
      </table>

      <p style=""margin:0 0 6px;font-size:12px;color:#9ca3af;line-height:1.5;"">
        If the button doesn't work, copy this link into your browser:
      </p>
      <p style=""margin:0 0 20px;font-size:12px;word-break:break-all;"">
        <a href=""{callbackUrl}"" style=""color:#667eea;text-decoration:none;"">{callbackUrl}</a>
      </p>

      <p style=""margin:0;font-size:12px;color:#9ca3af;line-height:1.5;"">
        Didn't request this? Your password will remain unchanged.
      </p>
    </td>
  </tr>

  <!-- Footer -->
  <tr>
    <td style=""padding:16px 32px;border-top:1px solid #f3f4f6;text-align:center;"">
      <p style=""margin:0;font-size:11px;color:#9ca3af;"">
        &copy; {DateTime.UtcNow.Year} Frelody &middot; Your music, beautifully organized
      </p>
    </td>
  </tr>

</table>
</td></tr>
</table>
</body>
</html>";
        }

        private void ValidateEmailDto(EmailDto emailDto)
        {
            if (emailDto == null) throw new ArgumentNullException(nameof(emailDto));
            if (string.IsNullOrWhiteSpace(emailDto.ToEmail) && string.IsNullOrWhiteSpace(_configuration["SmtpCredentials:Default:SupportEmail"]))
                throw new ArgumentException("ToEmail must be provided or configured.");
            if (string.IsNullOrWhiteSpace(emailDto.Subject)) throw new ArgumentException("Subject cannot be empty.");
            if (string.IsNullOrWhiteSpace(emailDto.Body)) throw new ArgumentException("Body cannot be empty.");

            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(emailDto.ToEmail))
                throw new ArgumentException("Invalid ToEmail format.");
        }

        private static string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid base64 input", nameof(base64EncodedData));
            }
        }
    }
}