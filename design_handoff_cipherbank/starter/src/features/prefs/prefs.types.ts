/** Supported display / valuation units. Fiat from manifest; BTC as crypto base. */
export type BaseCurrency = 'USD' | 'BTC' | 'EUR' | 'JPY';

export const BASE_CURRENCY_OPTIONS: BaseCurrency[] = ['USD', 'BTC', 'EUR', 'JPY'];

export type HomeSection = 'cora' | 'balance' | 'quickActions' | 'performance' | 'assets';

export type SendSpeedPref = 'instant' | 'ach';

export type AppearancePref = 'dark' | 'light';

export interface UserPrefs {
  homeOrder: HomeSection[];
  homeVisible: Record<HomeSection, boolean>;
  valuesHiddenOnLaunch: boolean;
  coraEnabled: boolean;
  defaultSendSpeed: SendSpeedPref;
  /** App chrome. Dark is default; light is opt-in. */
  appearance: AppearancePref;
  /** Portfolio total, hero, and primary chart denomination. */
  baseCurrency: BaseCurrency;
  /** Symbols the user wants visible on Home / selectors (uppercase). */
  enabledCurrencies: string[];
  /** What locale suggested on first prefs hydrate (audit / Profile hint). */
  localeInferredBase?: BaseCurrency;
  /** Lock the app shell after this many seconds of idle (default 60). */
  appLockIdleSec: number;
}

export const DEFAULT_ENABLED_CURRENCIES = ['BTC', 'ETH', 'USD'];

export const DEFAULT_PREFS: UserPrefs = {
  homeOrder: ['cora', 'balance', 'quickActions', 'performance', 'assets'],
  homeVisible: {
    cora: true,
    balance: true,
    quickActions: true,
    performance: true,
    assets: true,
  },
  valuesHiddenOnLaunch: false,
  coraEnabled: true,
  defaultSendSpeed: 'instant',
  appearance: 'dark',
  baseCurrency: 'USD',
  enabledCurrencies: [...DEFAULT_ENABLED_CURRENCIES],
  appLockIdleSec: 60,
};

export const HOME_SECTION_LABELS: Record<HomeSection, string> = {
  cora: 'Cora footer line',
  balance: 'Balance hero',
  quickActions: 'Quick actions',
  performance: 'Performance chart',
  assets: 'Asset list',
};

export function isBaseCurrency(sym: string): sym is BaseCurrency {
  return BASE_CURRENCY_OPTIONS.includes(sym.toUpperCase() as BaseCurrency);
}
