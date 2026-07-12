import { api } from '@/lib/apiClient';
import type { Portfolio } from './portfolio.types';

export const getPortfolio = () => api.get<Portfolio>('/portfolio');
