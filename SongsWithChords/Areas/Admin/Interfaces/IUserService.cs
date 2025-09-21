using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.UserDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<PaginationDetails<AppUserDto>>> GetAllUsers(int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<PaginationDetails<AppUserDto>>> SearchForUsers(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
        Task<ServiceResult<PaginationDetails<CreateUserResponseDto>>> SearchUserByKeywords(string keywords, int offSet, int limit, CancellationToken cancellationToken, string sortByColumn, bool sortAscending);
        Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchUsersForComboBoxes(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
    }
}