using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Data;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class ChordChartService : IChordChartService
    {
        private readonly SongDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly FileValidationService _fileValidationService;
        private readonly IChordService _chordService;
        private readonly ILogger<ChordChartService> _logger;

        public ChordChartService(SongDbContext context, ILogger<ChordChartService> logger, IWebHostEnvironment webHostEnvironment, FileValidationService fileValidationService, IChordService chordService)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _fileValidationService = fileValidationService;
            _chordService = chordService;
        }

        public async Task<ServiceResult<List<ChordChartEditDto>>> GetChordChartsAsync()
        {
            try
            {
                var charts = await _context.ChordCharts
                    .OrderBy(c => c.ChordId)
                    .ToListAsync();

                var chartsDto = charts.Adapt<List<ChordChartEditDto>>();
                return ServiceResult<List<ChordChartEditDto>>.Success(chartsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chord charts: {Error}", ex);
                return ServiceResult<List<ChordChartEditDto>>.Failure(
                    new Exception($"Error retrieving chord charts. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<ChordChartEditDto>> GetChordChartByIdAsync(string id)
        {
            try
            {
                var chart = await _context.ChordCharts.FindAsync(id);

                if (chart == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new NotFoundException($"Chord chart with ID: {id} does not exist."));

                var chartDto = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(chartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chord chart by ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<ChordChartEditDto>.Failure(
                    new Exception($"Error retrieving chord chart with ID: {id}. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<List<ChordChartEditDto>>> GetChartsByChordIdAsync(string chordId)
        {
            try
            {
                var charts = await _context.ChordCharts
                    .Where(c => c.ChordId == chordId)
                    .OrderBy(c => c.FretPosition)
                    .ToListAsync();
                var chartsDto = charts.Adapt<List<ChordChartEditDto>>();
                return ServiceResult<List<ChordChartEditDto>>.Success(chartsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chord charts by Chord ID: {ChordId}. Error: {Error}", chordId, ex);
                return ServiceResult<List<ChordChartEditDto>>.Failure(
                    new Exception($"Error retrieving chord charts by Chord ID: {chordId}. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<ChartWithParentChordDto>> GetChartWithParentChordByIdAsync(string id)
        {
            try
            {
                var chart = await _context.ChordCharts
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (chart == null)
                    return ServiceResult<ChartWithParentChordDto>.Failure(
                        new NotFoundException($"Chord chart with ID: {id} does not exist."));

                var chartDto = chart.Adapt<ChartWithParentChordDto>();
                return ServiceResult<ChartWithParentChordDto>.Success(chartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chart with parent chord by ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<ChartWithParentChordDto>.Failure(
                    new Exception($"Error retrieving chart with parent chord by ID: {id}. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<ChordChartEditDto>> CreateChordChartAsync(ChordChartCreateDto chartDto)
        {
            try
            {
                if (chartDto == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chord chart data is required."));

                var chartExists = await _context.ChordCharts
                    .AnyAsync(ch => ch.FilePath == chartDto.FilePath);

                if (chartExists)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new ConflictException($"Chart with file path: {chartDto.FilePath} already exists."));

                if (!string.IsNullOrEmpty(chartDto.ChordId))
                {
                    var chordExists = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);
                    if (!chordExists)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Chord with ID: {chartDto.ChordId} does not exist."));
                }

                var chart = chartDto.Adapt<ChordChart>();

                await _context.ChordCharts.AddAsync(chart);
                await _context.SaveChangesAsync();

                var newChartDto = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(newChartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chord chart: {FilePath}. Error: {Error}", chartDto?.FilePath, ex);
                return ServiceResult<ChordChartEditDto>.Failure(
                    new Exception($"Error creating chord chart. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<ChordChartEditDto>> UpdateChordChartAsync(ChordChartEditDto chartDto)
        {
            if (chartDto == null)
                return ServiceResult<ChordChartEditDto>.Failure(
                    new BadRequestException("Chord chart data is required."));

            var chart = await _context.ChordCharts.FindAsync(chartDto.Id);
            if (chart == null)
                return ServiceResult<ChordChartEditDto>.Failure(
                    new NotFoundException($"Chord chart with ID: {chartDto.Id} does not exist."));

            var chartExists = await _context.ChordCharts
                .AnyAsync(ch => ch.FilePath == chartDto.FilePath && ch.Id != chartDto.Id);

            if (chartExists)
                return ServiceResult<ChordChartEditDto>.Failure(
                    new ConflictException($"Chart: {chartDto.FilePath} already exists."));

            try
            {
                chart.FilePath = chartDto.FilePath;
                chart.ChordId = chartDto.ChordId;
                chart.FretPosition = chartDto.FretPosition;
                chart.ChartAudioFilePath = chartDto.ChartAudioFilePath;
                chart.PositionDescription = chartDto.PositionDescription;

                await _context.SaveChangesAsync();

                var updatedChart = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(updatedChart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chord chart: {FilePath}. Error: {Error}", chartDto.FilePath, ex);
                return ServiceResult<ChordChartEditDto>.Failure(new Exception(ex.Message));
            }
        }

        public async Task<ServiceResult<bool>> DeleteChordChartAsync(string id)
        {
            var chart = await _context.ChordCharts.FindAsync(id);
            if (chart == null)
                return ServiceResult<bool>.Failure(
                    new NotFoundException($"Chord chart with ID: {id} does not exist."));

            try
            {
                chart.IsDeleted = true;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chord chart with ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<bool>.Failure(new Exception(ex.Message));
            }
        }

        public async Task<ServiceResult<ChordChartEditDto>> CreateChordChartFilesAsync(
        ChordChartCreateDto chartDto,
        IFormFile chartImage,
        IFormFile? chartAudio)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate inputs
                if (chartDto == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chord chart data is required."));

                if (chartImage == null || chartImage.Length == 0)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chart image is required."));

                // Validate files
                var imageValidation = await _fileValidationService.ValidateFileAsync(chartImage);
                if (!imageValidation.IsValid)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException($"Image validation failed: {imageValidation.ErrorMessage}"));

                if (chartAudio != null)
                {
                    var audioValidation = await _fileValidationService.ValidateFileAsync(chartAudio);
                    if (!audioValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Audio validation failed: {audioValidation.ErrorMessage}"));
                }

                // Validate chord exists
                if (!string.IsNullOrEmpty(chartDto.ChordId))
                {
                    var chordExists = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);
                    if (!chordExists)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Chord with ID: {chartDto.ChordId} does not exist."));
                }

                // Generate file paths and save files
                var imagePath = await SaveChartFile(chartImage, "charts/images", chartDto);
                string? audioPath = null;

                if (chartAudio != null)
                {
                    audioPath = await SaveChartFile(chartAudio, "charts/audio", chartDto);
                }

                // Set file paths in DTO
                chartDto.FilePath = imagePath;
                chartDto.ChartAudioFilePath = audioPath;

                // Check for duplicate file path
                var chartExists = await _context.ChordCharts
                    .AnyAsync(ch => ch.FilePath == chartDto.FilePath);

                if (chartExists)
                {
                    // Clean up uploaded files before failing
                    CleanupFile(imagePath, _webHostEnvironment);
                    if (!string.IsNullOrEmpty(audioPath))
                        CleanupFile(audioPath, _webHostEnvironment);

                    return ServiceResult<ChordChartEditDto>.Failure(
                        new ConflictException($"Chart with file path: {chartDto.FilePath} already exists."));
                }

                // Create chart entity
                var chart = chartDto.Adapt<ChordChart>();
                await _context.ChordCharts.AddAsync(chart);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var newChartDto = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(newChartDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error creating chord chart with files: {FilePath}. Error: {Error}", chartDto?.FilePath, ex);
                return ServiceResult<ChordChartEditDto>.Failure(
                    new Exception($"Error creating chord chart. Details: {ex.Message}"));
            }
        }

        public async Task<ServiceResult<ChordChartEditDto>> UpdateChordChartFilesAsync(
            ChordChartEditDto chartDto,
            IFormFile? chartImage,
            IFormFile? chartAudio)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (chartDto == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chord chart data is required."));

                var chart = await _context.ChordCharts.FindAsync(chartDto.Id);
                if (chart == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new NotFoundException($"Chord chart with ID: {chartDto.Id} does not exist."));

                // Store old file paths for cleanup
                var oldImagePath = chart.FilePath;
                var oldAudioPath = chart.ChartAudioFilePath;

                // Handle image file upload
                if (chartImage != null && chartImage.Length > 0)
                {
                    var imageValidation = await _fileValidationService.ValidateFileAsync(chartImage);
                    if (!imageValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Image validation failed: {imageValidation.ErrorMessage}"));

                    var imagePath = await SaveChartFile(chartImage, "charts/images", chartDto);
                    chartDto.FilePath = imagePath;
                }

                // Handle audio file upload
                if (chartAudio != null && chartAudio.Length > 0)
                {
                    var audioValidation = await _fileValidationService.ValidateFileAsync(chartAudio);
                    if (!audioValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Audio validation failed: {audioValidation.ErrorMessage}"));

                    var audioPath = await SaveChartFile(chartAudio, "charts/audio", chartDto);
                    chartDto.ChartAudioFilePath = audioPath;
                }

                // Check for duplicate file path (excluding current chart)
                if (!string.IsNullOrEmpty(chartDto.FilePath))
                {
                    var duplicateExists = await _context.ChordCharts
                        .AnyAsync(ch => ch.FilePath == chartDto.FilePath && ch.Id != chartDto.Id);

                    if (duplicateExists)
                    {
                        // Clean up newly uploaded files
                        if (chartImage != null && !string.IsNullOrEmpty(chartDto.FilePath))
                            CleanupFile(chartDto.FilePath, _webHostEnvironment);
                        if (chartAudio != null && !string.IsNullOrEmpty(chartDto.ChartAudioFilePath))
                            CleanupFile(chartDto.ChartAudioFilePath, _webHostEnvironment);

                        return ServiceResult<ChordChartEditDto>.Failure(
                            new ConflictException($"Chart: {chartDto.FilePath} already exists."));
                    }
                }

                // Update chart properties
                chart.FilePath = chartDto.FilePath ?? chart.FilePath;
                chart.ChordId = chartDto.ChordId;
                chart.FretPosition = chartDto.FretPosition;
                chart.ChartAudioFilePath = chartDto.ChartAudioFilePath ?? chart.ChartAudioFilePath;
                chart.PositionDescription = chartDto.PositionDescription;

                await _context.SaveChangesAsync();

                // Commit transaction before cleaning up old files
                await transaction.CommitAsync();

                // Clean up old files only after successful commit
                if (chartImage != null && !string.IsNullOrEmpty(oldImagePath) && oldImagePath != chart.FilePath)
                    CleanupFile(oldImagePath, _webHostEnvironment);
                if (chartAudio != null && !string.IsNullOrEmpty(oldAudioPath) && oldAudioPath != chart.ChartAudioFilePath)
                    CleanupFile(oldAudioPath, _webHostEnvironment);

                var updatedChart = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(updatedChart);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error updating chord chart with files: {FilePath}. Error: {Error}", chartDto?.FilePath, ex);
                return ServiceResult<ChordChartEditDto>.Failure(new Exception(ex.Message));
            }
        }

        private async Task<string> SaveChartFile(
            IFormFile file,
            string subfolder,
            dynamic chartDto)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;

            // Get storage path
            string storagePath = Path.Combine("media", subfolder);
            string uploadPath = Path.Combine(webRootPath, storagePath);

            // Ensure directory exists
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Generate filename with chord info
            string extension = Path.GetExtension(file.FileName);
            string baseFileName = Guid.NewGuid().ToString();

            // Try to get chord name for filename
            if (!string.IsNullOrEmpty(chartDto.ChordId))
            {
                try
                {
                    var chordResult = await _chordService.GetChordByIdAsync(chartDto.ChordId);
                    if (chordResult.IsSuccess && chordResult.Data != null)
                    {
                        var chordName = chordResult.Data.ChordName;
                        var fretPosition = chartDto.FretPosition ?? 1;

                        var sanitizedChordName = string.Join("", chordName.Split(Path.GetInvalidFileNameChars()));
                        baseFileName += $"_{sanitizedChordName}_fret{fretPosition}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve chord information for filename generation");
                }
            }

            string fileName = $"{baseFileName}{extension}";
            string filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return web-friendly path
            return $"{storagePath}/{fileName}".Replace('\\', '/');
        }

        private void CleanupFile(string filePath, IWebHostEnvironment webHostEnvironment)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fullPath = Path.Combine(webHostEnvironment.WebRootPath, filePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup file: {FilePath}", filePath);
            }
        }
    }
}
