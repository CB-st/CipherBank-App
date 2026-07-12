if (typeof globalThis.structuredClone !== 'function') {
  globalThis.structuredClone = (v) => JSON.parse(JSON.stringify(v));
}
import 'react-native-gesture-handler';
import { registerRootComponent } from 'expo';
import App from './App';

registerRootComponent(App);
