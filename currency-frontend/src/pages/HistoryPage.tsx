import { useState } from 'react';
import { useHistory } from '../hooks/useHistory';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';

const CURRENCIES = ['EUR', 'USD', 'GBP', 'JPY', 'CHF', 'AUD', 'CAD'];

function toDateStr(d: Date) {
  return d.toISOString().split('T')[0];
}

export default function HistoryPage() {
  const [base, setBase] = useState('EUR');
  const [startDate, setStartDate] = useState(() => {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    return toDateStr(d);
  });
  const [endDate, setEndDate] = useState(() => toDateStr(new Date()));
  const [page, setPage] = useState(1);

  const { data, loading, error } = useHistory(base, startDate, endDate, page);

  const resetPage = () => setPage(1);

  const columnCurrencies = data?.items[0]
    ? Object.keys(data.items[0].rates).slice(0, 5)
    : [];

  return (
    <div className="max-w-4xl mx-auto">
      <Card>
        <CardHeader>
          <CardTitle>Historical Rates</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-1">
              <Label>Base</Label>
              <Select value={base} onValueChange={v => { if (v) { setBase(v); resetPage(); } }}>
                <SelectTrigger className="w-28">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CURRENCIES.map(c => (
                    <SelectItem key={c} value={c}>{c}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="startDate">Start</Label>
              <Input
                id="startDate"
                type="date"
                value={startDate}
                onChange={e => { setStartDate(e.target.value); resetPage(); }}
                className="w-40"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="endDate">End</Label>
              <Input
                id="endDate"
                type="date"
                value={endDate}
                onChange={e => { setEndDate(e.target.value); resetPage(); }}
                className="w-40"
              />
            </div>
          </div>

          {error && <p className="text-sm text-red-600" role="alert">{error}</p>}

          {loading && (
            <div className="space-y-2">
              {Array.from({ length: 10 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full rounded" />
              ))}
            </div>
          )}

          {data && data.items.length === 0 && (
            <p className="text-sm text-gray-500 py-6 text-center">
              No data for the selected period.
            </p>
          )}

          {data && data.items.length > 0 && (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Date</TableHead>
                    {columnCurrencies.map(c => (
                      <TableHead key={c} className="text-right">{c}</TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map(entry => (
                    <TableRow key={entry.date}>
                      <TableCell className="font-medium">{entry.date}</TableCell>
                      {columnCurrencies.map(c => (
                        <TableCell key={c} className="text-right tabular-nums">
                          {entry.rates[c]?.toFixed(4) ?? '—'}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex items-center justify-between pt-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p - 1)}
                  disabled={page <= 1}
                >
                  Previous
                </Button>
                <span className="text-sm text-gray-600">
                  Page {data.page} of {data.totalPages} ({data.totalItems} days)
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p + 1)}
                  disabled={page >= data.totalPages}
                >
                  Next
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
