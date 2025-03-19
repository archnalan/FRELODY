using SongsWithChords.Dtos.AuthDtos;
using SongsWithChords.Dtos.SubDtos;
using SongsWithChords.Dtos.UserDtos;
using SongsWithChords.ServiceHandler;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Data
{
    public interface IAuthorizationDAL
    {
        Task<ServiceResult<string>> AddUserToRoleAsync(string userId, string roleName);
        Task<ServiceResult<CreateUserResponseDto>> CreateUser([Required] CreateUserDto createUserDto, Guid tenantId);
        Task<ServiceResult<LoginResponseDto>> ExternalLoginCallback(string code, Guid tenantId);
        Task<ServiceResult<UpdateUserProfileOutDto>> GetUserProfile(string id = null, string userName = null);
        Task<ServiceResult<List<ComboBoxDto>>> GetUsersForComboBoxes();
        Task<ServiceResult<string>> InitiatePasswordReset(string emailAddress);
        Task<ServiceResult<LoginResponseDto>> Login(UserLogin userLogin);
        Task<ServiceResult<LoginResponseDto>> LoginUserNameOrPhone(LoginUserNameOrPhoneDto userLogin, Guid tenantId);
        Task<ServiceResult<string>> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<ServiceResult<string>> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<ServiceResult<CreateUserResponseDto>> UpdateUser(UpdateUserProfile updateUserProfile);
        Task<ServiceResult<LoginResponseDto>> RefreshToken(string accessToken, string refreshToken);
        Task<ServiceResult<bool>> RevokeToken(string refreshToken);
        Task<ServiceResult<bool>> LogSecurityEvent(string userId, string eventType, string description, string ipAddress);

    }
}