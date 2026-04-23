using FRELODYSHRD.Dtos.Org;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IOrganizationsApi
    {
        // -------- self-service --------

        [Get("/api/organizations/current")]
        Task<IApiResponse<OrganizationDto?>> GetCurrent();

        [Post("/api/organizations")]
        Task<IApiResponse<OrganizationDto>> Create([Body] CreateOrganizationDto dto);

        [Get("/api/organizations/preview-join/{targetOrganizationId}")]
        Task<IApiResponse<JoinOrgWarningDto>> PreviewJoin(string targetOrganizationId);

        [Post("/api/organizations/switch")]
        Task<IApiResponse<OrganizationDto>> Switch([Body] SwitchOrganizationDto dto);

        [Post("/api/organizations/leave")]
        Task<IApiResponse<bool>> Leave();

        [Post("/api/organizations/transfer-ownership")]
        Task<IApiResponse<bool>> TransferOwnership([Body] TransferOwnershipDto dto);

        // -------- org-admin: members & metrics --------

        [Get("/api/organizations/members")]
        Task<IApiResponse<List<OrganizationMemberRowDto>>> GetMembers();

        [Get("/api/organizations/metrics")]
        Task<IApiResponse<OrgMetricsDto>> GetMetrics();

        [Get("/api/organizations/activity-summary")]
        Task<IApiResponse<OrgActivitySummaryDto>> GetActivitySummary();

        [Post("/api/organizations/members")]
        Task<IApiResponse<OrganizationMemberRowDto>> CreateMember([Body] CreateOrgMemberDto dto);

        [Post("/api/organizations/members/invite")]
        Task<IApiResponse<OrganizationMemberRowDto>> InviteExisting([Body] InviteExistingMemberDto dto);

        [Post("/api/organizations/members/{userId}/resend-credentials")]
        Task<IApiResponse<bool>> ResendCredentials(string userId);

        [Post("/api/organizations/members/{userId}/disable")]
        Task<IApiResponse<bool>> DisableMember(string userId);

        [Post("/api/organizations/members/{userId}/enable")]
        Task<IApiResponse<bool>> EnableMember(string userId);

        [Post("/api/organizations/members/change-roles")]
        Task<IApiResponse<OrganizationMemberRowDto>> ChangeRoles([Body] ChangeMemberRolesDto dto);
    }
}
