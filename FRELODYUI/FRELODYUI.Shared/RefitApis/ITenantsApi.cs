using Refit;
using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos.AuthDtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.Org;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYLIB.ServiceHandler;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ITenantsApi
    {
        [Post("/api/tenants/create-tenant")]
        Task<IApiResponse<TenantDto>> CreateTenant([Body] TenantCreateDto dto);

        [Post("/api/tenants/complete-tenant-registration")]
        Task<IApiResponse<TenantDto>> CompleteTenantRegistration([Body] CompleteTenantRegistrationDto dto);

        [Post("/api/tenants/complete-user-registration")]
        Task<IApiResponse<bool>> CompleteUserRegistration([Body] CompleteUserRegistrationDto dto);

        [Get("/api/tenants/get-all-tenants")]
        Task<IApiResponse<PaginationDetails<TenantDto>>> GetAllTenants(
            [Query] int offset = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "TenantName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);

        [Post("/api/tenants/get-username-suggestions")]
        Task<IApiResponse<UsernameSuggestionsResponseDto>> GetUsernameSuggestions([Body] UsernameSuggestionsRequestDto request);

        [Post("/api/tenants/check-username-availability")]
        Task<IApiResponse<UsernameAvailabilityResponseDto>> CheckUsernameAvailability([Body] UsernameAvailabilityRequestDto request);
    }
}