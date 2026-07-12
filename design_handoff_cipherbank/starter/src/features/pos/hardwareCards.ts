import AsyncStorage from '@react-native-async-storage/async-storage';
import type { CardToken } from '@/features/vault/serverVault.api';

const ACTIVE_KEY = 'cb_pos_lab_card_id';

export type HardwareCard = CardToken & {
  hardwareTest?: boolean;
  label?: string;
};

export function isHardwareTestCard(card: HardwareCard): boolean {
  return card.hardwareTest === true;
}

export function defaultHardwareCardId(): string | undefined {
  return process.env.EXPO_PUBLIC_HARDWARE_CARD_ID || undefined;
}

export function requireTestCard(): boolean {
  return process.env.EXPO_PUBLIC_POS_REQUIRE_TEST_CARD !== 'false';
}

export async function getActivePosCardId(): Promise<string | null> {
  const stored = await AsyncStorage.getItem(ACTIVE_KEY);
  if (stored) return stored;
  return defaultHardwareCardId() ?? null;
}

export async function setActivePosCardId(id: string): Promise<void> {
  await AsyncStorage.setItem(ACTIVE_KEY, id);
}

export function pickLabCard(cards: HardwareCard[], activeId: string | null): HardwareCard | undefined {
  if (activeId) {
    const hit = cards.find((c) => c.id === activeId);
    if (hit) return hit;
  }
  const envId = defaultHardwareCardId();
  if (envId) {
    const hit = cards.find((c) => c.id === envId);
    if (hit) return hit;
  }
  return cards.find(isHardwareTestCard) ?? cards[0];
}
