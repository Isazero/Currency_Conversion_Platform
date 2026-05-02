import { useState, useEffect } from 'react';
import { getRates } from '../api/currency';
import type { ExchangeRatesResponse } from '../types/api';

export function useRates(base: string) {
  const [data, setData] = useState<ExchangeRatesResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getRates(base)
      .then(d => { if (!cancelled) setData(d); })
      .catch(err => { if (!cancelled) setError((err as Error).message ?? 'Failed to fetch rates'); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [base]);

  return { data, loading, error };
}
