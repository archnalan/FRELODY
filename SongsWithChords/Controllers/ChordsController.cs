using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChordsController : ControllerBase
    {
        private readonly IChordService _chordService;
        public ChordsController(IChordService chordService)
        {
            _chordService = chordService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChordDto>), 200)]
        public async Task<IActionResult> GetAllChords()
        {
            var chordResult = await _chordService.GetChordsAsync();
            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });

            return Ok(chordResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ChordDto), 200)]
        public async Task<IActionResult> GetChordById([FromQuery] string id)
        {
            var chordResult = await _chordService.GetChordByIdAsync(id);
            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            return Ok(chordResult.Data);
        }


        [HttpPost]
        [ProducesResponseType(typeof(ChordEditDto), 200)]
        public async Task<ActionResult> CreateChord([FromBody]ChordCreateDto chordDto)
        {
            var chordResult = await _chordService.CreateChordAsync(chordDto);

            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            
            return Ok(chordResult.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChordEditDto), 200)]
        public async Task<ActionResult> CreateSimpleChord([FromBody] ChordDto chordDto)
        {
            var chordResult = await _chordService.CreateSimpleChordAsync(chordDto);

            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            
            return Ok(chordResult.Data);
        }

    }
}
