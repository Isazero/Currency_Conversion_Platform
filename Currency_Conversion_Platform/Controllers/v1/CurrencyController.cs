using Asp.Versioning;
using CurrencyConversionPlatform.Models;
using CurrencyConversionPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversionPlatform.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/currency")]
[Authorize]
public sealed class CurrencyController : ControllerBase
{
    private readonly CurrencyService _service;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(CurrencyService service, ILogger<CurrencyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("rates")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestRates(
        [FromQuery] string @base = "EUR", CancellationToken ct = default)
    {
        var rates = await _service.GetLatestRatesAsync(@base, ct);
        return Ok(rates);
    }

    [HttpGet("convert")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Convert(
        [FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.ConvertAsync(from, to, amount, ct);
            return Ok(result);
        }
        catch (ExcludedCurrencyException ex)
        {
            _logger.LogWarning("Excluded currency attempted: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<HistoricalRateEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoricalRates(
        [FromQuery] string @base = "EUR",
        [FromQuery] DateOnly startDate = default,
        [FromQuery] DateOnly endDate = default,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (startDate == default) startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
        if (endDate == default) endDate = DateOnly.FromDateTime(DateTime.Today);

        var result = await _service.GetHistoricalRatesAsync(@base, startDate, endDate, page, pageSize, ct);
        return Ok(result);
    }
}
