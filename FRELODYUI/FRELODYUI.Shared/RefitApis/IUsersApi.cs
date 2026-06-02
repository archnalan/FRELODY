using Refit;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.UserDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IUsersApi
    {
        [Get("/api/users/get-user-profile")]
        Task<IApiResponse<UpdateUserProfileOutDto>> GetUserProfile([Query] string userId);

        [Put("/api/users/edit-user-profile")]
        Task<IApiResponse<UpdateUserProfileOutDto>> EditUserProfile([Body] UpdateUserProfile updateUserProfileInDto);

        [Get("/api/users/search-users-for-combo-boxes")]
        Task<IApiResponse<PaginationDetails<ComboBoxDto>>> SearchUsersForComboBoxes(
            [Query] string? keywords = null,
            [Query] int offSet = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "FirstName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);

        [Get("/api/users/search-user-by-keywords")]
        Task<IApiResponse<PaginationDetails<CreateUserResponseDto>>> SearchUserByKeywords(
            [Query] string? keywords = null,
            [Query] int offSet = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "FirstName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);

        [Get("/api/users/get-all-users")]
        Task<IApiResponse<PaginationDetails<AppUserDto>>> GetAllUsers(
            [Query] int offSet = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "FirstName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);

        [Get("/api/users/search-for-users")]
        Task<IApiResponse<PaginationDetails<AppUserDto>>> SearchForUsers(
            [Query] string? keywords = null,
            [Query] int offSet = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "FirstName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);

        [Post("/api/users/disable-user")]
        Task<IApiResponse<bool>> DisableUser([Query] string userId);

        [Post("/api/users/enable-user")]
        Task<IApiResponse<bool>> EnableUser([Query] string userId);

        // Format "o" → ISO-8601 round-trip so the server binds DateTimeOffset reliably
        // (default Refit emits culture-specific "MM/dd/yyyy HH:mm:ss zzz" which is fragile).
        [Get("/api/users/get-signup-stats")]
        Task<IApiResponse<UserSignupStatsDto>> GetSignupStats(
            [Query(Format = "o")] DateTimeOffset from,
            [Query(Format = "o")] DateTimeOffset to);
    }
}