# Cipherbank — Frontend Architecture & UI↔Backend Contract

Companion to `README.md`. This document defines the **file layout**, **component tree**, and — most importantly — **how the responsive UI talks to an asynchronous backend** so the interface stays instant while the backend does slow work (time-of-flight database loads, network settlement, rate quoting).

The clickable reference for every pattern below is `designs/Cipherbank Prototype.dc.html` — open it in a browser and tap around. It fakes all data client-side purely to demonstrate the *interaction contract*; this doc maps each behavior to real endpoints, cache, and streams.

---

## 1. Guiding principle: the shell never waits

> **Render the shell synchronously. Stream data into it asynchronously. Confirm actions optimistically.**

Three rules, applied everywhere:

1. **Shell-first.** Navigation chrome (tab bar, headers, Cora bar, buttons) renders from static/cached state on frame 1 — it never depends on a network call.
2. **Data streams in.** Balances, rates, transactions arrive async; while in flight the UI shows **skeletons** (shimmer placeholders), not spinners over a blank screen, and never a frozen tap target.
3. **Actions are optimistic.** A user action flips to a **pending** UI immediately (button spinner + toast), then reconciles when the backend settles out of band. Failures roll back with a clear error, not a silent hang.

This is what keeps the app feeling native-fast even when a database read or a settlement takes seconds.

---

## 2. Recommended stack & file layout

Mobile-first. The layout below assumes **React Native (Expo) + TypeScript**; the same structure maps 1:1 to a React PWA (swap `screens/` for routes) or SwiftUI (swap folders for feature modules).

```
cipherbank-app/
├─ app.json / expo config
├─ src/
│  ├─ app/                     # navigation + providers
│  │  ├─ App.tsx               # QueryClientProvider, ThemeProvider, NavigationContainer
│  │  ├─ TabNavigator.tsx      # Home · Convert · Pay · Send · Receive (the persistent shell)
│  │  └─ OnboardingStack.tsx   # Welcome → Keys → BankLink → Fund
│  │
│  ├─ theme/                   # ← generated from /tokens
│  │  ├─ tokens.ts             # import ../../tokens/tokens.json (typed)
│  │  ├─ colors.ts  typography.ts  radius.ts  shadows.ts
│  │  └─ index.ts
│  │
│  ├─ components/              # presentational, stateless, token-driven
│  │  ├─ primitives/           # Button, Card, Pill, Chip, Sheet, Toast, Skeleton
│  │  ├─ money/                # AssetGlyph, AmountInput, AssetSelector, BalanceHero,
│  │  │                        #   RateLockStrip, FundingMixBar, TxRow
│  │  ├─ cora/                 # CoraBar (avatar slot + line), CoraQuote
│  │  └─ chrome/               # Header, TabBar, ScreenScaffold, ConnectionChip
│  │
│  ├─ screens/                 # one folder per screen; composes components + hooks
│  │  ├─ home/        HomeScreen.tsx
│  │  ├─ convert/     ConvertScreen.tsx
│  │  ├─ pay/         PayScreen.tsx
│  │  ├─ send/        SendScreen.tsx
│  │  ├─ receive/     ReceiveScreen.tsx
│  │  └─ onboarding/  WelcomeScreen.tsx  KeysScreen.tsx  …
│  │
│  ├─ features/                # domain logic, hooks, state (the async layer)
│  │  ├─ portfolio/   usePortfolio.ts   portfolio.api.ts   portfolio.types.ts
│  │  ├─ quotes/      useQuoteLock.ts   quotes.api.ts
│  │  ├─ convert/     useConvert.ts     convert.api.ts
│  │  ├─ transfers/   useSend.ts  usePayMix.ts  transfers.api.ts
│  │  ├─ receive/     useReceive.ts
│  │  └─ session/     useSession.ts     custody.ts   (self-custody keys, biometrics)
│  │
│  ├─ lib/
│  │  ├─ apiClient.ts          # fetch wrapper: auth, retries, idempotency keys
│  │  ├─ queryClient.ts        # React Query config (staleTime, cache, offline)
│  │  ├─ socket.ts             # websocket/SSE for rate + settlement streams
│  │  ├─ money.ts              # formatUSD, formatAsset, bignumber math
│  │  └─ optimistic.ts         # helpers for optimistic mutate + rollback
│  │
│  └─ assets/                  # ← copied from /assets (logos, glyphs, ui icons)
│
├─ tokens/                     # ← from this handoff (source of truth)
│  ├─ tokens.json  tokens.css
└─ assets/                     # ← from this handoff (svgs + manifest.json)
```

