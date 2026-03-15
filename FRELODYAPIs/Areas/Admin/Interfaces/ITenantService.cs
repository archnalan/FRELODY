using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ITenantService
    {
        Task<ServiceResult<TenantDto>> CreateTenant(TenantCreateDto dto, string password);
        Task<ServiceResult<PaginationDetails<TenantDto>>> GetAllTenants(int offset, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken);
    }
}