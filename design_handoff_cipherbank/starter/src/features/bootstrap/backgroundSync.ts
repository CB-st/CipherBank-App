import { AppState, type AppStateStatus, type NativeEventSubscription } from 'react-native';
import * as Battery from 'expo-battery';
import type { Subscription } from 'expo-modules-core';
import { jobQueue } from './jobQueue';
import { heldSymbolsFromWallets } from '@/features/persist/walletsRepo';
import {
  upsertRatesSnapshot,
  upsertOhlcPoints,
  setSyncMeta,
  getSyncMeta,
} from '@/features/persist/marketRepo';
import { fetchRatesViaPublicApi } from '@/features/market/publicMarket.api';
import { getHistoryBulk, type HistoryResponse } from '@/features/history/history.api';

/** Quiet period after last P0/P1 before P3 may run. */
export const P3_QUIET_MS = 3 * 60_000;

type BackgroundSyncOptions = {
  /** Returns ms timestamp of last interactive (P0/P1) activation. */
  getLastInteractiveAt: () => number;
  /** When true, force-allow P3 (tests). */
  forceCharging?: boolean;
};

let started = false;
let appSub: NativeEventSubscription | null = null;
let battSub: Subscription | null = null;
let tickTimer: ReturnType<typeof setInterval> | null = null;
let charging = false;
let appActive = true;

async function refreshChargeState(force?: boolean) {
  if (force != null) {
    charging = force;
    return;
  }
  try {
    const available = await Battery.isAvailableAsync();
    if (!available) {
      // Degrade: treat as not charging so we don't surprise-drain; P3 stays paused.
      charging = false;
      return;
    }
    const state = await Battery.getBatteryStateAsync();
    charging =
      state === Battery.BatteryState.CHARGING || state === Battery.BatteryState.FULL;
  } catch {
    charging = false;
  }
}

function enqueueP3Jobs() {
  jobQueue.enqueue({
    id: 'p3-rates',
    priority: 3,
    run: async () => {
      const held = await heldSymbolsFromWallets();
      const snap = await fetchRatesViaPublicApi();
      const rows = held.length
        ? snap.rates.filter((r) => held.includes(r.symbol.toUpperCase()))
        : snap.rates.slice(0, 8);
      await upsertRatesSnapshot(rows);
      await setSyncMeta('last_p3_rates', String(Date.now()));
    },
  });

  void (async () => {
    const held = await heldSymbolsFromWallets();
    for (const symbol of held) {
      jobQueue.enqueue({
        id: `p3-ohlc-${symbol}`,
        priority: 3,
        symbol,
        run: async () => {
          const last = await getSyncMeta(`ohlc_filled_${symbol}`);
          if (last && Date.now() - Number(last) < 60 * 60_000) return;
          const hist: HistoryResponse = await getHistoryBulk({
            range: '1M',
            symbols: [symbol],
            granularity: '1h',
          });
          for (const series of hist.series) {
            if (series.symbol === 'WALLET') continue;
            await upsertOhlcPoints(series.symbol, series.granularity ?? '1h', series.points);
          }
          await setSyncMeta(`ohlc_filled_${symbol}`, String(Date.now()));
        },
      });
    }
  })();
}

function evaluate(opts: BackgroundSyncOptions) {
  const quiet = Date.now() - opts.getLastInteractiveAt() >= P3_QUIET_MS;
  const allow = appActive && charging && quiet;
  jobQueue.setPauseP3(!allow);
  if (allow) enqueueP3Jobs();
}

/**
 * Best-effort P3 watcher: idle (quiet after interactive) + charging.
 * Not a true OS daemon — runs while the JS process is alive.
 */
export function startBackgroundSync(opts: BackgroundSyncOptions): () => void {
  if (started) return () => {};
  started = true;

  void refreshChargeState(opts.forceCharging).then(() => evaluate(opts));

  appSub = AppState.addEventListener('change', (next: AppStateStatus) => {
    appActive = next === 'active';
    evaluate(opts);
  });

  try {
    battSub = Battery.addBatteryStateListener(() => {
      void refreshChargeState(opts.forceCharging).then(() => evaluate(opts));
    });
  } catch {
    /* web / unsupported */
  }

  tickTimer = setInterval(() => evaluate(opts), 60_000);

  return () => {
    started = false;
    appSub?.remove();
    battSub?.remove();
    if (tickTimer) clearInterval(tickTimer);
    appSub = null;
    battSub = null;
    tickTimer = null;
    jobQueue.setPauseP3(true);
  };
}
