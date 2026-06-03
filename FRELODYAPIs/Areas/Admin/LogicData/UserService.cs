using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.Dtos.UserDtos;
using FRELODYSHRD.ModelTypes;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class UserService : IUserService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(SongDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get UserProfile 
        public async Task<ServiceResult<UpdateUserProfileOutDto>> GetUserProfile(string userId)
        {
            try
            {
                var user = await _context.Users.AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == userId && (u.IsDeleted == false || u.IsDeleted == null))
                    .Select(x => new UpdateUserProfileOutDto
                    {
                        Id = x.Id,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        AboutMe = x.AboutMe,
                        Address = x.Address,
                        CoverPhotoUrl = x.CoverPhotoUrl,
                        Email = x.Email,
                        PhoneNumber = x.PhoneNumber,
                        ProfilePicUrl = x.ProfilePicUrl,
                        UserName = x.UserName,
                        TenantId = x.TenantId
                    })
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    return ServiceResult<UpdateUserProfileOutDto>.Failure(new Exception("User not found."));
                }
                var totalSongs = await _context.Songs.CountAsync(s => s.CreatedBy == userId);
                user.TotalSongs = totalSongs;
                return ServiceResult<UpdateUserProfileOutDto>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while retrieving user profile: {ex}", ex);
                return ServiceResult<UpdateUserProfileOutDto>.Failure(new Exception("Could not retrieve user profile."));
            }
        }
        #endregion

        #region Edit User Profile
        public async Task<ServiceResult<UpdateUserProfileOutDto>> EditUserProfile(UpdateUserProfile dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(dto.Id);
                if (user == null)
                {
                    return ServiceResult<UpdateUserProfileOutDto>.Failure(new Exception("User not found."));
                }
                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.Address = dto.Address;
                user.AboutMe = dto.AboutMe;
                user.PhoneNumber = dto.PhoneNumber;
                user.ProfilePicUrl = dto.ProfilePicUrl;
                user.CoverPhotoUrl = dto.CoverPhotoUrl;
                user.TenantId = dto.TenantId;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var outDto = dto.Adapt<UpdateUserProfileOutDto>();
                return ServiceResult<UpdateUserProfileOutDto>.Success(outDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while editing user profile: {ex}", ex);
                return ServiceResult<UpdateUserProfileOutDto>.Failure(new Exception("Could not edit user profile."));
            }
        }
        #endregion

        #region Search Users for combo boxes
        public async Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchUsersForComboBoxes(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            IQueryable<User> query = _context.Users.Where(u => u.UserType != UserType.SuperAdmin || u.UserType!=UserType.TenantAdmin);
                                        
            try
            {
                if (!string.IsNullOrEmpty(keywords))
                {
                    query = query.Where(x =>
                        x.FirstName != null && x.FirstName.Contains(keywords) ||
                        x.LastName != null && x.LastName.Contains(keywords) ||
                        x.UserName != null && x.UserName.Contains(keywords) ||
                        x.Email != null && x.Email.Contains(keywords) ||
                        x.Address != null && x.Address.Contains(keywords) ||
                        x.AboutMe != null && x.AboutMe.Contains(keywords) ||
                        x.Contact != null && x.Contact.Contains(keywords)
                    );
                }

                var result = await query.AsNoTracking().Select(x => new ComboBoxDto
                {
                    ValueId = 0, // Assuming ComboBoxDto.Id is int and not used for users
                    IdString = x.Id,
                    ValueText = $"{x.FirstName} {x.LastName}"
                }).ToPaginatedResultAsync(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<ComboBoxDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while searching Users: {ex}", ex);
                return ServiceResult<PaginationDetails<ComboBoxDto>>.Failure(new Exception("Could not search for users."));
            }
        }
        #endregion

        #region search Users from Database based On Keywords
        public async Task<ServiceResult<PaginationDetails<CreateUserResponseDto>>> SearchUserByKeywords(string keywords, int offSet, int limit, CancellationToken cancellationToken, string sortByColumn, bool sortAscending)
        {
            try
            {
                var query = _context.Users.AsNoTracking().Where(u => u.UserType != UserType.SuperAdmin || u.UserType != UserType.TenantAdmin);
                if (!string.IsNullOrEmpty(keywords))
                {
                    query = query.Where(c =>
                        c.FirstName != null && c.FirstName.Contains(keywords) ||
                        c.LastName != null && c.LastName.Contains(keywords) ||
                        c.Email != null && c.Email.Contains(keywords)
                    );
                }

                var users = await query.Select(x => new CreateUserResponseDto
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    AboutMe = x.AboutMe,
                    Address = x.Address,
                    CoverPhotoUrl = x.CoverPhotoUrl,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    ProfilePicUrl = x.ProfilePicUrl,
                    UserName = x.UserName
                }).ToPaginatedResultAsync<CreateUserResponseDto>(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<CreateUserResponseDto>>.Success(users);
            }
            catch (Exception ex)
            {
                _logger.LogError("Customer matching keywords: {keywords} could not found.{Error}", keywords, ex);
                return ServiceResult<PaginationDetails<CreateUserResponseDto>>.Failure(
                    new Exception($"Error while searching user. Please contact Admin"));
            }
        }
        #endregion

        #region Get All Users
        public async Task<ServiceResult<PaginationDetails<AppUserDto>>> GetAllUsers(int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken, UserAccountFilter filter = UserAccountFilter.Active)
        {
            try
            {
                // IgnoreQueryFilters so blocked/deleted rows are visible; we apply
                // explicit filter predicates below instead.
                IQueryable<User> query = _context.Users.IgnoreQueryFilters()
                    .Where(u => u.UserType != UserType.SuperAdmin && u.UserType != UserType.TenantAdmin);

                query = filter switch
                {
                    UserAccountFilter.Active  => query.Where(u => (u.IsActive == true || u.IsActive == null) && (u.IsDeleted == false || u.IsDeleted == null)),
                    UserAccountFilter.Blocked => query.Where(u => u.IsActive == false && (u.IsDeleted == false || u.IsDeleted == null)),
                    UserAccountFilter.Deleted => query.Where(u => u.IsDeleted == true),
                    _ => query.Where(u => (u.IsActive == true || u.IsActive == null) && (u.IsDeleted == false || u.IsDeleted == null))
                };

                var result = await query.AsNoTracking().Select(x => new AppUserDto
                {
                    UserId = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    UserName = x.UserName,
                    Address = x.Address,
                    Aboutme = x.AboutMe,
                    Contacts = x.Contact,
                    ProfilePicUrl = x.ProfilePicUrl,
                    CoverPhotoUrl = x.CoverPhotoUrl,
                    TenantId = x.TenantId,
                    Email = x.Email,
                    DateCreated = x.DateCreated,
                    UserType = x.UserType,
                    IsActive = x.IsActive,
                    IsDeleted = x.IsDeleted,
                    BillingStatus = x.BillingStatus,
                    BillingExpiresAt = x.BillingExpiresAt,
                    LastLoginDate = x.LastLoginDate,
                }).ToPaginatedResultAsync(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<AppUserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while getting all Users: {ex}", ex);
                return ServiceResult<PaginationDetails<AppUserDto>>.Failure(new Exception(ex.Message));
            }
        }
        #endregion

        #region Update User Phone Number
        public async Task<ServiceResult<bool>> UpdateUserPhoneNumberAsync(string userId, [Phone]string newPhoneNumber)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure(new Exception("User not found."));
                }
                user.PhoneNumber = newPhoneNumber;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while updating user phone number: {ex}", ex);
                return ServiceResult<bool>.Failure(new Exception("Could not update phone number."));
            }
        }
        #endregion

        #region Search For Users
        public async Task<ServiceResult<PaginationDetails<AppUserDto>>> SearchForUsers(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken, UserAccountFilter filter = UserAccountFilter.Active)
        {
            try
            {
                // IgnoreQueryFilters so blocked/deleted rows are visible; explicit predicates below.
                IQueryable<User> query = _context.Users.IgnoreQueryFilters()
                    .Where(u => u.UserType != UserType.SuperAdmin && u.UserType != UserType.TenantAdmin);

                query = filter switch
                {
                    UserAccountFilter.Active  => query.Where(u => (u.IsActive == true || u.IsActive == null) && (u.IsDeleted == false || u.IsDeleted == null)),
                    UserAccountFilter.Blocked => query.Where(u => u.IsActive == false && (u.IsDeleted == false || u.IsDeleted == null)),
                    UserAccountFilter.Deleted => query.Where(u => u.IsDeleted == true),
                    _ => query.Where(u => (u.IsActive == true || u.IsActive == null) && (u.IsDeleted == false || u.IsDeleted == null))
                };

                if (!string.IsNullOrEmpty(keywords))
                {
                    query = query.Where(x =>
                        x.FirstName != null && x.FirstName.Contains(keywords) ||
                        x.LastName != null && x.LastName.Contains(keywords) ||
                        x.UserName != null && x.UserName.Contains(keywords) ||
                        x.Email != null && x.Email.Contains(keywords) ||
                        x.Address != null && x.Address.Contains(keywords) ||
                        x.AboutMe != null && x.AboutMe.Contains(keywords) ||
                        x.Contact != null && x.Contact.Contains(keywords)
                    );
                }

                var result = await query.AsNoTracking().Select(x => new AppUserDto
                {
                    UserId = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    UserName = x.UserName,
                    Address = x.Address,
                    Aboutme = x.AboutMe,
                    Contacts = x.Contact,
                    ProfilePicUrl = x.ProfilePicUrl,
                    CoverPhotoUrl = x.CoverPhotoUrl,
                    TenantId = x.TenantId,
                    Email = x.Email,
                    DateCreated = x.DateCreated,
                    UserType = x.UserType,
                    IsActive = x.IsActive,
                    IsDeleted = x.IsDeleted,
                    BillingStatus = x.BillingStatus,
                    BillingExpiresAt = x.BillingExpiresAt,
                    LastLoginDate = x.LastLoginDate,
                }).ToPaginatedResultAsync(offSet, limit, cancellationToken, sortByColumn, sortAscending);

                return ServiceResult<PaginationDetails<AppUserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while searching Users: {ex}", ex);
                return ServiceResult<PaginationDetails<AppUserDto>>.Failure(new Exception(ex.Message));
            }
        }
        #endregion

        #region Get Signup Stats
        public async Task<ServiceResult<UserSignupStatsDto>> GetSignupStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            try
            {
                var baseQuery = _context.Users
                    .Where(u => u.UserType != UserType.SuperAdmin && u.UserType != UserType.TenantAdmin);

                var totalUsers = await baseQuery.CountAsync(cancellationToken);

                // Pull all DateCreated values up to `to` into memory for grouping
                var allDates = await baseQuery
                    .Where(u => u.DateCreated.HasValue && u.DateCreated.Value <= to)
                    .Select(u => u.DateCreated!.Value)
                    .ToListAsync(cancellationToken);

                // New signups in window grouped by day
                var newByDate = allDates
                    .Where(d => d >= from)
                    .GroupBy(d => d.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var newInWindow = newByDate.Values.Sum();

                // Baseline: count of users created before the window start
                var baseline = allDates.Count(d => d < from);

                // Build cumulative by walking each day in the window
                var cumulativeByDate = new Dictionary<DateTime, int>();
                var windowStartDate = from.Date;
                var windowEndDate = to.Date;
                var running = baseline;
                for (var day = windowStartDate; day <= windowEndDate; day = day.AddDays(1))
                {
                    if (newByDate.TryGetValue(day, out var dayCount))
                        running += dayCount;
                    cumulativeByDate[day] = running;
                }

                var dto = new UserSignupStatsDto
                {
                    NewByDate = newByDate,
                    CumulativeByDate = cumulativeByDate,
                    TotalUsers = totalUsers,
                    NewInWindow = newInWindow
                };

                return ServiceResult<UserSignupStatsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while retrieving signup stats: {ex}", ex);
                return ServiceResult<UserSignupStatsDto>.Failure(
                    new ServerErrorException("An error occurred while fetching signup stats."));
            }
        }
        #endregion

        #region Disable User
        public async Task<ServiceResult<bool>> DisableUser(string userId)
        {
            try
            {
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure(new Exception("User not found."));
                }

                if (user.UserType == UserType.SuperAdmin || user.UserType == UserType.TenantAdmin)
                {
                    return ServiceResult<bool>.Failure(new Exception("Cannot disable a system user."));
                }

                user.IsActive = false;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while disabling user: {ex}", ex);
                return ServiceResult<bool>.Failure(new Exception("Could not disable user."));
            }
        }
        #endregion

        #region Enable User
        public async Task<ServiceResult<bool>> EnableUser(string userId)
        {
            try
            {
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure(new Exception("User not found."));
                }

                if (user.UserType == UserType.SuperAdmin || user.UserType == UserType.TenantAdmin)
                {
                    return ServiceResult<bool>.Failure(new Exception("Cannot modify a system user."));
                }

                user.IsActive = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while enabling user: {ex}", ex);
                return ServiceResult<bool>.Failure(new Exception("Could not enable user."));
            }
        }
        #endregion

        #region Restore User (un-soft-delete)
        public async Task<ServiceResult<bool>> RestoreUser(string userId)
        {
            try
            {
                var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure(new Exception("User not found."));
                }

                if (user.UserType == UserType.SuperAdmin || user.UserType == UserType.TenantAdmin)
                {
                    return ServiceResult<bool>.Failure(new Exception("Cannot modify a system user."));
                }

                user.IsDeleted = false;
                user.IsActive = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while restoring user: {ex}", ex);
                return ServiceResult<bool>.Failure(new Exception("Could not restore user."));
            }
        }
        #endregion
    }
}
