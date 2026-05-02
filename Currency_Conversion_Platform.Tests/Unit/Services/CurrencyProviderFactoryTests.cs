using CurrencyConversionPlatform.Services;
using Moq;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Unit.Services;

public class CurrencyProviderFactoryTests
{
    [Fact]
    public void GetProvider_ReturnsFirst_WhenNameIsNull()
    {
        var provider1 = new Mock<ICurrencyProvider>();
        provider1.Setup(p => p.ProviderName).Returns("First");
        var provider2 = new Mock<ICurrencyProvider>();
        provider2.Setup(p => p.ProviderName).Returns("Second");

        var factory = new CurrencyProviderFactory(new[] { provider1.Object, provider2.Object });
        var result = factory.GetProvider(null);

        Assert.Equal("First", result.ProviderName);
    }

    [Fact]
    public void GetProvider_FindsByName_CaseInsensitive()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var factory = new CurrencyProviderFactory(new[] { provider.Object });
        var result = factory.GetProvider("frankfurter");

        Assert.Equal("Frankfurter", result.ProviderName);
    }

    [Fact]
    public void GetProvider_ThrowsInvalidOperation_ForUnknownName()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var factory = new CurrencyProviderFactory(new[] { provider.Object });

        Assert.Throws<InvalidOperationException>(() => factory.GetProvider("Unknown"));
    }
}
