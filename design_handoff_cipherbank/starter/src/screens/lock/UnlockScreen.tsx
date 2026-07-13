import React, { useCallback, useEffect, useRef, useState } from 'react';
import { View, Text, Pressable, Platform, useWindowDimensions, Image } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import * as LocalAuthentication from 'expo-local-authentication';
import { color, radius, font, shadow } from '@/theme';
import { Button } from '@/components/primitives/Button';
import { useSession } from '@/features/session/useSession';
import { hasPin, pinLockRemainingMs } from '@/features/vault/pinStore';
import {
  canUseDeviceOwnerAuth,
  ensureDemoCustody,
  unlockLocalCustody,
} from '@/features/vault/custody';
import { isSeedDemo, isMockApi } from '@/lib/runtimeFlags';

const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '', '0', '⌫'] as const;
const brandMark = require('../../../assets/logo/cipherbank-app-icon.png');
const IS_MOCK = isMockApi() || isSeedDemo();

/**
 * Full-screen app lock.
 * Primary path: Android/iOS system UI (fingerprint when enrolled, else device PIN/pattern).
 * In-app CipherBank PIN pad is a last-resort fallback (web / no device lock / demo).
 */
export function UnlockScreen() {
  const { unlock } = useSession();
  const insets = useSafeAreaInsets();
  const { height } = useWindowDimensions();
  const third = height / 3;
  const autoPrompted = useRef(false);

  const [pin, setPin] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hint, setHint] = useState<string | null>(null);
  const [osAuthAvailable, setOsAuthAvailable] = useState(Platform.OS !== 'web');
  const [bioTypes, setBioTypes] = useState<LocalAuthentication.AuthenticationType[]>([]);
  const [showAppPin, setShowAppPin] = useState(false);
  const [appPinSet, setAppPinSet] = useState(false);

  const refreshAuthCapabilities = useCallback(async () => {
    setAppPinSet(await hasPin());
    if (Platform.OS === 'web') {
      setOsAuthAvailable(false);
      setShowAppPin(true);
      return;
    }
    try {
      const canOs = await canUseDeviceOwnerAuth();
      setOsAuthAvailable(canOs);
      const types = canOs ? await LocalAuthentication.supportedAuthenticationTypesAsync() : [];
      setBioTypes(types);
      // Show app PIN pad only when OS unlock isn't available.
      setShowAppPin(!canOs);
    } catch {
      setOsAuthAvailable(false);
      setShowAppPin(true);
    }
  }, []);

  useEffect(() => {
    void refreshAuthCapabilities();
  }, [refreshAuthCapabilities]);

  const finishOk = useCallback(
    async (pinArg?: string, skipBio?: boolean) => {
      return unlock(pinArg, { skipBiometrics: skipBio, reason: 'app_unlock' });
    },
    [unlock],
  );

  /** Opens the system fingerprint sheet and/or built-in device PIN keypad. */
  const unlockWithDevice = useCallback(async () => {
    if (busy) return;
    setBusy(true);
    setError(null);
    setHint(null);

    const remaining = await pinLockRemainingMs();
    if (remaining > 0) {
      setError('Too many attempts — try again in ' + Math.ceil(remaining / 1000) + 's');
      setBusy(false);
      return;
    }

    if (osAuthAvailable) {
      const osOk = await unlockLocalCustody({
        force: true,
        promptMessage: 'Unlock CipherBank',
      });
      if (osOk) {
        await finishOk(undefined, true);
        setBusy(false);
        return;
      }
      // User cancelled or OS auth failed — offer app PIN only as secondary path.
      if (appPinSet) {
        setShowAppPin(true);
        setHint('Cancelled — unlock with fingerprint, device PIN, or CipherBank PIN');
      } else if (isSeedDemo()) {
        try {
          await ensureDemoCustody();
          setAppPinSet(await hasPin());
          setShowAppPin(true);
          setHint('Use fingerprint / device PIN, or CipherBank PIN 000000');
        } catch {
          setError('Could not unlock this device');
        }
      } else {
        setHint('Try again with fingerprint or your device PIN');
      }
      setBusy(false);
      return;
    }

    // No OS lock enrolled
    if (!appPinSet) {
      if (isSeedDemo()) {
        try {
          await ensureDemoCustody();
          setAppPinSet(await hasPin());
          setShowAppPin(true);
          setHint('Set a device PIN in Android Settings, or use demo PIN 000000');
        } catch {
          setError('Could not unlock this device');
        }
        setBusy(false);
        return;
      }
      const ok = await finishOk(undefined, true);
      if (!ok) setError('Set a screen lock in Android Settings to unlock');
      setBusy(false);
      return;
    }

    setShowAppPin(true);
    setHint('Enter your CipherBank PIN');
    setBusy(false);
  }, [busy, osAuthAvailable, appPinSet, finishOk]);

  // Auto-open the system unlock sheet once when OS auth is ready.
  useEffect(() => {
    if (autoPrompted.current || !osAuthAvailable || busy) return;
    autoPrompted.current = true;
    const t = setTimeout(() => {
      void unlockWithDevice();
    }, 400);
    return () => clearTimeout(t);
  }, [osAuthAvailable, busy, unlockWithDevice]);

  const submitPin = useCallback(
    async (code: string) => {
      if (code.length !== 6 || busy) return;
      setBusy(true);
      setError(null);
      setHint(null);
      const remaining = await pinLockRemainingMs();
      if (remaining > 0) {
        setError('Too many attempts — try again in ' + Math.ceil(remaining / 1000) + 's');
        setPin('');
        setBusy(false);
        return;
      }
      let ok = await finishOk(code, true);
      if (!ok && isSeedDemo() && code === '000000') {
        try {
          await ensureDemoCustody();
          setAppPinSet(await hasPin());
          ok = await finishOk('000000', true);
        } catch {
          /* keep failure */
        }
      }
      if (!ok) {
        setError('Incorrect PIN');
        setPin('');
      }
      setBusy(false);
    },
    [busy, finishOk],
  );

  useEffect(() => {
    if (pin.length === 6) void submitPin(pin);
  }, [pin, submitPin]);

  const onKey = (k: string) => {
    if (busy) return;
    setError(null);
    setHint(null);
    if (k === '⌫') setPin((p) => p.slice(0, -1));
    else if (k && pin.length < 6) setPin((p) => p + k);
  };

  const hasFingerprint = bioTypes.includes(LocalAuthentication.AuthenticationType.FINGERPRINT);
  const hasFace = bioTypes.includes(LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION);
  const unlockSubtitle = osAuthAvailable
    ? hasFingerprint
      ? 'Fingerprint or your Android PIN / pattern'
      : hasFace
        ? 'Face unlock or your device passcode'
        : 'Your Android PIN, pattern, or password'
    : 'CipherBank app PIN';

  const primaryLabel = busy
    ? 'Unlocking…'
    : osAuthAvailable
      ? hasFingerprint
        ? 'Unlock with fingerprint'
        : 'Unlock with device PIN'
      : 'Unlock';

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas, paddingHorizontal: 24, paddingTop: insets.top + 12 }}>
      <View style={{ height: third * 0.75, justifyContent: 'center', alignItems: 'center', gap: 10 }}>
        <Image source={brandMark} style={{ width: 72, height: 72, borderRadius: 18 }} />
        <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 26, color: color.text }}>
          CipherBank locked
        </Text>
        <Text
          style={{
            fontSize: 14,
            color: color.textMuted,
            textAlign: 'center',
            fontFamily: font.body,
            lineHeight: 20,
            paddingHorizontal: 12,
          }}
        >
          {unlockSubtitle}
        </Text>
      </View>

      <View style={{ flex: 1, justifyContent: 'center' }}>
        {showAppPin && appPinSet ? (
          <View style={[{ backgroundColor: color.surface, borderRadius: radius.panel, padding: 18, gap: 14 }, shadow.card]}>
            <Text style={{ fontWeight: '700', fontSize: 13, fontFamily: font.body, color: color.text, textAlign: 'center' }}>
              CipherBank PIN{IS_MOCK ? ' (demo: 000000)' : ''}
            </Text>
            <View style={{ flexDirection: 'row', justifyContent: 'center', gap: 10 }}>
              {Array.from({ length: 6 }).map((_, i) => (
                <View
                  key={i}
                  style={{
                    width: 12,
                    height: 12,
                    borderRadius: 6,
                    backgroundColor: i < pin.length ? color.gold : color.track,
                  }}
                />
              ))}
            </View>
            {hint && !error ? (
              <Text style={{ color: color.textSubtle, textAlign: 'center', fontSize: 12, fontFamily: font.body }}>
                {hint}
              </Text>
            ) : null}
            {error ? (
              <Text style={{ color: color.red, textAlign: 'center', fontSize: 12, fontFamily: font.body }}>{error}</Text>
            ) : null}
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, justifyContent: 'center' }}>
              {KEYS.map((k, i) => (
                <Pressable
                  key={i}
                  onPress={() => onKey(k)}
                  disabled={!k || busy}
                  style={{
                    width: '31%',
                    aspectRatio: 1.55,
                    borderRadius: radius.button,
                    backgroundColor: k ? color.track : 'transparent',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  {k ? (
                    <Text style={{ fontFamily: font.mono, fontSize: 20, fontWeight: '700', color: color.text }}>{k}</Text>
                  ) : null}
                </Pressable>
              ))}
            </View>
          </View>
        ) : (
          <View style={{ alignItems: 'center', paddingHorizontal: 20, gap: 8 }}>
            {hint && !error ? (
              <Text style={{ color: color.textSubtle, textAlign: 'center', fontFamily: font.body, fontSize: 13 }}>
                {hint}
              </Text>
            ) : null}
            {error ? (
              <Text style={{ color: color.red, textAlign: 'center', fontFamily: font.body }}>{error}</Text>
            ) : (
              <Text style={{ color: color.textSubtle, textAlign: 'center', fontFamily: font.body, fontSize: 13 }}>
                {osAuthAvailable
                  ? 'Tap unlock to open Android’s fingerprint or PIN screen.'
                  : 'No device lock detected — set one in Android Settings.'}
              </Text>
            )}
          </View>
        )}
      </View>

      <View
        style={{
          minHeight: third * 0.75,
          justifyContent: 'center',
          paddingBottom: Math.max(insets.bottom, 20) + 8,
          gap: 10,
        }}
      >
        <Button label={primaryLabel} busy={busy} onPress={unlockWithDevice} />
        {osAuthAvailable && appPinSet && !showAppPin ? (
          <Pressable onPress={() => setShowAppPin(true)} hitSlop={8}>
            <Text
              style={{
                textAlign: 'center',
                fontSize: 13,
                color: color.textSubtle,
                fontFamily: font.body,
                textDecorationLine: 'underline',
              }}
            >
              Use CipherBank PIN instead
            </Text>
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}
