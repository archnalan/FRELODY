using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.EntityFrameworkCore;

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

        #region Search Users for combo boxes
        public async Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchUsersForComboBoxes(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            IQueryable<User> query = _context.Users;
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
                    Id = 0, // Assuming ComboBoxDto.Id is int and not used for users
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
                var query = _context.Users.AsNoTracking();
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
        public async Task<ServiceResult<PaginationDetails<AppUserDto>>> GetAllUsers(int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            IQueryable<User> query = _context.Users;
            try
            {
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
                    CoverPhotoUrl = x.CoverPhotoUrl
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

        #region Search For Users
        public async Task<ServiceResult<PaginationDetails<AppUserDto>>> SearchForUsers(string keywords, int offSet, int limit, string sortByColumn, bool sortAscending, CancellationToken cancellationToken)
        {
            IQueryable<User> query = _context.Users;
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
                    CoverPhotoUrl = x.CoverPhotoUrl
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
    }
}
