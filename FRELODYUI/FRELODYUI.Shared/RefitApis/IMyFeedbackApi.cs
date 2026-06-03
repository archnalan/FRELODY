using Refit;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IMyFeedbackApi
    {
        [Get("/api/my-feedback/get-my-feedback")]
        Task<IApiResponse<List<UserFeedbackDto>>> GetMyFeedback();

        [Get("/api/my-feedback/get-my-feedback-paged")]
        Task<IApiResponse<PaginationDetails<UserFeedbackDto>>> GetMyFeedbackPaged(
            [Query] string? keywords = null,
            [Query] int offSet = 0,
            [Query] int limit = 20,
            [Query] string sortByColumn = "DateCreated",
            [Query] bool sortAscending = false,
            CancellationToken cancellationToken = default);

        [Get("/api/my-feedback/has-feedback")]
        Task<IApiResponse<bool>> HasFeedback();

        [Post("/api/my-feedback/reply")]
        Task<IApiResponse<UserFeedbackDto>> Reply([Query] string id, [Body] FeedbackReplyCreateDto reply);
    }
}
