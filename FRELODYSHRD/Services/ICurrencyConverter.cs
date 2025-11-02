using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Services
{
    public interface ICurrencyConverter
    {
        Task<ServiceResult<Dictionary<string, decimal>>> GetExchangeRatesAsync(string baseCurrency);
        Task<ServiceResult<decimal>> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount, RoundingFormat? roundingFormat = RoundingFormat.None);
    }
}
