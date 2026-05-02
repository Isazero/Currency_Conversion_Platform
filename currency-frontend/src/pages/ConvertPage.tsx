import { useState, type FormEvent } from 'react';
import { useConvert } from '../hooks/useConvert';
import { useRates } from '../hooks/useRates';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';

export default function ConvertPage() {
  const [from, setFrom] = useState('EUR');
  const [to, setTo] = useState('USD');
  const [amount, setAmount] = useState('100');
  const { result, loading, error, doConvert } = useConvert();
  const { data: ratesData } = useRates('EUR');

  const currencies = ratesData
    ? ['EUR', ...Object.keys(ratesData.rates)].sort()
    : ['EUR', 'USD', 'GBP', 'JPY', 'CHF', 'AUD', 'CAD'];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    doConvert(from, to, parseFloat(amount));
  };

  return (
    <div className="max-w-md mx-auto space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Convert Currency</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <Label>From</Label>
                <Select value={from} onValueChange={v => v && setFrom(v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {currencies.map(c => (
                      <SelectItem key={c} value={c}>{c}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>To</Label>
                <Select value={to} onValueChange={v => v && setTo(v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {currencies.map(c => (
                      <SelectItem key={c} value={c}>{c}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor="amount">Amount</Label>
              <Input
                id="amount"
                type="number"
                min="0.01"
                step="any"
                value={amount}
                onChange={e => setAmount(e.target.value)}
                required
              />
            </div>
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Converting…' : 'Convert'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {error && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="pt-4">
            <p className="text-sm text-red-700" role="alert">{error}</p>
          </CardContent>
        </Card>
      )}

      {result && (
        <Card className="border-green-200 bg-green-50">
          <CardContent className="pt-4 space-y-1">
            <p className="text-2xl font-semibold text-green-800">
              {result.convertedAmount.toFixed(4)} {result.to}
            </p>
            <p className="text-sm text-gray-600">
              {result.amount} {result.from} &nbsp;·&nbsp; rate {result.rate.toFixed(6)}
            </p>
            <p className="text-xs text-gray-400">As of {result.date}</p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
