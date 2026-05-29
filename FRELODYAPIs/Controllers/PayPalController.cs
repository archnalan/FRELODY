using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PayPalController : ControllerBase
    {
        private readonly IPayPalService _payPal;

        public PayPalController(IPayPalService payPal)
        {
            _payPal = payPal;
        }

        /// <summary>Public SDK config (client id + currency). Empty client id ⇒ hide PayPal.</summary>
        [HttpGet("config")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PayPalConfigDto), 200)]
        public IActionResult Config() => Ok(_payPal.GetConfig());

        /// <summary>Create a PayPal order for a plan; returns the order id for the JS SDK.</summary>
        [HttpPost("create-order")]
        [Authorize]
        [ProducesResponseType(typeof(PayPalCreateOrderResult), 200)]
        public async Task<IActionResult> CreateOrder([FromBody] PayPalCreateOrderRequest request)
        {
            var result = await _payPal.CreateOrderAsync(request.ProductId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        /// <summary>Capture an approved order and grant premium on success.</summary>
        [HttpPost("capture-order")]
        [Authorize]
        [ProducesResponseType(typeof(PayPalCaptureResult), 200)]
        public async Task<IActionResult> CaptureOrder([FromBody] PayPalCaptureRequest request)
        {
            var result = await _payPal.CaptureOrderAsync(request.OrderId, request.ProductId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }
    }
}
