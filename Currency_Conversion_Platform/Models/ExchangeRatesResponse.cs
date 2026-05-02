namespace CurrencyConversionPlatform.Models;
public sealed record ExchangeRatesResponse(
    decimal Amount, string Base, string Date,
    Dictionary<string, decimal> Rates);
