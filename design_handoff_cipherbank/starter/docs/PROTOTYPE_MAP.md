# Prototype map — pieces, placement, build-out

Inventory of the CipherBank Digital Teller prototype: where each piece lives, status, and what still needs building. Companion: [`PERSISTENCE.md`](./PERSISTENCE.md), [`AGENTS.md`](../AGENTS.md), [`NEXT_PHASE.md`](./NEXT_PHASE.md).

Statuses: **done** · **mock-wired** · **partial** · **stub** (absent)

---

## Screens (Expo navigation)

| Piece | Where | Status | Next build-out |
|-------|--------|--------|----------------|
| Splash | `src/components/loading/SplashScreen.tsx` | done | — |
| Welcome | `src/screens/onboarding/WelcomeScreen.tsx` | done | — |
| Keys / BackupQuiz / SetPin | `src/screens/onboarding/*` | done | Biometric policy polish |
| Home | `src/screens/home/HomeScreen.tsx` | mock-wired | Live balances; held-symbol OHLC; Cora art |
| Convert | `src/screens/convert/ConvertScreen.tsx` | mock-wired | Staging rates; WS `rate.tick` |
| Pay | `src/screens/pay/PayScreen.tsx` | partial | Editable mix; drop hardcoded $2400 |
| Send | `src/screens/send/SendScreen.tsx` | partial | Editable amount/recipient |
| Receive | `src/screens/receive/ReceiveScreen.tsx` | mock-wired | Real QR from wallet address; derive CTA |
| Profile | `src/screens/profile/ProfileScreen.tsx` | mock-wired | Live vault APIs |
| PosLab | `src/screens/pos/MockPosScreen.tsx` | mock-wired | HCE + VTS/MDES |
| BankLink / Fund | — | stub | Onboarding steps 3–4 |
| Activity tab | — | stub | Design had it; app uses Send/Receive |

Nav: `src/navigation/{Root,Onboarding,Main,Tab}Navigator.tsx`

---

## Feature modules (`src/features/`)

| Module | Where | Status | Next build-out |
|--------|--------|--------|----------------|
| portfolio | `portfolio/*` | mock-wired | Chain balances; server truth |
| history | `history/*` | mock-wired | Real OHLC feeder; P1-only RAM load |
| wallets | `wallets/*` + `registry.ts` | partial | Per-coin modules; SQLite index |
| market | `market/ratesCache.ts` | mock-wired | Persist snapshot; P1 pair-only |
| quotes / convert | `quotes/*`, `convert/*` | mock-wired | Live PriceCache alias |
| transfers | `transfers/*` | mock-wired | Wire Pay/Send UI |
| receive | `receive/*` | mock-wired | Request-amount |
| session / vault | `session/*`, `vault/*` | partial | Live session tokens; TEE |
| prefs | `prefs/*` | mock-wired | SQLite mirror |
| cora | `cora/*` | done (copy) | Avatar asset |
| pos | `pos/*` | partial | HCE HostApduService |
| assets | `assets/assetConfig.ts` | done | Securities flag |
| **persist** | `persist/*` | **this slice** | Device SQLite |
| **bootstrap** | `bootstrap/*` | **this slice** | P0–P3 activation |

---

## Design HTML (`designs/`)

| File | Role | App mapping |
|------|------|-------------|
| `Cipherbank App.dc.html` | Static 7-screen row | Screen layout reference |
| `Cipherbank Prototype.dc.html` | Clickable async UX | Skeleton / rate-lock / toasts |
| `Cipherbank Landing*.dc.html` | Marketing | Not in Expo |
| `Cipherbank Build Spec.dc.html` | Tokens + build order | Theme / ops |
| `Cipherbank Assets.dc.html` | Asset gallery | `assets/` |

Do **not** port `support.js` / `ios-frame.jsx` into the app.

---

## Contracts & docs

| Doc | Where |
|-----|--------|
| `/v1` shapes | `src/mocks/API_CONTRACT.md` |
| POS | `src/mocks/POS_API.md` |
| Custody | `docs/CUSTODY.md` |
| Monero link | `docs/MONERO_LINK.md` |
| Persistence / precedence | `docs/PERSISTENCE.md` |
| This map | `docs/PROTOTYPE_MAP.md` |

---

## Which goes where (data)

| Data | Device store | RAM policy | Server later |
|------|--------------|------------|--------------|
| Seed / PIN | SecureStore | P0 unlock session only | Never |
| Wallet public meta | SQLite `wallets` | P2 index; P0 active subset | `/wallets` |
| Prefs | SQLite `prefs` (+ migrate AsyncStorage) | P2 | `/prefs` |
| Rates snapshot | SQLite `rates_snapshot` | P2 held symbols; P1 pair | `/rates`, PriceCache |
| OHLC history | SQLite `market_ohlc` | **P1 only** for requested window | `/history` |
| Portfolio totals | React Query (network) | Shell; not bulk-cached offline yet | `/portfolio` |

---

## Homepage graph expectation

Device-held wallet symbols (from SQLite wallet index ∪ portfolio holdings) drive `GET /history` compare series so Performance / sparkline include user wallets—not a hardcoded BTC/ETH pair alone.
