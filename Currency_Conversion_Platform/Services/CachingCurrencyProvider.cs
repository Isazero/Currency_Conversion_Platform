using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CurrencyConversionPlatform.Services;

public sealed class CachingCurrencyProvider(
    ICurrencyProvider inner,
    IMemoryCache cache,
    IOptions<FrankfurterOptions> options) : ICurrencyProvider
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(options.Value.CacheDurationSeconds);

    public string ProviderName => inner.ProviderName;

    public Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        var key = $"latest:{baseCurrency}";
        return cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return inner.GetLatestRatesAsync(baseCurrency, ct);
        })!;
    }

    public Task<ExchangeRatesResponse> ConvertAsync(string from, string to, decimal amount, CancellationToken ct = default)
    {
        var key = $"convert:{from}:{to}:{amount}";
        return cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return inner.ConvertAsync(from, to, amount, ct);
        })!;
    }

    public Task<HistoricalRatesResponse> GetHistoricalRatesAsync(
        string baseCurrency, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var key = $"history:{baseCurrency}:{startDate}:{endDate}";
        return cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return inner.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, ct);
        })!;
    }
}
