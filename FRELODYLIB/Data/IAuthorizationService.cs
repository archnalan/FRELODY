using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Data
{
    public interface IAuthorizationService
    {
        Task<ServiceResult<string>> AddUserToRoleAsync(string userId, string roleName);
        Task<ServiceResult<CreateUserResponseDto>> CreateUser([Required] CreateUserDto createUserDto);
        Task<ServiceResult<LoginResponseDto>> ExternalLoginCallback(string code, string tenantId);
        Task<ServiceResult<UpdateUserProfileOutDto>> GetUserProfile(string id = null, string userName = null);
        Task<ServiceResult<List<ComboBoxDto>>> GetUsersForComboBoxes();
        Task<ServiceResult<string>> InitiatePasswordReset(string emailAddress);
        Task<ServiceResult<LoginResponseDto>> Login(UserLogin userLogin);
        Task<ServiceResult<LoginResponseDto>> LoginUserNameOrPhone(LoginUserNameOrPhoneDto userLogin, string tenantId);
        Task<ServiceResult<string>> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<ServiceResult<string>> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<ServiceResult<CreateUserResponseDto>> UpdateUser(UpdateUserProfile updateUserProfile);
        Task<ServiceResult<LoginResponseDto>> RefreshToken(string accessToken, string refreshToken);
        Task<ServiceResult<bool>> RevokeToken(string refreshToken);
        Task<ServiceResult<bool>> LogSecurityEvent(string userId, string eventType, string description, string ipAddress);

    }
}