using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Interfaces;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SettingsService : ISettingsService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(SongDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get all settings
        public async Task<ServiceResult<List<SettingDto>>> GetAllSettings(string? userId = null)
        {
            try
            {
                IQueryable<Setting> query = _context.Settings;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(s => s.CreatedBy == userId);
                }
                
                var settings = await query
                    .OrderBy(s => s.DateCreated)
                    .ToListAsync();

                var settingsDto = settings.Adapt<List<SettingDto>>();

                return ServiceResult<List<SettingDto>>.Success(settingsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving settings. {Error}", ex);
                return ServiceResult<List<SettingDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving settings."));
            }
        }
        #endregion

        #region Get setting by Id
        public async Task<ServiceResult<SettingDto>> GetSettingById(string settingId)
        {
            try
            {
                if (string.IsNullOrEmpty(settingId))
                {
                    return ServiceResult<SettingDto>.Failure(
                        new BadRequestException("Setting ID is required"));
                }

                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Id == settingId);

                if (setting == null)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new NotFoundException($"Setting not found. ID: {settingId}"));
                }

                var settingDto = setting.Adapt<SettingDto>();
                return ServiceResult<SettingDto>.Success(settingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving setting {SettingId}. {Error}", settingId, ex);
                return ServiceResult<SettingDto>.Failure(
                    new ServerErrorException("An error occurred while retrieving the setting."));
            }
        }
        #endregion

        #region Get user settings
        public async Task<ServiceResult<SettingDto>> GetUserSettings(string? userId = null)
        {
            try
            {
                Setting? setting = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    setting = await _context.Settings
                        .FirstOrDefaultAsync(s => s.CreatedBy == userId && s.IsDeleted != true);
                }

                if (setting == null)
                {
                    setting = await _context.Settings
                        .FirstOrDefaultAsync(s => s.IsDeleted != true);
                }

                if (setting == null)
                {
                    // Create default settings if none exist
                    var defaultSetting = new Setting
                    {
                        Id = Guid.NewGuid().ToString(),
                        ChordFont = "Arial",
                        LyricFont = "Arial",
                        ChordFontSize = "18px",
                        LyricFontSize = "16px",
                        SongDisplay = SongDisplay.LyricsAndChords,
                        Theme = Theme.System,
                        ChordDisplay = ChordDisplay.Above,
                        ChordDifficulty = ChordDifficulty.Easy,
                        PlayLevel = PlayLevel.Easy,
                        CreatedBy = userId,
                        DateCreated = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.Settings.Add(defaultSetting);
                    await _context.SaveChangesAsync();

                    var defaultSettingDto = defaultSetting.Adapt<SettingDto>();
                    return ServiceResult<SettingDto>.Success(defaultSettingDto);
                }

                var settingDto = setting.Adapt<SettingDto>();
                return ServiceResult<SettingDto>.Success(settingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving user settings for user {UserId}. {Error}", userId, ex);
                return ServiceResult<SettingDto>.Failure(
                    new ServerErrorException("An error occurred while retrieving user settings."));
            }
        }
        #endregion

        #region Create a setting
        public async Task<ServiceResult<SettingDto>> CreateSetting(SettingDto settingDto)
        {
            try
            {
                if (settingDto == null)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new BadRequestException("Setting data is required"));
                }

                var setting = settingDto.Adapt<Setting>();
                setting.Id = Guid.NewGuid().ToString();
                
                _context.Settings.Add(setting);
                await _context.SaveChangesAsync();
                
                var createdSettingDto = setting.Adapt<SettingDto>();
                return ServiceResult<SettingDto>.Success(createdSettingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating a setting for user {UserId}. {Error}", settingDto.CreatedBy, ex);
                return ServiceResult<SettingDto>.Failure(
                    new ServerErrorException("An error occurred while creating the setting."));
            }
        }
        #endregion

        #region Create or Update User Settings
        public async Task<ServiceResult<SettingDto>> CreateOrUpdateUserSettings(SettingDto settingDto)
        {
            try
            {
                if (settingDto == null)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new BadRequestException("Setting data is required"));
                }

                Setting? existingSetting = null;
                var userId = settingDto.CreatedBy;
                // Try to find existing user settings
                if (!string.IsNullOrEmpty(userId))
                {
                    existingSetting = await _context.Settings
                        .FirstOrDefaultAsync(s => s.CreatedBy == userId && s.IsDeleted != true);
                }

                if (existingSetting != null)
                {
                    // Update existing settings
                    existingSetting.ChordFont = settingDto.ChordFont;
                    existingSetting.LyricFont = settingDto.LyricFont;
                    existingSetting.ChordFontSize = settingDto.ChordFontSize;
                    existingSetting.LyricFontSize = settingDto.LyricFontSize;
                    existingSetting.SongDisplay = settingDto.SongDisplay;
                    existingSetting.Theme = settingDto.Theme;
                    existingSetting.ChordDisplay = settingDto.ChordDisplay;
                    existingSetting.ChordDifficulty = settingDto.ChordDifficulty;
                    existingSetting.PlayLevel = settingDto.PlayLevel;
                    existingSetting.DateModified = DateTime.UtcNow;
                    existingSetting.ModifiedBy = userId;

                    _context.Settings.Update(existingSetting);
                    await _context.SaveChangesAsync();

                    var updatedSettingDto = existingSetting.Adapt<SettingDto>();
                    return ServiceResult<SettingDto>.Success(updatedSettingDto);
                }
                else
                {
                    // Create new settings for the user
                    return await CreateSetting(settingDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating or updating user settings for user {UserId}. {Error}", settingDto.CreatedBy, ex);
                return ServiceResult<SettingDto>.Failure(
                    new ServerErrorException("An error occurred while saving user settings."));
            }
        }
        #endregion

        #region Update a setting
        public async Task<ServiceResult<SettingDto>> UpdateSetting(string settingId, SettingDto settingDto)
        {
            try
            {
                if (string.IsNullOrEmpty(settingId) || settingDto == null)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new BadRequestException("Setting ID and data are required"));
                }

                var existingSetting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Id == settingId && s.IsDeleted != true);
                
                if (existingSetting == null)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new NotFoundException($"Setting not found. ID: {settingId}"));
                }

                var userId = settingDto.CreatedBy;
                if (!string.IsNullOrEmpty(userId) && existingSetting.CreatedBy != userId)
                {
                    return ServiceResult<SettingDto>.Failure(
                        new UnauthorizedAccessException("You don't have permission to update this setting."));
                }

                existingSetting.ChordFont = settingDto.ChordFont;
                existingSetting.LyricFont = settingDto.LyricFont;
                existingSetting.ChordFontSize = settingDto.ChordFontSize;
                existingSetting.LyricFontSize = settingDto.LyricFontSize;
                existingSetting.SongDisplay = settingDto.SongDisplay;
                existingSetting.Theme = settingDto.Theme;
                existingSetting.ChordDisplay = settingDto.ChordDisplay;
                existingSetting.ChordDifficulty = settingDto.ChordDifficulty;
                existingSetting.PlayLevel = settingDto.PlayLevel;
                existingSetting.DateModified = DateTime.UtcNow;
                existingSetting.ModifiedBy = settingDto.ModifiedBy;

                _context.Settings.Update(existingSetting);
                await _context.SaveChangesAsync();

                var updatedSettingDto = existingSetting.Adapt<SettingDto>();
                return ServiceResult<SettingDto>.Success(updatedSettingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while updating setting {SettingId} for user {UserId}. {Error}", settingId, settingDto.CreatedBy, ex);
                return ServiceResult<SettingDto>.Failure(
                    new ServerErrorException("An error occurred while updating the setting."));
            }
        }
        #endregion

        #region Delete a setting
        public async Task<ServiceResult<bool>> DeleteSetting(string settingId)
        {
            try
            {
                if (string.IsNullOrEmpty(settingId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Setting ID is required"));
                }

                var existingSetting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Id == settingId && s.IsDeleted != true);
                
                if (existingSetting == null)
                {
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Setting not found. ID: {settingId}"));
                }

                // Soft delete by setting IsDeleted to true
                existingSetting.IsDeleted = true;
                existingSetting.DateModified = DateTime.UtcNow;
                
                _context.Settings.Update(existingSetting);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting setting {SettingId}. {Error}", settingId, ex);
                return ServiceResult<bool>.Failure(
                    new ServerErrorException("An error occurred while deleting the setting."));
            }
        }
        #endregion
    }
}