using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.WithUploads;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ChartsController : ControllerBase
	{
		private readonly IChordChartService _chartService;

        public ChartsController(IChordChartService chartService)
        {
			_chartService = chartService;            
        }

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var chartsResult = await _chartService.GetChordChartsAsync();

			if (!chartsResult.IsSuccess)
				return StatusCode(chartsResult.StatusCode,
					new { message = chartsResult.Error.Message });

			return Ok(chartsResult.Data);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetChordChartById(string id)
		{

			var chartResult = await _chartService.GetChordChartByIdAsync(id);

			if (!chartResult.IsSuccess)
				return StatusCode(chartResult.StatusCode, new { message = chartResult.Error.Message });					

			return Ok(chartResult.Data);
		}
		
        [HttpPost("create")]
		public async Task<IActionResult> CreateChordChart([FromForm] ChordChartCreateDto chartCreateDto)
		{
			if (chartCreateDto == null) return BadRequest("Chord Chart data is required.");			

			if (!ModelState.IsValid) return BadRequest(ModelState);			

			 var chartCreateResult = await _chartService.CreateChordChartAsync(chartCreateDto);

			if (!chartCreateResult.IsSuccess) return StatusCode(chartCreateResult.StatusCode, 
												new { message = chartCreateResult.Error.Message });

			var newChartDto = chartCreateResult.Data;

			return CreatedAtAction(nameof(GetChordChartById), new { id = newChartDto.Id}, newChartDto);
		}

		[HttpPut("edit/{id}")]
		public async Task<IActionResult> EditChordChart(string id, [FromForm]ChordChartEditDto chartEditDto)
		{
			if (chartEditDto == null) return BadRequest("Chord Chart data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != chartEditDto.Id)
				return BadRequest($"Chord charts of IDs: {id} and {chartEditDto.Id} are not the same");

			var editedChartResult = await _chartService.UpdateChordChartAsync(chartEditDto);

			if (!editedChartResult.IsSuccess) return StatusCode(editedChartResult.StatusCode, new
			{message = editedChartResult.Error.Message});

			return Ok(editedChartResult);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteChordChart(string id)
		{
			var removalResult = await _chartService.DeleteChordChartAsync(id);

			if (!removalResult.IsSuccess)
			{
				return StatusCode(removalResult.StatusCode, 
					new {message =  removalResult.Error.Message});
			}

			return NoContent();
		}
	}
}
