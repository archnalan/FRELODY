using FRELODYAPP.Data;
using FRELODYSHRD.Dtos.UploadDtos;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]/[action]")]
[ApiController]
public class FileUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _config;
    private readonly FileValidationService _fileValidationService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IWebHostEnvironment webHostEnvironment,
        IConfiguration config,
        FileValidationService fileValidationService,
        ILogger<FileUploadController> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _config = config;
        _fileValidationService = fileValidationService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileUploadResult), 200)]
    public async Task<IActionResult> UploadChartImage(IFormFile file)
    {
        return await UploadFile(file, "charts/images");
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileUploadResult), 200)]
    public async Task<IActionResult> UploadChartAudio(IFormFile file)
    {
        return await UploadFile(file, "charts/audio");
    }

    private async Task<IActionResult> UploadFile(IFormFile file, string subfolder)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file was provided or file is empty" });

            // Validate file
            var validationResult = await _fileValidationService.ValidateFileAsync(file);
            if (!validationResult.IsValid)
                return BadRequest(new { message = validationResult.ErrorMessage });

            // Save file
            var saveResult = await SaveFile(file, subfolder);
            if (saveResult.ReturnCode == "200")
            {
                return Ok(new FileUploadResult
                {
                    FilePath = saveResult.Link,
                    OriginalFileName = file.FileName,
                    Size = file.Length,
                    Success = true
                });
            }

            return BadRequest(new { message = saveResult.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for file: {FileName}", file?.FileName);
            return StatusCode(500, new { message = "File upload failed" });
        }
    }

    private async Task<ResultObject> SaveFile(IFormFile file, string subfolder)
    {
        string webRootPath = _webHostEnvironment.WebRootPath;
        var obj = new ResultObject();

        try
        {
            // Get storage path from configuration
            string storagePath = Path.Combine("media", subfolder);
            string uploadPath = Path.Combine(webRootPath, storagePath);

            // Ensure directory exists
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Generate unique filename
            string extension = Path.GetExtension(file.FileName);
            string fileNewName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadPath, fileNewName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Generate web-friendly URL
            string link = $"{storagePath}/{fileNewName}".Replace('\\', '/');

            _logger.LogInformation("File uploaded successfully: {FileName}", file.FileName);

            obj.ReturnCode = "200";
            obj.Message = "Upload successful";
            obj.Link = link;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File save failed: {FileName}", file?.FileName);
            obj.ReturnCode = "500";
            obj.Message = $"File save failed: {ex.Message}";
        }

        return obj;
    }
}