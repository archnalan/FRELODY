using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.PesaPalDtos;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ProductService : IProductService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ILogger<ProductService> logger, SongDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ServiceResult<List<ProductDto>>> GetProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .OrderBy(p => p.Price)
                    .ToListAsync();
                var productsDto = products.Adapt<List<ProductDto>>();
                return ServiceResult<List<ProductDto>>.Success(productsDto);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products.");
                return ServiceResult<List<ProductDto>>.Failure(
                    new ServerErrorException("An error occurred while fetching products."));
            }
        }

        public async Task<ServiceResult<ProductDto>> GetProductByIdAsync(string productId)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null)
                {
                    return ServiceResult<ProductDto>.Failure(
                        new NotFoundException($"Product not found."));
                }
                var productDto = product.Adapt<ProductDto>();
                return ServiceResult<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching product with ID {productId}.");
                return ServiceResult<ProductDto>.Failure(
                    new ServerErrorException("An error occurred while fetching the product."));
            }
        }

        public async Task<ServiceResult<ProductDto>> AddProduct(ProductDto product)
        {
            try
            {
                var productEntity = product.Adapt<Product>();

                _context.Products.Add(productEntity);

                await _context.SaveChangesAsync();

                var productDto = productEntity.Adapt<ProductDto>();
                return ServiceResult<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a new product.");
                return ServiceResult<ProductDto>.Failure(
                    new ServerErrorException("An error occurred while adding the product."));
            }
        }

        public async Task<ServiceResult<ProductDto>> UpdateProduct(ProductDto product)
        {
            try
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == product.Id);
                if (existingProduct == null)
                {
                    return ServiceResult<ProductDto>.Failure(
                        new NotFoundException($"Product not found."));
                }
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Currency = product.Currency;

                await _context.SaveChangesAsync();
                var productDto = existingProduct.Adapt<ProductDto>();
                return ServiceResult<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating the product.");
                return ServiceResult<ProductDto>.Failure(
                    new ServerErrorException("An error occurred while updating the product."));
            }
        }

        public async Task<ServiceResult<bool>> DeleteProduct(string productId)
        {
            try
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);
                if (existingProduct == null)
                {
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Product not found."));
                }
                _context.Products.Remove(existingProduct);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting the product.");
                return ServiceResult<bool>.Failure(
                    new ServerErrorException("An error occurred while deleting the product."));
            }
        }
    }
}
