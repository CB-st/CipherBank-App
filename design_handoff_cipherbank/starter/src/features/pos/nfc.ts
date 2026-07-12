import { Platform } from 'react-native';
import type { NfcPresentPayload } from './pos.types';

export type NfcSupport = {
  supported: boolean;
  enabled: boolean;
  platform: typeof Platform.OS;
  reason?: string;
};

/**
 * Platform NFC adapter.
 * Android: react-native-nfc-manager when available (dev client / EAS).
 * iOS/web: stub — same API surface, clear unsupported reason.
 *
 * v1 presentment writes an NDEF text/JSON payload with sessionId + tokenRef only.
 * Real HCE payment APDUs are processor-specific and out of scope here.
 */
export async function nfcIsSupported(): Promise<NfcSupport> {
  if (Platform.OS === 'web') {
    return { supported: false, enabled: false, platform: 'web', reason: 'NFC not available on web — use Simulate tap.' };
  }
  if (Platform.OS === 'ios') {
    return {
      supported: false,
      enabled: false,
      platform: 'ios',
      reason: 'Consumer HCE is Android-first. iOS uses different Apple programs — stub only for now.',
    };
  }
  try {
    // Dynamic require so web bundles do not crash if native module is missing.
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const NfcManager = require('react-native-nfc-manager').default;
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const { NfcEvents } = require('react-native-nfc-manager');
    void NfcEvents;
    await NfcManager.start();
    const supported = await NfcManager.isSupported();
    const enabled = supported ? await NfcManager.isEnabled() : false;
    return { supported, enabled, platform: 'android' };
  } catch {
    return {
      supported: false,
      enabled: false,
      platform: 'android',
      reason: 'NFC native module unavailable — use a development/EAS build, or Simulate tap.',
    };
  }
}

export async function nfcPresent(payload: NfcPresentPayload): Promise<{ ok: boolean; mode: 'nfc' | 'stub'; detail?: string }> {
  const support = await nfcIsSupported();
  if (!support.supported || Platform.OS !== 'android') {
    return {
      ok: false,
      mode: 'stub',
      detail: support.reason ?? 'NFC unsupported on this platform',
    };
  }
  try {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const NfcManager = require('react-native-nfc-manager').default;
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const { NfcTech, Ndef } = require('react-native-nfc-manager');

    await NfcManager.requestTechnology(NfcTech.Ndef, {
      alertMessage: 'Hold near the POS reader to present CipherBank token',
    });
    const bytes = Ndef.encodeMessage([
      Ndef.textRecord(JSON.stringify({ v: 1, ...payload })),
    ]);
    if (bytes) {
      await NfcManager.ndefHandler.writeNdefMessage(bytes);
    }
    await NfcManager.cancelTechnologyRequest().catch(() => {});
    return { ok: true, mode: 'nfc' };
  } catch (e: any) {
    try {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const NfcManager = require('react-native-nfc-manager').default;
      await NfcManager.cancelTechnologyRequest();
    } catch {
      /* ignore */
    }
    return { ok: false, mode: 'nfc', detail: e?.message ?? 'NFC presentment failed' };
  }
}

/** Mock tap for lab / web — records the same payload shape without RF. */
export function mockTap(payload: NfcPresentPayload): { ok: true; mode: 'stub'; payload: NfcPresentPayload } {
  return { ok: true, mode: 'stub', payload };
}
