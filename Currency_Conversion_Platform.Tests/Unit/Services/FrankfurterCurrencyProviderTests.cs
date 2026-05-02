using System.Net;
using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Unit.Services;

public class FrankfurterCurrencyProviderTests
{
    private static FrankfurterCurrencyProvider CreateProvider(HttpMessageHandler handler)
    {
        var options = Options.Create(new FrankfurterOptions { BaseUrl = "https://api.frankfurter.app" });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(nameof(FrankfurterCurrencyProvider))).Returns(client);
        var logger = new Mock<ILogger<FrankfurterCurrencyProvider>>();
        return new FrankfurterCurrencyProvider(factory.Object, options, logger.Object);
    }

    private static HttpMessageHandler FakeHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        return mock.Object;
    }

    [Fact]
    public async Task GetLatestRatesAsync_ReturnsRates_WhenApiRespondsOk()
    {
        var json = """{"amount":1.0,"base":"EUR","date":"2024-01-15","rates":{"USD":1.0943,"GBP":0.8586}}""";
        var provider = CreateProvider(FakeHandler(json));
        var result = await provider.GetLatestRatesAsync("EUR");
        Assert.Equal("EUR", result.Base);
        Assert.True(result.Rates.ContainsKey("USD"));
        Assert.Equal(1.0943m, result.Rates["USD"]);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsConversionResult_WhenApiRespondsOk()
    {
        var json = """{"amount":100.0,"base":"EUR","date":"2024-01-15","rates":{"USD":109.43}}""";
        var provider = CreateProvider(FakeHandler(json));
        var result = await provider.ConvertAsync("EUR", "USD", 100m);
        Assert.Equal("EUR", result.Base);
        Assert.True(result.Rates.ContainsKey("USD"));
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsHistoricalData()
    {
        var json = """{"amount":1.0,"base":"EUR","start_date":"2020-01-02","end_date":"2020-01-03","rates":{"2020-01-02":{"USD":1.1174},"2020-01-03":{"USD":1.1165}}}""";
        var provider = CreateProvider(FakeHandler(json));
        var result = await provider.GetHistoricalRatesAsync("EUR", new DateOnly(2020, 1, 2), new DateOnly(2020, 1, 3));
        Assert.Equal(2, result.Rates.Count);
        Assert.True(result.Rates.ContainsKey("2020-01-02"));
    }

    [Fact]
    public async Task GetLatestRatesAsync_ThrowsHttpRequestException_OnNon200()
    {
        var provider = CreateProvider(FakeHandler("{}", HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetLatestRatesAsync("EUR"));
    }
}
