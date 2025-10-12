using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.Constants;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class TenantService : ITenantService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<TenantService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TenantService(SongDbContext context, ILogger<TenantService> logger, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ServiceResult<TenantDto>> CreateTenant(TenantCreateDto dto, string password)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Tenant tenant;
                User user;

                if (!string.IsNullOrWhiteSpace(dto.UserFullName) && !string.IsNullOrWhiteSpace(dto.UserEmail))
                {
                    // Personal mode: create tenant from personal details
                    var names = dto.UserFullName.Split(' ', 2);
                    tenant = new Tenant
                    {
                        TenantName = dto.UserFullName,
                        Email = dto.UserEmail,
                        DateCreated = DateTime.UtcNow
                    };

                    user = new User
                    {
                        FirstName = names.Length > 0 ? names[0] : dto.UserFullName,
                        LastName = names.Length > 1 ? names[1] : "",
                        UserName = dto.UserEmail,
                        Email = dto.UserEmail,
                        EmailConfirmed = true,
                        TenantId = tenant.Id
                    };
                }
                else
                {
                    // Company mode: use company details
                    tenant = new Tenant
                    {
                        TenantName = dto.TenantName,
                        Address = dto.Address,
                        City = dto.City,
                        Country = dto.Country,
                        Email = dto.Email,
                        DateCreated = DateTime.UtcNow
                    };

                    user = new User
                    {
                        FirstName = "Super",
                        LastName = "Admin",
                        UserName = dto.Email,
                        Email = dto.Email,
                        EmailConfirmed = true,
                        IsSystemUser = true,
                        TenantId = tenant.Id
                    };
                }

                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<TenantDto>.Failure(new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
                }

                // Assign all roles to the new user
                foreach (var role in UserRoles.AllRoles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                await transaction.CommitAsync();
                var tenantDto = tenant.Adapt<TenantDto>();
                return ServiceResult<TenantDto>.Success(tenantDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating tenant and user");
                return ServiceResult<TenantDto>.Failure(ex);
            }
        }

        public async Task<ServiceResult<PaginationDetails<TenantDto>>> GetAllTenants(int offset, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.Tenants.AsNoTracking();
                var result = await query.Select(t => new TenantDto
                {
                    Id = t.Id,
                    TenantName = t.TenantName,
                    Address = t.Address,
                    City = t.City,
                    State = t.State,
                    PostalCode = t.PostalCode,
                    Country = t.Country,
                    PhoneNumber = t.PhoneNumber,
                    Email = t.Email,
                    Website = t.Website,
                    Industry = t.Industry,
                    BusinessRegNumber = t.BusinessRegNumber,
                    TaxIdentificationNumber = t.TaxIdentificationNumber,
                    DateCreated = t.DateCreated,
                    DateModified = t.DateModified,
                    Access = t.Access
                }).ToPaginatedResultAsync(offset, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<TenantDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tenants");
                return ServiceResult<PaginationDetails<TenantDto>>.Failure(ex);
            }
        }
    }
}