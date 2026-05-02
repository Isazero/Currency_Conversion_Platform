using CurrencyConversionPlatform.Models;

namespace CurrencyConversionPlatform.Services;

public interface ICurrencyProvider
{
    string ProviderName { get; }
    Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default);
    Task<ExchangeRatesResponse> ConvertAsync(string from, string to, decimal amount, CancellationToken ct = default);
    Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
}
