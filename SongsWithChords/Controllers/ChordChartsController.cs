using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYAPP.Data;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChordChartsController : ControllerBase
    {
        private readonly IChordChartService _chordChartService;
        private readonly ILogger<ChordChartsController> _logger;

        public ChordChartsController(IChordChartService chordChartService, ILogger<ChordChartsController> logger)
        {
            _chordChartService = chordChartService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ChordChartEditDto>), 200)]
        public async Task<IActionResult> GetAllChordCharts()
        {
            var result = await _chordChartService.GetChordChartsAsync();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ChordChartEditDto), 200)]
        public async Task<IActionResult> GetChordChartById([FromQuery] string id)
        {
            var result = await _chordChartService.GetChordChartByIdAsync(id);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }


        [HttpGet]
        public async Task<IActionResult> GetChartsByChordId(string chordId)
        {
            var chartsResult = await _chordChartService.GetChartsByChordIdAsync(chordId);

            if (!chartsResult.IsSuccess)
                return StatusCode(chartsResult.StatusCode, new { message = chartsResult.Error.Message });

            return Ok(chartsResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ChartWithParentChordDto), 200)]
        public async Task<IActionResult> GetChartWithParentChordById([FromQuery] string id)
        {
            var result = await _chordChartService.GetChartWithParentChordByIdAsync(id);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChordChartEditDto), 201)]
        public async Task<IActionResult> CreateChordChart([FromBody] ChordChartCreateDto chartDto)
        {
            var result = await _chordChartService.CreateChordChartAsync(chartDto);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return CreatedAtAction(nameof(GetChordChartById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ChordChartEditDto), 200)]
        public async Task<IActionResult> UpdateChordChart([FromBody] ChordChartEditDto chartDto)
        {
            var result = await _chordChartService.UpdateChordChartAsync(chartDto);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteChordChart([FromQuery] string id)
        {
            var result = await _chordChartService.DeleteChordChartAsync(id);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return NoContent();
        }


        [HttpPost]
        [ProducesResponseType(typeof(ChordChartEditDto), 200)]
        public async Task<IActionResult> CreateChordChartFiles(
            IFormFile? chartImage,
            IFormFile? chartAudio,
           [FromQuery] string chartDataJson)
        {
            try
            {
                // Deserialize the chart data
                var chartDto = JsonSerializer.Deserialize<ChordChartCreateDto>(chartDataJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chartDto == null)
                    return BadRequest(new { message = "Invalid chart data" });

                if (chartImage == null || chartImage.Length == 0)
                    return BadRequest(new { message = "Chart image is required" });

                var result = await _chordChartService.CreateChordChartFilesAsync(
                    chartDto, chartImage, chartAudio);

                if (!result.IsSuccess)
                    return StatusCode(result.StatusCode, new { message = result.Error?.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chord chart with files");
                return StatusCode(500, new { message = "An error occurred while creating the chart" });
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(ChordChartEditDto), 200)]
        public async Task<IActionResult> UpdateChordChartFiles(
            IFormFile? chartImage,
            IFormFile? chartAudio,
            [FromQuery] string chartDataJson)
        {
            try
            {
                var chartDto = JsonSerializer.Deserialize<ChordChartEditDto>(chartDataJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chartDto == null)
                    return BadRequest(new { message = "Invalid chart data" });

                var result = await _chordChartService.UpdateChordChartFilesAsync(
                    chartDto, chartImage, chartAudio);

                if (!result.IsSuccess)
                    return StatusCode(result.StatusCode, new { message = result.Error?.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chord chart with files");
                return StatusCode(500, new { message = "An error occurred while updating the chart" });
            }
        }
    }
}