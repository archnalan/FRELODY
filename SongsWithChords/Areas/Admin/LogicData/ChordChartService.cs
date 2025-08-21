using Mapster;
using FRELODYAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class ChordChartService : IChordChartService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ChordChartService> _logger;

        public ChordChartService(SongDbContext context, ILogger<ChordChartService> logger)
        {
            _context = context;
            _logger = logger;
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
                _context.ChordCharts.Remove(chart);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chord chart with ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<bool>.Failure(new Exception(ex.Message));
            }
        }
    }
}
