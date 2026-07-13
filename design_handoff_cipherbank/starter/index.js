import 'react-native-get-random-values';
import * as ExpoCrypto from 'expo-crypto';
import { TextEncoder, TextDecoder } from 'text-encoding';

// Noble / scure expect WebCrypto getRandomValues (Hermes does not provide it by default).
if (typeof globalThis.crypto !== 'object' || globalThis.crypto == null) {
  globalThis.crypto = {};
}
if (typeof globalThis.crypto.getRandomValues !== 'function') {
  globalThis.crypto.getRandomValues = ExpoCrypto.getRandomValues.bind(ExpoCrypto);
}

// Hermes lacks TextEncoder/TextDecoder — required by custody decrypt and some noble helpers.
if (typeof globalThis.TextEncoder !== 'function') {
  globalThis.TextEncoder = TextEncoder;
}
if (typeof globalThis.TextDecoder !== 'function') {
  globalThis.TextDecoder = TextDecoder;
}

if (typeof globalThis.structuredClone !== 'function') {
  globalThis.structuredClone = (v) => JSON.parse(JSON.stringify(v));
}

import 'react-native-gesture-handler';
import { registerRootComponent } from 'expo';
import App from './App';

registerRootComponent(App);
