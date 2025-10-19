using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TenantDto), 200)]
        public async Task<IActionResult> CreateTenant([FromBody] TenantCreateDto dto, [FromQuery] string password)
        {
            var result = await _tenantService.CreateTenant(dto, password);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.Error.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Owner}")]
        [ProducesResponseType(typeof(PaginationDetails<TenantDto>), 200)]
        public async Task<IActionResult> GetAllTenants(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string sortByColumn = "TenantName",
            [FromQuery] bool sortAscending = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _tenantService.GetAllTenants(offset, limit, sortByColumn, sortAscending, cancellationToken);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.Error.Message);
            }
            return Ok(result.Data);
        }
    }
}