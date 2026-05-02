namespace CurrencyConversionPlatform.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "CurrencyPlatform";
    public string Audience { get; init; } = "CurrencyPlatformUsers";
    public int ExpiryMinutes { get; init; } = 60;
}
