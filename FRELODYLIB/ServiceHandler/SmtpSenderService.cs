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
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f7;"">
<tr><td align=""center"" style=""padding:40px 20px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">

  <!-- Header -->
  <tr>
    <td style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:32px 32px 24px;text-align:center;"">
      <h1 style=""margin:0;font-size:24px;font-weight:700;color:#ffffff;letter-spacing:-0.5px;"">FRELODY</h1>
      <p style=""margin:8px 0 0;font-size:14px;color:rgba(255,255,255,0.85);"">Password Reset</p>
    </td>
  </tr>

  <!-- Body -->
  <tr>
    <td style=""padding:32px;"">
      <p style=""margin:0 0 8px;font-size:18px;font-weight:600;color:#1a1a2e;"">Hi there,</p>
      <p style=""margin:0 0 24px;font-size:15px;color:#4a4a68;line-height:1.6;"">
        We received a request to reset your password. Click the button below to choose a new one.
      </p>

      <!-- CTA button -->
      <div style=""text-align:center;margin:0 0 24px;"">
        <a href=""{callbackUrl}""
           style=""display:inline-block;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:#ffffff;font-size:16px;font-weight:600;text-decoration:none;padding:14px 36px;border-radius:8px;"">
          Reset Password
        </a>
      </div>

      <p style=""margin:0 0 16px;font-size:13px;color:#8888a0;line-height:1.5;"">
        If the button doesn't work, copy and paste this link into your browser:
      </p>
      <p style=""margin:0 0 24px;font-size:13px;word-break:break-all;"">
        <a href=""{callbackUrl}"" style=""color:#667eea;text-decoration:none;"">{callbackUrl}</a>
      </p>

      <p style=""margin:0;font-size:13px;color:#8888a0;line-height:1.5;"">
        If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.
      </p>
    </td>
  </tr>

  <!-- Footer -->
  <tr>
    <td style=""padding:20px 32px;background:#fafafe;border-top:1px solid #eeeeee;text-align:center;"">
      <p style=""margin:0;font-size:12px;color:#aaaacc;"">
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