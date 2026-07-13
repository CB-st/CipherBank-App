/**
 * Payment / receive URIs for QR payloads.
 * Prefer BIP21-style for UTXO coins; EIP-681 for ETH; plain address fallback.
 */

export type PaymentUriOpts = {
  amount?: string | number;
  label?: string;
  message?: string;
};

export function buildPaymentUri(symbol: string, address: string, opts: PaymentUriOpts = {}): string {
  const sym = symbol.toUpperCase();
  const addr = address.trim();
  if (!addr) return '';

  const amount = opts.amount != null && String(opts.amount).length ? String(opts.amount) : undefined;
  const params = new URLSearchParams();
  if (amount) params.set('amount', amount);
  if (opts.label) params.set('label', opts.label);
  if (opts.message) params.set('message', opts.message);
  const q = params.toString();
  const qs = q ? '?' + q : '';

  switch (sym) {
    case 'BTC':
      return `bitcoin:${addr}${qs}`;
    case 'LTC':
      return `litecoin:${addr}${qs}`;
    case 'DOGE':
      return `dogecoin:${addr}${qs}`;
    case 'ETH':
      // EIP-681 — value in wei omitted unless we add conversion later
      return amount ? `ethereum:${addr}?value=${amount}` : `ethereum:${addr}`;
    case 'XMR':
      return amount ? `monero:${addr}?tx_amount=${amount}` : `monero:${addr}`;
    case 'USD':
    case 'EUR':
    case 'JPY':
      // Fiat rail — CipherBank handle-style URI when no chain address
      return `cipherbank:receive/${sym}${addr ? '?address=' + encodeURIComponent(addr) : ''}`;
    default:
      return addr;
  }
}

/** Short preview for mono UI rows. */
export function shortenAddress(address: string, head = 8, tail = 6): string {
  const a = address.trim();
  if (a.length <= head + tail + 1) return a;
  return a.slice(0, head) + '…' + a.slice(-tail);
}
