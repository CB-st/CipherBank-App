/**
 * Public address generators for supported light-wallet modules.
 * Secrets never leave `derive.ts` / custody — this module is the UI-facing API.
 */
export {
  deriveAddress,
  deriveBtcAddress,
  deriveEthAddress,
  deriveLtcAddress,
  deriveDogeAddress,
  isDerivableSymbol,
  type DerivedAddress,
} from './derive';

export { buildPaymentUri, shortenAddress, type PaymentUriOpts } from './paymentUri';
export { validateWatchAddress, canGenerateAddress, type AddressCheck } from './addressValidate';
