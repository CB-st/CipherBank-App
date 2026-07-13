import { useCallback, useState } from 'react';
import { authorizePos, confirmPos, createPosSession } from './pos.api';
import { mockTap, nfcIsSupported, nfcPresent } from './nfc';
import { runSimulatedExchange, type ExchangeStage } from './nfcExchange';
import { requireAuth } from '@/features/vault/requireAuth';
import type { NfcPresentPayload, PosAuthorizeResult, PosSession } from './pos.types';
import { requireTestCard, type HardwareCard, isHardwareTestCard } from './hardwareCards';

type Phase = 'idle' | 'unlocking' | 'authorizing' | 'presenting' | 'exchanging' | 'done' | 'error';

export function usePosPay() {
  const [phase, setPhase] = useState<Phase>('idle');
  const [session, setSession] = useState<PosSession | null>(null);
  const [auth, setAuth] = useState<PosAuthorizeResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lastTap, setLastTap] = useState<NfcPresentPayload | null>(null);
  const [exchangeStages, setExchangeStages] = useState<ExchangeStage[]>([]);

  const reset = useCallback(() => {
    setPhase('idle');
    setSession(null);
    setAuth(null);
    setError(null);
    setLastTap(null);
    setExchangeStages([]);
  }, []);

  const startAndAuthorize = useCallback(
    async (opts: {
      amount: string;
      currency?: string;
      merchantId?: string;
      label?: string;
      sources: { asset: string; value: string }[];
      card: HardwareCard;
    }) => {
      setError(null);
      setExchangeStages([]);
      if (requireTestCard() && !isHardwareTestCard(opts.card)) {
        setPhase('error');
        setError('test_card_required: select a hardware-test card for the POS lab.');
        return null;
      }

      setPhase('unlocking');
      const unlocked = await requireAuth({ reason: 'pos_authorize', force: true });
      if (!unlocked) {
        setPhase('error');
        setError('wallet_locked: unlock cancelled.');
        return null;
      }

      try {
        setPhase('authorizing');
        const sess = await createPosSession({
          merchantId: opts.merchantId ?? 'merchant_lab_sunset',
          amount: opts.amount,
          currency: opts.currency ?? 'USD',
          label: opts.label ?? 'POS lab',
          posDeviceId: 'lab_mock_pos',
        });
        setSession(sess);

        const result = await authorizePos({
          sessionId: sess.sessionId,
          sources: opts.sources,
          cardId: opts.card.id,
          deviceAttestation: 'unlocked_local_custody_v1',
        });
        setAuth(result);

        const confirmed = await confirmPos(sess.sessionId);
        setSession((s) => (s ? { ...s, status: confirmed.status, presentment: result.presentment } : s));
        setPhase('presenting');
        return result;
      } catch (e: any) {
        setPhase('error');
        setError(e?.detail?.code ? e.detail.code + ': ' + e.message : e?.message ?? 'POS authorize failed');
        return null;
      }
    },
    [],
  );

  const presentMock = useCallback(async () => {
    if (!auth || !session) return;
    // Anti-skimming: re-auth immediately before token leaves the device.
    setPhase('unlocking');
    const ok = await requireAuth({ reason: 'pos_present', force: true });
    if (!ok) {
      setPhase('presenting');
      setError('presentment_locked: unlock cancelled before terminal handoff.');
      return;
    }
    const payload: NfcPresentPayload = {
      sessionId: session.sessionId,
      tokenRef: auth.presentment.tokenRef,
      merchantId: session.merchantId,
    };
    setPhase('exchanging');
    const exchange = await runSimulatedExchange({
      payload,
      brand: auth.presentment.brand,
      onUpdate: setExchangeStages,
    });
    mockTap(payload);
    setLastTap(exchange.payload);
    setPhase('done');
    return exchange;
  }, [auth, session]);

  const presentNfc = useCallback(async () => {
    if (!auth || !session) return;
    setPhase('unlocking');
    const ok = await requireAuth({ reason: 'pos_present', force: true });
    if (!ok) {
      setPhase('presenting');
      setError('presentment_locked: unlock cancelled before NFC handoff.');
      return;
    }
    const payload: NfcPresentPayload = {
      sessionId: session.sessionId,
      tokenRef: auth.presentment.tokenRef,
      merchantId: session.merchantId,
    };
    const support = await nfcIsSupported();
    if (!support.supported) {
      setPhase('exchanging');
      const exchange = await runSimulatedExchange({
        payload,
        brand: auth.presentment.brand,
        onUpdate: setExchangeStages,
      });
      setLastTap(exchange.payload);
      setPhase('done');
      return { ok: true as const, mode: 'stub' as const, detail: support.reason, exchange };
    }
    setPhase('exchanging');
    const res = await nfcPresent(payload);
    if (res.ok) {
      setLastTap(payload);
      await runSimulatedExchange({
        payload,
        brand: auth.presentment.brand,
        stepMs: 80,
        onUpdate: setExchangeStages,
      });
      setPhase('done');
    } else {
      setError(res.detail ?? 'NFC failed');
      setPhase('presenting');
    }
    return res;
  }, [auth, session]);

  return {
    phase,
    session,
    auth,
    error,
    lastTap,
    exchangeStages,
    reset,
    startAndAuthorize,
    presentMock,
    presentNfc,
    checkNfc: nfcIsSupported,
  };
}
