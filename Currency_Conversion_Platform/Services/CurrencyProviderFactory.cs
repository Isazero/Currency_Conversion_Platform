using CurrencyConversionPlatform.Services;

namespace Currency_Conversion_Platform.Services;

public sealed class CurrencyProviderFactory(IEnumerable<ICurrencyProvider> providers) : ICurrencyProviderFactory
{
    public ICurrencyProvider GetProvider(string? providerName = null)
    {
        if (providerName is null)
            return providers.First();

        var provider = providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new InvalidOperationException($"Provider '{providerName}' not found.");
    }
}
