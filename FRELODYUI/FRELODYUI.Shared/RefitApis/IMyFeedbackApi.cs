using Refit;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IMyFeedbackApi
    {
        [Get("/api/myfeedback/get-my-feedback")]
        Task<IApiResponse<List<UserFeedbackDto>>> GetMyFeedback();

        [Get("/api/myfeedback/has-feedback")]
        Task<IApiResponse<bool>> HasFeedback();

        [Post("/api/myfeedback/reply")]
        Task<IApiResponse<UserFeedbackDto>> Reply([Query] string id, [Body] FeedbackReplyCreateDto reply);
    }
}
