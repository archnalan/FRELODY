using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SongsWithChords.Data
{
    public class FileValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileValidationService> _logger;

        public FileValidationService(IConfiguration configuration, ILogger<FileValidationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateFileAsync(IFormFile file)
        {
            if (file == null)
            {
                return (false, "No file was provided.");
            }

            // Check file size
            double maxSizeMB = _configuration.GetValue<double>("FileUploads:MaxSizeMB");
            long maxSizeBytes = (long)(maxSizeMB * 1024 * 1024);

            if (file.Length > maxSizeBytes)
            {
                return (false, $"File size exceeds the maximum allowed size of {maxSizeMB}MB.");
            }

            // Check file extension
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExtensions = _configuration.GetSection("FileUploads:AllowedImageExtensions").Get<string[]>();

            if (allowedExtensions == null || !allowedExtensions.Contains(extension))
            {
                return (false, $"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions ?? new[] { ".jpg", ".jpeg", ".png" })}.");
            }

            // Check file content (simple MIME type validation)
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var fileType = GetFileType(stream);
                if (!IsValidImageType(fileType))
                {
                    return (false, "File content does not match an allowed image type.");
                }
            }

            return (true, null);
        }

        private string GetFileType(Stream stream)
        {
            // Simple file signature check
            var buffer = new byte[8];
            stream.Read(buffer, 0, buffer.Length);
            stream.Position = 0;

            // Check file signatures (magic numbers)
            if (buffer[0] == 0xFF && buffer[1] == 0xD8) // JPEG
            {
                return "image/jpeg";
            }
            else if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) // PNG
            {
                return "image/png";
            }

            return "unknown";
        }

        private bool IsValidImageType(string fileType)
        {
            return fileType == "image/jpeg" || fileType == "image/png";
        }

        public string GetSafeFilename(string filename)
        {
            // Remove any invalid characters from filename
            return string.Join("", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}