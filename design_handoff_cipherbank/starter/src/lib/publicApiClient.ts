import { useMock, mockRequest } from '@/mocks';

/**
 * CipherBank public HTTP API (PriceCache / currencies).
 * Host: api.cipherbank.money — paths are NOT under /v1.
 * Wire format: SCREAMING_SNAKE_CASE · amounts as number (double).
 * Spec: docs/CB_InitialAPIRef.html
 *
 * Live requests require a parseable `Date` header (same as CB_InitialAPIRef).
 */
const PUBLIC_BASE = (
  process.env.EXPO_PUBLIC_PUBLIC_API_BASE ?? 'https://api.cipherbank.money'
).replace(/\/$/, '');

type Opts = { signal?: AbortSignal };

function statusMessage(status: number): string {
  switch (status) {
    case 424:
      return 'Price or wallet dependency unavailable.';
    case 422:
      return 'Quote or currency request was invalid.';
    case 417:
      return 'Request body or Date header was rejected.';
    case 415:
      return 'Content-Type must be application/json.';
    case 406:
      return 'Accept header must allow JSON.';
    default:
      return `Public API request failed (${status}).`;
  }
}

async function publicRequest<T>(method: string, path: string, body?: unknown, opts: Opts = {}): Promise<T> {
  if (useMock()) {
    return mockRequest<T>(method, path, body, opts);
  }

  const res = await fetch(PUBLIC_BASE + path, {
    method,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Date: new Date().toUTCString(),
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
      : statusMessage(res.status) || res.statusText;
  return Object.assign(new Error(message), { status: res.status, detail });
}

export const publicApi = {
  post: <T>(path: string, body: unknown = {}, opts?: Opts) =>
    publicRequest<T>('POST', path, body, opts),
};

export function getPublicApiBase(): string {
  return PUBLIC_BASE;
}
