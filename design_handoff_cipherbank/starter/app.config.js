export default ({ config }) => ({
  ...config,
  name: 'CipherBank',
  slug: 'cipherbank',
  version: '0.1.0',
  orientation: 'portrait',
  icon: './assets/icon.png',
  userInterfaceStyle: 'automatic',
  scheme: 'cipherbank',
  splash: {
    image: './assets/splash.png',
    resizeMode: 'contain',
    backgroundColor: '#0C0D11',
  },
  assetBundlePatterns: ['**/*'],
  ios: {
    supportsTablet: false,
    bundleIdentifier: 'com.cipherbank.app',
    infoPlist: {
      NFCReaderUsageDescription:
        'CipherBank uses NFC to present a tokenized card at participating point-of-sale terminals. Payment credentials never leave the secure vault.',
      // Core NFC reader entitlement is reserved for later Mac builds; HCE is Android-first.
    },
  },
  android: {
    adaptiveIcon: {
      foregroundImage: './assets/icon.png',
      backgroundColor: '#2B1E3E',
    },
    package: 'com.cipherbank.app',
    permissions: ['android.permission.NFC'],
    intentFilters: [
      {
        action: 'android.nfc.action.NDEF_DISCOVERED',
        category: ['android.intent.category.DEFAULT'],
        data: [{ scheme: 'cipherbank', host: 'pos' }],
      },
    ],
  },
  web: {
    favicon: './assets/favicon.png',
  },
  plugins: [
    'expo-font',
    'expo-secure-store',
    'expo-dev-client',
    [
      './plugins/withNfcAndroid.js',
      {
        includeNfcTech: true,
      },
    ],
  ],
  extra: {
    eas: {
      projectId: process.env.EAS_PROJECT_ID ?? '00000000-0000-0000-0000-000000000000',
    },
  },
});
