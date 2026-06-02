using Refit;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IMyFeedbackApi
    {
        [Get("/api/my-feedback/get-my-feedback")]
        Task<IApiResponse<List<UserFeedbackDto>>> GetMyFeedback();

        [Get("/api/my-feedback/has-feedback")]
        Task<IApiResponse<bool>> HasFeedback();

        [Post("/api/my-feedback/reply")]
        Task<IApiResponse<UserFeedbackDto>> Reply([Query] string id, [Body] FeedbackReplyCreateDto reply);
    }
}
