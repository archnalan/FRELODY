using Microsoft.JSInterop;

namespace FRELODYUI.Shared.Services
{
    public class DeviceService
    {
        private readonly IStorageService _storage;
        private readonly IJSRuntime _js;
        private const string DeviceIdKey = "frelody_device_id";

        public DeviceService(IStorageService storage, IJSRuntime js)
        {
            _storage = storage;
            _js = js;
        }

        public async Task<string> GetOrCreateDeviceIdAsync()
        {
            try
            {
                var existing = await _storage.GetItemAsync<string>(DeviceIdKey);
                if (!string.IsNullOrEmpty(existing))
                    return existing;

                var newId = Guid.NewGuid().ToString("N");
                await _storage.SetItemAsync(DeviceIdKey, newId);
                return newId;
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        public async Task<string> GetDeviceNameAsync()
        {
            try
            {
                return await _js.InvokeAsync<string>("frelodyGetDeviceName");
            }
            catch
            {
                return "Unknown device";
            }
        }
    }
}
