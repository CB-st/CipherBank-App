import * as Localization from 'expo-localization';
import {
  BASE_CURRENCY_OPTIONS,
  DEFAULT_ENABLED_CURRENCIES,
  DEFAULT_PREFS,
  type BaseCurrency,
  type UserPrefs,
  isBaseCurrency,
} from './prefs.types';

const EU_REGIONS = new Set([
  'AT', 'BE', 'CY', 'DE', 'EE', 'ES', 'FI', 'FR', 'GR', 'HR', 'IE', 'IT', 'LT', 'LU', 'LV', 'MT', 'NL', 'PT', 'SI', 'SK',
]);

/** Map device locale → supported base currency, else USD. */
export function inferBaseCurrencyFromLocale(): BaseCurrency {
  try {
    const locales = Localization.getLocales();
    const primary = locales[0];
    if (!primary) return 'USD';

    const code = primary.currencyCode?.toUpperCase();
    if (code && isBaseCurrency(code)) return code;

    const region = primary.regionCode?.toUpperCase();
    if (region === 'JP') return 'JPY';
    if (region && EU_REGIONS.has(region)) return 'EUR';
    if (region === 'US') return 'USD';
  } catch {
    /* web / SSR */
  }
  return 'USD';
}

export function normalizePrefs(partial: Partial<UserPrefs>): UserPrefs {
  const hadBase = partial.baseCurrency != null && isBaseCurrency(partial.baseCurrency);
  const localeBase = inferBaseCurrencyFromLocale();

  const enabled =
    partial.enabledCurrencies?.length
      ? [...new Set(partial.enabledCurrencies.map((s) => s.toUpperCase()))]
      : [...DEFAULT_ENABLED_CURRENCIES];

  return {
    ...DEFAULT_PREFS,
    ...partial,
    homeOrder: partial.homeOrder ?? [...DEFAULT_PREFS.homeOrder],
    homeVisible: { ...DEFAULT_PREFS.homeVisible, ...partial.homeVisible },
    appearance: partial.appearance === 'light' ? 'light' : 'dark',
    baseCurrency: hadBase ? (partial.baseCurrency as BaseCurrency) : localeBase,
    enabledCurrencies: enabled,
    localeInferredBase: partial.localeInferredBase ?? (hadBase ? undefined : localeBase),
    appLockIdleSec:
      typeof partial.appLockIdleSec === 'number' && partial.appLockIdleSec >= 15
        ? Math.min(partial.appLockIdleSec, 30 * 60)
        : DEFAULT_PREFS.appLockIdleSec,
  };
}

export function baseCurrencyLabel(base: BaseCurrency): string {
  if (base === 'BTC') return 'Bitcoin (BTC)';
  if (base === 'EUR') return 'Euro (EUR)';
  if (base === 'JPY') return 'Japanese Yen (JPY)';
  return 'US Dollar (USD)';
}

export function isSupportedBaseOption(sym: string): sym is BaseCurrency {
  return BASE_CURRENCY_OPTIONS.includes(sym as BaseCurrency);
}
