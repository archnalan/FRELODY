using FRELODYAPP.Data;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYSHRD.Dtos.AuthDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationDAL;

        public AuthorizationController(IAuthorizationService authorizationDAL)
        {
            _authorizationDAL = authorizationDAL;
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> AddUserToRole([FromBody] AddUserToRoleDto UserRoleDto)
        {
            var result = await _authorizationDAL.AddUserToRoleAsync(UserRoleDto.UserId, UserRoleDto.RoleName);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateUserResponseDto), 200)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var result = await _authorizationDAL.CreateUser(createUserDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        public async Task<IActionResult> ExternalLoginCallback([FromBody] ExternalLoginDto ExternalLogin)
        {
            var result = await _authorizationDAL.ExternalLoginCallback(ExternalLogin.Code, ExternalLogin.TenantId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(UpdateUserProfileOutDto), 200)]
        public async Task<IActionResult> GetUserProfile([FromQuery] string id = null, [FromQuery] string userName = null)
        {
            var result = await _authorizationDAL.GetUserProfile(id, userName);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ComboBoxDto>), 200)]
        public async Task<IActionResult> GetUsersForComboBoxes()
        {
            var result = await _authorizationDAL.GetUsersForComboBoxes();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> InitiatePasswordReset([FromBody] string EmailAddress)
        {
            var result = await _authorizationDAL.InitiatePasswordReset(EmailAddress);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        public async Task<IActionResult> Login([FromBody] UserLogin userLogin)
        {
            var result = await _authorizationDAL.Login(userLogin);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        public async Task<IActionResult> LoginUserNameOrPhone([FromBody] LoginUserNameOrPhoneDto loginDto, [FromQuery]string TenantId)
        {
            var result = await _authorizationDAL.LoginUserNameOrPhone(loginDto, TenantId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> RemoveUserFromRole([FromQuery]string UserId, [FromQuery] string RoleName)
        {
            var result = await _authorizationDAL.RemoveUserFromRoleAsync(UserId, RoleName);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authorizationDAL.ResetPassword(resetPasswordDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(CreateUserResponseDto), 200)]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserProfile updateUserProfile)
        {
            var result = await _authorizationDAL.UpdateUser(updateUserProfile);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenDto RefreshTokenDto)
        {
            var result = await _authorizationDAL.RefreshToken(RefreshTokenDto.AccessToken, RefreshTokenDto.RefreshToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> RevokeToken([FromBody] string RefreshToken)
        {
            var result = await _authorizationDAL.RevokeToken(RefreshToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> LogSecurityEvent([FromBody] LogSecurityEventDto logSecurityEventDto)
        {
            var result = await _authorizationDAL.LogSecurityEvent(
                logSecurityEventDto.UserId, 
                logSecurityEventDto.EventType, 
                logSecurityEventDto.Description, 
                logSecurityEventDto.IpAddress);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}