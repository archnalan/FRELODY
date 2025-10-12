using Refit;
using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYLIB.ServiceHandler;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ITenantsApi
    {
        [Post("/api/tenants/create-tenant")]
        Task<IApiResponse<TenantDto>> CreateTenant([Body] TenantCreateDto dto, [Query] string password);

        [Get("/api/tenants/get-all-tenants")]
        Task<IApiResponse<PaginationDetails<TenantDto>>> GetAllTenants(
            [Query] int offset = 0,
            [Query] int limit = 10,
            [Query] string sortByColumn = "TenantName",
            [Query] bool sortAscending = true,
            CancellationToken cancellationToken = default);
    }
}