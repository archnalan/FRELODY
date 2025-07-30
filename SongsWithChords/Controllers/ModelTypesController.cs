using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Interfaces;
using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ModelTypesController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public ActionResult<IEnumerable<ComboBoxDto>> GetSongSections()
        {
            var songSections = Enum.GetValues(typeof(SongSection))
                                 .Cast<SongSection>()
                                 .Select(e => new ComboBoxDto
                                 {
                                     Id = (int)e,
                                     ValueText = e.ToString(),
                                     IdString = e.ToString()
                                 })
                                 .ToList();
            return Ok(songSections);
        }
        
        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public ActionResult<IEnumerable<ComboBoxDto>> GetChordDifficulties()
        {
            var chordDifficulties = Enum.GetValues(typeof(ChordDifficulty))
                                 .Cast<ChordDifficulty>()
                                 .Select(e => new ComboBoxDto
                                 {
                                     Id = (int)e,
                                     ValueText = e.ToString(),
                                     IdString = e.ToString()
                                 })
                                 .ToList();
            return Ok(chordDifficulties);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public ActionResult<IEnumerable<ComboBoxDto>> GetChordTypes()
        {
            var chordTypes = Enum.GetValues(typeof(ChordType))
                                 .Cast<ChordType>()
                                 .Select(e => new ComboBoxDto
                                 {
                                     Id = (int)e,
                                     ValueText = e.ToString(),
                                     IdString = e.ToString()
                                 })
                                 .ToList();
            return Ok(chordTypes);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public ActionResult<IEnumerable<ComboBoxDto>> GetPlayLevels()
        {
            var chordVoicings = Enum.GetValues(typeof(PlayLevel))
                                 .Cast<PlayLevel>()
                                 .Select(e => new ComboBoxDto
                                 {
                                     Id = (int)e,
                                     ValueText = e.ToString(),
                                     IdString = e.ToString()
                                 })
                                 .ToList();
            return Ok(chordVoicings);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public ActionResult<IEnumerable<ComboBoxDto>> GetFeedbackStatuses()
        {
            var chordVoicings = Enum.GetValues(typeof(FeedbackStatus))
                                 .Cast<FeedbackStatus>()
                                 .Select(e => new ComboBoxDto
                                 {
                                     Id = (int)e,
                                     ValueText = e.ToString(),
                                     IdString = e.ToString()
                                 })
                                 .ToList();
            return Ok(chordVoicings);
        }
    }
}
