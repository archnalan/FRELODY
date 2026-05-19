using FRELODYAPIs.Authorization;
using FRELODYAPP.Services.Seed;
using FRELODYSHRD.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/admin/seed")]
    public class AdminSeedController : ControllerBase
    {
        private readonly IStandardChordSeedService _chordSeed;

        public AdminSeedController(IStandardChordSeedService chordSeed)
        {
            _chordSeed = chordSeed;
        }

        [HttpPost("chords")]
        [OrgRole(UserRoles.SuperAdmin)]
        public async Task<ActionResult<SeedResult>> SeedChords([FromQuery] bool force = false, CancellationToken ct = default)
        {
            var result = await _chordSeed.SeedIfNeededAsync(force, ct);
            return Ok(result);
        }
    }
}
