import { useCallback, useState } from 'react';
import { authorizePos, confirmPos, createPosSession } from './pos.api';
import { mockTap, nfcIsSupported, nfcPresent } from './nfc';
import { unlockLocalCustody } from '@/features/vault/custody';
import type { NfcPresentPayload, PosAuthorizeResult, PosSession } from './pos.types';
import { requireTestCard, type HardwareCard, isHardwareTestCard } from './hardwareCards';

type Phase = 'idle' | 'unlocking' | 'authorizing' | 'presenting' | 'done' | 'error';

export function usePosPay() {
  const [phase, setPhase] = useState<Phase>('idle');
  const [session, setSession] = useState<PosSession | null>(null);
  const [auth, setAuth] = useState<PosAuthorizeResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lastTap, setLastTap] = useState<NfcPresentPayload | null>(null);

  const reset = useCallback(() => {
    setPhase('idle');
    setSession(null);
    setAuth(null);
    setError(null);
    setLastTap(null);
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
      if (requireTestCard() && !isHardwareTestCard(opts.card)) {
        setPhase('error');
        setError('test_card_required: select a hardware-test card for the POS lab.');
        return null;
      }

      setPhase('unlocking');
      const unlocked = await unlockLocalCustody();
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
    const payload: NfcPresentPayload = {
      sessionId: session.sessionId,
      tokenRef: auth.presentment.tokenRef,
      merchantId: session.merchantId,
    };
    const res = mockTap(payload);
    setLastTap(res.payload);
    setPhase('done');
    return res;
  }, [auth, session]);

  const presentNfc = useCallback(async () => {
    if (!auth || !session) return;
    const payload: NfcPresentPayload = {
      sessionId: session.sessionId,
      tokenRef: auth.presentment.tokenRef,
      merchantId: session.merchantId,
    };
    const support = await nfcIsSupported();
    if (!support.supported) {
      setError(support.reason ?? 'nfc_not_supported');
      return { ok: false as const, mode: 'stub' as const, detail: support.reason };
    }
    const res = await nfcPresent(payload);
    if (res.ok) {
      setLastTap(payload);
      setPhase('done');
    } else {
      setError(res.detail ?? 'NFC failed');
    }
    return res;
  }, [auth, session]);

  return {
    phase,
    session,
    auth,
    error,
    lastTap,
    reset,
    startAndAuthorize,
    presentMock,
    presentNfc,
    checkNfc: nfcIsSupported,
  };
}
