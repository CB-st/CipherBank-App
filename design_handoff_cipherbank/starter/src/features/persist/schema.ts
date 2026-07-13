/** Logical SQLite DDL for public user env. Secrets never land here. */

export const SCHEMA_VERSION = 2;

export const SCHEMA_SQL = `
CREATE TABLE IF NOT EXISTS schema_meta (
  key TEXT PRIMARY KEY NOT NULL,
  value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS wallets (
  id TEXT PRIMARY KEY NOT NULL,
  symbol TEXT NOT NULL,
  label TEXT NOT NULL,
  address TEXT,
  derivation_path TEXT,
  account_index INTEGER,
  source TEXT NOT NULL,
  mode TEXT,
  sync_json TEXT,
  view_key_fp TEXT,
  created_at INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_wallets_symbol ON wallets(symbol);

CREATE TABLE IF NOT EXISTS prefs (
  key TEXT PRIMARY KEY NOT NULL,
  value_json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS rates_snapshot (
  symbol TEXT PRIMARY KEY NOT NULL,
  usd REAL NOT NULL,
  change24h REAL NOT NULL DEFAULT 0,
  updated_at INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS market_ohlc (
  symbol TEXT NOT NULL,
  granularity TEXT NOT NULL,
  t INTEGER NOT NULL,
  o REAL,
  h REAL,
  l REAL,
  c REAL,
  v REAL NOT NULL,
  PRIMARY KEY (symbol, granularity, t)
);

CREATE INDEX IF NOT EXISTS idx_ohlc_lookup ON market_ohlc(symbol, granularity, t);

CREATE TABLE IF NOT EXISTS sync_meta (
  key TEXT PRIMARY KEY NOT NULL,
  value TEXT NOT NULL,
  updated_at INTEGER NOT NULL
);

-- Known ACH (and CipherBank-handle) send recipients. Account numbers stay on-device.
CREATE TABLE IF NOT EXISTS ach_recipients (
  id TEXT PRIMARY KEY NOT NULL,
  display_name TEXT NOT NULL,
  account_holder_name TEXT NOT NULL,
  routing_number TEXT,
  account_number TEXT,
  account_type TEXT,
  bank_name TEXT,
  account_last4 TEXT,
  rail TEXT NOT NULL DEFAULT 'ACH',
  handle TEXT,
  memo TEXT,
  initials TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_ach_recipients_name ON ach_recipients(display_name);
`;

/** Incremental DDL applied when schema_meta.version < SCHEMA_VERSION. */
export const MIGRATIONS: { to: number; sql: string }[] = [
  {
    to: 2,
    sql: `
CREATE TABLE IF NOT EXISTS ach_recipients (
  id TEXT PRIMARY KEY NOT NULL,
  display_name TEXT NOT NULL,
  account_holder_name TEXT NOT NULL,
  routing_number TEXT,
  account_number TEXT,
  account_type TEXT,
  bank_name TEXT,
  account_last4 TEXT,
  rail TEXT NOT NULL DEFAULT 'ACH',
  handle TEXT,
  memo TEXT,
  initials TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_ach_recipients_name ON ach_recipients(display_name);
`,
  },
];
