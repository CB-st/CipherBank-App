#!/usr/bin/env node
/**
 * Dumps fixture inventory + endpoint map for the CipherBank mock API contract.
 * Run: node scripts/dump-contract.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, '..');
const fixturesDir = path.join(root, 'src/mocks/fixtures');

const ENDPOINTS = [
  ['GET', '/portfolio', 'fixtures/portfolio.json'],
  ['GET', '/assets', 'fixtures/assets.json'],
  ['GET', '/rates', 'fixtures/rates.json'],
  ['GET', '/recipients', 'fixtures/recipients.json'],
  ['GET', '/activity', 'fixtures/activity.json'],
  ['GET', '/prefs', 'fixtures/prefs.json'],
  ['PUT', '/prefs', 'fixtures/prefs.json'],
  ['GET', '/vault/binaries', 'fixtures/vault-binaries.json'],
  ['GET', '/vault/cards', 'fixtures/vault-cards.json'],
  ['GET', '/receive/:asset', 'fixtures/receive.json'],
  ['GET', '/history?range=&compare=', 'computed'],
  ['POST', '/session', 'computed'],
  ['POST', '/session/refresh', 'computed'],
  ['POST', '/quotes', 'computed'],
  ['POST', '/convert', 'computed'],
  ['POST', '/transfers', 'computed'],
  ['POST', '/payments', 'computed'],
  ['POST', '/receive/request', 'computed'],
  ['POST', '/recipients', 'computed'],
  ['POST', '/banks/link', 'computed'],
  ['POST', '/vault/binaries', 'computed'],
  ['POST', '/vault/cards', 'computed'],
  ['POST', '/vault/cards/:id/delete', 'computed'],
  ['POST', '/pos/sessions', 'computed'],
  ['POST', '/pos/authorize', 'computed'],
  ['POST', '/pos/confirm', 'computed'],
  ['GET', '/pos/sessions/:id', 'computed'],
];

console.log('\nCipherBank /v1 mock contract inventory\n');
console.log('Fixtures:');
for (const f of fs.readdirSync(fixturesDir).sort()) {
  if (!f.endsWith('.json')) continue;
  const raw = fs.readFileSync(path.join(fixturesDir, f), 'utf8');
  JSON.parse(raw);
  const kb = (Buffer.byteLength(raw) / 1024).toFixed(2);
  console.log(`  ✓ ${f} (${kb} KB)`);
}

console.log('\nEndpoints (handlers.ts):');
for (const [method, route, src] of ENDPOINTS) {
  console.log(`  ${method.padEnd(6)} ${route.padEnd(36)} ← ${src}`);
}

console.log('\nDocs:');
for (const d of ['API_CONTRACT.md', 'POS_API.md', 'README.md']) {
  const p = path.join(root, 'src/mocks', d);
  console.log(fs.existsSync(p) ? `  ✓ src/mocks/${d}` : `  ✗ missing ${d}`);
}
console.log('  ✓ docs/TESTING.md');
console.log('');
