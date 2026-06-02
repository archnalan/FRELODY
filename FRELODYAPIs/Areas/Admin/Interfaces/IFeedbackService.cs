using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IFeedbackService
    {
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackAsync();
        Task<ServiceResult<UserFeedbackDto>> GetFeedbackByIdAsync(string id);
        Task<ServiceResult<UserFeedbackDto>> CreateFeedbackAsync(UserFeedbackCreateDto feedbackDto);
        Task<ServiceResult<UserFeedbackDto>> SubmitSupportRequestAsync(UserFeedbackCreateDto feedbackDto);
        Task<ServiceResult<UserFeedbackDto>> UpdateFeedbackStatusAsync(string id, FeedbackStatus status);
        Task<ServiceResult<bool>> DeleteFeedbackAsync(string id);
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackBySongIdAsync(string songId);
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackByUserIdAsync(string userId);
        Task<ServiceResult<PaginationDetails<UserFeedbackDto>>> GetFeedbackPagedAsync(
            string? keywords, FeedbackStatus? status, int offSet, int limit,
            string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<UserFeedbackDto>> ReplyToFeedbackAsync(string feedbackId, string body);
        Task<ServiceResult<UserFeedbackDto>> LogUserReplyAsync(string feedbackId, string body);

        // User-scoped methods (owner-only — scoped to current _userId)
        Task<ServiceResult<List<UserFeedbackDto>>> GetMyFeedbackAsync();
        Task<ServiceResult<bool>> HasMyFeedbackAsync();
        Task<ServiceResult<UserFeedbackDto>> AddUserReplyAsync(string feedbackId, string body);
    }
}