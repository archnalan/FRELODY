using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.PesaPalDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Owner}")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ProductDto>), 200)]
        public async Task<IActionResult> GetProducts()
        {
            var result = await _productService.GetProductsAsync();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ProductDto), 200)]
        public async Task<IActionResult> GetProductById([FromQuery] string productId)
        {
            var result = await _productService.GetProductByIdAsync(productId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), 201)]
        public async Task<IActionResult> AddProduct([FromBody] ProductDto product)
        {
            var result = await _productService.AddProduct(product);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return CreatedAtAction(nameof(GetProductById), new { productId = result.Data!.Id }, result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ProductDto), 200)]
        public async Task<IActionResult> UpdateProduct([FromBody] ProductDto product)
        {
            var result = await _productService.UpdateProduct(product);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteProduct([FromQuery] string productId)
        {
            var result = await _productService.DeleteProduct(productId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }
    }
}
