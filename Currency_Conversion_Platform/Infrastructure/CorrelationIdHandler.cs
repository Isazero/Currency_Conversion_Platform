namespace CurrencyConversionPlatform.Infrastructure;

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = accessor.HttpContext?.Items["CorrelationId"] as string
                            ?? Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}
