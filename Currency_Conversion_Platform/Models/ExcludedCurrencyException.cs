namespace CurrencyConversionPlatform.Models;
public sealed class ExcludedCurrencyException : Exception
{
    public ExcludedCurrencyException(string currency)
        : base($"Currency '{currency}' is not supported for conversion.") { }
}
