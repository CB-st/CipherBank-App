import { useMock, mockRequest } from '@/mocks';

/**
 * CipherBank public HTTP API (PriceCache / currencies).
 * Host: api.cipherbank.money — paths are NOT under /v1.
 * Wire format: SCREAMING_SNAKE_CASE · amounts as number (double).
 * Spec: docs/CB_InitialAPIRef.html
 */
const PUBLIC_BASE =
  process.env.EXPO_PUBLIC_PUBLIC_API_BASE ?? 'https://api.cipherbank.money';

type Opts = { signal?: AbortSignal };

async function publicRequest<T>(method: string, path: string, body?: unknown, opts: Opts = {}): Promise<T> {
  if (useMock()) {
    return mockRequest<T>(method, path, body, opts);
  }

  const res = await fetch(PUBLIC_BASE + path, {
    method,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: body !== undefined ? JSON.stringify(body) : JSON.stringify({}),
    signal: opts.signal,
  });
  if (!res.ok) throw await toPublicApiError(res);
  return res.status === 204 ? (undefined as T) : res.json();
}

async function toPublicApiError(res: Response) {
  let detail: unknown = null;
  try {
    detail = await res.json();
  } catch {
    /* empty */
  }
  const message =
    detail && typeof detail === 'object' && 'message' in detail
      ? String((detail as { message: unknown }).message)
      : res.statusText;
  return Object.assign(new Error(message), { status: res.status, detail });
}

export const publicApi = {
  post: <T>(path: string, body: unknown = {}, opts?: Opts) =>
    publicRequest<T>('POST', path, body, opts),
};
