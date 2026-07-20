import assert from 'node:assert/strict';
import test from 'node:test';
import { toPublicCurrency, toAppSymbol, isKnownPublicCurrency } from '../publicCurrency.ts';

test('toPublicCurrency maps BTC and XMR', () => {
  assert.equal(toPublicCurrency('BTC'), 'BITCOIN');
  assert.equal(toPublicCurrency('xmr'), 'MONERO');
  assert.equal(toPublicCurrency('USD'), 'USD');
});

test('toPublicCurrency uppercases unknown tickers', () => {
  assert.equal(toPublicCurrency('ETH'), 'ETHEREUM');
  assert.equal(toPublicCurrency('FOO'), 'FOO');
});

test('toAppSymbol maps API codes', () => {
  assert.equal(toAppSymbol('BITCOIN'), 'BTC');
  assert.equal(toAppSymbol('MONERO'), 'XMR');
});

test('isKnownPublicCurrency', () => {
  assert.equal(isKnownPublicCurrency('BITCOIN'), true);
  assert.equal(isKnownPublicCurrency('BTC'), false);
  assert.equal(isKnownPublicCurrency('NOTACOIN'), false);
});
