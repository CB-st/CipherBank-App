/**
 * Lab-only contactless exchange timeline.
 * Mirrors EMV contactless phases without scheme crypto — see docs/DIGITAL_CARDS_NFC.md.
 */
import type { NfcPresentPayload } from './pos.types';

export type ExchangeStageId =
  | 'rf_field'
  | 'select_ppse'
  | 'select_aid'
  | 'get_processing_options'
  | 'generate_ac'
  | 'outcome';

export type ExchangeStageStatus = 'pending' | 'active' | 'ok' | 'fail';

export interface ExchangeStage {
  id: ExchangeStageId;
  label: string;
  detail: string;
  /** Rough EMV / HCE analogue shown in the lab UI */
  apduHint: string;
  status: ExchangeStageStatus;
  at?: number;
}

export interface ExchangeResult {
  ok: boolean;
  mode: 'simulated' | 'nfc';
  stages: ExchangeStage[];
  payload: NfcPresentPayload;
  brandHint: string;
}

const BASE_STAGES: Omit<ExchangeStage, 'status' | 'at'>[] = [
  {
    id: 'rf_field',
    label: 'RF field',
    detail: 'Reader energizes the interface (ISO 14443)',
    apduHint: 'FIELD_ON',
  },
  {
    id: 'select_ppse',
    label: 'SELECT PPSE',
    detail: 'Proximity Payment System Environment',
    apduHint: '00 A4 04 00 0E 2PAY.SYS.DDF01',
  },
  {
    id: 'select_aid',
    label: 'SELECT AID',
    detail: 'Visa / Mastercard application selected',
    apduHint: '00 A4 04 00 … AID',
  },
  {
    id: 'get_processing_options',
    label: 'GET PROCESSING OPTIONS',
    detail: 'Terminal sends amount / TTQ; card returns AFL',
    apduHint: '80 A8 00 00 … PDOL',
  },
  {
    id: 'generate_ac',
    label: 'GENERATE AC',
    detail: 'Application cryptogram — lab uses tokenRef only',
    apduHint: '80 AE 80 00 … CDOL',
  },
  {
    id: 'outcome',
    label: 'Outcome',
    detail: 'Terminal accepts tokenized presentment',
    apduHint: 'APPROVED (lab)',
  },
];

export function brandAidHint(brand: string): string {
  const b = brand.toLowerCase();
  if (b.includes('master')) return 'A0000000041010 (Mastercard)';
  if (b.includes('amex')) return 'A00000002501 (Amex)';
  return 'A0000000031010 (Visa)';
}

export function buildExchangeStages(brand: string): ExchangeStage[] {
  return BASE_STAGES.map((s) => ({
    ...s,
    status: 'pending' as const,
    detail:
      s.id === 'select_aid'
        ? `Application: ${brandAidHint(brand)}`
        : s.detail,
  }));
}

/**
 * Runs the staged exchange with delays. Calls `onUpdate` after each stage.
 * Does not perform RF — pure simulation for emulator / web / lab.
 */
export async function runSimulatedExchange(opts: {
  payload: NfcPresentPayload;
  brand: string;
  onUpdate?: (stages: ExchangeStage[]) => void;
  stepMs?: number;
}): Promise<ExchangeResult> {
  const stepMs = opts.stepMs ?? 420;
  const stages = buildExchangeStages(opts.brand);
  opts.onUpdate?.([...stages]);

  for (let i = 0; i < stages.length; i++) {
    stages[i] = { ...stages[i], status: 'active', at: Date.now() };
    opts.onUpdate?.([...stages]);
    await delay(stepMs);
    if (stages[i].id === 'generate_ac') {
      stages[i] = {
        ...stages[i],
        status: 'ok',
        detail: `Cryptogram stand-in → tokenRef ${opts.payload.tokenRef.slice(0, 14)}…`,
      };
    } else if (stages[i].id === 'outcome') {
      stages[i] = {
        ...stages[i],
        status: 'ok',
        detail: `Settled lab tap · session ${opts.payload.sessionId.slice(0, 12)}…`,
      };
    } else {
      stages[i] = { ...stages[i], status: 'ok' };
    }
    opts.onUpdate?.([...stages]);
  }

  return {
    ok: true,
    mode: 'simulated',
    stages,
    payload: opts.payload,
    brandHint: brandAidHint(opts.brand),
  };
}

function delay(ms: number) {
  return new Promise((r) => setTimeout(r, ms));
}
