using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SettingDto>), 200)]
        public async Task<IActionResult> GetAllSettings([FromQuery]string? userId = null)
        {
            var result = await _settingsService.GetAllSettings(userId);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SettingDto), 200)]
        public async Task<IActionResult> GetSettingById([FromQuery] string settingId)
        {
            var result = await _settingsService.GetSettingById(settingId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SettingDto), 200)]
        public async Task<IActionResult> GetUserSettings([FromQuery] string? userId = null)
        {
            var result = await _settingsService.GetUserSettings(userId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SettingDto), 200)]
        public async Task<IActionResult> CreateSetting([FromBody] SettingDto settingDto)
        {
            var result = await _settingsService.CreateSetting(settingDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(SettingDto), 200)]
        public async Task<IActionResult> UpdateSetting([FromBody] SettingDto settingDto)
        {
            var result = await _settingsService.UpdateSetting(settingDto.Id, settingDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SettingDto), 200)] 
        public async Task<IActionResult> CreateOrUpdateUserSettings([FromBody] SettingDto settingDto)
        {
            var result = await _settingsService.CreateOrUpdateUserSettings(settingDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
                
        [HttpPost]
        [ProducesResponseType(typeof(SettingDto), 200)]
        public async Task<IActionResult> ToggleNotifications([FromQuery] string userId, [FromQuery] bool enable)
        {
            var result = await _settingsService.ToggleNotifications(userId, enable);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteSetting([FromQuery] string settingId)
        {
            var result = await _settingsService.DeleteSetting(settingId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}