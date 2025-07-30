using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChordChartsController : ControllerBase
    {
        private readonly IChordChartService _chordChartService;

        public ChordChartsController(IChordChartService chordChartService)
        {
            _chordChartService = chordChartService;
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
    }
}