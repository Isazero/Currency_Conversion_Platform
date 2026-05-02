namespace CurrencyConversionPlatform.Configuration;

public sealed class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";
    public string BaseUrl { get; init; } = "https://api.frankfurter.app";
    public int CacheDurationSeconds { get; init; } = 300;
    public int RetryCount { get; init; } = 3;
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;
    public int CircuitBreakerSamplingSeconds { get; init; } = 60;
    public int CircuitBreakerMinThroughput { get; init; } = 5;
    public int CircuitBreakerBreakSeconds { get; init; } = 30;
}
