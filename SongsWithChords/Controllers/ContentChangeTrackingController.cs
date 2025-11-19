using FRELODYAPIs.Areas.Admin.LogicData;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ContentChangeTrackingController : ControllerBase
    {
        private readonly ContentChangeTrackingService _contentChangeTrackingService;

        public ContentChangeTrackingController(ContentChangeTrackingService contentChangeTrackingService)
        {
            _contentChangeTrackingService = contentChangeTrackingService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DashboardActivitySummary), 200)]
        public async Task<IActionResult> GetActivitySinceLastLogin([FromQuery]string userId)
        {
            var response = await _contentChangeTrackingService.GetActivitySinceLastLogin(userId);
            if (!response.IsSuccess)
            {
              return StatusCode(response.StatusCode, response.Error);
            }
            return Ok(response.Data);
        }
    }
}
