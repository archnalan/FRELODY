using AutoMapper;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using FRELODYAPP.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
    [ApiController]
    [Area("Admin")]
    public class Api_ChordChartsController : ControllerBase
    {
        private readonly SongDbContext _context;
        private readonly IMapper _mapper;

        public Api_ChordChartsController(SongDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        //GET admin/api_chordcharts
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var chordCharts = await _context.ChordCharts
                            .OrderBy(ch => ch.ChordId)
                            .ToListAsync();

            var chordChartsDto = _mapper.Map<List<ChordChartEditDto>>(chordCharts);

            return Ok(chordChartsDto);
        }

        //GET admin/api_chordcharts/charts
        [HttpGet("charts")]
        public async Task<IActionResult> ChordCharts()
        {
            var chordCharts = await _context.ChordCharts
                            .OrderBy(ch => ch.ChordId)
                            .ToListAsync();

            if (chordCharts == null || chordCharts.Any() == false) return BadRequest("No charts added yet!");

            var chordChartsDto = chordCharts.Select(_mapper.Map<ChordChart, ChartWithParentChordDto>)
                                            .ToList();
            return Ok(chordChartsDto);
        }

        //GET admin/api_chordcharts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChordChartById(int id)
        {
            var chordChart = await _context.ChordCharts.FindAsync(id);

            if (chordChart == null) return BadRequest($"Chord with ID: {id} does not exist");

            var chordChartDto = _mapper.Map<ChordChart, ChordChartEditDto>(chordChart);

            return Ok(chordChartDto);
        }

        //GET admin/api_chordcharts/charts/5
        [HttpGet("charts/{id}")]
        public async Task<IActionResult> GetChartWithChordById(Guid id)
        {
            var chordChart = await _context.ChordCharts
                                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (chordChart == null) return BadRequest($"Chord with ID: {id} does not exist");

            var chordChartDto = _mapper.Map<ChordChart, ChartWithParentChordDto>(chordChart);

            return Ok(chordChartDto);
        }

        //GET admin/api_chordcharts/by_ids
        [HttpGet]
        [Route("by_ids")]
        public async Task<IActionResult> GetChordChartsById(List<Guid> Ids)
        {
            if (Ids == null || Ids.Any() == false)
                return BadRequest("Chord Chart Ids are required.");

            var chords = await _context.ChordCharts
                        .Where(ch => Ids.Contains(ch.Id))
                        .ToListAsync();

            var foundChartsDto = _mapper.Map<List<ChordChartEditDto>>(chords)
                                        .ToList();

            var notFoundChartsDto = Ids.Except(foundChartsDto.Select(ch => ch.Id)).ToList();

            if (notFoundChartsDto.Count == Ids.Count) return NotFound(notFoundChartsDto);

            if (notFoundChartsDto.Any())
            {
                return Ok(new
                {
                    Found = foundChartsDto,
                    NotFound = notFoundChartsDto
                });
            }

            return Ok(foundChartsDto);

        }

        //GET admin/api_chordcharts/charts/by_ids
        [HttpGet]
        [Route("charts/by_ids")]
        public async Task<IActionResult> GetChartsWithParentChordsById(List<Guid> ids)
        {
            if (ids == null || ids.Any() == false)
                return BadRequest("Chord Chart Ids are required.");

            var chords = await _context.ChordCharts
                        .Where(ct => ids.Contains(ct.Id))
                        .ToListAsync();

            var foundChartsDto = _mapper.Map<List<ChartWithParentChordDto>>(chords)
                                        .ToList();

            var notFoundChartsDto = ids.Except(chords.Select(ch => ch.Id)).ToList();

            if (notFoundChartsDto.Count == ids.Count) return NotFound(notFoundChartsDto);

            if (notFoundChartsDto.Any())
            {
                return Ok(new
                {
                    Found = foundChartsDto,
                    NotFound = notFoundChartsDto
                });
            }

            return Ok(foundChartsDto);

        }

        //POST admin/api_chordcharts/create
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] ChordChartCreateDto chordChartDto)
        {
            if (chordChartDto == null) return BadRequest("Chord Chart data is required.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var chartExists = await _context.ChordCharts
                            .AnyAsync(ch => ch.FilePath == chordChartDto.FilePath);

            if (chartExists) return Conflict($"Chart with file Path: {chordChartDto.FilePath} already exists.");

            if (chordChartDto.ChordId != null)
            {
                var chordExists = await _context.Chords
                    .AnyAsync(ch => ch.Id == chordChartDto.ChordId);
                if (chordExists == false) return BadRequest($"Chord with ID:{chordChartDto.ChordId} does not exist.");
            }

            var chordChart = _mapper.Map<ChordChartCreateDto, ChordChart>(chordChartDto);

            try
            {
                _context.ChordCharts.Add(chordChart);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var newChordChartDto = _mapper.Map<ChordChart, ChordChartCreateDto>(chordChart);

            return CreatedAtAction(nameof(GetChordChartById), new { id = chordChart.Id }, newChordChartDto);

        }

        //POST admin/api_chordcharts/create_many
        [HttpPost]
        [Route("create_many")]
        public async Task<IActionResult> CreateChordCharts(List<ChordChartCreateDto> chordChartDtos)
        {
            if (chordChartDtos == null || chordChartDtos.Count == 0)
                return BadRequest("Chord Chart Data is required");

            var chartsToAdd = new List<ChordChart>();

            //To show Id in the output
            var createdCharts = new List<ChordChartEditDto>();

            var errors = new List<string>();
            var noRepeatCharts = new HashSet<(string FilePath, long? ChordId)>();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {

                foreach (var chordChartDto in chordChartDtos)
                {
                    if (!TryValidateModel(chordChartDto))
                    {
                        errors.Add($"Invalid data for: {chordChartDto.FilePath}");
                        continue;
                    }

                    var chartExists = await _context.ChordCharts
                                    .AnyAsync(ch => ch.FilePath == chordChartDto.FilePath);
                    if (chartExists)
                    {
                        errors.Add($"Chord chart: {chordChartDto.FilePath} already exists.");
                        continue;
                    }

                    if (chordChartDto.ChordId != null)
                    {
                        var chordExists = await _context.Chords
                            .AnyAsync(ch => ch.Id == chordChartDto.ChordId);
                        if (chordExists == false)
                        {
                            errors.Add($"Chord with ID: {chordChartDto.ChordId} does not exist.");
                            continue;
                        }

                    }
                    var chartTuple = (chordChartDto.FilePath, chordChartDto.ChordId);
                    if (noRepeatCharts.Contains(chartTuple))
                    {
                        errors.Add($"Invalid! Duplicate chart {chordChartDto.FilePath} detected for same chord.");
                        continue;
                    }
                    var chart = _mapper.Map<ChordChartCreateDto, ChordChart>(chordChartDto);

                    chartsToAdd.Add(chart);
                    noRepeatCharts.Add(chartTuple);
                }

                if (errors.Any()) return BadRequest(errors);

                if (chartsToAdd.Count > 0)
                {
                    try
                    {
                        await _context.ChordCharts.AddRangeAsync(chartsToAdd);

                        await _context.SaveChangesAsync();

                        foreach (var chart in chartsToAdd)
                        {
                            var newChartDto = _mapper.Map<ChordChart, ChordChartEditDto>(chart);
                            createdCharts.Add(newChartDto);
                        }

                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
                await transaction.CommitAsync();

                return Ok(createdCharts);
            };

        }

        //PUT admin/api_chordcharts/edit/5
        [HttpPut]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, ChordChartEditDto chartDto)
        {
            if (chartDto == null) return BadRequest("Chord chart Data is required.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id != chartDto.Id)
                return Conflict($"Invalid attempt! Chart IDs: {id} and {chartDto.Id} are not the same.");

            var chartInDb = await _context.ChordCharts.FindAsync(id);

            if (chartInDb == null) return BadRequest($"Chart with ID: {id} does not exist.");

            if (chartDto.ChordId != null)
            {
                var chordInDb = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);

                if (chordInDb == false) return BadRequest($"Chord with ID: {chartDto.ChordId} does not exist.");
            }

            var chordChartExits = await _context.ChordCharts.Where(ch => ch.Id != id)
                                    .AnyAsync(ch => ch.FilePath == chartDto.FilePath);

            if (chordChartExits) return Conflict($"Chart: {chartDto.FilePath} already exists.");

            if (chartInDb.FilePath == chartDto.FilePath &&
            chartInDb.ChordId == chartDto.ChordId &&
            chartInDb.ChartAudioFilePath == chartDto.ChartAudioFilePath &&
            chartInDb.FretPosition == chartDto.FretPosition &&
            chartInDb.PositionDescription == chartDto.PositionDescription)
                return Ok(new
                {
                    Message = $"Chart: {chartDto.FilePath} is already up-to-date"
                });

            var chordchart = _mapper.Map(chartDto, chartInDb);

            _context.ChordCharts.Update(chordchart);

            await _context.SaveChangesAsync();

            var newchordDto = _mapper.Map<ChordChart, ChordChartEditDto>(chordchart);

            return Ok(newchordDto);
        }

        //PUT admin/api_chordcharts/edit_many
        [HttpPut]
        [Route("edit_many")]
        public async Task<IActionResult> EditChords(List<ChordChartEditDto> chartDtos)
        {
            if (chartDtos == null || chartDtos.Count == 0) return BadRequest("Chart data is required.");

            var chartsToEdit = new List<ChordChart>();
            var editedChartDtos = new List<ChordChartEditDto>();

            var errors = new List<string>();
            var noRepeatCharts = new HashSet<(string FilePath, long? ChordId)>();

            using (var transaction = _context.Database.BeginTransaction())
            {
                foreach (var chartDto in chartDtos)
                {
                    if (!TryValidateModel(chartDto))
                    {
                        errors.Add($"Invalid data for Chart: {chartDto.FilePath}");
                        continue;
                    }
                    var chartInDb = await _context.ChordCharts.FindAsync(chartDto.Id);

                    if (chartInDb == null)
                    {
                        errors.Add($"Chart with ID: {chartDto.Id} does not exist");
                        continue;
                    }

                    var chartExists = await _context.ChordCharts
                                    .Where(ch => ch.Id != chartDto.Id)
                                    .AnyAsync(ch => ch.FilePath == chartDto.FilePath);

                    if (chartExists)
                    {
                        errors.Add($"Chart: {chartDto.FilePath} already exists.");
                        continue;
                    }

                    if (chartDto.ChordId != null)
                    {
                        var chordInDb = await _context.Chords.AnyAsync(ch => ch.Id == chartDto.ChordId);

                        if (chordInDb == false)
                        {
                            errors.Add($"Chord with ID: {chartDto.ChordId} does not exist.");

                            continue;
                        }
                    }

                    if (chartInDb.FilePath == chartDto.FilePath &&
                        chartInDb.ChordId == chartDto.ChordId &&
                        chartInDb.ChartAudioFilePath == chartDto.ChartAudioFilePath &&
                        chartInDb.FretPosition == chartDto.FretPosition &&
                        chartInDb.PositionDescription == chartDto.PositionDescription)
                    {
                        errors.Add($"Chart: {chartDto.FilePath} already up-to-date.");
                        continue;
                    }

                    if (noRepeatCharts.Contains((chartDto.FilePath, chartDto.ChordId)))
                    {
                        errors.Add($"Invalid! Duplicate Chart {chartDto.FilePath} detected for same chord.");
                        continue;
                    }
                    try
                    {
                        var chart = _mapper.Map(chartDto, chartInDb);

                        _context.ChordCharts.Update(chart);

                        chartsToEdit.Add(chart);
                        noRepeatCharts.Add((chart.FilePath, chart.ChordId));
                    }
                    catch (Exception ex) 
                    { 
                        await transaction.RollbackAsync(); 
                        return BadRequest(ex.Message); 
                    }
                }

                if (errors.Any()) return BadRequest(errors);

                try
                {
                    await _context.SaveChangesAsync();

                    foreach (var chart in chartsToEdit)
                    {
                        var newChartDto = _mapper.Map<ChordChart, ChordChartEditDto>(chart);
                        editedChartDtos.Add(newChartDto);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ex.Message);
                }

                await transaction.CommitAsync();

                return Ok(editedChartDtos);
            }
        }

        //DELETE admin/api_chordcharts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var chart = await _context.ChordCharts.FindAsync(id);

            if (chart == null) return BadRequest($"Chart with ID: {id} does not exist.");

            try
            {
                _context.ChordCharts.Remove(chart);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is
                    Microsoft.Data.SqlClient.SqlException sqlEx
                    && sqlEx.Number == 547)
                    return BadRequest(sqlEx.Message);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        //DELETE admin/api_chordcharts/by_ids
        [HttpDelete]
        [Route("by_ids")]
        public async Task<IActionResult> DeleteChords(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("Chart Ids required.");

            var deletedIds = new List<int>();
            var errors = new List<string>();

            foreach (int id in ids)
            {
                var chart = await _context.ChordCharts.FindAsync(id);

                if (chart == null)
                {
                    errors.Add($"Chart with ID: {id} does not exist.");
                    continue;
                }
                _context.ChordCharts.Remove(chart);
                deletedIds.Add(id);
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }

            if (errors.Count == ids.Count) return BadRequest(errors);

            if (errors.Any())
            {
                return Ok(new
                {
                    Deleted = deletedIds,
                    NotDeleted = errors
                });
            }

            return NoContent();
        }


    }
}
