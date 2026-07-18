/**
 * CipherBank public API currency codes (SCREAMING names) ↔ app ticker symbols.
 * Source: docs/CB_InitialAPIRef.html · POST /currencies · /quote · /iquote
 */

const APP_TO_PUBLIC: Record<string, string> = {
  BTC: 'BITCOIN',
  XMR: 'MONERO',
  USD: 'USD',
  ETH: 'ETHEREUM',
  LTC: 'LITECOIN',
  DOGE: 'DOGECOIN',
  EUR: 'EUR',
  JPY: 'JPY',
};

const PUBLIC_TO_APP: Record<string, string> = Object.fromEntries(
  Object.entries(APP_TO_PUBLIC).map(([app, pub]) => [pub, app]),
);

/** App ticker (BTC) → public API code (BITCOIN). Unknown symbols pass through uppercased. */
export function toPublicCurrency(symbol: string): string {
  const u = symbol.trim().toUpperCase();
  return APP_TO_PUBLIC[u] ?? u;
}

/** Public API code (BITCOIN) → app ticker (BTC). */
export function toAppSymbol(code: string): string {
  const u = code.trim().toUpperCase();
  return PUBLIC_TO_APP[u] ?? u;
}

export function isKnownPublicCurrency(code: string): boolean {
  const u = code.trim().toUpperCase();
  return u in PUBLIC_TO_APP || Object.values(APP_TO_PUBLIC).includes(u);
}
