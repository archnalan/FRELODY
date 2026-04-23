using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Authorization;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.Org;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FRELODYAPIs.Controllers
{
    /// <summary>
    /// Front-facing organization API. Wraps the legacy <c>TenantsController</c>
    /// with cleaner naming and modern endpoints. The underlying entity is still
    /// <c>Tenant</c>; the database column remains <c>TenantId</c>.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/organizations")]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationService _orgService;

        public OrganizationsController(IOrganizationService orgService)
        {
            _orgService = orgService;
        }

        // -------- self-service --------

        [HttpGet("current")]
        [ProducesResponseType(typeof(OrganizationDto), 200)]
        public async Task<IActionResult> GetCurrent()
        {
            var result = await _orgService.GetCurrentAsync();
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrganizationDto), 200)]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            var result = await _orgService.CreateAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpGet("preview-join/{targetOrganizationId}")]
        [ProducesResponseType(typeof(JoinOrgWarningDto), 200)]
        public async Task<IActionResult> PreviewJoin(string targetOrganizationId)
        {
            var result = await _orgService.PreviewJoinAsync(targetOrganizationId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("switch")]
        [ProducesResponseType(typeof(OrganizationDto), 200)]
        public async Task<IActionResult> Switch([FromBody] SwitchOrganizationDto dto)
        {
            var result = await _orgService.JoinAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("leave")]
        public async Task<IActionResult> Leave()
        {
            var result = await _orgService.LeaveAsync();
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("transfer-ownership")]
        [OrgRole(UserRoles.Owner)]
        public async Task<IActionResult> TransferOwnership([FromBody] TransferOwnershipDto dto)
        {
            var result = await _orgService.TransferOwnershipAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        // -------- org-admin: members & metrics --------

        [HttpGet("members")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin, UserRoles.Manager)]
        public async Task<IActionResult> GetMembers()
        {
            var result = await _orgService.GetMembersAsync();
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpGet("metrics")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin, UserRoles.Manager)]
        public async Task<IActionResult> GetMetrics()
        {
            var result = await _orgService.GetMetricsAsync();
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpGet("activity-summary")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin, UserRoles.Manager)]
        public async Task<IActionResult> GetActivitySummary()
        {
            var result = await _orgService.GetActivitySummaryAsync();
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> CreateMember([FromBody] CreateOrgMemberDto dto)
        {
            var result = await _orgService.CreateMemberAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members/invite")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> InviteExisting([FromBody] InviteExistingMemberDto dto)
        {
            var result = await _orgService.InviteExistingMemberAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members/{userId}/resend-credentials")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> ResendCredentials(string userId)
        {
            var result = await _orgService.ResendCredentialsAsync(userId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members/{userId}/disable")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> DisableMember(string userId)
        {
            var result = await _orgService.DisableMemberAsync(userId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members/{userId}/enable")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> EnableMember(string userId)
        {
            var result = await _orgService.EnableMemberAsync(userId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost("members/change-roles")]
        [OrgRole(UserRoles.Owner, UserRoles.Admin)]
        public async Task<IActionResult> ChangeRoles([FromBody] ChangeMemberRolesDto dto)
        {
            var result = await _orgService.ChangeMemberRolesAsync(dto);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }
    }
}
