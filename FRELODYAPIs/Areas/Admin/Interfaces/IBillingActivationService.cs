using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    /// <summary>
    /// Grants premium access after a successful payment by mapping the purchased
    /// product's billing period to a <c>BillingStatus</c> + expiry on the user.
    /// </summary>
    public interface IBillingActivationService
    {
        Task<ServiceResult<bool>> ActivatePremiumAsync(string userId, Product product);
    }
}
