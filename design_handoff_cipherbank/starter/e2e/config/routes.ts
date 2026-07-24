const route = (name: string, fallback: string): string => process.env[name] ?? fallback;

/**
 * Playwright intercept globs for future live mode.
 * Default Expo e2e uses in-process mocks — these routes are unused until CB_TEST_MODE=live.
 */
export const routes = {
  login: route('CB_ROUTE_LOGIN', '**/v1/session'),
  accountCreate: route('CB_ROUTE_ACCOUNT_CREATE', '**/v1/account/**'),
  accountRecover: route('CB_ROUTE_ACCOUNT_RECOVER', '**/v1/account/bootstrap'),
  walletCreate: route('CB_ROUTE_WALLET_CREATE', '**/v1/wallets'),
  walletReceive: route('CB_ROUTE_WALLET_RECEIVE', '**/v1/wallets/*/receive'),
  walletActivity: route('CB_ROUTE_WALLET_ACTIVITY', '**/v1/wallets/*/activity'),
  cardQuote: route('CB_ROUTE_CARD_QUOTE', '**/v1/cards/quote'),
  cardCreate: route('CB_ROUTE_CARD_CREATE', '**/v1/cards'),
  guestCardSession: route('CB_ROUTE_GUEST_CARD_SESSION', '**/v1/guest/cards/session'),
  paymentQuote: route('CB_ROUTE_PAYMENT_QUOTE', '**/v1/quotes'),
  paymentCreate: route('CB_ROUTE_PAYMENT_CREATE', '**/v1/transfers/**'),
  marketCurrent: route('CB_ROUTE_MARKET_CURRENT', '**/iquote*'),
  marketHistory: route('CB_ROUTE_MARKET_HISTORY', '**/v1/history/**'),
} as const;
