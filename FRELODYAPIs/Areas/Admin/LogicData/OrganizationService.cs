using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.Org;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    /// <summary>
    /// "Organization" facade over the underlying <c>Tenant</c> entity. Handles
    /// org lifecycle, membership, role management, and admin operations.
    /// </summary>
    public class OrganizationService : IOrganizationService
    {
        private readonly SongDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITenantProvider _tenantProvider;
        private readonly ISmtpSenderService _emailService;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(
            SongDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITenantProvider tenantProvider,
            ISmtpSenderService emailService,
            ILogger<OrganizationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantProvider = tenantProvider;
            _emailService = emailService;
            _logger = logger;
        }

        // -----------------------------------------------------------------
        // Self-service
        // -----------------------------------------------------------------

        public async Task<ServiceResult<OrganizationDto?>> GetCurrentAsync()
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<OrganizationDto?>.Failure(new UnAuthorizedException("Not authenticated"));

                var user = await _context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || string.IsNullOrEmpty(user.TenantId))
                    return ServiceResult<OrganizationDto?>.Success(null);

                var tenant = await _context.Tenants.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == user.TenantId);
                if (tenant == null)
                    return ServiceResult<OrganizationDto?>.Success(null);

                var dto = MapToOrgDto(tenant);
                dto.MemberCount = await _context.Users.IgnoreQueryFilters()
                    .CountAsync(u => u.TenantId == tenant.Id && (u.IsDeleted == null || u.IsDeleted == false));
                return ServiceResult<OrganizationDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCurrentAsync failed");
                return ServiceResult<OrganizationDto?>.Failure(new ServerErrorException("Failed to get current organization."));
            }
        }

        public async Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationDto dto)
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<OrganizationDto>.Failure(new UnAuthorizedException("Not authenticated"));

                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<OrganizationDto>.Failure(new NotFoundException("User not found."));
                if (!string.IsNullOrEmpty(user.TenantId))
                    return ServiceResult<OrganizationDto>.Failure(new BadRequestException("You already belong to an organization. Leave it before creating a new one."));

                var tenant = new Tenant
                {
                    TenantName = dto.Name,
                    Address = dto.Address,
                    City = dto.City,
                    State = dto.State,
                    PostalCode = dto.PostalCode,
                    Country = dto.Country,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Website = dto.Website,
                    Industry = dto.Industry,
                    DateCreated = DateTime.UtcNow,
                };

                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();
                    await _context.Tenants.AddAsync(tenant);
                    await _context.SaveChangesAsync();

                    user.TenantId = tenant.Id;
                    await _userManager.UpdateAsync(user);

                    await EnsureRoleAsync(UserRoles.Owner);
                    await _userManager.AddToRoleAsync(user, UserRoles.Owner);

                    await tx.CommitAsync();

                    var resultDto = MapToOrgDto(tenant);
                    resultDto.MemberCount = 1;
                    return ServiceResult<OrganizationDto>.Success(resultDto);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAsync failed");
                return ServiceResult<OrganizationDto>.Failure(new ServerErrorException("Failed to create organization."));
            }
        }

        public async Task<ServiceResult<JoinOrgWarningDto>> PreviewJoinAsync(string targetOrganizationId)
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<JoinOrgWarningDto>.Failure(new UnAuthorizedException("Not authenticated"));

                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<JoinOrgWarningDto>.Failure(new NotFoundException("User not found."));

                var target = await _context.Tenants.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == targetOrganizationId);
                if (target == null) return ServiceResult<JoinOrgWarningDto>.Failure(new NotFoundException("Target organization not found."));

                var warning = new JoinOrgWarningDto
                {
                    TargetOrgName = target.TenantName,
                    RequiresContentForfeitConfirmation = false
                };

                if (!string.IsNullOrEmpty(user.TenantId) && user.TenantId != targetOrganizationId)
                {
                    var current = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId);
                    warning.CurrentOrgName = current?.TenantName;
                    warning.ContentCounts = await CountUserOrgContentAsync(user.Id, user.TenantId!);
                    warning.RequiresContentForfeitConfirmation = warning.ContentCounts.Total > 0;
                }
                return ServiceResult<JoinOrgWarningDto>.Success(warning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreviewJoinAsync failed");
                return ServiceResult<JoinOrgWarningDto>.Failure(new ServerErrorException("Failed to preview join."));
            }
        }

        public async Task<ServiceResult<OrganizationDto>> JoinAsync(SwitchOrganizationDto dto)
        {
            try
            {
                var preview = await PreviewJoinAsync(dto.TargetOrganizationId);
                if (!preview.IsSuccess)
                    return ServiceResult<OrganizationDto>.Failure(preview.Error);

                if (preview.Data!.RequiresContentForfeitConfirmation && !dto.ConfirmContentForfeit)
                    return ServiceResult<OrganizationDto>.Failure(
                        new BadRequestException("Switching organizations will forfeit access to your prior org's content. Confirm to proceed."));

                var userId = _tenantProvider.GetUserId();
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<OrganizationDto>.Failure(new NotFoundException("User not found."));

                // Owner cannot leave without transferring ownership.
                if (!string.IsNullOrEmpty(user.TenantId) && user.TenantId != dto.TargetOrganizationId
                    && await _userManager.IsInRoleAsync(user, UserRoles.Owner))
                {
                    var ownerCount = await CountOwnersAsync(user.TenantId!);
                    if (ownerCount <= 1)
                        return ServiceResult<OrganizationDto>.Failure(
                            new BadRequestException("You are the sole Owner of your current organization. Transfer ownership before switching."));
                }

                // Drop all existing org-tier roles, set tenant, give Viewer in new org.
                await StripOrgRolesAsync(user);
                user.TenantId = dto.TargetOrganizationId;
                await _userManager.UpdateAsync(user);
                await EnsureRoleAsync(UserRoles.Viewer);
                await _userManager.AddToRoleAsync(user, UserRoles.Viewer);

                var target = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == dto.TargetOrganizationId);
                var resultDto = MapToOrgDto(target!);
                resultDto.MemberCount = await _context.Users.IgnoreQueryFilters()
                    .CountAsync(u => u.TenantId == target!.Id);
                return ServiceResult<OrganizationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinAsync failed");
                return ServiceResult<OrganizationDto>.Failure(new ServerErrorException("Failed to switch organization."));
            }
        }

        public async Task<ServiceResult<bool>> LeaveAsync()
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<bool>.Failure(new NotFoundException("User not found."));
                if (string.IsNullOrEmpty(user.TenantId))
                    return ServiceResult<bool>.Failure(new BadRequestException("You are not in any organization."));

                if (await _userManager.IsInRoleAsync(user, UserRoles.Owner))
                {
                    var ownerCount = await CountOwnersAsync(user.TenantId!);
                    if (ownerCount <= 1)
                        return ServiceResult<bool>.Failure(
                            new BadRequestException("You are the sole Owner. Transfer ownership before leaving."));
                }

                await StripOrgRolesAsync(user);
                user.TenantId = null;
                await _userManager.UpdateAsync(user);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaveAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to leave organization."));
            }
        }

        public async Task<ServiceResult<bool>> TransferOwnershipAsync(TransferOwnershipDto dto)
        {
            try
            {
                var callerId = _tenantProvider.GetUserId();
                var caller = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == callerId);
                if (caller == null) return ServiceResult<bool>.Failure(new NotFoundException("User not found."));
                if (!await _userManager.IsInRoleAsync(caller, UserRoles.Owner))
                    return ServiceResult<bool>.Failure(new ForbiddenException("Only an Owner can transfer ownership."));
                if (string.IsNullOrEmpty(caller.TenantId))
                    return ServiceResult<bool>.Failure(new BadRequestException("You are not in any organization."));

                var newOwner = await _context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == dto.NewOwnerUserId && u.TenantId == caller.TenantId);
                if (newOwner == null)
                    return ServiceResult<bool>.Failure(new NotFoundException("Target user is not a member of your organization."));

                await EnsureRoleAsync(UserRoles.Owner);
                await EnsureRoleAsync(UserRoles.Admin);
                if (!await _userManager.IsInRoleAsync(newOwner, UserRoles.Owner))
                    await _userManager.AddToRoleAsync(newOwner, UserRoles.Owner);

                // Demote original owner to Admin (still has full rights bar deletion).
                await _userManager.RemoveFromRoleAsync(caller, UserRoles.Owner);
                if (!await _userManager.IsInRoleAsync(caller, UserRoles.Admin))
                    await _userManager.AddToRoleAsync(caller, UserRoles.Admin);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TransferOwnershipAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to transfer ownership."));
            }
        }

        // -----------------------------------------------------------------
        // Org-admin: members & metrics
        // -----------------------------------------------------------------

        public async Task<ServiceResult<List<OrganizationMemberRowDto>>> GetMembersAsync()
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                if (string.IsNullOrEmpty(orgId))
                    return ServiceResult<List<OrganizationMemberRowDto>>.Failure(new BadRequestException("No active organization."));

                var users = await _context.Users.IgnoreQueryFilters()
                    .Where(u => u.TenantId == orgId && (u.IsDeleted == null || u.IsDeleted == false))
                    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                    .ToListAsync();

                var rows = new List<OrganizationMemberRowDto>(users.Count);
                foreach (var u in users)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    rows.Add(new OrganizationMemberRowDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        UserName = u.UserName,
                        PhoneNumber = u.PhoneNumber,
                        ProfilePicUrl = u.ProfilePicUrl,
                        Roles = roles.Where(UserRoles.IsOrgRole).ToList(),
                        IsActive = u.IsActive ?? true,
                        MustChangePassword = u.MustChangePassword,
                        LastLoginDate = u.LastLoginDate,
                        DateCreated = u.DateCreated
                    });
                }
                return ServiceResult<List<OrganizationMemberRowDto>>.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMembersAsync failed");
                return ServiceResult<List<OrganizationMemberRowDto>>.Failure(new ServerErrorException("Failed to load members."));
            }
        }

        public async Task<ServiceResult<OrgMetricsDto>> GetMetricsAsync()
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                if (string.IsNullOrEmpty(orgId))
                    return ServiceResult<OrgMetricsDto>.Failure(new BadRequestException("No active organization."));

                var members = await _context.Users.IgnoreQueryFilters()
                    .Where(u => u.TenantId == orgId && (u.IsDeleted == null || u.IsDeleted == false))
                    .ToListAsync();

                int admins = 0;
                foreach (var u in members)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    if (roles.Contains(UserRoles.Owner) || roles.Contains(UserRoles.Admin))
                        admins++;
                }

                var counts = await CountAllOrgContentAsync(orgId);
                var dto = new OrgMetricsDto
                {
                    Members = members.Count,
                    ActiveMembers = members.Count(m => m.IsActive ?? true),
                    Admins = admins,
                    Songs = counts.Songs,
                    Playlists = counts.Playlists,
                    SongBooks = counts.SongBooks,
                    Albums = counts.Albums,
                };
                return ServiceResult<OrgMetricsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMetricsAsync failed");
                return ServiceResult<OrgMetricsDto>.Failure(new ServerErrorException("Failed to load metrics."));
            }
        }

        public async Task<ServiceResult<OrgActivitySummaryDto>> GetActivitySummaryAsync()
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                if (string.IsNullOrEmpty(orgId))
                    return ServiceResult<OrgActivitySummaryDto>.Failure(new BadRequestException("No active organization."));

                var since = DateTimeOffset.UtcNow.AddDays(-7);
                var dto = new OrgActivitySummaryDto
                {
                    SongsCreatedLast7Days = await _context.Songs.IgnoreQueryFilters()
                        .CountAsync(x => x.TenantId == orgId && x.DateCreated >= since && (x.IsDeleted == null || x.IsDeleted == false)),
                    PlaylistsCreatedLast7Days = await _context.Playlists.IgnoreQueryFilters()
                        .CountAsync(x => x.TenantId == orgId && x.DateCreated >= since && (x.IsDeleted == null || x.IsDeleted == false)),
                    NewMembersLast7Days = await _context.Users.IgnoreQueryFilters()
                        .CountAsync(u => u.TenantId == orgId && u.DateCreated >= since && (u.IsDeleted == null || u.IsDeleted == false)),
                };

                var lastSong = await _context.Songs.IgnoreQueryFilters()
                    .Where(x => x.TenantId == orgId && (x.IsDeleted == null || x.IsDeleted == false))
                    .OrderByDescending(x => x.DateCreated)
                    .Select(x => new { x.DateCreated, x.CreatedBy })
                    .FirstOrDefaultAsync();
                if (lastSong != null)
                {
                    dto.LastContentCreatedAt = lastSong.DateCreated;
                    if (!string.IsNullOrEmpty(lastSong.CreatedBy))
                    {
                        dto.LastContentCreatedByName = await _context.Users.IgnoreQueryFilters()
                            .Where(u => u.Id == lastSong.CreatedBy)
                            .Select(u => (u.FirstName ?? "") + " " + (u.LastName ?? ""))
                            .FirstOrDefaultAsync();
                    }
                }
                return ServiceResult<OrgActivitySummaryDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetActivitySummaryAsync failed");
                return ServiceResult<OrgActivitySummaryDto>.Failure(new ServerErrorException("Failed to load activity."));
            }
        }

        public async Task<ServiceResult<OrganizationMemberRowDto>> CreateMemberAsync(CreateOrgMemberDto dto)
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                if (string.IsNullOrEmpty(orgId))
                    return ServiceResult<OrganizationMemberRowDto>.Failure(new BadRequestException("No active organization."));

                var existing = await _userManager.FindByEmailAsync(dto.Email);
                if (existing != null)
                    return ServiceResult<OrganizationMemberRowDto>.Failure(
                        new ConflictException("A user with this email already exists. Use 'Invite existing user' instead."));

                var tempPassword = string.IsNullOrWhiteSpace(dto.TemporaryPassword)
                    ? GenerateTemporaryPassword()
                    : dto.TemporaryPassword!;

                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    UserName = string.IsNullOrWhiteSpace(dto.UserName) ? dto.Email : dto.UserName,
                    EmailConfirmed = true,
                    PhoneNumber = dto.PhoneNumber,
                    TenantId = orgId,
                    IsActive = true,
                    MustChangePassword = true,
                    DateCreated = DateTimeOffset.UtcNow,
                };

                var createResult = await _userManager.CreateAsync(user, tempPassword);
                if (!createResult.Succeeded)
                    return ServiceResult<OrganizationMemberRowDto>.Failure(
                        new BadRequestException(string.Join("; ", createResult.Errors.Select(e => e.Description))));

                var rolesToAssign = (dto.OrgRoles?.Where(UserRoles.IsOrgRole).ToList())
                                    ?? new List<string> { UserRoles.Viewer };
                if (rolesToAssign.Count == 0) rolesToAssign.Add(UserRoles.Viewer);
                foreach (var role in rolesToAssign)
                {
                    await EnsureRoleAsync(role);
                    await _userManager.AddToRoleAsync(user, role);
                }

                await SendCredentialsEmailAsync(user, tempPassword);

                var row = new OrganizationMemberRowDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    Roles = rolesToAssign,
                    IsActive = true,
                    MustChangePassword = true,
                    DateCreated = user.DateCreated
                };
                return ServiceResult<OrganizationMemberRowDto>.Success(row);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateMemberAsync failed");
                return ServiceResult<OrganizationMemberRowDto>.Failure(new ServerErrorException("Failed to create member."));
            }
        }

        public async Task<ServiceResult<bool>> InviteExistingMemberAsync(InviteExistingMemberDto dto)
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                if (string.IsNullOrEmpty(orgId))
                    return ServiceResult<bool>.Failure(new BadRequestException("No active organization."));

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                    return ServiceResult<bool>.Failure(new NotFoundException("No user with that email exists."));

                if (user.TenantId == orgId)
                    return ServiceResult<bool>.Failure(new ConflictException("User is already a member of this organization."));

                if (!string.IsNullOrEmpty(user.TenantId) && user.TenantId != orgId)
                {
                    var contentCounts = await CountUserOrgContentAsync(user.Id, user.TenantId!);
                    if (contentCounts.Total > 0 && !dto.ConfirmContentForfeit)
                        return ServiceResult<bool>.Failure(
                            new BadRequestException("Invitee has content under another organization. Caller must confirm forfeit."));
                }

                await StripOrgRolesAsync(user);
                user.TenantId = orgId;
                await _userManager.UpdateAsync(user);

                var rolesToAssign = (dto.OrgRoles?.Where(UserRoles.IsOrgRole).ToList())
                                    ?? new List<string> { UserRoles.Viewer };
                if (rolesToAssign.Count == 0) rolesToAssign.Add(UserRoles.Viewer);
                foreach (var role in rolesToAssign)
                {
                    await EnsureRoleAsync(role);
                    await _userManager.AddToRoleAsync(user, role);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InviteExistingMemberAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to invite member."));
            }
        }

        public async Task<ServiceResult<bool>> ResendCredentialsAsync(string userId)
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<bool>.Failure(new NotFoundException("User not found."));
                if (user.TenantId != orgId)
                    return ServiceResult<bool>.Failure(new ForbiddenException("User is not a member of your organization."));

                var newTemp = GenerateTemporaryPassword();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newTemp);
                if (!resetResult.Succeeded)
                    return ServiceResult<bool>.Failure(new BadRequestException(
                        string.Join("; ", resetResult.Errors.Select(e => e.Description))));

                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);
                await SendCredentialsEmailAsync(user, newTemp);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResendCredentialsAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to resend credentials."));
            }
        }

        public Task<ServiceResult<bool>> DisableMemberAsync(string userId) => SetMemberActiveAsync(userId, false);
        public Task<ServiceResult<bool>> EnableMemberAsync(string userId) => SetMemberActiveAsync(userId, true);

        private async Task<ServiceResult<bool>> SetMemberActiveAsync(string userId, bool active)
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResult<bool>.Failure(new NotFoundException("User not found."));
                if (user.TenantId != orgId)
                    return ServiceResult<bool>.Failure(new ForbiddenException("User is not a member of your organization."));
                user.IsActive = active;
                await _userManager.UpdateAsync(user);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetMemberActiveAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to update member status."));
            }
        }

        public async Task<ServiceResult<bool>> ChangeMemberRolesAsync(ChangeMemberRolesDto dto)
        {
            try
            {
                var orgId = _tenantProvider.GetTenantId();
                var callerId = _tenantProvider.GetUserId();
                var caller = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == callerId);
                if (caller == null) return ServiceResult<bool>.Failure(new NotFoundException("Caller not found."));

                var target = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (target == null) return ServiceResult<bool>.Failure(new NotFoundException("Target user not found."));
                if (target.TenantId != orgId)
                    return ServiceResult<bool>.Failure(new ForbiddenException("Target user is not in your organization."));

                var callerRoles = await _userManager.GetRolesAsync(caller);
                var targetRoles = await _userManager.GetRolesAsync(target);

                // Caller must outrank both the target's current roles and the new roles.
                if (!UserRoles.CanManage(callerRoles, targetRoles)
                    || !UserRoles.CanManage(callerRoles, dto.OrgRoles))
                    return ServiceResult<bool>.Failure(
                        new ForbiddenException("You cannot assign or remove roles at or above your own rank."));

                // Reject illegal role names early.
                var newRoles = dto.OrgRoles.Where(UserRoles.IsOrgRole).ToList();
                if (newRoles.Count == 0)
                    return ServiceResult<bool>.Failure(new BadRequestException("At least one valid org role is required."));

                // Owner role can only be granted via TransferOwnership.
                if (newRoles.Contains(UserRoles.Owner) && !targetRoles.Contains(UserRoles.Owner))
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Use Transfer Ownership to grant the Owner role."));

                // Strip current org roles and re-apply the new set.
                var existingOrgRoles = targetRoles.Where(UserRoles.IsOrgRole).ToList();
                if (existingOrgRoles.Any())
                    await _userManager.RemoveFromRolesAsync(target, existingOrgRoles);
                foreach (var role in newRoles)
                {
                    await EnsureRoleAsync(role);
                    await _userManager.AddToRoleAsync(target, role);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeMemberRolesAsync failed");
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to change member roles."));
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static OrganizationDto MapToOrgDto(Tenant tenant) => new()
        {
            Id = tenant.Id,
            Name = tenant.TenantName,
            Address = tenant.Address,
            City = tenant.City,
            State = tenant.State,
            PostalCode = tenant.PostalCode,
            Country = tenant.Country,
            PhoneNumber = tenant.PhoneNumber,
            Email = tenant.Email,
            Website = tenant.Website,
            Industry = tenant.Industry,
            BusinessRegNumber = tenant.BusinessRegNumber,
            TaxIdentificationNumber = tenant.TaxIdentificationNumber,
            DateCreated = tenant.DateCreated,
        };

        private async Task<int> CountOwnersAsync(string orgId)
        {
            int count = 0;
            var members = await _context.Users.IgnoreQueryFilters()
                .Where(u => u.TenantId == orgId && (u.IsDeleted == null || u.IsDeleted == false))
                .ToListAsync();
            foreach (var m in members)
                if (await _userManager.IsInRoleAsync(m, UserRoles.Owner)) count++;
            return count;
        }

        private async Task EnsureRoleAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        private async Task StripOrgRolesAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var orgRoles = roles.Where(UserRoles.IsOrgRole).ToList();
            if (orgRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, orgRoles);
        }

        private async Task<OrgContentCounts> CountAllOrgContentAsync(string orgId)
        {
            return new OrgContentCounts
            {
                Songs = await _context.Songs.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && (x.IsDeleted == null || x.IsDeleted == false)),
                Playlists = await _context.Playlists.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && (x.IsDeleted == null || x.IsDeleted == false)),
                SongBooks = await _context.SongBooks.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && (x.IsDeleted == null || x.IsDeleted == false)),
                Albums = await _context.Albums.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && (x.IsDeleted == null || x.IsDeleted == false)),
            };
        }

        private async Task<OrgContentCounts> CountUserOrgContentAsync(string userId, string orgId)
        {
            return new OrgContentCounts
            {
                Songs = await _context.Songs.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && x.CreatedBy == userId && (x.IsDeleted == null || x.IsDeleted == false)),
                Playlists = await _context.Playlists.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && x.CreatedBy == userId && (x.IsDeleted == null || x.IsDeleted == false)),
                SongBooks = await _context.SongBooks.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && x.CreatedBy == userId && (x.IsDeleted == null || x.IsDeleted == false)),
                Albums = await _context.Albums.IgnoreQueryFilters()
                    .CountAsync(x => x.TenantId == orgId && x.CreatedBy == userId && (x.IsDeleted == null || x.IsDeleted == false)),
            };
        }

        private static string GenerateTemporaryPassword()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#";
            var bytes = new byte[12];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            return new string(chars);
        }

        private async Task SendCredentialsEmailAsync(User user, string tempPassword)
        {
            try
            {
                var email = new EmailDto(isHtml: true)
                {
                    ToEmail = user.Email,
                    Subject = "Your FRELODY account credentials",
                    Body =
                        $"<p>Hi {user.FirstName},</p>" +
                        $"<p>An organization admin has created an account for you on FRELODY.</p>" +
                        $"<ul>" +
                        $"<li><strong>Username:</strong> {user.UserName}</li>" +
                        $"<li><strong>Temporary password:</strong> {tempPassword}</li>" +
                        $"</ul>" +
                        $"<p>Please sign in and change your password immediately.</p>"
                };
                await _emailService.SendMailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send credentials email to {Email}", user.Email);
            }
        }
    }
}
