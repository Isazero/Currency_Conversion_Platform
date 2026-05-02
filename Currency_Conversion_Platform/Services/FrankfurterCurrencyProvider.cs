using System.Net.Http.Json;
using System.Text.Json;
using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CurrencyConversionPlatform.Services;

public sealed class FrankfurterCurrencyProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<FrankfurterOptions> options,
    ILogger<FrankfurterCurrencyProvider> logger)
    : ICurrencyProvider
{
    private readonly FrankfurterOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public string ProviderName => "Frankfurter";

    public async Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(nameof(FrankfurterCurrencyProvider));
        var url = $"{_options.BaseUrl}/latest?base={baseCurrency}";
        logger.LogDebug("Fetching latest rates for {Base} from {Url}", baseCurrency, url);
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>(JsonOptions, ct))!;
    }

    public async Task<ExchangeRatesResponse> ConvertAsync(string from, string to, decimal amount, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(nameof(FrankfurterCurrencyProvider));
        var url = $"{_options.BaseUrl}/latest?from={from}&to={to}&amount={amount}";
        logger.LogDebug("Converting {Amount} {From} to {To}", amount, from, to);
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>(JsonOptions, ct))!;
    }

    public async Task<HistoricalRatesResponse> GetHistoricalRatesAsync(
        string baseCurrency, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(nameof(FrankfurterCurrencyProvider));
        var url = $"{_options.BaseUrl}/{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?base={baseCurrency}";
        logger.LogDebug("Fetching historical rates for {Base} from {Start} to {End}", baseCurrency, startDate, endDate);
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HistoricalRatesResponse>(JsonOptions, ct))!;
    }
}
