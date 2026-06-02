using Refit;
using FRELODYLIB.ServiceHandler;
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

        [Post("/api/feedback/submit-support-request")]
        Task<IApiResponse<UserFeedbackDto>> SubmitSupportRequest([Body] UserFeedbackCreateDto feedback);

        [Put("/api/feedback/update-feedback-status")]
        Task<IApiResponse<UserFeedbackDto>> UpdateFeedbackStatus([Query] string id, [Query] FeedbackStatus status);

        [Delete("/api/feedback/delete-feedback")]
        Task<IApiResponse<bool>> DeleteFeedback([Query] string id);

        [Get("/api/feedback/get-feedback-by-song-id")]
        Task<IApiResponse<IEnumerable<UserFeedbackDto>>> GetFeedbackBySongId([Query] string songId);

        [Get("/api/feedback/get-feedback-by-user-id")]
        Task<IApiResponse<IEnumerable<UserFeedbackDto>>> GetFeedbackByUserId([Query] string userId);

        [Get("/api/feedback/get-feedback-paged")]
        Task<IApiResponse<PaginationDetails<UserFeedbackDto>>> GetFeedbackPaged(
            [Query] string? keywords = null,
            [Query] FeedbackStatus? status = null,
            [Query] int offSet = 0,
            [Query] int limit = 20,
            [Query] string sortByColumn = "DateCreated",
            [Query] bool sortAscending = false,
            CancellationToken cancellationToken = default);

        [Post("/api/feedback/reply-to-feedback")]
        Task<IApiResponse<UserFeedbackDto>> ReplyToFeedback([Query] string id, [Body] FeedbackReplyCreateDto reply);

        [Post("/api/feedback/log-user-reply")]
        Task<IApiResponse<UserFeedbackDto>> LogUserReply([Query] string id, [Body] FeedbackReplyCreateDto reply);
    }
}