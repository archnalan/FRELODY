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
                    baseAmount);

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

        public string GetCountryCode()
        {
            try
            {
                // Get country code from user's regional settings
                var regionInfo = RegionInfo.CurrentRegion;

                // Extract the numeric country calling code
                // RegionInfo doesn't directly provide calling codes, so we map from TwoLetterISORegionName
                var countryCode = GetCallingCodeFromRegion(regionInfo.TwoLetterISORegionName);

                return countryCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine country code from culture settings, defaulting to Uganda (256)");
                // Fallback to Uganda if detection fails
                return "256";
            }
        }

        private string GetCallingCodeFromRegion(string twoLetterISOCode)
        {
            // Map common East African and global country codes
            // This is a simplified mapping - expand as needed
            var countryCallingCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // East Africa
        { "UG", "256" },  // Uganda
        { "KE", "254" },  // Kenya
        { "TZ", "255" },  // Tanzania
        { "RW", "250" },  // Rwanda
        { "BI", "257" },  // Burundi
        { "SS", "211" },  // South Sudan
        { "ET", "251" },  // Ethiopia
        
        // Other African countries
        { "NG", "234" },  // Nigeria
        { "ZA", "27" },   // South Africa
        { "GH", "233" },  // Ghana
        { "EG", "20" },   // Egypt
        
        // Major global markets
        { "US", "1" },    // United States
        { "GB", "44" },   // United Kingdom
        { "IN", "91" },   // India
        { "CN", "86" },   // China
        { "CA", "1" },    // Canada
        { "AU", "61" },   // Australia
    };

            return countryCallingCodes.TryGetValue(twoLetterISOCode, out var callingCode)
                ? callingCode
                : "256"; // Default to Uganda
        }
    }

}
