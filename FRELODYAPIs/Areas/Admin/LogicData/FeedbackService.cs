using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler;
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
                    .Include(f => f.Replies)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (feedback == null)
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new NotFoundException($"Feedback with ID: {id} does not exist."));

                var feedbackDto = feedback.Adapt<UserFeedbackDto>();
                // Map replies ordered by date ascending for chronological thread display
                feedbackDto.Replies = feedback.Replies?
                    .OrderBy(r => r.DateCreated)
                    .Adapt<List<FeedbackReplyDto>>();
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

        #region Get Feedback Paged
        public async Task<ServiceResult<PaginationDetails<UserFeedbackDto>>> GetFeedbackPagedAsync(
            string? keywords, FeedbackStatus? status, int offSet, int limit,
            string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.UserFeedback.AsNoTracking().AsQueryable();

                if (status.HasValue)
                    query = query.Where(f => f.Status == status.Value);

                if (!string.IsNullOrWhiteSpace(keywords))
                {
                    query = query.Where(f =>
                        (f.Subject != null && f.Subject.Contains(keywords)) ||
                        (f.Comment != null && f.Comment.Contains(keywords)) ||
                        (f.Title != null && f.Title.Contains(keywords)) ||
                        (f.Email != null && f.Email.Contains(keywords)) ||
                        (f.FullName != null && f.FullName.Contains(keywords)));
                }

                var result = await query
                    .Select(f => new UserFeedbackDto
                    {
                        Id = f.Id,
                        DateCreated = f.DateCreated,
                        DateModified = f.DateModified,
                        CreatedBy = f.CreatedBy,
                        ModifiedBy = f.ModifiedBy,
                        IsDeleted = f.IsDeleted,
                        TenantId = f.TenantId,
                        Subject = f.Subject,
                        Comment = f.Comment,
                        Title = f.Title,
                        Email = f.Email,
                        FullName = f.FullName,
                        SongId = f.SongId,
                        UserId = f.UserId,
                        Status = f.Status,
                        Song = f.Song == null ? null : new FRELODYAPP.Dtos.SongDto
                        {
                            Id = f.Song.Id,
                            Title = f.Song.Title
                        }
                    })
                    .ToPaginatedResultAsync(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<UserFeedbackDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged feedback");
                return ServiceResult<PaginationDetails<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion

        #region Reply To Feedback (Admin sends email reply, persists thread message)
        public async Task<ServiceResult<UserFeedbackDto>> ReplyToFeedbackAsync(string feedbackId, string body)
        {
            try
            {
                if (string.IsNullOrEmpty(feedbackId))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Feedback ID is required."));
                if (string.IsNullOrWhiteSpace(body))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Reply body cannot be empty."));

                var feedback = await _context.UserFeedback.FindAsync(feedbackId);
                if (feedback == null)
                    return ServiceResult<UserFeedbackDto>.Failure(new NotFoundException($"Feedback with ID: {feedbackId} does not exist."));

                if (string.IsNullOrWhiteSpace(feedback.Email))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("This feedback has no email address to reply to."));

                // Persist the reply
                var reply = new FeedbackReply
                {
                    FeedbackId = feedbackId,
                    Body = body,
                    Direction = FeedbackReplyDirection.AdminToUser,
                    AuthorUserId = _userId
                };
                await _context.FeedbackReplies.AddAsync(reply);

                // Auto-advance status from Pending to UnderReview
                if (feedback.Status == FeedbackStatus.Pending)
                {
                    feedback.Status = FeedbackStatus.UnderReview;
                    feedback.ModifiedBy = _userId;
                }
                await _context.SaveChangesAsync();

                // Send the email — failure is non-fatal; the reply is already persisted
                try
                {
                    var supportEmail = _config["ApplicationInfo:SupportEmail"] ?? "support@frelody.com";
                    var emailDto = new EmailDto(true)
                    {
                        ToEmail = feedback.Email,
                        ReplyToEmail = supportEmail,
                        Subject = $"Re: {feedback.Subject}",
                        Body = BuildReplyEmailHtml(feedback.FullName ?? feedback.Email, body, feedback.Subject)
                    };
                    var mailResult = await _emailSender.SendMailAsync(emailDto);
                    if (!mailResult.IsSuccess)
                        _logger.LogError(mailResult.Error, "Reply for feedback {Id} persisted but email failed.", feedbackId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reply for feedback {Id} persisted but email threw.", feedbackId);
                }

                return await GetFeedbackByIdAsync(feedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to feedback {Id}", feedbackId);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }

        private static string BuildReplyEmailHtml(string recipientName, string body, string originalSubject)
        {
            var enc = HtmlEncoder.Default;
            var name = enc.Encode(recipientName);
            var subject = enc.Encode(originalSubject ?? "(no subject)");
            var message = enc.Encode(body).Replace("\n", "<br>");

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
      <h1 style=""margin:0;font-size:18px;color:#111827;"">Re: {subject}</h1>
      <p style=""margin:6px 0 0;font-size:13px;color:#6b7280;"">Hi {name}, here is a reply from the Frelody support team.</p>
    </td>
  </tr>
  <tr>
    <td style=""padding:20px 32px 28px;"">
      <p style=""margin:8px 0 0;font-size:14px;color:#374151;line-height:1.6;"">{message}</p>
      <p style=""margin:24px 0 0;font-size:12px;color:#9ca3af;"">Reply to this email to continue the conversation.</p>
    </td>
  </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }
        #endregion

        #region Log User Reply (paste a reply the user sent back)
        public async Task<ServiceResult<UserFeedbackDto>> LogUserReplyAsync(string feedbackId, string body)
        {
            try
            {
                if (string.IsNullOrEmpty(feedbackId))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Feedback ID is required."));
                if (string.IsNullOrWhiteSpace(body))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Reply body cannot be empty."));

                var feedback = await _context.UserFeedback.FindAsync(feedbackId);
                if (feedback == null)
                    return ServiceResult<UserFeedbackDto>.Failure(new NotFoundException($"Feedback with ID: {feedbackId} does not exist."));

                var reply = new FeedbackReply
                {
                    FeedbackId = feedbackId,
                    Body = body,
                    Direction = FeedbackReplyDirection.UserToAdmin,
                    AuthorName = feedback.FullName ?? feedback.Email
                };
                await _context.FeedbackReplies.AddAsync(reply);
                await _context.SaveChangesAsync();

                return await GetFeedbackByIdAsync(feedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user reply for feedback {Id}", feedbackId);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }
        #endregion

        #region Get My Feedback (user-scoped)
        public async Task<ServiceResult<List<UserFeedbackDto>>> GetMyFeedbackAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_userId))
                    return ServiceResult<List<UserFeedbackDto>>.Success(new List<UserFeedbackDto>());

                var feedbackList = await _context.UserFeedback
                    .Include(f => f.Song)
                    .Include(f => f.Replies)
                    .Where(f => f.UserId == _userId)
                    .OrderByDescending(f => f.DateCreated)
                    .ToListAsync();

                var dtos = feedbackList.Select(f =>
                {
                    var dto = f.Adapt<UserFeedbackDto>();
                    dto.Replies = f.Replies?
                        .OrderBy(r => r.DateCreated)
                        .Adapt<List<FeedbackReplyDto>>();
                    return dto;
                }).ToList();

                return ServiceResult<List<UserFeedbackDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for user {UserId}", _userId);
                return ServiceResult<List<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get My Feedback Paged (user-scoped, keyword search)
        public async Task<ServiceResult<PaginationDetails<UserFeedbackDto>>> GetMyFeedbackPagedAsync(
            string? keywords, int offSet, int limit,
            string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_userId))
                    return ServiceResult<PaginationDetails<UserFeedbackDto>>.Success(
                        new PaginationDetails<UserFeedbackDto> { OffSet = offSet, Limit = limit, TotalSize = 0, HasMore = false, Data = new List<UserFeedbackDto>() });

                var query = _context.UserFeedback
                    .AsNoTracking()
                    .Where(f => f.UserId == _userId);

                if (!string.IsNullOrWhiteSpace(keywords))
                {
                    query = query.Where(f =>
                        (f.Subject != null && f.Subject.Contains(keywords)) ||
                        (f.Comment != null && f.Comment.Contains(keywords)));
                }

                var result = await query
                    .Select(f => new UserFeedbackDto
                    {
                        Id = f.Id,
                        DateCreated = f.DateCreated,
                        DateModified = f.DateModified,
                        CreatedBy = f.CreatedBy,
                        ModifiedBy = f.ModifiedBy,
                        IsDeleted = f.IsDeleted,
                        TenantId = f.TenantId,
                        Subject = f.Subject,
                        Comment = f.Comment,
                        Title = f.Title,
                        Email = f.Email,
                        FullName = f.FullName,
                        SongId = f.SongId,
                        UserId = f.UserId,
                        Status = f.Status,
                        Song = f.Song == null ? null : new FRELODYAPP.Dtos.SongDto
                        {
                            Id = f.Song.Id,
                            Title = f.Song.Title
                        },
                        Replies = f.Replies == null ? null : f.Replies
                            .OrderBy(r => r.DateCreated)
                            .Select(r => new FeedbackReplyDto
                            {
                                Id = r.Id,
                                Body = r.Body,
                                Direction = r.Direction,
                                DateCreated = r.DateCreated,
                                AuthorUserId = r.AuthorUserId,
                                FeedbackId = r.FeedbackId
                            }).ToList()
                    })
                    .ToPaginatedResultAsync(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<UserFeedbackDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged feedback for user {UserId}", _userId);
                return ServiceResult<PaginationDetails<UserFeedbackDto>>.Failure(ex);
            }
        }
        #endregion

        #region Has My Feedback (cheap existence check)
        public async Task<ServiceResult<bool>> HasMyFeedbackAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_userId))
                    return ServiceResult<bool>.Success(false);

                var has = await _context.UserFeedback.AnyAsync(f => f.UserId == _userId);
                return ServiceResult<bool>.Success(has);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feedback existence for user {UserId}", _userId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Add User Reply (user sends a reply from the platform)
        public async Task<ServiceResult<UserFeedbackDto>> AddUserReplyAsync(string feedbackId, string body)
        {
            try
            {
                if (string.IsNullOrEmpty(feedbackId))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Feedback ID is required."));
                if (string.IsNullOrWhiteSpace(body))
                    return ServiceResult<UserFeedbackDto>.Failure(new BadRequestException("Reply body cannot be empty."));

                var feedback = await _context.UserFeedback.FindAsync(feedbackId);
                // Do not leak existence: treat "not found" and "not owned" identically.
                if (feedback == null || feedback.UserId != _userId)
                    return ServiceResult<UserFeedbackDto>.Failure(
                        new NotFoundException("Feedback not found."));

                var reply = new FeedbackReply
                {
                    FeedbackId = feedbackId,
                    Body = body,
                    Direction = FeedbackReplyDirection.UserToAdmin,
                    AuthorUserId = _userId,
                    AuthorName = feedback.FullName ?? feedback.Email
                };
                await _context.FeedbackReplies.AddAsync(reply);

                // A user replying reopens / advances the thread status.
                if (feedback.Status == FeedbackStatus.Pending || feedback.Status == FeedbackStatus.Addressed)
                {
                    feedback.Status = FeedbackStatus.UnderReview;
                    feedback.ModifiedBy = _userId;
                }

                await _context.SaveChangesAsync();

                // Notify the support team off-platform; failure is non-fatal.
                try
                {
                    var supportEmail = _config["ApplicationInfo:SupportEmail"] ?? "support@frelody.com";
                    var enc = System.Text.Encodings.Web.HtmlEncoder.Default;
                    var name = string.IsNullOrWhiteSpace(feedback.FullName) ? (feedback.Email ?? "A user") : enc.Encode(feedback.FullName);
                    var subject = enc.Encode(feedback.Subject ?? "(no subject)");
                    var message = enc.Encode(body).Replace("\n", "<br>");
                    var receivedAt = DateTime.UtcNow.ToString("MMM dd, yyyy 'at' hh:mm tt 'UTC'");

                    var emailHtml = $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background:#f4f4f7;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f4f7;"">
<tr><td align=""center"" style=""padding:40px 20px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.07);"">
  <tr><td style=""padding:28px 32px 0;text-align:center;"">
    <span style=""font-size:13px;font-weight:700;letter-spacing:0.12em;text-transform:uppercase;color:#667eea;"">FRELODY</span>
  </td></tr>
  <tr><td style=""padding:20px 32px 0;"">
    <h1 style=""margin:0;font-size:18px;color:#111827;"">Re: {subject} — user replied</h1>
    <p style=""margin:6px 0 0;font-size:13px;color:#6b7280;"">Received {receivedAt} via the in-app conversation panel.</p>
  </td></tr>
  <tr><td style=""padding:12px 32px 0;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;"">
      <tr><td style=""padding:14px 16px;"">
        <span style=""font-size:11px;font-weight:600;letter-spacing:0.06em;text-transform:uppercase;color:#6b7280;"">From</span><br>
        <span style=""font-size:14px;font-weight:600;color:#111827;"">{name}</span>
        {(string.IsNullOrWhiteSpace(feedback.Email) ? "" : $"<span style=\"font-size:13px;color:#6b7280;\"> &lt;{enc.Encode(feedback.Email)}&gt;</span>")}
      </td></tr>
    </table>
  </td></tr>
  <tr><td style=""padding:20px 32px 28px;"">
    <span style=""font-size:11px;font-weight:600;letter-spacing:0.06em;text-transform:uppercase;color:#6b7280;"">Message</span>
    <p style=""margin:8px 0 0;font-size:14px;color:#374151;line-height:1.6;"">{message}</p>
    <p style=""margin:24px 0 0;font-size:12px;color:#9ca3af;"">Reply via the admin feedback panel or directly to this email.</p>
  </td></tr>
</table>
</td></tr>
</table>
</body>
</html>";

                    var emailDto = new EmailDto(true)
                    {
                        ToEmail = supportEmail,
                        ReplyToEmail = feedback.Email,
                        Subject = $"Re: {feedback.Subject} — user replied",
                        Body = emailHtml
                    };
                    var mailResult = await _emailSender.SendMailAsync(emailDto);
                    if (!mailResult.IsSuccess)
                        _logger.LogError(mailResult.Error, "User reply for feedback {Id} persisted but notification email failed.", feedbackId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User reply for feedback {Id} persisted but notification email threw.", feedbackId);
                }

                return await GetFeedbackByIdAsync(feedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user reply for feedback {Id}", feedbackId);
                return ServiceResult<UserFeedbackDto>.Failure(ex);
            }
        }
        #endregion
    }
}