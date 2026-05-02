import { useState } from 'react';
import { useRates } from '../hooks/useRates';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';

const CURRENCIES = ['EUR', 'USD', 'GBP', 'JPY', 'CHF', 'AUD', 'CAD'];

export default function RatesPage() {
  const [base, setBase] = useState('EUR');
  const { data, loading, error } = useRates(base);

  return (
    <div className="max-w-2xl mx-auto">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle>Latest Rates</CardTitle>
          <Select value={base} onValueChange={v => v && setBase(v)}>
            <SelectTrigger className="w-28">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {CURRENCIES.map(c => (
                <SelectItem key={c} value={c}>{c}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardHeader>
        <CardContent>
          {error && <p className="text-sm text-red-600" role="alert">{error}</p>}

          {loading && (
            <div className="space-y-2">
              {Array.from({ length: 10 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full rounded" />
              ))}
            </div>
          )}

          {data && (
            <>
              <p className="text-xs text-gray-400 mb-3">As of {data.date}</p>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Currency</TableHead>
                    <TableHead className="text-right">Rate</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {Object.entries(data.rates)
                    .sort(([a], [b]) => a.localeCompare(b))
                    .map(([currency, rate]) => (
                      <TableRow key={currency}>
                        <TableCell className="font-medium">{currency}</TableCell>
                        <TableCell className="text-right tabular-nums">
                          {rate.toFixed(4)}
                        </TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
