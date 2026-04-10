using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FRELODYUI.Shared.Services
{
    public class WebCameraService : ICameraService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<WebCameraService> _logger;

        public WebCameraService(IJSRuntime jsRuntime, ILogger<WebCameraService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        // Web browsers generally support camera via file input
        public bool IsCameraAvailable => true;

        public async Task<CameraResult?> CapturePhotoAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<CameraJsResult?>("cameraInterop.capturePhoto", TimeSpan.FromMinutes(10));
                if (result == null || string.IsNullOrEmpty(result.Base64))
                    return null;

                return new CameraResult
                {
                    ImageData = Convert.FromBase64String(result.Base64),
                    FileName = result.FileName ?? "captured_photo.jpg"
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo from camera.");
                return null;
            }
        }

        public async Task<CameraResult?> PickPhotoFromGalleryAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<CameraJsResult?>("cameraInterop.pickPhoto", TimeSpan.FromMinutes(10));
                if (result == null || string.IsNullOrEmpty(result.Base64))
                    return null;

                return new CameraResult
                {
                    ImageData = Convert.FromBase64String(result.Base64),
                    FileName = result.FileName ?? "selected_photo.jpg"
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error picking photo from gallery.");
                return null;
            }
        }

        private class CameraJsResult
        {
            public string? Base64 { get; set; }
            public string? FileName { get; set; }
        }
    }
}
