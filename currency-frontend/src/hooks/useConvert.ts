import { useState } from 'react';
import { convert } from '../api/currency';
import type { ConversionResponse } from '../types/api';
import { ApiError } from '../api/client';

export function useConvert() {
  const [result, setResult] = useState<ConversionResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const doConvert = async (from: string, to: string, amount: number) => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      setResult(await convert(from, to, amount));
    } catch (err) {
      setError(err instanceof ApiError ? err.body.error : 'Something went wrong.');
    } finally {
      setLoading(false);
    }
  };

  return { result, loading, error, doConvert };
}
