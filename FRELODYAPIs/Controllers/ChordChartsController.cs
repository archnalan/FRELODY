using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYAPP.Data;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Models.ChordDraw;
using Microsoft.AspNetCore.Authorization;
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

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
        [Authorize]
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
        [Authorize(Roles = $"{UserRoles.Moderator},{UserRoles.Admin},{UserRoles.Owner}")]
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
                var chartDto = JsonSerializer.Deserialize<ChordChartCreateDto>(chartDataJson, JsonOpts);

                if (chartDto == null)
                    return BadRequest(new { message = "Invalid chart data" });

                if (chartDto.Source == ChordSource.Image && (chartImage == null || chartImage.Length == 0))
                    return BadRequest(new { message = "Chart image is required for image-source charts" });

                if (chartDto.Source == ChordSource.Drawing && chartDto.ChordData == null)
                    return BadRequest(new { message = "Chord drawing data is required for drawing-source charts" });

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
                var chartDto = JsonSerializer.Deserialize<ChordChartEditDto>(chartDataJson, JsonOpts);

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

        [HttpPost]
        [Produces("image/svg+xml")]
        public IActionResult PreviewSvg([FromBody] ChordDrawData chartData)
        {
            var result = _chordChartService.RenderSvg(chartData);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error?.Message });
            return Content(result.Data!, "image/svg+xml");
        }
    }
}
