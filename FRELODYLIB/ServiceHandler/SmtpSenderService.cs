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

            // Use config for base URL; fallback to provided
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? requestorUri;
            var callbackUrl = $"{baseUrl}/resetpassword/{code}"; // Removed placeholder "isLocal"

            var emailDto = new EmailDto(true)
            {
                Subject = "Reset Your Password",
                ToEmail = appuser.Email,
                Body = $"You have requested to reset your password with Billtrick POS. <br/><br/>If you want to reset your password, please click this link.<br/><br/><a href='{callbackUrl}'>{callbackUrl}</a><br/><br/> Thank you for being part of Billtrick POS."
            };

            return await SendMailAsync(emailDto);
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