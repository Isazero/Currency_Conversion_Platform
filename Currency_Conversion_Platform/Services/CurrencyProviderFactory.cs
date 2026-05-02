namespace CurrencyConversionPlatform.Services;

public sealed class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly IEnumerable<ICurrencyProvider> _providers;

    public CurrencyProviderFactory(IEnumerable<ICurrencyProvider> providers)
    {
        _providers = providers;
    }

    public ICurrencyProvider GetProvider(string? providerName = null)
    {
        if (providerName is null)
            return _providers.First();

        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new InvalidOperationException($"Provider '{providerName}' not found.");
    }
}
