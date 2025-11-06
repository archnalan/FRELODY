using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Models.OpenExchange;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "UsdExchangeRates";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

        public CurrencyConverter(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<ServiceResult<decimal>> ConvertCurrencyAsync(
            string fromCurrency,
            string toCurrency,
            decimal amount,
            RoundingFormat? roundingFormat = null)
        {
            fromCurrency = fromCurrency.ToUpperInvariant();
            toCurrency = toCurrency.ToUpperInvariant();

            var usdRatesResult = await GetUsdRatesAsync();
            if (!usdRatesResult.IsSuccess)
            {
                return ServiceResult<decimal>.Failure(new Exception("Failed to fetch USD rates."));
            }

            var usdRates = usdRatesResult.Data!;

            if (!usdRates.ContainsKey(fromCurrency) || !usdRates.ContainsKey(toCurrency))
            {
                return ServiceResult<decimal>.Failure(new Exception("One or both currencies not supported."));
            }

            decimal fromRate = usdRates[fromCurrency];
            decimal toRate = usdRates[toCurrency];
            decimal converted = amount * (toRate / fromRate);

            // rouding based on magnitude
            if (!roundingFormat.HasValue)
            {
                roundingFormat = DetermineRoundingByMagnitude(converted);
            }

            converted = ApplyRounding(converted, roundingFormat.Value);

            return ServiceResult<decimal>.Success(converted);
        }

        private RoundingFormat DetermineRoundingByMagnitude(decimal amount)
        {
            decimal absoluteAmount = Math.Abs(amount);

            // rounding thresholds
            if (absoluteAmount >= 10000) return RoundingFormat.Thousands;     // 11,999 → 12,000
            if (absoluteAmount >= 1000) return RoundingFormat.Hundreds;       // 1,199 → 1,200
            if (absoluteAmount >= 100) return RoundingFormat.Tens;            // 119 → 120
            if (absoluteAmount >= 10) return RoundingFormat.WholeNumber;      // 11.9 → 12
            if (absoluteAmount >= 1) return RoundingFormat.TwoDecimals;       // 1.199 → 1.29
            return RoundingFormat.TwoDecimals;                                // 0.1199 → 0.12
        }

        private decimal ApplyRounding(decimal amount, RoundingFormat roundingFormat)
        {
            return roundingFormat switch
            {
                RoundingFormat.WholeNumber => Math.Round(amount, 0, MidpointRounding.AwayFromZero),
                RoundingFormat.OneDecimal => Math.Round(amount, 1, MidpointRounding.AwayFromZero),
                RoundingFormat.TwoDecimals => Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                RoundingFormat.Tens => Math.Round(amount / 10, 0, MidpointRounding.AwayFromZero) * 10,
                RoundingFormat.Hundreds => Math.Round(amount / 100, 0, MidpointRounding.AwayFromZero) * 100,
                RoundingFormat.Thousands => Math.Round(amount / 1000, 0, MidpointRounding.AwayFromZero) * 1000,
                RoundingFormat.None or _ => amount
            };
        }

        // Your existing GetExchangeRatesAsync and GetUsdRatesAsync methods remain unchanged
        public async Task<ServiceResult<Dictionary<string, decimal>>> GetExchangeRatesAsync(string baseCurrency)
        {
            baseCurrency = baseCurrency.ToUpperInvariant();

            var usdRatesResult = await GetUsdRatesAsync();
            if (!usdRatesResult.IsSuccess)
            {
                return ServiceResult<Dictionary<string, decimal>>.Failure(
                    new Exception("Failed to fetch rates."));
            }

            var usdRates = usdRatesResult.Data!;

            if (!usdRates.ContainsKey(baseCurrency))
            {
                return ServiceResult<Dictionary<string, decimal>>.Failure(
                    new Exception($"Base currency '{baseCurrency}' not supported."));
            }

            if (baseCurrency == "USD")
            {
                return ServiceResult<Dictionary<string, decimal>>.Success(usdRates);
            }

            decimal baseRate = usdRates[baseCurrency];
            var adjustedRates = new Dictionary<string, decimal>();
            foreach (var kvp in usdRates)
            {
                adjustedRates[kvp.Key] = kvp.Value / baseRate;
            }
            adjustedRates[baseCurrency] = 1m;

            return ServiceResult<Dictionary<string, decimal>>.Success(adjustedRates);
        }

        private async Task<ServiceResult<Dictionary<string, decimal>>> GetUsdRatesAsync()
        {
            if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal> cachedRates))
            {
                return ServiceResult<Dictionary<string, decimal>>.Success(cachedRates);
            }

            try
            {
                string? baseUrl = _configuration["CurrencyConverter:LatestEndpointUrl"];
                string? appId = _configuration["CurrencyConverter:AppId"];

                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(appId))
                {
                    return ServiceResult<Dictionary<string, decimal>>.Failure(
                        new Exception("Currency API configuration missing."));
                }

                string apiUrl = $"{baseUrl}?app_id={appId}";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return ServiceResult<Dictionary<string, decimal>>.Failure(
                        new Exception($"API request failed: {response.StatusCode}"));
                }

                string json = await response.Content.ReadAsStringAsync();
                var rateData = JsonSerializer.Deserialize<RateResponseDto>(json);

                if (rateData?.Rates == null)
                {
                    return ServiceResult<Dictionary<string, decimal>>.Failure(
                        new Exception("Invalid API response."));
                }

                _cache.Set(CacheKey, rateData.Rates, CacheDuration);
                return ServiceResult<Dictionary<string, decimal>>.Success(rateData.Rates);
            }
            catch (Exception ex)
            {
                return ServiceResult<Dictionary<string, decimal>>.Failure(
                    new Exception($"Exception fetching rates: {ex.Message}"));
            }
        }
    }
}
