using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FRELODYUI.Services
{
    public class MauiCameraService : ICameraService
    {
        private readonly ILogger<MauiCameraService> _logger;

        public MauiCameraService(ILogger<MauiCameraService> logger)
        {
            _logger = logger;
        }

        public bool IsCameraAvailable => MediaPicker.Default.IsCaptureSupported;

        public async Task<CameraResult?> CapturePhotoAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        _logger.LogWarning("Camera permission denied");
                        return null;
                    }
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return null;

                return await ReadFileResult(photo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo");
                return null;
            }
        }

        public async Task<CameraResult?> PickPhotoFromGalleryAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return null;

                return await ReadFileResult(photo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error picking photo from gallery");
                return null;
            }
        }

        private static async Task<CameraResult> ReadFileResult(FileResult fileResult)
        {
            using var stream = await fileResult.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            return new CameraResult
            {
                ImageData = ms.ToArray(),
                FileName = fileResult.FileName
            };
        }
    }
}
