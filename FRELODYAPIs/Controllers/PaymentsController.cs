using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Models.PesaPal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayments()
        {
            var result = await _paymentService.GetPaymentsAsync();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddPayment([FromBody]PesaPayment request)
        {
            var result = await _paymentService.AddPayment(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }
            return Ok(result.Data);
        }

        // Path is produced by the [action] route token + SlugifyParameterTransformer
        // (Program.cs) → /api/payments/get-revenue-stats. Do NOT add an explicit
        // [HttpGet("...")] template here: it would combine with the [action] token
        // and yield /api/payments/get-revenue-stats/get-revenue-stats (404).
        [HttpGet]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(typeof(RevenueStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueStats(
            [FromQuery] DateTimeOffset from,
            [FromQuery] DateTimeOffset to)
        {
            var result = await _paymentService.GetRevenueStatsAsync(from, to);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }
            return Ok(result.Data);
        }
    }
}
