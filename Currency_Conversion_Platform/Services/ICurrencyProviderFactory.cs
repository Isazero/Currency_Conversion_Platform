namespace CurrencyConversionPlatform.Services;

public interface ICurrencyProviderFactory
{
    ICurrencyProvider GetProvider(string? providerName = null);
}
