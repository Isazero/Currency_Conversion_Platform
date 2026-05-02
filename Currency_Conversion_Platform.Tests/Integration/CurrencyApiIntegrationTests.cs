using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyConversionPlatform.Data;
using CurrencyConversionPlatform.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Currency_Conversion_Platform.Tests.Integration;

public class CurrencyApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CurrencyApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseSqlite("Data Source=integration_test.db"));
            });
        });
    }

    private async Task<string> GetTokenAsync(HttpClient client, string username = "admin", string password = "Admin123!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/token", new AuthRequest(username, password));
        response.EnsureSuccessStatusCode();
        var tokenResp = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResp!.Token;
    }

    [Fact]
    public async Task Health_Returns200_WithoutAuth()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthToken_Returns200_WithValidCredentials()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/token",
            new AuthRequest("admin", "Admin123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Bearer", body.TokenType);
    }

    [Fact]
    public async Task AuthToken_Returns401_WithInvalidPassword()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/token",
            new AuthRequest("admin", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthToken_Returns401_WithUnknownUser()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/token",
            new AuthRequest("nobody", "password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRates_Returns401_WithoutToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/currency/rates?base=EUR");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Convert_Returns400_ForExcludedCurrencyTRY()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/currency/convert?from=TRY&to=USD&amount=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Convert_Returns400_ForExcludedCurrencyPLN()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/currency/convert?from=EUR&to=PLN&amount=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetRates_ReturnsCorrelationIdHeader()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "test-corr-123");

        var response = await client.GetAsync("/api/v1/currency/rates?base=EUR");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("test-corr-123", response.Headers.GetValues("X-Correlation-ID").First());
    }

    [Fact]
    public async Task AdminInfo_Returns200_ForAdminRole()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "admin", "Admin123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminInfo_Returns403_ForUserRole()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "user", "User123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/info");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
