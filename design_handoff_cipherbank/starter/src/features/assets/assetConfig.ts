// Per-asset display + scaling config. One source of truth for glyphs, decimals, colors.
export type AssetType = 'crypto' | 'fiat' | 'security';

export interface AssetSpec {
  symbol: string;
  name: string;
  glyph: string;
  type: AssetType;
  decimals: number;      // display precision in asset units
  tint: string;          // chip background (brand hue @ ~14%)
  fg: string;            // glyph color
  fiatSymbol?: string;   // for fiat display formatting
  note?: string;         // 'shielded', 'instant ACH'
  badge?: string;        // 'NEW'
  enabled?: boolean;
}

export const ASSETS: Record<string, AssetSpec> = {
  BTC:  { symbol:'BTC',  name:'Bitcoin',      glyph:'₿', type:'crypto', decimals:6, tint:'#F2C14E1f', fg:'#B8860B' },
  ETH:  { symbol:'ETH',  name:'Ethereum',     glyph:'Ξ', type:'crypto', decimals:4, tint:'#7B4DFF18', fg:'#7B4DFF' },
  DOGE: { symbol:'DOGE', name:'Dogecoin',     glyph:'Ð', type:'crypto', decimals:0, tint:'#C9971F1a', fg:'#B8860B' },
  XMR:  { symbol:'XMR',  name:'Monero',       glyph:'ɱ', type:'crypto', decimals:4, tint:'#2B1E3E12', fg:'#2B1E3E', note:'shielded' },
  LTC:  { symbol:'LTC',  name:'Litecoin',     glyph:'Ł', type:'crypto', decimals:3, tint:'#33333310', fg:'#575757' },
  USD:  { symbol:'USD',  name:'US Dollar',    glyph:'$', type:'fiat', decimals:2, tint:'#3FA46A18', fg:'#2E7D51', fiatSymbol:'$', note:'instant ACH' },
  EUR:  { symbol:'EUR',  name:'Euro',         glyph:'€', type:'fiat', decimals:2, tint:'#7B4DFF14', fg:'#5B34D6', fiatSymbol:'€' },
  JPY:  { symbol:'JPY',  name:'Japanese Yen', glyph:'¥', type:'fiat', decimals:0, tint:'#C9971F14', fg:'#B8860B', fiatSymbol:'¥' },
  AAPL: { symbol:'AAPL', name:'Apple',        glyph:'', type:'security', decimals:4, tint:'#2B1E3E12', fg:'#2B1E3E', badge:'NEW', enabled:false },
};

export const assetSpec = (symbol: string): AssetSpec =>
  ASSETS[symbol] ?? { symbol, name: symbol, glyph: symbol[0], type:'crypto', decimals:4, tint:'#33333310', fg:'#575757' };

export const listAssets = (opts?: { type?: AssetType; enabledOnly?: boolean }) =>
  Object.values(ASSETS).filter(a =>
    (!opts?.type || a.type === opts.type) &&
    (!opts?.enabledOnly || a.enabled !== false));
