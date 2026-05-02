# Currency Conversion Platform — Frontend Design

**Date:** 2026-05-02
**Stack:** Vite + React + TypeScript + Tailwind CSS + shadcn/ui + React Router v6

---

## Goal

Build a React SPA that consumes the local ASP.NET Core backend (`http://localhost:5000/api/v1/`). Users log in with a JWT, then access three features: currency conversion, latest rates, and historical rates with pagination.

---

## Architecture

```
currency-frontend/
├── public/
├── src/
│   ├── api/
│   │   ├── client.ts          # base fetch wrapper (attaches Authorization header)
│   │   ├── auth.ts            # POST /auth/token
│   │   └── currency.ts        # GET /currency/rates, /currency/convert, /currency/history
│   ├── components/
│   │   ├── NavBar.tsx          # top nav with route links + logout button
│   │   └── ProtectedRoute.tsx  # redirects to /login if no token
│   ├── hooks/
│   │   ├── useAuth.ts          # login(), logout(), token state
│   │   ├── useRates.ts
│   │   ├── useConvert.ts
│   │   └── useHistory.ts
│   ├── pages/
│   │   ├── LoginPage.tsx
│   │   ├── ConvertPage.tsx
│   │   ├── RatesPage.tsx
│   │   └── HistoryPage.tsx
│   ├── types/
│   │   └── api.ts             # DTOs matching backend response shapes
│   ├── App.tsx                # router setup
│   └── main.tsx
├── index.html
├── vite.config.ts
├── tailwind.config.ts
├── tsconfig.json
└── package.json
```

---

## Routes

| Route | Page | Auth |
|---|---|---|
| `/login` | Login form | Public |
| `/convert` | Conversion form + result | Protected |
| `/rates` | Latest rates table | Protected |
| `/history` | Historical rates + pagination | Protected |

- Default route `/` redirects to `/convert` (if authenticated) or `/login`.
- `ProtectedRoute` checks for JWT in `localStorage`; redirects to `/login` if absent.
- After successful login, redirect to `/convert`.
- Logout clears `localStorage` and redirects to `/login`.

---

## API Integration

Base URL: `http://localhost:5000/api/v1`

All authenticated requests include `Authorization: Bearer <token>` header, set by the base client wrapper.

### Endpoints used

| Method | Path | Page |
|---|---|---|
| POST | `/auth/token` | LoginPage |
| GET | `/currency/rates?base=EUR` | RatesPage |
| GET | `/currency/convert?from=EUR&to=USD&amount=100` | ConvertPage |
| GET | `/currency/history?base=EUR&startDate=...&endDate=...&page=1&pageSize=10` | HistoryPage |

---

## Types (matching backend DTOs)

```ts
interface TokenResponse { token: string; tokenType: string; expiresIn: number; }
interface ExchangeRatesResponse { amount: number; base: string; date: string; rates: Record<string, number>; }
interface ConversionResponse { amount: number; from: string; to: string; rate: number; convertedAmount: number; date: string; }
interface HistoricalRateEntry { date: string; rates: Record<string, number>; }
interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalItems: number; totalPages: number; }
```

---

## Pages

### LoginPage
- Username + password inputs, Submit button
- Calls `POST /auth/token`; on success stores token in `localStorage`, redirects to `/convert`
- On 401: shows "Invalid credentials" error message inline

### ConvertPage
- `from` and `to` currency dropdowns (populated from latest rates endpoint for EUR base)
- Amount number input
- "Convert" button — calls `/currency/convert`
- Result card: shows `convertedAmount`, rate, date
- Error state: clear message for excluded currencies (TRY, PLN, THB, MXN return 400) and network errors

### RatesPage
- Base currency dropdown (default EUR)
- Table: currency code | rate
- Loading skeleton while fetching

### HistoryPage
- Base currency dropdown (default EUR)
- Start date + end date pickers (default: last 30 days)
- Paginated table: date | currency rates (key columns)
- Prev / Next pagination controls, shows "Page X of Y"

---

## Auth State

JWT stored in `localStorage` under key `jwt_token`. The `useAuth` hook exposes:
- `token: string | null`
- `login(username, password): Promise<void>` — fetches token, stores it
- `logout(): void` — clears storage, navigates to `/login`

No refresh token flow — JWT expires per backend config (dev: 24h).

---

## Error Handling

- 401 on any protected request → logout + redirect to `/login`
- 400 from convert endpoint → display `error` field from response body
- Network errors → generic "Something went wrong" message
- Loading states on all async operations

---

## Excluded Currencies

TRY, PLN, THB, MXN are blocked by the backend. The frontend does not filter them from dropdowns — it lets the backend return 400 and displays the error message returned in the response body.

---

## Vite Proxy

`vite.config.ts` proxies `/api` → `http://localhost:5000` to avoid CORS issues in dev.

---

## Docker

`currency-frontend/Dockerfile` — multi-stage build: Node build stage → nginx serving static files on port 80. Referenced in `compose.yaml` frontend service.
