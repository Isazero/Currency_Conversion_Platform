using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Models;
using CurrencyConversionPlatform.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Unit.Services;

public class CachingCurrencyProviderTests
{
    private static (CachingCurrencyProvider caching, Mock<ICurrencyProvider> innerMock) Create(int cacheSecs = 300)
    {
        var innerMock = new Mock<ICurrencyProvider>();
        innerMock.Setup(p => p.ProviderName).Returns("Frankfurter");
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new FrankfurterOptions { CacheDurationSeconds = cacheSecs });
        return (new CachingCurrencyProvider(innerMock.Object, cache, options), innerMock);
    }

    [Fact]
    public void ProviderName_DelegatesToInner()
    {
        var (caching, _) = Create();
        Assert.Equal("Frankfurter", caching.ProviderName);
    }

    [Fact]
    public async Task GetLatestRatesAsync_CallsInner_OnFirstCall()
    {
        var (caching, inner) = Create();
        inner.Setup(p => p.GetLatestRatesAsync("EUR", default))
            .ReturnsAsync(new ExchangeRatesResponse(1m, "EUR", "2024-01-01", new() { ["USD"] = 1.1m }));

        var result = await caching.GetLatestRatesAsync("EUR");

        Assert.Equal("EUR", result.Base);
        inner.Verify(p => p.GetLatestRatesAsync("EUR", default), Times.Once);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ReturnsCachedResult_OnSecondCall()
    {
        var (caching, inner) = Create();
        inner.Setup(p => p.GetLatestRatesAsync("EUR", default))
            .ReturnsAsync(new ExchangeRatesResponse(1m, "EUR", "2024-01-01", new() { ["USD"] = 1.1m }));

        await caching.GetLatestRatesAsync("EUR");
        await caching.GetLatestRatesAsync("EUR");

        inner.Verify(p => p.GetLatestRatesAsync("EUR", default), Times.Once);
    }

    [Fact]
    public async Task ConvertAsync_CallsInner_OnFirstCall()
    {
        var (caching, inner) = Create();
        inner.Setup(p => p.ConvertAsync("EUR", "USD", 100m, default))
            .ReturnsAsync(new ExchangeRatesResponse(100m, "EUR", "2024-01-01", new() { ["USD"] = 109m }));

        var result = await caching.ConvertAsync("EUR", "USD", 100m);

        Assert.True(result.Rates.ContainsKey("USD"));
        inner.Verify(p => p.ConvertAsync("EUR", "USD", 100m, default), Times.Once);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsCachedResult_OnSecondCall()
    {
        var (caching, inner) = Create();
        inner.Setup(p => p.ConvertAsync("EUR", "USD", 100m, default))
            .ReturnsAsync(new ExchangeRatesResponse(100m, "EUR", "2024-01-01", new() { ["USD"] = 109m }));

        await caching.ConvertAsync("EUR", "USD", 100m);
        await caching.ConvertAsync("EUR", "USD", 100m);

        inner.Verify(p => p.ConvertAsync("EUR", "USD", 100m, default), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_CallsInner_OnFirstCall()
    {
        var (caching, inner) = Create();
        var start = new DateOnly(2020, 1, 1);
        var end = new DateOnly(2020, 1, 31);
        inner.Setup(p => p.GetHistoricalRatesAsync("EUR", start, end, default))
            .ReturnsAsync(new HistoricalRatesResponse(1m, "EUR", "2020-01-01", "2020-01-31",
                new() { ["2020-01-02"] = new() { ["USD"] = 1.1m } }));

        var result = await caching.GetHistoricalRatesAsync("EUR", start, end);

        Assert.Single(result.Rates);
        inner.Verify(p => p.GetHistoricalRatesAsync("EUR", start, end, default), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsCachedResult_OnSecondCall()
    {
        var (caching, inner) = Create();
        var start = new DateOnly(2020, 1, 1);
        var end = new DateOnly(2020, 1, 31);
        inner.Setup(p => p.GetHistoricalRatesAsync("EUR", start, end, default))
            .ReturnsAsync(new HistoricalRatesResponse(1m, "EUR", "2020-01-01", "2020-01-31",
                new() { ["2020-01-02"] = new() { ["USD"] = 1.1m } }));

        await caching.GetHistoricalRatesAsync("EUR", start, end);
        await caching.GetHistoricalRatesAsync("EUR", start, end);

        inner.Verify(p => p.GetHistoricalRatesAsync("EUR", start, end, default), Times.Once);
    }
}
