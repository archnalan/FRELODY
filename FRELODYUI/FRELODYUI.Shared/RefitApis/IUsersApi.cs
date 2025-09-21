using Refit;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.UserDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IUsersApi
    {
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
    }
}