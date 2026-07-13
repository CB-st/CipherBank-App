# User configurability — implementation plan

Companion to [`PROTOTYPE_MAP.md`](./PROTOTYPE_MAP.md) and [`PERSISTENCE.md`](./PERSISTENCE.md). Covers display currency, enabled currencies, wallet manifest expansion, and non-blocking stale data UX.

## Locked decisions

| Topic | Decision |
|-------|----------|
| **BTC base chart** | Client-side convert WALLET series from USD using cached rates. Present immediately; refresh when P1/P3 daemon fills data. Small corner spinner when stale — never block taps. |
| **Enabled vs held** | Disabling a currency **hides** holdings (no block). Collapsed **Other assets** section at bottom of AssetList for hidden wallet/card/account rows. |
| **Locale base currency** | On first launch, if `expo-localization` yields a supported fiat (USD/EUR/JPY), use as default `baseCurrency`. Otherwise USD. User may switch to USD / BTC / EUR / JPY in Profile. |

## Prefs schema (`UserPrefs`)

```ts
baseCurrency: 'USD' | 'BTC' | 'EUR' | 'JPY'   // default from locale or USD
enabledCurrencies: string[]                    // uppercased symbols user wants visible
localeInferredBase?: string                    // audit only — what locale suggested
```

Defaults: `enabledCurrencies: ['BTC','ETH','USD']`, `baseCurrency` from `inferBaseCurrencyFromLocale()`.

SQLite `prefs` row mirrors fields; `GET/PUT /prefs` extended in `API_CONTRACT.md`.

## Base currency behavior

| Surface | Behavior |
|---------|----------|
| Home hero total | `formatBaseValue(usdTotal, base, rates)` |
| 24h change chip | Base-denominated amount + unchanged `%` |
| Hero sparkline | Convert WALLET `v` points: `v / rate(base)` when base ≠ USD |
| Performance chart | Same % indexing as today; WALLET label shows base unit |
| Convert default `to` | User `baseCurrency` when fiat; BTC when base is BTC |
| Rates (P2) | Ensure base symbol + BTC/USD cross present in snapshot |

**Stale UX:** `useRatesCache` + history query `isFetching` → `StaleBadge` (12px `ActivityIndicator` top-right on BalanceHero / PerformanceCard). Data stays on screen.

## Enabled currencies

- Profile toggles per catalog asset (`listAssets({ enabledOnly: true })`).
- User can disable BTC even with BTC wallets → row moves to **Other assets**.
- `useVisibleHoldings(portfolio, enabledCurrencies)` splits `{ visible, hidden }`.
- AssetList renders visible rows + expandable “Other assets (N)” footer.

## Wallet manifest expansion

Register in `registry.ts` (same shape as BTC/ETH/XMR):

| Symbol | addModes | canDerive | Phase |
|--------|----------|-----------|-------|
| LTC | derive, watch | yes (BIP84 coin 2) | watch + derive |
| DOGE | derive, watch | yes (BIP44 coin 3) | watch + derive |

`derive.ts`: `deriveLtcAddress`, `deriveDogeAddress`. Mocks already include LTC/DOGE in `/rates`.

## File map

| Action | Path |
|--------|------|
| Create | `docs/USER_CONFIG.md` (this file) |
| Create | `src/features/prefs/localeCurrency.ts` |
| Create | `src/features/prefs/useBaseCurrency.ts` |
| Create | `src/features/portfolio/useVisibleHoldings.ts` |
| Create | `src/components/primitives/StaleBadge.tsx` |
| Modify | `prefs.types.ts`, `prefsRepo.ts`, `usePrefs.tsx` |
| Modify | `lib/money.ts`, `BalanceHero.tsx`, `PerformanceCard.tsx`, `AssetList.tsx` |
| Modify | `HomeScreen.tsx`, `ProfileScreen.tsx`, `registry.ts`, `derive.ts` |
| Modify | `fixtures/prefs.json`, `API_CONTRACT.md`, `AGENTS.md` |

## Verification

- `npm run typecheck`
- Toggle base USD ↔ BTC: hero + sparkline re-label without network block
- Disable ETH: ETH holding appears under Other assets
- Locale mock: JP region → JPY default on fresh prefs
- LTC/DOGE: Add-wallet shows derive + watch chips

## Out of scope

- `/history?denom=` server param
- Securities as base currency
- Blocking disable when wallets exist
