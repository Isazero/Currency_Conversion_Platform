import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi, describe, it, expect } from 'vitest';
import ProtectedRoute from './ProtectedRoute';
import { AuthContext } from '../context/AuthContext';

vi.mock('./NavBar', () => ({ default: () => <nav data-testid="navbar" /> }));

function renderWithAuth(token: string | null, initialPath = '/protected') {
  return render(
    <AuthContext.Provider value={{ token, login: vi.fn(), logout: vi.fn() }}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/protected" element={<div>Protected Content</div>} />
          </Route>
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>
  );
}

describe('ProtectedRoute', () => {
  it('renders the child route when token is present', () => {
    renderWithAuth('valid-token');
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('redirects to /login when no token', () => {
    renderWithAuth(null);
    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders the navbar when authenticated', () => {
    renderWithAuth('valid-token');
    expect(screen.getByTestId('navbar')).toBeInTheDocument();
  });
});
