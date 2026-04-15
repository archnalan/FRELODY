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
using FRELODYSHRD.Dtos.UserDtos;

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

        public async Task<ServiceResult<TenantDto>> CreateTenant(TenantCreateDto dto)
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
                    var userEmailParts = dto.UserEmail.Split('@');
                    user = new User
                    {
                        FirstName = names.Length > 0 ? names[0] : dto.UserFullName,
                        LastName = names.Length > 1 ? names[1] : "",
                        UserName = userEmailParts != null && userEmailParts.Length > 0
                            && !string.IsNullOrWhiteSpace(userEmailParts[0])
                            ? $"{userEmailParts[0]}"
                            : dto.UserEmail,
                        Email = dto.UserEmail,
                        EmailConfirmed = true,
                        IsActive = true,
                        DateCreated = DateTimeOffset.UtcNow,
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

                    var emailParts = dto.Email?.Split('@');
                    user = new User
                    {
                        FirstName = "Admin",
                        LastName = "User",                        
                        UserName = emailParts != null && emailParts.Length > 0 
                        && !string.IsNullOrWhiteSpace(emailParts[0])
                            ? $"{emailParts[0]}"
                            : dto.Email,
                        Email = dto.Email,
                        EmailConfirmed = true,
                        IsActive = true,
                        UserType = UserType.Admin,
                        DateCreated = DateTimeOffset.UtcNow,
                        TenantId = tenant.Id
                    };
                }

                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                var result = await _userManager.CreateAsync(user, dto.Password);
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

        public async Task<ServiceResult<TenantDto>> CompleteTenantRegistration(CompleteTenantRegistrationDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                    return ServiceResult<TenantDto>.Failure(new NotFoundException("User not found."));

                if (!user.EmailConfirmed)
                    return ServiceResult<TenantDto>.Failure(new BadRequestException("Email has not been verified yet."));

                // Set password
                var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.Password);
                if (!addPasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                    return ServiceResult<TenantDto>.Failure(new BadRequestException(errors));
                }

                // Update tenant details if provided
                var tenant = await _context.Tenants.FindAsync(dto.TenantId);
                if (tenant != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Address)) tenant.Address = dto.Address;
                    if (!string.IsNullOrWhiteSpace(dto.City)) tenant.City = dto.City;
                    if (!string.IsNullOrWhiteSpace(dto.Country)) tenant.Country = dto.Country;
                    if (!string.IsNullOrWhiteSpace(dto.TenantName)) tenant.TenantName = dto.TenantName;
                    tenant.DateModified = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var tenantDto = tenant?.Adapt<TenantDto>() ?? new TenantDto { Id = dto.TenantId };
                return ServiceResult<TenantDto>.Success(tenantDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing tenant registration for user {UserId}", dto.UserId);
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