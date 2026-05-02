using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Currency_Conversion_Platform.Services;
using CurrencyConversionPlatform.Configuration;
using CurrencyConversionPlatform.Data;
using CurrencyConversionPlatform.Infrastructure;
using CurrencyConversionPlatform.Middleware;
using CurrencyConversionPlatform.Models;
using CurrencyConversionPlatform.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog
builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

// ── Configuration
builder.Services.Configure<FrankfurterOptions>(
    builder.Configuration.GetSection(FrankfurterOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// ── Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Caching
builder.Services.AddMemoryCache();

// ── HTTP Context
builder.Services.AddHttpContextAccessor();

// ── Resilient HTTP Client (Polly retry + circuit breaker)
builder.Services.AddTransient<CorrelationIdHandler>();
var frankfurterOpts = builder.Configuration
    .GetSection(FrankfurterOptions.SectionName)
    .Get<FrankfurterOptions>() ?? new FrankfurterOptions();

builder.Services
    .AddHttpClient(nameof(FrankfurterCurrencyProvider), client =>
    {
        client.BaseAddress = new Uri(frankfurterOpts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddResilienceHandler("frankfurter", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = frankfurterOpts.RetryCount,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = frankfurterOpts.CircuitBreakerFailureRatio,
            SamplingDuration = TimeSpan.FromSeconds(frankfurterOpts.CircuitBreakerSamplingSeconds),
            MinimumThroughput = frankfurterOpts.CircuitBreakerMinThroughput,
            BreakDuration = TimeSpan.FromSeconds(frankfurterOpts.CircuitBreakerBreakSeconds)
        });
    });

// ── Currency Services
builder.Services.AddScoped<FrankfurterCurrencyProvider>();
builder.Services.AddScoped<ICurrencyProvider>(sp =>
    new CachingCurrencyProvider(
        sp.GetRequiredService<FrankfurterCurrencyProvider>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FrankfurterOptions>>()));
builder.Services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();
builder.Services.AddScoped<CurrencyService>();

// ── Identity / Password Hashing
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ── JWT Auth
var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SecretKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// ── Rate Limiting
var rateLimitCfg = builder.Configuration.GetSection("RateLimit");
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("global", o =>
    {
        o.PermitLimit = rateLimitCfg.GetValue("PermitLimit", 100);
        o.Window = TimeSpan.FromSeconds(rateLimitCfg.GetValue("WindowSeconds", 60));
        o.QueueLimit = rateLimitCfg.GetValue("QueueLimit", 0);
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── API Versioning
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'VVV";
    opts.SubstituteApiVersionInUrl = true;
});

// ── Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Health Checks
builder.Services.AddHealthChecks();

// ── CORS (for React frontend)
builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Migrate DB + seed users
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Users.AnyAsync(u => u.Username == "admin"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var adminUser = new User { Username = "admin", Role = "Admin" };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");
        db.Users.Add(adminUser);

        var regularUser = new User { Username = "user", Role = "User" };
        regularUser.PasswordHash = hasher.HashPassword(regularUser, "User123!");
        db.Users.Add(regularUser);

        await db.SaveChangesAsync();
    }
}

// ── Middleware Pipeline
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("global");
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
