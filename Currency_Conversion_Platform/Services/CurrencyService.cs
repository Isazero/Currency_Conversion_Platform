using CurrencyConversionPlatform.Models;

namespace CurrencyConversionPlatform.Services;

public class CurrencyService(ICurrencyProviderFactory factory)
{
    private static readonly HashSet<string> ExcludedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "TRY", "PLN", "THB", "MXN" };

    public virtual async Task<ExchangeRatesResponse> GetLatestRatesAsync(
        string baseCurrency, CancellationToken ct = default)
    {
        return await factory.GetProvider().GetLatestRatesAsync(baseCurrency, ct);
    }

    public virtual async Task<ConversionResponse> ConvertAsync(
        string from, string to, decimal amount, CancellationToken ct = default)
    {
        if (ExcludedCurrencies.Contains(from)) throw new ExcludedCurrencyException(from);
        if (ExcludedCurrencies.Contains(to)) throw new ExcludedCurrencyException(to);

        var rates = await factory.GetProvider().ConvertAsync(from, to, amount, ct);
        var convertedAmount = rates.Rates.TryGetValue(to, out var rate) ? rate : 0m;
        var rateValue = amount > 0 ? convertedAmount / amount : 0m;

        return new ConversionResponse(amount, from, to, rateValue, convertedAmount, rates.Date);
    }

    public virtual async Task<PagedResponse<HistoricalRateEntry>> GetHistoricalRatesAsync(
        string baseCurrency, DateOnly startDate, DateOnly endDate,
        int page, int pageSize, CancellationToken ct = default)
    {
        var historical = await factory.GetProvider().GetHistoricalRatesAsync(baseCurrency, startDate, endDate, ct);

        var allEntries = historical.Rates
            .OrderBy(kv => kv.Key)
            .Select(kv => new HistoricalRateEntry(kv.Key, kv.Value))
            .ToList();

        var totalItems = allEntries.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var items = allEntries.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<HistoricalRateEntry>(items, page, pageSize, totalItems, totalPages);
    }
}
