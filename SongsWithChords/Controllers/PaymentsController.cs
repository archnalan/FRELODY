using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Models.PesaPal;
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

    }
}