**Boundaries that matter:**
- `components/` are **pure and stateless** — props in, JSX out, styled only from `theme/`. They never call the network.
- `features/*/use*.ts` **own all async** — data fetching, caching, optimistic mutations, streams. Screens read from these hooks.
- `lib/apiClient.ts` and `lib/queryClient.ts` are the **only** places that know about HTTP/cache mechanics.

---

## 3. State model — three tiers

| Tier | Lives in | Examples | Tool |
|---|---|---|---|
| **Server cache** | React Query (or SWR / TanStack) | portfolio, quotes, tx history, recipients | `useQuery` / `useMutation` |
| **Session** | Context (`useSession`) | auth token, custody unlock, biometrics, `valuesHidden` | React Context |
| **Ephemeral UI** | local `useState` in screen | convert amount, chosen speed, mix sources, lock countdown | component state |

Rule of thumb: **if the backend is the source of truth, it belongs in the server-cache tier** (never mirror it into `useState`). Form inputs and timers are ephemeral.

---

## 4. Time-of-flight loading (the async DB read)

**Pattern:** shell renders instantly → query fires → skeleton until resolved → data fades in. Stale-while-revalidate keeps subsequent visits instant.

```ts
// features/portfolio/usePortfolio.ts
export function usePortfolio() {
  return useQuery({
    queryKey: ['portfolio'],
    queryFn: () => api.get<Portfolio>('/v1/portfolio'),
    staleTime: 15_000,          // serve cached instantly for 15s
    placeholderData: keepPreviousData, // never flash empty on refetch
  });
}
```

```tsx
// screens/home/HomeScreen.tsx
const { data, isLoading, isError, refetch } = usePortfolio();

return (
  <ScreenScaffold header={<Header/>} tabBar={<TabBar/>}>   {/* shell: synchronous */}
    <CoraBar line={isLoading ? CORA.loading : CORA.home} />
    {isLoading ? <BalanceHero.Skeleton/> : <BalanceHero total={data.total} change={data.change}/>}
    {isLoading
      ? <AssetList.Skeleton rows={4}/>
      : isError
        ? <ErrorCard onRetry={refetch}/>          {/* explicit error, not a hang */}
        : <AssetList assets={data.assets}/>}
  </ScreenScaffold>
);
```

Guidelines:
- **Skeletons mirror final layout** (same row heights/spacing) so nothing reflows when data lands. Use the shimmer `Skeleton` primitive — see `.sk` in the prototype.
- **Progressive hydration:** if the DB read is genuinely slow, split it — return the cheap summary (total + top 3 assets) first, lazy-load the full list and history behind it. Each slice is its own query with its own skeleton.
- **Prefetch on intent:** `queryClient.prefetchQuery` for the screen a tab is about to open (e.g. prefetch quotes when Home mounts) so Convert opens warm.
- **Offline / cold cache:** hydrate React Query from persisted storage on launch so a returning user sees last-known balances instantly, with a subtle "updating…" indicator while revalidating. The `ConnectionChip` (live/offline dot) reflects socket state.

---

## 5. Live rate-lock (a streamed value that can't exist at load)

Convert quotes are **server-issued, client-counted**. The server returns a quote with a TTL; the client renders a countdown and requests a fresh quote on expiry. Never compute the rate on the client.

```ts
// features/quotes/useQuoteLock.ts
export function useQuoteLock(from: Asset, to: Asset, amount: string) {
  const [secondsLeft, setSecondsLeft] = useState(0);

  const { data: quote, refetch } = useQuery({
    queryKey: ['quote', from, to, amount],
    queryFn: () => api.post<Quote>('/v1/quotes', { from, to, amount }), // {rate, expiresAt, quoteId}
    enabled: !!amount,
    refetchOnWindowFocus: false,
  });

  useEffect(() => {                           // client-side countdown
    if (!quote) return;
    const id = setInterval(() => {
      const left = Math.max(0, Math.round((quote.expiresAt - Date.now()) / 1000));
      setSecondsLeft(left);
      if (left === 0) refetch();              // auto re-lock a fresh quote
    }, 1000);
    return () => clearInterval(id);
  }, [quote]);

  return { quote, secondsLeft, expired: secondsLeft === 0, relock: refetch };
}
```

- The `RateLockStrip` component is dumb — it just renders `rate`, `secondsLeft`, and an `expired` flag (gold → red styling per the design).
- **Critical:** the `quoteId` must be passed to the convert mutation so the backend settles at the *quoted* rate, not the live one. If the quote expired mid-tap, block the action and re-quote.
- For true real-time ticking prices elsewhere (tickers, portfolio deltas), subscribe via `lib/socket.ts` (websocket/SSE) and write into the query cache with `queryClient.setQueryData` — don't poll.

---

## 6. Optimistic actions (Convert / Send / Pay)

