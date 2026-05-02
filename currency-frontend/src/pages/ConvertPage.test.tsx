import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ConvertPage from './ConvertPage';
import { ApiError } from '../api/client';

vi.mock('../hooks/useRates', () => ({
  useRates: () => ({ data: null, loading: false, error: null }),
}));

const mockDoConvert = vi.fn();
let mockConvertState = {
  result: null as null | { convertedAmount: number; to: string; from: string; amount: number; rate: number; date: string },
  loading: false,
  error: null as string | null,
};

vi.mock('../hooks/useConvert', () => ({
  useConvert: () => ({ ...mockConvertState, doConvert: mockDoConvert }),
}));

function renderPage() {
  return render(
    <MemoryRouter>
      <ConvertPage />
    </MemoryRouter>
  );
}

describe('ConvertPage', () => {
  beforeEach(() => {
    mockDoConvert.mockClear();
    mockConvertState = { result: null, loading: false, error: null };
  });

  it('renders the Convert button and amount field', () => {
    renderPage();
    expect(screen.getByRole('button', { name: /convert/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/amount/i)).toBeInTheDocument();
  });

  it('calls doConvert with correct arguments on submit', async () => {
    renderPage();
    const amountInput = screen.getByLabelText(/amount/i);
    await userEvent.clear(amountInput);
    await userEvent.type(amountInput, '250');
    await userEvent.click(screen.getByRole('button', { name: /convert/i }));

    expect(mockDoConvert).toHaveBeenCalledWith('EUR', 'USD', 250);
  });

  it('displays excluded currency error from backend', async () => {
    mockConvertState.error = "Currency 'TRY' is not supported for conversion.";
    renderPage();

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent("Currency 'TRY' is not supported")
    );
  });

  it('shows conversion result when successful', () => {
    mockConvertState.result = {
      convertedAmount: 109.43,
      to: 'USD',
      from: 'EUR',
      amount: 100,
      rate: 1.0943,
      date: '2024-01-15',
    };
    renderPage();

    expect(screen.getByText(/109\.4300 USD/)).toBeInTheDocument();
  });
});
