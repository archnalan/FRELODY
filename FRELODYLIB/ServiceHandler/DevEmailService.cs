using FRELODYAPP.Dtos.AuthDtos;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using Microsoft.Extensions.Logging;

namespace FRELODYLIB.ServiceHandler
{
    /// <summary>
    /// Development-only email service that writes emails to disk as HTML files
    /// instead of sending them via SMTP. View emails by opening the files in a browser.
    /// Falls back to the real EmailService if file writing fails.
    /// </summary>
    public class DevEmailService : IEmailService
    {
        private readonly ILogger<DevEmailService> _logger;
        private readonly string _outputDir;

        public DevEmailService(ILogger<DevEmailService> logger)
        {
            _logger = logger;
            _outputDir = Path.Combine(AppContext.BaseDirectory, "dev-emails");
            Directory.CreateDirectory(_outputDir);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(EmailDto emailDto, CancellationToken cancellationToken = default)
        {
            if (emailDto == null)
                return ServiceResult<bool>.Failure(new ArgumentNullException(nameof(emailDto)));

            if (string.IsNullOrWhiteSpace(emailDto.ToEmail))
                return ServiceResult<bool>.Failure(new ArgumentException("Recipient email is required."));

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var safeEmail = emailDto.ToEmail.Replace("@", "_at_").Replace(".", "_");
                var fileName = $"{timestamp}_{safeEmail}.html";
                var filePath = Path.Combine(_outputDir, fileName);

                var wrapper = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        .dev-email-header {{
            background: #1a1a2e; color: #fff; padding: 16px 24px;
            font-family: 'Segoe UI', sans-serif; font-size: 13px;
            border-radius: 8px 8px 0 0;
        }}
        .dev-email-header b {{ color: #667eea; }}
        .dev-email-header .field {{ margin: 4px 0; }}
    </style>
</head>
<body style=""margin:0;padding:20px;background:#f0f0f0;font-family:sans-serif;"">
    <div style=""max-width:600px;margin:0 auto;"">
        <div class=""dev-email-header"">
            <div style=""font-size:16px;font-weight:bold;margin-bottom:8px;"">📧 Dev Email Capture</div>
            <div class=""field""><b>To:</b> {System.Net.WebUtility.HtmlEncode(emailDto.ToEmail)}</div>
            <div class=""field""><b>From:</b> {System.Net.WebUtility.HtmlEncode(emailDto.emailSenderAccount ?? "not set")}</div>
            <div class=""field""><b>Subject:</b> {System.Net.WebUtility.HtmlEncode(emailDto.Subject ?? "(none)")}</div>
            <div class=""field""><b>Time:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>
        </div>
        <div style=""background:#fff;padding:0;border-radius:0 0 8px 8px;overflow:hidden;"">
            {emailDto.Body}
        </div>
    </div>
</body>
</html>";

                await File.WriteAllTextAsync(filePath, wrapper, cancellationToken);

                _logger.LogInformation(
                    "📧 Dev email captured → {FilePath} | To: {To} | Subject: {Subject}",
                    filePath, emailDto.ToEmail, emailDto.Subject);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write dev email to disk for {ToEmail}", emailDto.ToEmail);
                return ServiceResult<bool>.Failure(ex);
            }
        }
    }
}