Every money-moving action follows the same lifecycle. The UI commits **immediately**; the backend settles asynchronously; the UI reconciles on the settlement event.

```
tap → validate locally → OPTIMISTIC pending (spinner + toast)
    → POST with idempotency key
    → await ack (accepted, txId) …………………… feels instant to user
    → settlement arrives (socket or poll) → SETTLED (success toast, balances update)
    → on error → ROLLBACK optimistic change + error toast + retry affordance
```

```ts
// features/convert/useConvert.ts
export function useConvert() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { quoteId: string; amount: string }) =>
      api.post('/v1/convert', v, { idempotencyKey: uuid() }),   // idempotency = safe retries
    onMutate: async (v) => {                        // OPTIMISTIC
      await qc.cancelQueries({ queryKey: ['portfolio'] });
      const prev = qc.getQueryData(['portfolio']);
      qc.setQueryData(['portfolio'], applyConvertLocally(prev, v));
      return { prev };
    },
    onError: (_e, _v, ctx) => qc.setQueryData(['portfolio'], ctx.prev), // ROLLBACK
    onSettled: () => qc.invalidateQueries({ queryKey: ['portfolio'] }), // reconcile w/ truth
  });
}
```

UI mapping (see prototype `doConvert` / `doSend`):
- **Pending:** button shows inline spinner + label swaps ("Convert instantly" → "Settling…"), button disabled to prevent double-submit, toast `kind:'pending'` (purple, spinner icon).
- **Settled:** success toast `kind:'ok'` (green, check icon) with the settled figure + fee; balances already updated optimistically, now confirmed.
- **Async settlement channel:** the definitive settled event should come over the socket (`transfer.settled`, `convert.settled`) so multi-second network settlement updates the UI without polling. Fall back to poll-on-`onSettled` if the socket is down.
- **Idempotency keys** on every mutation make retries (flaky network, app resumed mid-flight) safe — the backend dedupes.

### Pay-with-a-mix specifics
- The **funding mix** is client-assembled `sources[]`; the UI blocks confirm until `sum(sources) ≥ total` (the stacked `FundingMixBar` shows coverage).
- On confirm, send the mix to the backend which **mediates the multi-asset exchange server-side** and settles clean funds to the recipient. The UI shows one pending→settled lifecycle for the whole payment, with a per-source breakdown in the receipt. The recipient never sees the mix.

---

## 7. Error, empty & edge states (design for all of them)

| State | UI treatment |
|---|---|
| Loading | Shimmer skeletons matching final layout |
| Empty (new account) | Illustrated empty card + primary CTA (e.g. "Add funds") |
| Error (fetch) | Inline `ErrorCard` with retry — never a blank screen |
| Offline | `ConnectionChip` → offline; show cached data + "reconnecting" banner; queue mutations |
| Rate expired | Red lock strip, auto re-quote, block stale confirm |
| Insufficient balance | Disable CTA, inline hint under amount |
| Pending too long | After ~10s, toast → "Still settling — we'll notify you" and release the UI |
| Settlement failed | Rollback + error toast + retry; keep the user's inputs |

---

## 8. Performance & responsiveness checklist

- Shell (tab bar, header, Cora) renders < 16ms, independent of any query.
- All lists virtualized (`FlashList`/`FlatList`) — asset lists and history can grow.
- Heavy formatting (bignumber money math) memoized; never block the JS thread on a tap.
- Optimistic first, network second — no await between tap and visible feedback.
- Skeletons over spinners; stale-while-revalidate over refetch-blocking.
- Prefetch the likely-next screen's data on navigation intent.
- Debounce amount inputs before re-quoting (e.g. 300ms) to avoid quote spam.
- One in-flight mutation per action (disable button while pending) + idempotency keys.

---

## 9. Endpoint sketch (align with backend)

| Concern | Endpoint | Notes |
|---|---|---|
| Portfolio | `GET /v1/portfolio` | total, assets[], change; cache 15s, socket deltas |
| Quote | `POST /v1/quotes` | `{from,to,amount}` → `{quoteId,rate,expiresAt}` |
| Convert | `POST /v1/convert` | `{quoteId,amount}` + idempotency key |
| Send | `POST /v1/transfers` | `{recipient,amount,source,speed}` instant\|ach |
| Pay mix | `POST /v1/payments` | `{recipient,total,sources[]}` server-mediated |
| Receive | `GET /v1/receive/:asset` | handle + address + optional requested amount |
| Streams | `WSS /v1/stream` | `rate.tick`, `transfer.settled`, `convert.settled`, `balance.update` |
| Session | `POST /v1/session` | self-custody: keys generated on-device, never sent |

Keep the **open API standard** in mind: these consumer endpoints are the same contract banks/developers embed, so version them (`/v1`) and document request/response shapes as the public standard.
