using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.AuthDtos;
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
using System.Text.RegularExpressions;

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
                // Bypass query filters — user may still be invisible to the tenant-scoped context
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (user == null)
                    return ServiceResult<TenantDto>.Failure(new NotFoundException("User not found."));

                if (!user.EmailConfirmed)
                    return ServiceResult<TenantDto>.Failure(new BadRequestException("Email has not been verified yet."));

                // Set chosen username if provided
                if (!string.IsNullOrWhiteSpace(dto.UserName))
                {
                    var normalizedUsername = dto.UserName.Trim().ToLowerInvariant();
                    // Validate username format
                    if (!Regex.IsMatch(normalizedUsername, @"^[a-zA-Z0-9_.-]{3,30}$"))
                        return ServiceResult<TenantDto>.Failure(new BadRequestException("Username must be 3-30 characters and contain only letters, numbers, underscores, dots, or hyphens."));

                    // Check availability (within the same tenant)
                    var usernameExists = await _context.Users
                        .IgnoreQueryFilters()
                        .AnyAsync(u => u.NormalizedUserName == normalizedUsername.ToUpperInvariant()
                                       && u.Id != dto.UserId
                                       && (u.IsDeleted == false || u.IsDeleted == null));
                    if (usernameExists)
                        return ServiceResult<TenantDto>.Failure(new BadRequestException("Username is already taken. Please choose a different one."));

                    user.UserName = normalizedUsername;
                    user.NormalizedUserName = normalizedUsername.ToUpperInvariant();
                }

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

        public async Task<ServiceResult<UsernameSuggestionsResponseDto>> GetUsernameSuggestions(UsernameSuggestionsRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ServiceResult<UsernameSuggestionsResponseDto>.Failure(new BadRequestException("Email is required."));

                var suggestions = new List<string>();

                // Base from email prefix
                var emailPrefix = request.Email.Split('@')[0].ToLowerInvariant();
                // Sanitize: keep only alphanumeric, underscore, dot, hyphen
                emailPrefix = Regex.Replace(emailPrefix, @"[^a-z0-9_.-]", "");
                if (emailPrefix.Length >= 3)
                    suggestions.Add(emailPrefix);

                // From full name
                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    var nameParts = request.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length >= 2)
                    {
                        // firstname.lastname
                        var nameCombo = $"{nameParts[0]}.{nameParts[^1]}".ToLowerInvariant();
                        nameCombo = Regex.Replace(nameCombo, @"[^a-z0-9_.-]", "");
                        if (nameCombo.Length >= 3 && !suggestions.Contains(nameCombo))
                            suggestions.Add(nameCombo);

                        // firstnamelastname
                        var nameJoined = $"{nameParts[0]}{nameParts[^1]}".ToLowerInvariant();
                        nameJoined = Regex.Replace(nameJoined, @"[^a-z0-9_.-]", "");
                        if (nameJoined.Length >= 3 && !suggestions.Contains(nameJoined))
                            suggestions.Add(nameJoined);
                    }
                    else if (nameParts.Length == 1)
                    {
                        var singleName = nameParts[0].ToLowerInvariant();
                        singleName = Regex.Replace(singleName, @"[^a-z0-9_.-]", "");
                        if (singleName.Length >= 3 && !suggestions.Contains(singleName))
                            suggestions.Add(singleName);
                    }
                }

                // Add numbered variants for diversity
                var random = new Random();
                var baseCandidates = suggestions.ToList();
                foreach (var baseName in baseCandidates)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var numbered = $"{baseName}{random.Next(10, 999)}";
                        if (!suggestions.Contains(numbered))
                            suggestions.Add(numbered);
                    }
                }

                // Filter out taken usernames
                var available = new List<string>();
                foreach (var suggestion in suggestions)
                {
                    var normalized = suggestion.ToUpperInvariant();
                    var exists = await _context.Users
                        .IgnoreQueryFilters()
                        .AnyAsync(u => u.NormalizedUserName == normalized
                                       && u.Id != request.ExcludeUserId
                                       && (u.IsDeleted == false || u.IsDeleted == null));
                    if (!exists)
                        available.Add(suggestion);

                    if (available.Count >= 5)
                        break;
                }

                return ServiceResult<UsernameSuggestionsResponseDto>.Success(new UsernameSuggestionsResponseDto
                {
                    Suggestions = available
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating username suggestions");
                return ServiceResult<UsernameSuggestionsResponseDto>.Failure(ex);
            }
        }

        public async Task<ServiceResult<UsernameAvailabilityResponseDto>> CheckUsernameAvailability(UsernameAvailabilityRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                    return ServiceResult<UsernameAvailabilityResponseDto>.Failure(new BadRequestException("Username is required."));

                var username = request.Username.Trim().ToLowerInvariant();

                // Validate format
                if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_.-]{3,30}$"))
                {
                    return ServiceResult<UsernameAvailabilityResponseDto>.Success(new UsernameAvailabilityResponseDto
                    {
                        IsAvailable = false,
                        Username = username,
                        Message = "Username must be 3-30 characters: letters, numbers, underscores, dots, or hyphens."
                    });
                }

                var normalized = username.ToUpperInvariant();
                var exists = await _context.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(u => u.NormalizedUserName == normalized
                                   && u.Id != request.ExcludeUserId
                                   && (u.IsDeleted == false || u.IsDeleted == null));

                return ServiceResult<UsernameAvailabilityResponseDto>.Success(new UsernameAvailabilityResponseDto
                {
                    IsAvailable = !exists,
                    Username = username,
                    Message = exists ? "Username is already taken." : "Username is available!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability");
                return ServiceResult<UsernameAvailabilityResponseDto>.Failure(ex);
            }
        }
    }
}