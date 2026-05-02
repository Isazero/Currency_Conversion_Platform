using CurrencyConversionPlatform.Controllers.v1;
using CurrencyConversionPlatform.Models;
using CurrencyConversionPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Unit.Controllers;

public class CurrencyControllerTests
{
    private static (CurrencyController ctrl, Mock<CurrencyService> svcMock) CreateController()
    {
        var factoryMock = new Mock<ICurrencyProviderFactory>();
        var svcMock = new Mock<CurrencyService>(factoryMock.Object);
        var logger = new Mock<ILogger<CurrencyController>>();
        var ctrl = new CurrencyController(svcMock.Object, logger.Object);
        return (ctrl, svcMock);
    }

    [Fact]
    public async Task GetLatestRates_Returns200_WithRates()
    {
        var (ctrl, svc) = CreateController();
        svc.Setup(s => s.GetLatestRatesAsync("EUR", default))
            .ReturnsAsync(new ExchangeRatesResponse(1m, "EUR", "2024-01-15",
                new Dictionary<string, decimal> { ["USD"] = 1.09m }));

        var result = await ctrl.GetLatestRates("EUR");

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<ExchangeRatesResponse>(ok.Value);
        Assert.Equal("EUR", data.Base);
    }

    [Fact]
    public async Task Convert_Returns400_ForExcludedCurrency()
    {
        var (ctrl, svc) = CreateController();
        svc.Setup(s => s.ConvertAsync("TRY", "USD", 100m, default))
            .ThrowsAsync(new ExcludedCurrencyException("TRY"));

        var result = await ctrl.Convert("TRY", "USD", 100m);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Convert_Returns200_ForValidCurrencies()
    {
        var (ctrl, svc) = CreateController();
        svc.Setup(s => s.ConvertAsync("EUR", "USD", 100m, default))
            .ReturnsAsync(new ConversionResponse(100m, "EUR", "USD", 1.09m, 109m, "2024-01-15"));

        var result = await ctrl.Convert("EUR", "USD", 100m);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<ConversionResponse>(ok.Value);
        Assert.Equal(109m, data.ConvertedAmount);
    }

    [Fact]
    public async Task GetHistoricalRates_Returns200_WithPagedResults()
    {
        var (ctrl, svc) = CreateController();
        var entries = new List<HistoricalRateEntry>
        {
            new("2020-01-02", new Dictionary<string, decimal> { ["USD"] = 1.11m })
        };
        svc.Setup(s => s.GetHistoricalRatesAsync("EUR",
                new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), 1, 10, default))
            .ReturnsAsync(new PagedResponse<HistoricalRateEntry>(entries, 1, 10, 1, 1));

        var result = await ctrl.GetHistoricalRates("EUR", new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), 1, 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<PagedResponse<HistoricalRateEntry>>(ok.Value);
        Assert.Single(data.Items);
    }
}
