export interface AuthRequest {
  username: string;
  password: string;
}

export interface TokenResponse {
  token: string;
  tokenType: string;
  expiresIn: number;
}

export interface ExchangeRatesResponse {
  amount: number;
  base: string;
  date: string;
  rates: Record<string, number>;
}

export interface ConversionResponse {
  amount: number;
  from: string;
  to: string;
  rate: number;
  convertedAmount: number;
  date: string;
}

export interface HistoricalRateEntry {
  date: string;
  rates: Record<string, number>;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
