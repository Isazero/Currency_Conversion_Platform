namespace CurrencyConversionPlatform.Models;
public sealed record HistoricalRatesResponse(
    decimal Amount, string Base, string StartDate, string EndDate,
    Dictionary<string, Dictionary<string, decimal>> Rates);
