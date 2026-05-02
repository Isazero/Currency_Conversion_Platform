namespace CurrencyConversionPlatform.Models;
public sealed record HistoricalRateEntry(string Date, Dictionary<string, decimal> Rates);
