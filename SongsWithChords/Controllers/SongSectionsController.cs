using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SongSectionsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<ComboBoxDto>> GetComboBoxSongSections()
        {
            var sections = System.Enum.GetValues(typeof(SongSection))
                .Cast<SongSection>()
                .Select((section, idx) => new ComboBoxDto
                {
                    Id = idx,
                    ValueText = section.ToString(),
                    IdString = section.ToString()
                })
                .ToList();

            return Ok(sections);
        }
    }
}
