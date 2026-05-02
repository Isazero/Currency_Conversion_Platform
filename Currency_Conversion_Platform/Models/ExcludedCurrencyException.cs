namespace CurrencyConversionPlatform.Models;
public sealed class ExcludedCurrencyException(string currency) : Exception($"Currency '{currency}' is not supported for conversion.");
