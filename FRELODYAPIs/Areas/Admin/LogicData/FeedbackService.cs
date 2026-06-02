using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class FeedbackService : IFeedbackService
    {
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly string _userId;
        private readonly ILogger<FeedbackService> _logger;
        private readonly ISmtpSenderService _emailSender;
        private readonly IConfiguration _config;

        public FeedbackService(SongDbContext context, ILogger<FeedbackService> logger, ITenantProvider tenantProvider,
            ISmtpSenderService emailSender, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
            _emailSender = emailSender;
            _config = config;
        }

        #region Get All Feedback
        public async Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackAsync()
        {
            try
            {
                var feedback = await _context.UserFeedback
                    .Include(f => f.Song)
                    .OrderByDescending(f => f.DateCreated)
                    .ToListAsync();

                var feedbackDto = feedback.Adapt<List<UserFeedbackDto>>();
                return ServiceResult<List<UserFeedbackDto>>.Success(feedbackDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback");
                return ServiceResult<List<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get Feedback by Id
        public async Task<ServiceResult<UserFeedbackDto>> GetFeedbackByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new BadRequestException("Feedback ID is required."));

                var feedback = await _context.UserFeedback
                    .Include(f => f.Song)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (feedback == null)
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new NotFoundException($"Feedback with ID: {id} does not exist."));

                var feedbackDto = feedback.Adapt<UserFeedbackDto>();
                return ServiceResult<UserFeedbackDto>.Success(feedbackDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback by ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }
        #endregion

        #region Create Feedback
        public async Task<ServiceResult<UserFeedbackDto>> CreateFeedbackAsync(UserFeedbackCreateDto feedbackDto)
        {
            try
            {
                if (feedbackDto == null)
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new BadRequestException("Feedback data is required."));

                var feedback = feedbackDto.Adapt<UserFeedback>();
                feedback.Status = FeedbackStatus.Pending;
                feedback.UserId = string.IsNullOrEmpty(feedbackDto.UserId) ? _userId : feedbackDto.UserId;

                await _context.UserFeedback.AddAsync(feedback);
                await _context.SaveChangesAsync();

                var createdFeedback = await GetFeedbackByIdAsync(feedback.Id);
                if (createdFeedback.IsSuccess)
                {
                    return ServiceResult<UserFeedbackDto>.Success(createdFeedback.Data);
                }
                else
                {
                    var feedbackDtoResult = feedback.Adapt<UserFeedbackDto>();
                    return ServiceResult<UserFeedbackDto>.Success(feedbackDtoResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback: {Subject}. Error: {Error}", feedbackDto.Subject, ex);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }
        #endregion

        #region Submit Support Request
        public async Task<ServiceResult<UserFeedbackDto>> SubmitSupportRequestAsync(UserFeedbackCreateDto feedbackDto)
        {
            if (feedbackDto == null)
                return ServiceResult<UserFeedbackDto>.Failure(
                    new BadRequestException("Support request data is required."));

            if (string.IsNullOrWhiteSpace(feedbackDto.Email))
                return ServiceResult<UserFeedbackDto>.Failure(
                    new BadRequestException("An email address is required so we can reply to you."));

            if (string.IsNullOrWhiteSpace(feedbackDto.Comment))
                return ServiceResult<UserFeedbackDto>.Failure(
                    new BadRequestException("Please describe how we can help."));

            // 1. Persist the request so it shows up in the admin feedback queue.
            var created = await CreateFeedbackAsync(feedbackDto);
            if (!created.IsSuccess)
                return created;

            // 2. Email the support inbox. Persistence already succeeded, so a mail
            //    failure is logged but does not fail the user's submission.
            try
            {
                var supportEmail = _config["ApplicationInfo:SupportEmail"] ?? "support@frelody.com";
                var appName = _config["ApplicationInfo:Name"] ?? "Frelody";

                var emailDto = new EmailDto(true)
                {
                    ToEmail = supportEmail,
                    ReplyToEmail = feedbackDto.Email,
                    Subject = $"[Support] {feedbackDto.Subject}",
                    Body = BuildSupportEmailHtml(feedbackDto, appName)
                };

                var mailResult = await _emailSender.SendMailAsync(emailDto);
                if (!mailResult.IsSuccess)
                    _logger.LogError(mailResult.Error,
                        "Support request {Id} saved but support email failed to send.", created.Data?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Support request {Id} saved but support email threw.", created.Data?.Id);
            }

            return created;
        }

        private static string BuildSupportEmailHtml(UserFeedbackCreateDto dto, string appName)
        {
            var enc = HtmlEncoder.Default;
            var name = string.IsNullOrWhiteSpace(dto.FullName) ? "A user" : enc.Encode(dto.FullName);
            var email = enc.Encode(dto.Email ?? "");
            var subject = enc.Encode(dto.Subject ?? "(no subject)");
            // Preserve the user's line breaks in the message body.
            var message = enc.Encode(dto.Comment ?? "").Replace("\n", "<br>");
            var receivedAt = DateTime.UtcNow.ToString("MMM dd, yyyy 'at' hh:mm tt 'UTC'");

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background:#f4f4f7;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f4f7;"">
<tr><td align=""center"" style=""padding:40px 20px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.07);"">

  <tr>
    <td style=""padding:28px 32px 0;text-align:center;"">
      <span style=""font-size:13px;font-weight:700;letter-spacing:0.12em;text-transform:uppercase;color:#667eea;"">FRELODY</span>
    </td>
  </tr>

  <tr>
    <td style=""padding:20px 32px 0;"">
      <h1 style=""margin:0;font-size:18px;color:#111827;"">New support request</h1>
      <p style=""margin:6px 0 0;font-size:13px;color:#6b7280;"">Received {receivedAt} via the {enc.Encode(appName)} support page.</p>
    </td>
  </tr>

  <tr>
    <td style=""padding:20px 32px 0;"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
             style=""background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;"">
        <tr><td style=""padding:14px 16px;border-bottom:1px solid #e5e7eb;"">
          <span style=""font-size:11px;font-weight:600;letter-spacing:0.06em;text-transform:uppercase;color:#6b7280;"">From</span><br>
          <span style=""font-size:14px;font-weight:600;color:#111827;"">{name}</span>
          <span style=""font-size:13px;color:#6b7280;""> &lt;{email}&gt;</span>
        </td></tr>
        <tr><td style=""padding:14px 16px;"">
          <span style=""font-size:11px;font-weight:600;letter-spacing:0.06em;text-transform:uppercase;color:#6b7280;"">Subject</span><br>
          <span style=""font-size:14px;font-weight:600;color:#111827;"">{subject}</span>
        </td></tr>
      </table>
    </td>
  </tr>

  <tr>
    <td style=""padding:20px 32px 28px;"">
      <span style=""font-size:11px;font-weight:600;letter-spacing:0.06em;text-transform:uppercase;color:#6b7280;"">Message</span>
      <p style=""margin:8px 0 0;font-size:14px;color:#374151;line-height:1.6;"">{message}</p>
      <p style=""margin:24px 0 0;font-size:12px;color:#9ca3af;"">Reply directly to this email to respond to {name}.</p>
    </td>
  </tr>

</table>
</td></tr>
</table>
</body>
</html>";
        }
        #endregion

        #region Update Feedback Status
        public async Task<ServiceResult<UserFeedbackDto>> UpdateFeedbackStatusAsync(string id, FeedbackStatus status)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new BadRequestException("Feedback ID is required."));

                var feedback = await _context.UserFeedback.FindAsync(id);
                if (feedback == null)
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new NotFoundException($"Feedback with ID: {id} does not exist."));

                feedback.Status = status;
                feedback.ModifiedBy = _userId;

                await _context.SaveChangesAsync();

                var updatedFeedback = await GetFeedbackByIdAsync(feedback.Id);
                if (updatedFeedback.IsSuccess)
                {
                    return ServiceResult<UserFeedbackDto>.Success(updatedFeedback.Data);
                }
                else
                {
                    var feedbackDto = feedback.Adapt<UserFeedbackDto>();
                    return ServiceResult<UserFeedbackDto>.Success(feedbackDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback status for ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }
        #endregion

        #region Delete Feedback
        public async Task<ServiceResult<bool>> DeleteFeedbackAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Feedback ID is required."));

                var feedback = await _context.UserFeedback.FindAsync(id);
                if (feedback == null)
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Feedback with ID: {id} does not exist."));

                feedback.IsDeleted = true;
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback with ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Get Feedback by Song ID
        public async Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackBySongIdAsync(string songId)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                    return ServiceResult<List<UserFeedbackDto>>.Failure(
                        new BadRequestException("Song ID is required."));

                var feedback = await _context.UserFeedback
                    .Include(f => f.Song)
                    .Where(f => f.SongId == songId)
                    .OrderByDescending(f => f.DateCreated)
                    .ToListAsync();

                var feedbackDto = feedback.Adapt<List<UserFeedbackDto>>();
                return ServiceResult<List<UserFeedbackDto>>.Success(feedbackDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for song ID: {SongId}. Error: {Error}", songId, ex);
                return ServiceResult<List<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get Feedback by User ID
        public async Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<List<UserFeedbackDto>>.Failure(
                        new BadRequestException("User ID is required."));

                var feedback = await _context.UserFeedback
                    .Include(f => f.Song)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.DateCreated)
                    .ToListAsync();

                var feedbackDto = feedback.Adapt<List<UserFeedbackDto>>();
                return ServiceResult<List<UserFeedbackDto>>.Success(feedbackDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for user ID: {UserId}. Error: {Error}", userId, ex);
                return ServiceResult<List<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion
    }
}