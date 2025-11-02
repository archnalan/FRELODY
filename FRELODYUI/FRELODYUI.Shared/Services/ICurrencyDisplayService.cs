using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public interface ICurrencyDisplayService
    {
        Task<ServiceResult<CurrencyDisplayInfo>> GetDisplayCurrencyAsync(decimal baseAmount, string baseCurrency = "UGX");
        string FormatCurrency(decimal amount, string currencyCode);
        Task<ServiceResult<decimal>> ConvertToUserCurrency(decimal amount, string fromCurrency = "UGX");
        string GetCountryCode();
    }
}
