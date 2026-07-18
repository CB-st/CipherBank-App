import { useMock, mockRequest } from '@/mocks';
import { decodeResponseBody, encodeRequestBody } from '@/lib/wireFormat';

const BASE = process.env.EXPO_PUBLIC_API_BASE ?? '';

type Opts = { idempotencyKey?: string; signal?: AbortSignal };

async function request<T>(method: string, path: string, body?: unknown, opts: Opts = {}): Promise<T> {
  const wireBody = body === undefined ? undefined : encodeRequestBody(path, body);

  if (useMock()) {
    const wireRes = await mockRequest<T>(method, path, wireBody, opts);
    return decodeResponseBody(path, wireRes);
  }

  const res = await fetch(BASE + path, {
    method,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      // Authorization: 'Bearer ' + (await getToken()),
      ...(opts.idempotencyKey ? { 'Idempotency-Key': opts.idempotencyKey } : {}),
    },
    body: wireBody !== undefined ? JSON.stringify(wireBody) : undefined,
    signal: opts.signal,
  });
  if (!res.ok) throw await toApiError(res);
  if (res.status === 204) return undefined as T;
  const json = (await res.json()) as T;
  return decodeResponseBody(path, json);
}

async function toApiError(res: Response) {
  let detail: unknown = null;
  try {
    detail = await res.json();
  } catch {
    /* empty */
  }
  const message =
    detail && typeof detail === 'object' && detail !== null && 'MESSAGE' in detail
      ? String((detail as { MESSAGE: unknown }).MESSAGE)
      : detail && typeof detail === 'object' && detail !== null && 'message' in detail
        ? String((detail as { message: unknown }).message)
        : res.statusText;
  return Object.assign(new Error(message), { status: res.status, detail });
}

export const api = {
  get: <T>(p: string, o?: Opts) => request<T>('GET', p, undefined, o),
  post: <T>(p: string, b?: unknown, o?: Opts) => request<T>('POST', p, b, o),
  put: <T>(p: string, b?: unknown, o?: Opts) => request<T>('PUT', p, b, o),
};

export const uuid = () =>
  'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
