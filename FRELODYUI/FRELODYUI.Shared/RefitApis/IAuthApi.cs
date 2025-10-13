using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using Refit;
using FRELODYSHRD.Dtos.AuthDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IAuthApi
    {
        [Post("/api/authorization/add-user-to-role")]
        Task<IApiResponse<string>> AddUserToRole([Body] AddUserToRoleDto userRoleDto);

        [Post("/api/authorization/create-user")]
        Task<IApiResponse<CreateUserResponseDto>> CreateUser([Body] CreateUserDto createUserDto);

        [Post("/api/authorization/external-login-callback")]
        Task<IApiResponse<LoginResponseDto>> ExternalLoginCallback([Body] ExternalLoginDto externalLogin);

        [Get("/api/authorization/get-user-profile")]
        Task<IApiResponse<UpdateUserProfileOutDto>> GetUserProfile([Query] string id = null, [Query] string userName = null);

        [Get("/api/authorization/get-users-for-combo-boxes")]
        Task<IApiResponse<List<ComboBoxDto>>> GetUsersForComboBoxes();

        [Post("/api/authorization/initiate-password-reset")]
        Task<IApiResponse<string>> InitiatePasswordReset([Body] string emailAddress);

        [Post("/api/authorization/login")]
        Task<IApiResponse<LoginResponseDto>> Login([Body] UserLogin userLogin);

        [Post("/api/authorization/login-user-name-or-phone")]
        Task<IApiResponse<LoginResponseDto>> LoginUserNameOrPhone([Body] LoginUserNameOrPhoneDto loginDto, [Query] string tenantId);

        [Post("/api/authorization/remove-user-from-role")]
        Task<IApiResponse<string>> RemoveUserFromRole([Query] string userId, [Query] string roleName);

        [Post("/api/authorization/reset-password")]
        Task<IApiResponse<string>> ResetPassword([Body] ResetPasswordDto resetPasswordDto);

        [Put("/api/authorization/update-user")]
        Task<IApiResponse<CreateUserResponseDto>> UpdateUser([Body] UpdateUserProfile updateUserProfile);

        [Post("/api/authorization/refresh-token")]
        Task<IApiResponse<LoginResponseDto>> RefreshToken([Body] RefreshTokenDto refreshTokenDto);

        [Post("/api/authorization/revoke-token")]
        Task<IApiResponse<bool>> RevokeToken([Body] string refreshToken);

        [Post("/api/authorization/log-security-event")]
        Task<IApiResponse<bool>> LogSecurityEvent([Body] LogSecurityEventDto logSecurityEventDto);
    }
}