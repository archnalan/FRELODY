using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.Org;
using System.Threading.Tasks;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    /// <summary>
    /// Front-facing "Organization" facade that delegates to the existing
    /// <c>Tenant</c> entity / services. New code should use this; legacy
    /// <see cref="ITenantService"/> remains for backward compatibility.
    /// </summary>
    public interface IOrganizationService
    {
        // ----- self-service / discovery -----
        Task<ServiceResult<OrganizationDto?>> GetCurrentAsync();
        Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationDto dto);
        Task<ServiceResult<OrganizationDto>> JoinAsync(SwitchOrganizationDto dto);
        Task<ServiceResult<JoinOrgWarningDto>> PreviewJoinAsync(string targetOrganizationId);
        Task<ServiceResult<bool>> LeaveAsync();
        Task<ServiceResult<bool>> TransferOwnershipAsync(TransferOwnershipDto dto);

        // ----- org-admin: members -----
        Task<ServiceResult<System.Collections.Generic.List<OrganizationMemberRowDto>>> GetMembersAsync();
        Task<ServiceResult<OrgMetricsDto>> GetMetricsAsync();
        Task<ServiceResult<OrgActivitySummaryDto>> GetActivitySummaryAsync();
        Task<ServiceResult<OrganizationMemberRowDto>> CreateMemberAsync(CreateOrgMemberDto dto);
        Task<ServiceResult<bool>> InviteExistingMemberAsync(InviteExistingMemberDto dto);
        Task<ServiceResult<bool>> ResendCredentialsAsync(string userId);
        Task<ServiceResult<bool>> DisableMemberAsync(string userId);
        Task<ServiceResult<bool>> EnableMemberAsync(string userId);
        Task<ServiceResult<bool>> ChangeMemberRolesAsync(ChangeMemberRolesDto dto);
    }
}
