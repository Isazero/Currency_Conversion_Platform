using CurrencyConversionPlatform.Models;
using CurrencyConversionPlatform.Services;
using Moq;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Unit.Services;

public class CurrencyServiceTests
{
    private static (CurrencyService service, Mock<ICurrencyProvider> providerMock) CreateService()
    {
        var providerMock = new Mock<ICurrencyProvider>();
        var factoryMock = new Mock<ICurrencyProviderFactory>();
        factoryMock.Setup(f => f.GetProvider(null)).Returns(providerMock.Object);
        var service = new CurrencyService(factoryMock.Object);
        return (service, providerMock);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ReturnsMappedResponse()
    {
        var (service, providerMock) = CreateService();
        providerMock.Setup(p => p.GetLatestRatesAsync("EUR", default))
            .ReturnsAsync(new ExchangeRatesResponse(1m, "EUR", "2024-01-15",
                new Dictionary<string, decimal> { ["USD"] = 1.09m }));

        var result = await service.GetLatestRatesAsync("EUR");

        Assert.Equal("EUR", result.Base);
        Assert.True(result.Rates.ContainsKey("USD"));
    }

    [Theory]
    [InlineData("TRY", "USD")]
    [InlineData("USD", "PLN")]
    [InlineData("THB", "EUR")]
    [InlineData("EUR", "MXN")]
    public async Task ConvertAsync_ThrowsExcludedCurrencyException_ForExcludedCurrencies(string from, string to)
    {
        var (service, _) = CreateService();
        var ex = await Assert.ThrowsAsync<ExcludedCurrencyException>(
            () => service.ConvertAsync(from, to, 100m));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsConversionResponse_ForValidCurrencies()
    {
        var (service, providerMock) = CreateService();
        providerMock.Setup(p => p.ConvertAsync("EUR", "USD", 100m, default))
            .ReturnsAsync(new ExchangeRatesResponse(100m, "EUR", "2024-01-15",
                new Dictionary<string, decimal> { ["USD"] = 109.43m }));

        var result = await service.ConvertAsync("EUR", "USD", 100m);

        Assert.Equal("EUR", result.From);
        Assert.Equal("USD", result.To);
        Assert.Equal(109.43m, result.ConvertedAmount);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsPaginatedResults()
    {
        var (service, providerMock) = CreateService();
        var rates = new Dictionary<string, Dictionary<string, decimal>>
        {
            ["2020-01-02"] = new() { ["USD"] = 1.1174m },
            ["2020-01-03"] = new() { ["USD"] = 1.1165m },
            ["2020-01-06"] = new() { ["USD"] = 1.1134m },
        };
        providerMock.Setup(p => p.GetHistoricalRatesAsync("EUR",
                new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), default))
            .ReturnsAsync(new HistoricalRatesResponse(1m, "EUR", "2020-01-01", "2020-01-31", rates));

        var result = await service.GetHistoricalRatesAsync("EUR",
            new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), page: 1, pageSize: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsSecondPage()
    {
        var (service, providerMock) = CreateService();
        var rates = Enumerable.Range(1, 5)
            .ToDictionary(i => $"2020-01-0{i}", i => new Dictionary<string, decimal> { ["USD"] = 1.1m });
        providerMock.Setup(p => p.GetHistoricalRatesAsync(It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync(new HistoricalRatesResponse(1m, "EUR", "2020-01-01", "2020-01-05", rates));

        var result = await service.GetHistoricalRatesAsync("EUR",
            new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 5), page: 2, pageSize: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }
}
