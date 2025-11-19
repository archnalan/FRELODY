using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISettingsApi
    {
        [Get("/api/settings/get-all-settings")]
        Task<IApiResponse<List<SettingDto>>> GetAllSettings([Query] string? userId = null);

        [Get("/api/settings/get-setting-by-id")]
        Task<IApiResponse<SettingDto>> GetSettingById([Query] string settingId);

        [Get("/api/settings/get-user-settings")]
        Task<IApiResponse<SettingDto>> GetUserSettings([Query] string? userId = null);

        [Post("/api/settings/create-setting")]
        Task<IApiResponse<SettingDto>> CreateSetting([Body] SettingDto settingDto);

        [Put("/api/settings/update-setting")]
        Task<IApiResponse<SettingDto>> UpdateSetting([Body] SettingDto settingDto);

        [Post("/api/settings/create-or-update-user-settings")]
        Task<IApiResponse<SettingDto>> CreateOrUpdateUserSettings([Body] SettingDto settingDto);
        
        [Post("/api/settings/toggle-notifications")]
        Task<IApiResponse<SettingDto>> ToggleNotifications([Query] string userId, [Query] bool enable);

        [Delete("/api/settings/delete-setting")]
        Task<IApiResponse<bool>> DeleteSetting([Query] string settingId);
    }
}