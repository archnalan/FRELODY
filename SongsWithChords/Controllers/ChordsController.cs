using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using Microsoft.AspNetCore.Authorization;
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
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginationDetails<ChordDto>), 200)]
        public async Task<IActionResult> GetAllChords([FromQuery]int? offset, [FromQuery]int? limit)
        {
            var offset1 = offset ?? 0;
            var limit1 = limit ?? int.MaxValue;
            var chordResult = await _chordService.GetChordsAsync(offset1, limit1);
            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });

            return Ok(chordResult.Data);
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ChordDto), 200)]
        public async Task<IActionResult> GetChordById([FromQuery] string id)
        {
            var chordResult = await _chordService.GetChordByIdAsync(id);
            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            return Ok(chordResult.Data);
        }


        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Editor},{UserRoles.Contributor},{UserRoles.Admin},{UserRoles.Owner}")]
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

        [HttpPut]
        [Authorize(Roles = $"{UserRoles.Editor},{UserRoles.Contributor},{UserRoles.Admin},{UserRoles.Owner}")]
        [ProducesResponseType(typeof(ChordEditDto), 200)]
        public async Task<ActionResult> UpdateChord([FromBody] ChordEditDto chordDto)
        {
            var chordResult = await _chordService.UpdateChordAsync(chordDto);

            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            
            return Ok(chordResult.Data);
        }

        [HttpDelete]
        [Authorize(Roles = $"{UserRoles.Moderator},{UserRoles.Admin},{UserRoles.Owner}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteChord([FromQuery] string id)
        {
            var chordResult = await _chordService.DeleteChordAsync(id);

            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            
            return Ok(chordResult.Data);
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginationDetails<ChordDto>), 200)]
        public async Task<IActionResult> SearchChords([FromQuery]string? keywords, [FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            var chordResult = await _chordService.SearchChordsAsync(keywords, offset, limit);
            
            if (!chordResult.IsSuccess)
                return StatusCode(chordResult.StatusCode, new { message = chordResult.Error.Message });
            
            return Ok(chordResult.Data);
        }

    }
}
