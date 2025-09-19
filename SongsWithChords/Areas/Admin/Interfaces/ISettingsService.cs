using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISettingsService
    {
        Task<ServiceResult<SettingDto>> CreateSetting(SettingDto settingDto);
        Task<ServiceResult<List<SettingDto>>> GetAllSettings(string? userId = null);
        Task<ServiceResult<SettingDto>> GetSettingById(string settingId);
        Task<ServiceResult<SettingDto>> GetUserSettings(string? userId = null);
        Task<ServiceResult<SettingDto>> UpdateSetting(string settingId, SettingDto settingDto);
        Task<ServiceResult<SettingDto>> CreateOrUpdateUserSettings(SettingDto settingDto);
        Task<ServiceResult<bool>> DeleteSetting(string settingId);
    }
}