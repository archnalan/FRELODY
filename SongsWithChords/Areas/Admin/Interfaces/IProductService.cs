using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.PesaPalDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IProductService
    {
        Task<ServiceResult<ProductDto>> AddProduct(ProductDto product);
        Task<ServiceResult<bool>> DeleteProduct(string productId);
        Task<ServiceResult<ProductDto>> GetProductByIdAsync(string productId);
        Task<ServiceResult<List<ProductDto>>> GetProductsAsync();
        Task<ServiceResult<ProductDto>> UpdateProduct(ProductDto product);
    }
}