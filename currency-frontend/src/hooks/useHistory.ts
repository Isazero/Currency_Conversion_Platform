import { useState, useEffect } from 'react';
import { getHistory } from '../api/currency';
import type { HistoricalRateEntry, PagedResponse } from '../types/api';

export function useHistory(
  base: string,
  startDate: string,
  endDate: string,
  page: number
) {
  const [data, setData] = useState<PagedResponse<HistoricalRateEntry> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!startDate || !endDate) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    getHistory(base, startDate, endDate, page, 10)
      .then(d => { if (!cancelled) setData(d); })
      .catch(err => { if (!cancelled) setError((err as Error).message ?? 'Failed to fetch history'); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [base, startDate, endDate, page]);

  return { data, loading, error };
}
