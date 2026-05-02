import { post } from './client';
import type { TokenResponse } from '../types/api';

export const login = (username: string, password: string) =>
  post<TokenResponse>('/auth/token', { username, password });
