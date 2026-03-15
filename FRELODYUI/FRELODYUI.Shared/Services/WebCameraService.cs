using FRELODYUI.Shared.Services;
using Microsoft.JSInterop;

namespace FRELODYUI.Shared.Services
{
    public class WebCameraService : ICameraService
    {
        private readonly IJSRuntime _jsRuntime;

        public WebCameraService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // Web browsers generally support camera via file input
        public bool IsCameraAvailable => true;

        public async Task<CameraResult?> CapturePhotoAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<CameraJsResult?>("cameraInterop.capturePhoto");
                if (result == null || string.IsNullOrEmpty(result.Base64))
                    return null;

                return new CameraResult
                {
                    ImageData = Convert.FromBase64String(result.Base64),
                    FileName = result.FileName ?? "captured_photo.jpg"
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<CameraResult?> PickPhotoFromGalleryAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<CameraJsResult?>("cameraInterop.pickPhoto");
                if (result == null || string.IsNullOrEmpty(result.Base64))
                    return null;

                return new CameraResult
                {
                    ImageData = Convert.FromBase64String(result.Base64),
                    FileName = result.FileName ?? "selected_photo.jpg"
                };
            }
            catch
            {
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
