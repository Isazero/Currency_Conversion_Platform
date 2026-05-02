namespace CurrencyConversionPlatform.Models;
public sealed record ConversionResponse(
    decimal Amount, string From, string To,
    decimal Rate, decimal ConvertedAmount, string Date);
