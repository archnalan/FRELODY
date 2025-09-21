using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<ComboBoxDto>), 200)]
        public async Task<IActionResult> SearchUsersForComboBoxes(
            [FromQuery] string? keywords = null,
            [FromQuery] int offSet = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string sortByColumn = "FirstName",
            [FromQuery] bool sortAscending = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.SearchUsersForComboBoxes(
                keywords ?? string.Empty, offSet, limit, sortByColumn, sortAscending, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<CreateUserResponseDto>), 200)]
        public async Task<IActionResult> SearchUserByKeywords(
            [FromQuery] string? keywords = null,
            [FromQuery] int offSet = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string sortByColumn = "FirstName",
            [FromQuery] bool sortAscending = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.SearchUserByKeywords(
                keywords ?? string.Empty, offSet, limit, cancellationToken, sortByColumn, sortAscending);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<AppUserDto>), 200)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int offSet = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string sortByColumn = "FirstName",
            [FromQuery] bool sortAscending = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.GetAllUsers(offSet, limit, sortByColumn, sortAscending, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<AppUserDto>), 200)]
        public async Task<IActionResult> SearchForUsers(
            [FromQuery] string? keywords = null,
            [FromQuery] int offSet = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string sortByColumn = "FirstName",
            [FromQuery] bool sortAscending = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.SearchForUsers(
                keywords ?? string.Empty, offSet, limit, sortByColumn, sortAscending, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }
    }
}