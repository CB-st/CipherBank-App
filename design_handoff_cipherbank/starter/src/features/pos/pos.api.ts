import { api, uuid } from '@/lib/apiClient';
import type {
  PosAuthorizeBody,
  PosAuthorizeResult,
  PosConfirmResult,
  PosSession,
  PosSessionCreate,
} from './pos.types';

export const createPosSession = (body: PosSessionCreate) =>
  api.post<PosSession>('/pos/sessions', body, { idempotencyKey: uuid() });

export const getPosSession = (sessionId: string) =>
  api.get<PosSession>('/pos/sessions/' + sessionId);

export const authorizePos = (body: PosAuthorizeBody) =>
  api.post<PosAuthorizeResult>('/pos/authorize', body, { idempotencyKey: uuid() });

export const confirmPos = (sessionId: string) =>
  api.post<PosConfirmResult>('/pos/confirm', { sessionId }, { idempotencyKey: uuid() });
