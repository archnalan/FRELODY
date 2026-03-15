namespace FRELODYUI.Shared.Services
{
    public interface ICameraService
    {
        /// <summary>
        /// Opens the device camera to capture a photo.
        /// Returns the image bytes and file name, or null if cancelled.
        /// </summary>
        Task<CameraResult?> CapturePhotoAsync();

        /// <summary>
        /// Opens the device gallery/photo picker to select an image.
        /// Returns the image bytes and file name, or null if cancelled.
        /// </summary>
        Task<CameraResult?> PickPhotoFromGalleryAsync();

        /// <summary>
        /// Whether camera capture is available on the current platform.
        /// </summary>
        bool IsCameraAvailable { get; }
    }

    public class CameraResult
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
