using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using FRELODYLIB.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class FeedbackService : IFeedbackService
    {
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly string _userId;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(SongDbContext context, ILogger<FeedbackService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
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