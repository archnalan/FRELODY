using FRELODYSHRD.Constants;
using FRELODYSHRD.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CurrencyConverterController : ControllerBase
    {
        private readonly ICurrencyConverter _currencyConverter;
        public CurrencyConverterController(ICurrencyConverter currencyConverter)
        {
            _currencyConverter = currencyConverter;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
        public async Task<IActionResult> GetExchangeRates([FromQuery] string baseCurrency)
        {
            var result = await _currencyConverter.GetExchangeRatesAsync(baseCurrency);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(decimal), 200)]
        public async Task<IActionResult> ConvertCurrency([FromQuery] string fromCurrency, [FromQuery] string toCurrency, [FromQuery] decimal amount, [FromQuery] int? roundingFormat)
        {
            var result = await _currencyConverter.ConvertCurrencyAsync(fromCurrency, toCurrency, amount, roundingFormat.HasValue ? (RoundingFormat)roundingFormat.Value : null);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }   
    }
}
