using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IFeedbackService
    {
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackAsync();
        Task<ServiceResult<UserFeedbackDto>> GetFeedbackByIdAsync(string id);
        Task<ServiceResult<UserFeedbackDto>> CreateFeedbackAsync(UserFeedbackCreateDto feedbackDto);
        Task<ServiceResult<UserFeedbackDto>> UpdateFeedbackStatusAsync(string id, FRELODYSHRD.ModelTypes.FeedbackStatus status);
        Task<ServiceResult<bool>> DeleteFeedbackAsync(string id);
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackBySongIdAsync(string songId);
        Task<ServiceResult<List<UserFeedbackDto>>> GetFeedbackByUserIdAsync(string userId);
    }
}