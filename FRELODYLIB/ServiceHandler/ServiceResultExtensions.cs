using FRELODYLIB.ServiceHandler.ResultModels;
using Microsoft.AspNetCore.Mvc;

namespace SongsWithChords.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ServiceResult<T> serviceResult)
        {
            if (serviceResult.IsSuccess)
            {
                return new OkObjectResult(serviceResult.Data);
            }

            // Let the global exception handler take care of the error
            throw serviceResult.Error;
        }

        public static async Task<IActionResult> ToActionResultAsync<T>(this Task<ServiceResult<T>> serviceResultTask)
        {
            var serviceResult = await serviceResultTask;
            return serviceResult.ToActionResult();
        }
    }
}