using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.UserDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<bool>> DisableUser(string userId);
        Task<ServiceResult<UpdateUserProfileOutDto>> EditUserProfile(EditUserProfile dto);
        Task<ServiceResult<PaginationDetails<AppUserDto>>> GetAllUsers(int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<UpdateUserProfileOutDto>> GetUserProfile(string userId);
        Task<ServiceResult<PaginationDetails<AppUserDto>>> SearchForUsers(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<PaginationDetails<CreateUserResponseDto>>> SearchUserByKeywords(string keywords, int offSet, int limit, CancellationToken cancellationToken, string sortByColumn, bool sortAscending);
        Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchUsersForComboBoxes(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<bool>> UpdateUserPhoneNumberAsync(string userId, string newPhoneNumber);
    }
}