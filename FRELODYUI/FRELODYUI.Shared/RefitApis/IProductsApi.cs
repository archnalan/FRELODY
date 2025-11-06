using Refit;
using FRELODYSHRD.Dtos.PesaPalDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IProductsApi
    {
        //GetProducts
        [Get("/api/products/get-products")]
        Task<IApiResponse<List<ProductDto>>> GetProducts();

        //GetProductById
        [Get("/api/products/get-product-by-id")]
        Task<IApiResponse<ProductDto>> GetProductById([Query] string productId);

        //AddProduct
        [Post("/api/products/add-product")]
        Task<IApiResponse<ProductDto>> AddProduct([Body] ProductDto product);

        //UpdateProduct
        [Put("/api/products/update-product")]
        Task<IApiResponse<ProductDto>> UpdateProduct([Body] ProductDto product);

        //DeleteProduct
        [Delete("/api/products/delete-product")]
        Task<IApiResponse<bool>> DeleteProduct([Query] string productId);
    }
}
