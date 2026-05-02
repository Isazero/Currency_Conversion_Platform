namespace CurrencyConversionPlatform.Infrastructure;

public sealed class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public CorrelationIdHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _accessor.HttpContext?.Items["CorrelationId"] as string
                            ?? Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}
