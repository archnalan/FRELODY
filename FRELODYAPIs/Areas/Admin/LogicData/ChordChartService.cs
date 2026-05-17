using FRELODYAPIs.Services.ChordCharts;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Data;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Profiles;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Models.ChordDraw;
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
        private readonly ChordSvgRenderer _svgRenderer;
        private readonly ILogger<ChordChartService> _logger;

        public ChordChartService(
            SongDbContext context,
            ILogger<ChordChartService> logger,
            IWebHostEnvironment webHostEnvironment,
            FileValidationService fileValidationService,
            IChordService chordService,
            ChordSvgRenderer svgRenderer)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _fileValidationService = fileValidationService;
            _chordService = chordService;
            _svgRenderer = svgRenderer;
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

                if (!string.IsNullOrEmpty(chartDto.ChordId))
                {
                    var chordExists = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);
                    if (!chordExists)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Chord with ID: {chartDto.ChordId} does not exist."));
                }

                var chart = chartDto.Adapt<ChordChart>();
                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData != null)
                {
                    chart.RenderedSvg = SafeRenderSvg(chartDto.ChordData);
                }

                await _context.ChordCharts.AddAsync(chart);
                await _context.SaveChangesAsync();

                var newChartDto = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(newChartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chord chart. Error: {Error}", ex);
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

            try
            {
                chart.FilePath = chartDto.FilePath;
                chart.ChordId = chartDto.ChordId;
                chart.FretPosition = chartDto.FretPosition;
                chart.ChartAudioFilePath = chartDto.ChartAudioFilePath;
                chart.PositionDescription = chartDto.PositionDescription;
                chart.Source = chartDto.Source;

                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData != null)
                {
                    chart.ChordDataJson = MappingConfig.SerializeChordData(chartDto.ChordData);
                    chart.RenderedSvg = SafeRenderSvg(chartDto.ChordData);
                }

                await _context.SaveChangesAsync();

                var updatedChart = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(updatedChart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chord chart. Error: {Error}", ex);
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
            IFormFile? chartImage,
            IFormFile? chartAudio)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (chartDto == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chord chart data is required."));

                if (chartDto.Source == ChordSource.Image && (chartImage == null || chartImage.Length == 0))
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chart image is required for image-source charts."));

                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData == null)
                    return ServiceResult<ChordChartEditDto>.Failure(
                        new BadRequestException("Chord data is required for drawing-source charts."));

                if (chartImage != null && chartImage.Length > 0)
                {
                    var imageValidation = await _fileValidationService.ValidateFileAsync(chartImage);
                    if (!imageValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Image validation failed: {imageValidation.ErrorMessage}"));
                }

                if (chartAudio != null)
                {
                    var audioValidation = await _fileValidationService.ValidateFileAsync(chartAudio);
                    if (!audioValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Audio validation failed: {audioValidation.ErrorMessage}"));
                }

                if (!string.IsNullOrEmpty(chartDto.ChordId))
                {
                    var chordExists = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);
                    if (!chordExists)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Chord with ID: {chartDto.ChordId} does not exist."));
                }

                string? imagePath = null;
                string? audioPath = null;

                if (chartImage != null && chartImage.Length > 0)
                {
                    imagePath = await SaveChartFile(chartImage, "charts/images", chartDto);
                    chartDto.FilePath = imagePath;
                }

                if (chartAudio != null)
                {
                    audioPath = await SaveChartFile(chartAudio, "charts/audio", chartDto);
                    chartDto.ChartAudioFilePath = audioPath;
                }

                var chart = chartDto.Adapt<ChordChart>();
                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData != null)
                {
                    chart.RenderedSvg = SafeRenderSvg(chartDto.ChordData);
                }

                await _context.ChordCharts.AddAsync(chart);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var newChartDto = chart.Adapt<ChordChartEditDto>();
                return ServiceResult<ChordChartEditDto>.Success(newChartDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating chord chart with files: {Error}", ex);
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

                var oldImagePath = chart.FilePath;
                var oldAudioPath = chart.ChartAudioFilePath;

                if (chartImage != null && chartImage.Length > 0)
                {
                    var imageValidation = await _fileValidationService.ValidateFileAsync(chartImage);
                    if (!imageValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Image validation failed: {imageValidation.ErrorMessage}"));

                    var imagePath = await SaveChartFile(chartImage, "charts/images", chartDto);
                    chartDto.FilePath = imagePath;
                }

                if (chartAudio != null && chartAudio.Length > 0)
                {
                    var audioValidation = await _fileValidationService.ValidateFileAsync(chartAudio);
                    if (!audioValidation.IsValid)
                        return ServiceResult<ChordChartEditDto>.Failure(
                            new BadRequestException($"Audio validation failed: {audioValidation.ErrorMessage}"));

                    var audioPath = await SaveChartFile(chartAudio, "charts/audio", chartDto);
                    chartDto.ChartAudioFilePath = audioPath;
                }

                chart.FilePath = chartDto.FilePath ?? chart.FilePath;
                chart.ChordId = chartDto.ChordId;
                chart.FretPosition = chartDto.FretPosition;
                chart.ChartAudioFilePath = chartDto.ChartAudioFilePath ?? chart.ChartAudioFilePath;
                chart.PositionDescription = chartDto.PositionDescription;
                chart.Source = chartDto.Source;

                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData != null)
                {
                    chart.ChordDataJson = MappingConfig.SerializeChordData(chartDto.ChordData);
                    chart.RenderedSvg = SafeRenderSvg(chartDto.ChordData);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
                _logger.LogError(ex, "Error updating chord chart with files: {Error}", ex);
                return ServiceResult<ChordChartEditDto>.Failure(new Exception(ex.Message));
            }
        }

        public ServiceResult<string> RenderSvg(ChordDrawData chartData)
        {
            if (chartData == null)
                return ServiceResult<string>.Failure(new BadRequestException("Chord data is required."));

            try
            {
                var svg = _svgRenderer.Render(chartData);
                return ServiceResult<string>.Success(svg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering chord SVG: {Error}", ex);
                return ServiceResult<string>.Failure(new Exception($"Error rendering chord SVG. Details: {ex.Message}"));
            }
        }

        private string? SafeRenderSvg(ChordDrawData data)
        {
            try { return _svgRenderer.Render(data); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to render server-side SVG for chord chart; persisting JSON only.");
                return null;
            }
        }

        private async Task<string> SaveChartFile(
            IFormFile file,
            string subfolder,
            dynamic chartDto)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;

            string storagePath = Path.Combine("media", subfolder);
            string uploadPath = Path.Combine(webRootPath, storagePath);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string extension = Path.GetExtension(file.FileName);
            string baseFileName = Guid.NewGuid().ToString();

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

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

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
