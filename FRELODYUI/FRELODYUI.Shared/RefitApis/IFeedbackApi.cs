using Refit;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IFeedbackApi
    {
        [Get("/api/feedback/get-all-feedback")]
        Task<IApiResponse<IEnumerable<UserFeedbackDto>>> GetAllFeedback();

        [Get("/api/feedback/get-feedback-by-id")]
        Task<IApiResponse<UserFeedbackDto>> GetFeedbackById([Query] string id);

        [Post("/api/feedback/create-feedback")]
        Task<IApiResponse<UserFeedbackDto>> CreateFeedback([Body] UserFeedbackCreateDto feedback);

        [Put("/api/feedback/update-feedback-status")]
        Task<IApiResponse<UserFeedbackDto>> UpdateFeedbackStatus([Query] string id, [Query] FeedbackStatus status);

        [Delete("/api/feedback/delete-feedback")]
        Task<IApiResponse<bool>> DeleteFeedback([Query] string id);

        [Get("/api/feedback/get-feedback-by-song-id")]
        Task<IApiResponse<IEnumerable<UserFeedbackDto>>> GetFeedbackBySongId([Query] string songId);

        [Get("/api/feedback/get-feedback-by-user-id")]
        Task<IApiResponse<IEnumerable<UserFeedbackDto>>> GetFeedbackByUserId([Query] string userId);
    }
}