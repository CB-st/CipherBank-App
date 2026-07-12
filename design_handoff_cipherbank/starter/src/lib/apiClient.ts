import { useMock, mockRequest } from '@/mocks';

const BASE = process.env.EXPO_PUBLIC_API_BASE ?? '';

type Opts = { idempotencyKey?: string; signal?: AbortSignal };

async function request<T>(method: string, path: string, body?: unknown, opts: Opts = {}): Promise<T> {
  if (useMock()) {
    return mockRequest<T>(method, path, body, opts);
  }

  const res = await fetch(BASE + path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      // Authorization: 'Bearer ' + (await getToken()),
      ...(opts.idempotencyKey ? { 'Idempotency-Key': opts.idempotencyKey } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
    signal: opts.signal,
  });
  if (!res.ok) throw await toApiError(res);
  return res.status === 204 ? (undefined as T) : res.json();
}

async function toApiError(res: Response) {
  let detail: any = null;
  try {
    detail = await res.json();
  } catch {}
  return Object.assign(new Error(detail?.message ?? res.statusText), { status: res.status, detail });
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
