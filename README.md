# Currency Conversion Platform

A full-stack currency conversion platform built with **ASP.NET Core 10** (backend) and **React + TypeScript** (frontend), sourcing live exchange rate data from the [Frankfurter API](https://api.frankfurter.app/).

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Setup Instructions](#setup-instructions)
- [Running Tests](#running-tests)
- [CI/CD Readiness](#cicd-readiness)
- [AI Usage](#ai-usage)
- [Assumptions & Trade-offs](#assumptions--trade-offs)
- [Potential Future Improvements](#potential-future-improvements)

---

## Architecture Overview

```
┌─────────────────────┐        ┌──────────────────────────────────┐
│   React Frontend     │  HTTP  │       ASP.NET Core Web API        │
│  (Vite + shadcn/ui)  │◄──────►│  /api/v1/auth, /currency/...     │
└─────────────────────┘        └────────────┬─────────────────────┘
                                             │
                                   ┌─────────▼─────────┐
                                   │  CachingCurrency   │
                                   │    Provider        │
                                   │  (decorator)       │
                                   └─────────┬──────────┘
                                             │  cache miss
                                   ┌─────────▼──────────┐
                                   │ FrankfurterCurrency │
                                   │    Provider         │
                                   │  (Polly: retry +    │
                                   │  circuit breaker)   │
                                   └─────────┬───────────┘
                                             │
                                   ┌─────────▼──────────┐
                                   │  Frankfurter API    │
                                   │  (external)         │
                                   └────────────────────┘
```

### Backend

- **ASP.NET Core 10 Web API** — versioned via `Asp.Versioning.Mvc` (`/api/v1/`)
- **Provider pattern** — `ICurrencyProvider` interface with `FrankfurterCurrencyProvider` implementation; `CachingCurrencyProvider` wraps it as a decorator
- **Factory pattern** — `ICurrencyProviderFactory` resolves providers by name, making it trivial to add new exchange rate sources
- **Resilience** — `Microsoft.Extensions.Http.Resilience` (Polly 8): exponential backoff retry + circuit breaker on the Frankfurter HTTP client
- **Caching** — `IMemoryCache` with configurable TTL; cache key includes base currency and date range
- **Auth** — SQLite + EF Core users table, `IPasswordHasher<User>`, JWT Bearer tokens with role claims (`Admin` / `User`)
- **Rate limiting** — ASP.NET Core built-in fixed-window limiter
- **Observability** — Serilog structured logging; custom `RequestLoggingMiddleware` records client IP, client ID (from JWT), method, path, status, and response time per request; `CorrelationIdHandler` propagates `X-Correlation-ID` to all Frankfurter calls

### Frontend

- **Vite + React 18 + TypeScript** — SPA with React Router v6
- **shadcn/ui + Tailwind CSS v4** — component library
- **Auth flow** — JWT stored in `localStorage`; `ProtectedRoute` wraps all authenticated pages
- **Pages** — `/convert`, `/rates`, `/history` (all protected), `/login` (public)
- **API layer** — typed fetch wrapper (`src/api/client.ts`) that attaches the Bearer header and handles 401 globally
- **State** — local React state + custom hooks (`useRates`, `useConvert`, `useHistory`)

### Data persistence

SQLite via EF Core — chosen for zero-infrastructure setup in this assessment. Swapping to PostgreSQL requires only a one-line change in `Program.cs` (`opts.UseNpgsql(...)`).

---

## Setup Instructions

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- (Optional) [Docker](https://www.docker.com/) for containerised run

---

### Backend

```bash
# 1. Clone the repo
git clone <repo-url>
cd Currency_Conversion_Platform

# 2. Set the JWT secret (dev default works out of the box)
#    For production, set via environment variable:
#    export Jwt__SecretKey="your-32+-char-secret"

# 3. Run — EF migrations and seed users run automatically on startup
cd Currency_Conversion_Platform
dotnet run
```

API is available at `http://localhost:5000`.  
Swagger UI: `http://localhost:5000/swagger`

**Seed users (dev only):**

| Username | Password  | Role  |
|----------|-----------|-------|
| admin    | Admin123! | Admin |
| user     | User123!  | User  |

---

### Frontend

```bash
cd currency-frontend
npm install
npm run dev
```

Frontend is available at `http://localhost:5173` (or next available port).  
The Vite dev server proxies `/api` → `http://localhost:5000`.

---

### Docker (full stack)

```bash
docker compose up --build
```

| Service  | URL                        |
|----------|----------------------------|
| API      | http://localhost:8080       |
| Frontend | http://localhost:3000       |

---

### Environment configuration

| File                             | Purpose                          |
|----------------------------------|----------------------------------|
| `appsettings.json`               | Base defaults                    |
| `appsettings.Development.json`   | Short cache TTL, debug logging   |
| `appsettings.Production.json`    | Warn-level logging, longer cache |

All sensitive values (`Jwt__SecretKey`, `ConnectionStrings__DefaultConnection`) are overridable via environment variables — no secrets are committed.

---

## Running Tests

```bash
# All tests
dotnet test

# With coverage report (HTML in TestResults/CoverageReport/)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
```

**Backend coverage: 99%** across services, providers, and controllers.

Tests include:
- Unit tests for `CurrencyService`, `FrankfurterCurrencyProvider`, `CachingCurrencyProvider`, `CurrencyProviderFactory`, `CurrencyController`
- Integration tests via `WebApplicationFactory<Program>` covering auth, excluded currencies, correlation ID header propagation, and 401 enforcement

---

## CI/CD Readiness

The project is structured for straightforward CI/CD integration:

- `dotnet build` — compiles cleanly with 0 errors
- `dotnet test` — runs all 34 tests; add `--collect:"XPlat Code Coverage"` for coverage gate
- `docker compose build` — produces production images
- All secrets are environment-variable-driven — no hardcoded credentials in committed files
- Three environment configs (`Development` / `Production` + base) map naturally to CI/CD pipeline stages

A GitHub Actions workflow would look like:

```yaml
# .github/workflows/ci.yml
on: [push, pull_request]
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --collect:"XPlat Code Coverage"
  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd currency-frontend && npm ci && npm run build
```

---

## AI Usage

**Tool used:** Claude Code (claude-sonnet-4-6) via the Anthropic CLI.

### How AI was used

AI was used throughout the project as a **collaborative implementation assistant**, not as an autonomous code generator. The workflow was driven by structured back-and-forth:

- **Architecture decisions** were discussed first — I described the requirements, proposed the approach (factory + decorator pattern, Polly via `Microsoft.Extensions.Http.Resilience`, SQLite for simplicity), and asked AI to validate or challenge the design before any code was written.
- **Implementation** — once a design was agreed upon, AI generated code within the constraints I defined. For each piece (providers, services, controllers, middleware), I reviewed the output, explained what was wrong or what needed adjusting, and directed the next iteration.
- **Debugging** — when issues arose (Swashbuckle namespace conflicts with OpenApi 2.x, Tailwind v3/v4 mismatch with shadcn, TypeScript `erasableSyntaxOnly` incompatibility with constructor parameter properties), I diagnosed the root cause myself and instructed AI on the specific fix rather than accepting its first suggestion.
- **Test generation** — AI generated test scaffolding based on the service contracts I defined. I reviewed every test for correctness, added edge cases (second-page pagination, zero-amount conversion), and caught cases where mocked return values didn't match actual method signatures.
- **Refactoring** — primary constructor refactoring (C# 12) was directed by me once the codebase was stable, to reduce boilerplate without changing behaviour.

### What I validated or changed manually

- **Dependency selection** — AI initially suggested `Microsoft.AspNetCore.Identity.Core` for password hashing; I narrowed this to just `IPasswordHasher<T>` to avoid pulling in the full Identity stack, which was unnecessary for this use case.
- **Swagger security** — AI's initial `Program.cs` setup used `AddSwaggerGen()` without JWT Bearer definition; I specifically requested the `AddSecurityDefinition` + `AddSecurityRequirement` setup so the Swagger UI is actually usable for testing authenticated endpoints.
- **Integration test database** — AI's first integration test setup attempted to use a shared in-memory SQLite connection; I identified the seeding race condition and directed the fix to ensure migrations and seed data run within the test scope.
- **Frontend error handling** — AI's initial `useConvert` hook swallowed all errors into a generic message; I directed it to extract the `error` field from the 400 response body specifically, so the excluded-currency message from the backend is surfaced directly to the user.
- **gitignore** — the initial commit accidentally included `.idea/`, `TestResults/`, the SQLite dev database, and log files; I caught this, diagnosed the root cause (missing Node/IDE/runtime entries), and directed the corrective commit.

### What I did not blindly accept

- AI suggested adding XML doc comments (`<summary>`) to all public methods. I rejected this — the codebase is small and well-named; comments would have added noise without value.
- AI initially placed `[Authorize(Roles = "Admin")]` on all currency endpoints. I removed this because both roles need read access to rates and conversion; RBAC differentiation only makes sense if there's a write/admin operation, which this API doesn't have.
- AI proposed a `RefreshToken` flow for the frontend. I rejected it — unnecessary complexity for this scope; the JWT expiry is 24h in dev and the task doesn't require it.
- AI suggested `React Query` for server state management. I rejected it in favour of simple custom hooks (`useRates`, `useConvert`, `useHistory`) to keep the dependency footprint small and demonstrate that I understand React state patterns directly.

---

## Assumptions & Trade-offs

| Decision | Rationale |
|----------|-----------|
| **SQLite over PostgreSQL** | Zero-infrastructure setup for the assessment. Production swap is a one-liner (`UseNpgsql`). Noted as a simplification in the interest of submission time. |
| **In-memory JWT auth (no user management)** | The task requires JWT + RBAC but not a full user registration flow. Two seed users (`admin`, `user`) demonstrate the auth and role model. Documented as a trade-off. |
| **`IMemoryCache` over Redis** | Single-instance caching is sufficient for this scope. Redis would be required for true horizontal scaling; the `CachingCurrencyProvider` decorator isolates this concern so the swap is straightforward. |
| **No refresh token** | 24h expiry covers normal usage in dev. Production would need refresh tokens or token rotation. |
| **Excluded currencies enforced server-side only** | Frontend does not filter the currency dropdowns — it relies on the backend 400 response. This is intentional: the source of truth for business rules belongs in the API, not duplicated in the UI. |
| **Pagination implemented in-memory** | Frankfurter returns the full date range in one response; pagination is applied after the fact in `CurrencyService`. A real-world provider with large datasets would require server-side pagination. |

---

## Horizontal Scaling

The current setup is intentionally single-instance (suitable for this assessment). Three concrete changes are required before running multiple replicas:

### 1. Replace in-process cache with Redis

`CachingCurrencyProvider` currently uses `IMemoryCache`, which is per-process. Each replica caches independently, so cache misses are multiplied and TTLs diverge across nodes.

**`Program.cs`** — swap the cache registration:
```csharp
// remove:
builder.Services.AddMemoryCache();

// add:
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));
```

**`CachingCurrencyProvider.cs`** — change the injected type:
```csharp
// remove:
private readonly IMemoryCache _cache;
public CachingCurrencyProvider(ICurrencyProvider inner, IMemoryCache cache, ...)

// add:
private readonly IDistributedCache _cache;
public CachingCurrencyProvider(ICurrencyProvider inner, IDistributedCache cache, ...)
```

Cache reads/writes change from `_cache.GetOrCreateAsync` to `_cache.GetAsync` / `_cache.SetAsync` with `DistributedCacheEntryOptions`. No other files change.

### 2. Replace SQLite with PostgreSQL

SQLite is file-based and single-writer; it cannot be shared across containers.

**`Program.cs`** — one line:
```csharp
// remove:
opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))

// add:
opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
```

Add `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package. Re-run `dotnet ef migrations add` to generate a PostgreSQL-compatible migration. No model or service code changes.

### 3. Replace in-process rate limiter with a distributed one

`AddRateLimiter` (fixed-window) tracks request counts in local memory. With N replicas each client effectively gets N× the configured limit.

Options:
- **Redis + `AspNetCoreRateLimit`** package — stores counters in Redis, works across replicas
- **API Gateway** (AWS API Gateway, Azure APIM, nginx) — offload rate limiting entirely before requests reach the app

---

## Environment Configuration

| Environment | Config file | Activation |
|---|---|---|
| Development | `appsettings.Development.json` | `ASPNETCORE_ENVIRONMENT=Development` (default for `dotnet run`) |
| Production | `appsettings.Production.json` | `ASPNETCORE_ENVIRONMENT=Production` |
| Testing | *(no dedicated file)* | Overridden in `WebHostBuilder.ConfigureServices` |

A dedicated `appsettings.Testing.json` is not present — the integration tests override the database connection directly in `WebApplicationFactory`:

```csharp
builder.ConfigureServices(services => {
    services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)));
    services.AddDbContext<AppDbContext>(opts => opts.UseSqlite("Data Source=integration_test.db"));
});
```

To formalise a `Testing` environment, add `appsettings.Testing.json` with a dedicated DB connection string and set `ASPNETCORE_ENVIRONMENT=Testing` in the test host builder — the override in `ConfigureServices` can then be removed.

---

## Potential Future Improvements

- **Additional currency providers** — the `ICurrencyProvider` / `ICurrencyProviderFactory` design makes this a matter of implementing the interface and registering the new provider; no existing code changes required
- **Redis distributed cache** — replace `IMemoryCache` with `IDistributedCache` for horizontal scaling; the `CachingCurrencyProvider` decorator isolates this concern so the swap is straightforward
- **Refresh token flow** — extend the auth model with refresh tokens and token rotation; JWT expiry is 24 h in dev, which is sufficient for this scope
- **User registration endpoint** — currently users are seeded at startup; a proper `POST /api/v1/auth/register` would complete the auth model for self-service signup
- **Frontend tests for RatesPage and HistoryPage** — `LoginPage`, `ProtectedRoute`, and `ConvertPage` are covered; adding tests for `RatesPage` (rate table rendering) and `HistoryPage` (pagination controls, empty state) would reach full frontend coverage
- **OpenTelemetry** — replace the custom `RequestLoggingMiddleware` with OTEL traces exported to Jaeger/Zipkin for distributed tracing and richer observability
- **GitHub Actions CI** — the workflow skeleton in the CI/CD section above can be dropped in directly; no other changes needed
- **PostgreSQL** — production-grade persistence; EF Core migration is the only required change (`opts.UseNpgsql(...)`)
- **HTTPS enforcement** — add HSTS middleware and HTTP→HTTPS redirect for production deployments
