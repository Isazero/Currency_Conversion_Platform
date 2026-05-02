import { get } from './client';
import type {
  ConversionResponse,
  ExchangeRatesResponse,
  HistoricalRateEntry,
  PagedResponse,
} from '../types/api';

export const getRates = (base: string) =>
  get<ExchangeRatesResponse>(`/currency/rates?base=${base}`);

export const convert = (from: string, to: string, amount: number) =>
  get<ConversionResponse>(`/currency/convert?from=${from}&to=${to}&amount=${amount}`);

export const getHistory = (
  base: string,
  startDate: string,
  endDate: string,
  page: number,
  pageSize: number
) =>
  get<PagedResponse<HistoricalRateEntry>>(
    `/currency/history?base=${base}&startDate=${startDate}&endDate=${endDate}&page=${page}&pageSize=${pageSize}`
  );
