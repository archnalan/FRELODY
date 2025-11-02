using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Models;
using FRELODYSHRD.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class CurrencyDisplayService : ICurrencyDisplayService
    {
        private readonly ICurrencyConverter _currencyConverter;
        private readonly ILogger<CurrencyDisplayService> _logger;
        private string? _userCurrencyCode;

        public CurrencyDisplayService(ICurrencyConverter currencyConverter, ILogger<CurrencyDisplayService> logger)
        {
            _currencyConverter = currencyConverter;
            _logger = logger;
        }

        public async Task<ServiceResult<CurrencyDisplayInfo>> GetDisplayCurrencyAsync(decimal baseAmount, string baseCurrency = "UGX")
        {
            try
            {
                // Get user's regional currency
                var userCurrency = GetUserCurrency();
                _userCurrencyCode = userCurrency;

                // If user currency is same as base, no conversion needed
                if (userCurrency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<CurrencyDisplayInfo>.Success(new CurrencyDisplayInfo
                    {
                        Amount = baseAmount,
                        CurrencyCode = baseCurrency,
                        FormattedAmount = FormatCurrency(baseAmount, baseCurrency),
                        IsConverted = false
                    });
                }

                // Convert currency
                var conversionResult = await _currencyConverter.ConvertCurrencyAsync(
                    baseCurrency,
                    userCurrency,
                    baseAmount,
                    RoundingFormat.WholeNumber);

                if (!conversionResult.IsSuccess)
                {
                    _logger.LogWarning("Currency conversion failed, falling back to base currency");
                    return ServiceResult<CurrencyDisplayInfo>.Success(new CurrencyDisplayInfo
                    {
                        Amount = baseAmount,
                        CurrencyCode = baseCurrency,
                        FormattedAmount = FormatCurrency(baseAmount, baseCurrency),
                        IsConverted = false
                    });
                }

                return ServiceResult<CurrencyDisplayInfo>.Success(new CurrencyDisplayInfo
                {
                    Amount = conversionResult.Data,
                    CurrencyCode = userCurrency,
                    FormattedAmount = FormatCurrency(conversionResult.Data, userCurrency),
                    IsConverted = true,
                    OriginalAmount = baseAmount,
                    OriginalCurrency = baseCurrency
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting display currency");
                return ServiceResult<CurrencyDisplayInfo>.Failure(
                    new Exception("Failed to determine display currency"));
            }
        }

        public async Task<ServiceResult<decimal>> ConvertToUserCurrency(decimal amount, string fromCurrency = "UGX")
        {
            var userCurrency = GetUserCurrency();

            if (userCurrency.Equals(fromCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<decimal>.Success(amount);
            }

            return await _currencyConverter.ConvertCurrencyAsync(
                fromCurrency,
                userCurrency,
                amount);
        }

        public string FormatCurrency(decimal amount, string currencyCode)
        {
            try
            {
                var culture = GetCultureForCurrency(currencyCode);
                return amount.ToString("C", culture);
            }
            catch
            {
                // Fallback formatting
                return $"{currencyCode} {amount:N2}";
            }
        }

        private string GetUserCurrency()
        {
            try
            {
                // Get currency from user's regional settings
                var regionInfo = RegionInfo.CurrentRegion;
                return regionInfo.ISOCurrencySymbol;
            }
            catch
            {
                // Fallback to UGX if detection fails
                return "UGX";
            }
        }

        private CultureInfo GetCultureForCurrency(string currencyCode)
        {
            try
            {
                // Try to find a culture that uses this currency
                var culture = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                    .FirstOrDefault(c =>
                    {
                        try
                        {
                            var region = new RegionInfo(c.Name);
                            return region.ISOCurrencySymbol.Equals(currencyCode, StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    });

                return culture ?? CultureInfo.CurrentCulture;
            }
            catch
            {
                return CultureInfo.CurrentCulture;
            }
        }
    }

}
