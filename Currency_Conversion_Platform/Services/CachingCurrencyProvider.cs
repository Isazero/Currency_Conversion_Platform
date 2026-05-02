using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CurrencyConversionPlatform.Services;

public sealed class CachingCurrencyProvider : ICurrencyProvider
{
    private readonly ICurrencyProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public string ProviderName => _inner.ProviderName;

    public CachingCurrencyProvider(
        ICurrencyProvider inner,
        IMemoryCache cache,
        IOptions<FrankfurterOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _cacheDuration = TimeSpan.FromSeconds(options.Value.CacheDurationSeconds);
    }

    public Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        var key = $"latest:{baseCurrency}";
        return _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return _inner.GetLatestRatesAsync(baseCurrency, ct);
        })!;
    }

    public Task<ExchangeRatesResponse> ConvertAsync(string from, string to, decimal amount, CancellationToken ct = default)
    {
        var key = $"convert:{from}:{to}:{amount}";
        return _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return _inner.ConvertAsync(from, to, amount, ct);
        })!;
    }

    public Task<HistoricalRatesResponse> GetHistoricalRatesAsync(
        string baseCurrency, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var key = $"history:{baseCurrency}:{startDate}:{endDate}";
        return _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return _inner.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, ct);
        })!;
    }
}
