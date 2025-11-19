using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.RefitApis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace FRELODYUI.Shared.Services
{
    public class UserSettingsService
    {
        private readonly ISettingsApi _settingsApi;
        private readonly ILogger<UserSettingsService> _logger;
        public SettingDto? CurrentSettings { get; private set; }

        public UserSettingsService(ISettingsApi api, ILogger<UserSettingsService> logger)
        {
            _settingsApi = api;
            _logger = logger;
        }

        public async Task<bool> LoadUserSettingsAsync(string userId)
        {
            try
            {
                var response = await _settingsApi.GetUserSettings(userId);
                if (response.IsSuccessStatusCode)
                {
                    CurrentSettings = response.Content;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user settings");
            }
            return false;
        }
        public async Task<bool> ReloadCurrentUserSettings(string userId) => await LoadUserSettingsAsync(userId);
    }
}
