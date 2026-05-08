using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Interfaces;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.AuthDtos;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class OtpService : IOtpService
    {
        private readonly SongDbContext _context;
        private readonly ISmtpSenderService _smtpSender;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SecurityUtilityService _securityUtility;
        private readonly ILogger<OtpService> _logger;

        private const int OTP_EXPIRY_HOURS = 24;
        private const int MAX_VERIFY_ATTEMPTS = 5;
        private const int MAX_SEND_ATTEMPTS_PER_HOUR = 5;
        private const int MAX_SEND_ATTEMPTS_PER_DAY = 10;

        public OtpService(
            SongDbContext context,
            ISmtpSenderService smtpSender,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            SecurityUtilityService securityUtility,
            ILogger<OtpService> logger)
        {
            _context = context;
            _smtpSender = smtpSender;
            _userManager = userManager;
            _roleManager = roleManager;
            _securityUtility = securityUtility;
            _logger = logger;
        }

        public async Task<ServiceResult<SendOtpResponseDto>> SendOtp(SendOtpRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ServiceResult<SendOtpResponseDto>.Failure(new BadRequestException("Email is required."));

                // Rate limit: max sends per hour per email
                var rateLimitKey = $"otp_send_{request.Email.ToLowerInvariant()}";
                if (!_securityUtility.CheckRateLimit(rateLimitKey, MAX_SEND_ATTEMPTS_PER_HOUR, TimeSpan.FromHours(1)))
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new TooManyRequestsException("Too many verification requests. Please try again later."));

                // Rate limit: max sends per day per email
                var dailyKey = $"otp_send_daily_{request.Email.ToLowerInvariant()}";
                if (!_securityUtility.CheckRateLimit(dailyKey, MAX_SEND_ATTEMPTS_PER_DAY, TimeSpan.FromHours(24)))
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new TooManyRequestsException("Daily verification limit reached. Please try again tomorrow."));

                // Check if user already exists — bypass query filters since
                // anonymous callers have no tenant context and unconfirmed users are inactive
                var normalizedEmail = request.Email.ToUpperInvariant();
                var existingUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail
                                              && (u.IsDeleted == false || u.IsDeleted == null));

                if (existingUser != null && existingUser.EmailConfirmed)
                {
                    // Email verified but no password = incomplete registration, let them resume
                    if (string.IsNullOrEmpty(existingUser.PasswordHash))
                    {
                        return ServiceResult<SendOtpResponseDto>.Success(new SendOtpResponseDto
                        {
                            TenantId = existingUser.TenantId ?? "",
                            UserId = existingUser.Id,
                            Email = request.Email,
                            Message = "Email already verified. Please set your password.",
                            EmailAlreadyVerified = true
                        });
                    }

                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new BadRequestException("An account with this email already exists. Please sign in."));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                string tenantId;
                string userId;

                if (existingUser != null && !existingUser.EmailConfirmed)
                {
                    // Re-use existing unconfirmed account
                    userId = existingUser.Id;
                    tenantId = existingUser.TenantId ?? "";
                }
                else
                {
                    // Create lightweight tenant + user (unconfirmed)
                    var isPersonal = !string.IsNullOrWhiteSpace(request.FullName);
                    var tenantName = isPersonal ? request.FullName : request.TenantName ?? request.Email;
                    var email = request.Email;

                    var tenant = new Tenant
                    {
                        TenantName = tenantName!,
                        Email = email,
                        DateCreated = DateTime.UtcNow
                    };
                    await _context.Tenants.AddAsync(tenant);
                    await _context.SaveChangesAsync();

                    var names = (request.FullName ?? "").Split(' ', 2);
                    var emailParts = email.Split('@');
                    var userName = !string.IsNullOrWhiteSpace(emailParts[0]) ? emailParts[0] : email;

                    var user = new User
                    {
                        FirstName = names.Length > 0 && !string.IsNullOrWhiteSpace(names[0]) ? names[0] : (isPersonal ? request.FullName! : "Admin"),
                        LastName = names.Length > 1 ? names[1] : (isPersonal ? "" : "User"),
                        UserName = userName,
                        Email = email,
                        EmailConfirmed = false,
                        IsActive = false,
                        DateCreated = DateTimeOffset.UtcNow,
                        TenantId = tenant.Id,
                        UserType = isPersonal ? default : UserType.Admin
                    };

                    // Create user without password — password set later after verification
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        return ServiceResult<SendOtpResponseDto>.Failure(new BadRequestException(errors));
                    }

                    tenantId = tenant.Id;
                    userId = user.Id;
                }

                // Invalidate any existing unused OTPs for this email
                var existingOtps = await _context.EmailOtpVerifications
                    .Where(o => o.Email == request.Email.ToLowerInvariant() && !o.IsUsed)
                    .ToListAsync();
                foreach (var old in existingOtps)
                    old.IsUsed = true;

                // Generate cryptographically secure OTP
                var otpCode = GenerateSecureOtp();

                var otp = new EmailOtpVerification
                {
                    Email = request.Email.ToLowerInvariant(),
                    OtpCode = otpCode,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(OTP_EXPIRY_HOURS),
                    TenantId = tenantId,
                    UserId = userId
                };

                await _context.EmailOtpVerifications.AddAsync(otp);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send email
                var emailDto = new EmailDto(true)
                {
                    ToEmail = request.Email,
                    Subject = "Your FRELODY verification code",
                    Body = BuildVerificationEmailHtml(otpCode)
                };

                var emailResult = await _smtpSender.SendMailAsync(emailDto);
                if (!emailResult.IsSuccess)
                {
                    _logger.LogError("Failed to send OTP email to {Email}", request.Email);
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new BadRequestException("Failed to send verification email. Please try again."));
                }

                return ServiceResult<SendOtpResponseDto>.Success(new SendOtpResponseDto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Email = request.Email,
                    Message = "Verification code sent. Check your inbox."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP for {Email}", request.Email);
                return ServiceResult<SendOtpResponseDto>.Failure(
                    new BadRequestException("An error occurred. Please try again."));
            }
        }

        public async Task<ServiceResult<VerifyOtpResponseDto>> VerifyOtp(VerifyOtpRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
                    return ServiceResult<VerifyOtpResponseDto>.Failure(
                        new BadRequestException("Email and verification code are required."));

                // Rate limit verification attempts
                var rateLimitKey = $"otp_verify_{request.Email.ToLowerInvariant()}";
                if (!_securityUtility.CheckRateLimit(rateLimitKey, MAX_VERIFY_ATTEMPTS, TimeSpan.FromMinutes(15)))
                    return ServiceResult<VerifyOtpResponseDto>.Failure(
                        new TooManyRequestsException("Too many verification attempts. Please wait 15 minutes."));

                var otp = await _context.EmailOtpVerifications
                    .Where(o => o.Email == request.Email.ToLowerInvariant()
                                && !o.IsUsed
                                && o.ExpiresAt > DateTimeOffset.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return ServiceResult<VerifyOtpResponseDto>.Failure(
                        new BadRequestException("No valid verification code found. Please request a new one."));

                otp.AttemptCount++;

                if (otp.AttemptCount > MAX_VERIFY_ATTEMPTS)
                {
                    otp.IsUsed = true;
                    await _context.SaveChangesAsync();
                    return ServiceResult<VerifyOtpResponseDto>.Failure(
                        new BadRequestException("Too many failed attempts. Please request a new code."));
                }

                // Constant-time comparison to prevent timing attacks
                if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(request.OtpCode),
                    System.Text.Encoding.UTF8.GetBytes(otp.OtpCode)))
                {
                    await _context.SaveChangesAsync();
                    var remaining = MAX_VERIFY_ATTEMPTS - otp.AttemptCount;
                    return ServiceResult<VerifyOtpResponseDto>.Failure(
                        new BadRequestException($"Invalid code. {remaining} attempt(s) remaining."));
                }

                // Mark OTP as used
                otp.IsUsed = true;
                await _context.SaveChangesAsync();

                // Find user bypassing query filters (user is still inactive at this point)
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == otp.UserId);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    user.IsActive = true;
                    await _userManager.UpdateAsync(user);

                    // Assign all roles
                    foreach (var role in UserRoles.AllRoles)
                    {
                        if (await _roleManager.RoleExistsAsync(role))
                            await _userManager.AddToRoleAsync(user, role);
                    }
                }

                return ServiceResult<VerifyOtpResponseDto>.Success(new VerifyOtpResponseDto
                {
                    Verified = true,
                    TenantId = otp.TenantId ?? "",
                    UserId = otp.UserId ?? "",
                    Message = "Email verified successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", request.Email);
                return ServiceResult<VerifyOtpResponseDto>.Failure(
                    new BadRequestException("Verification failed. Please try again."));
            }
        }

        public async Task<ServiceResult<SendOtpResponseDto>> ResendOtp(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<SendOtpResponseDto>.Failure(new BadRequestException("Email is required."));

                // Rate limit resend
                var rateLimitKey = $"otp_send_{email.ToLowerInvariant()}";
                if (!_securityUtility.CheckRateLimit(rateLimitKey, MAX_SEND_ATTEMPTS_PER_HOUR, TimeSpan.FromHours(1)))
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new TooManyRequestsException("Too many verification requests. Please try again later."));

                // Find existing unverified account
                var existingOtp = await _context.EmailOtpVerifications
                    .Where(o => o.Email == email.ToLowerInvariant())
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingOtp == null)
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new BadRequestException("No pending verification found. Please start registration again."));

                // Invalidate old OTPs
                var oldOtps = await _context.EmailOtpVerifications
                    .Where(o => o.Email == email.ToLowerInvariant() && !o.IsUsed)
                    .ToListAsync();
                foreach (var old in oldOtps)
                    old.IsUsed = true;

                // Generate new OTP
                var otpCode = GenerateSecureOtp();

                var newOtp = new EmailOtpVerification
                {
                    Email = email.ToLowerInvariant(),
                    OtpCode = otpCode,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(OTP_EXPIRY_HOURS),
                    TenantId = existingOtp.TenantId,
                    UserId = existingOtp.UserId
                };

                await _context.EmailOtpVerifications.AddAsync(newOtp);
                await _context.SaveChangesAsync();

                // Send email
                var emailDto = new EmailDto(true)
                {
                    ToEmail = email,
                    Subject = "Your FRELODY verification code",
                    Body = BuildVerificationEmailHtml(otpCode)
                };

                var emailResult = await _smtpSender.SendMailAsync(emailDto);
                if (!emailResult.IsSuccess)
                    return ServiceResult<SendOtpResponseDto>.Failure(
                        new BadRequestException("Failed to send verification email. Please try again."));

                return ServiceResult<SendOtpResponseDto>.Success(new SendOtpResponseDto
                {
                    TenantId = existingOtp.TenantId ?? "",
                    UserId = existingOtp.UserId ?? "",
                    Email = email,
                    Message = "New verification code sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP for {Email}", email);
                return ServiceResult<SendOtpResponseDto>.Failure(
                    new BadRequestException("An error occurred. Please try again."));
            }
        }

        private static string GenerateSecureOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private static string BuildVerificationEmailHtml(string code)
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
      <p style=""margin:0 0 6px;font-size:18px;font-weight:600;color:#111827;"">Verify your email</p>
      <p style=""margin:0 0 24px;font-size:14px;color:#6b7280;line-height:1.6;"">
        Use the code below to complete your sign-up. It expires in <strong style=""color:#374151;"">24&nbsp;hours</strong>.
      </p>

      <!-- Code -->
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:24px;"">
        <tr>
          <td align=""center"">
            <div style=""display:inline-block;background:#f5f3ff;border-radius:8px;padding:14px 36px;"">
              <span style=""font-size:34px;font-weight:700;letter-spacing:10px;color:#667eea;font-family:'Courier New',monospace;"">{code}</span>
            </div>
          </td>
        </tr>
      </table>

      <p style=""margin:0;font-size:12px;color:#9ca3af;line-height:1.5;"">
        Didn't request this? You can safely ignore this email.
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
    }
}
