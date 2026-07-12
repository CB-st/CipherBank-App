const { withAndroidManifest } = require('@expo/config-plugins');

/**
 * Ensures NFC feature + permission are declared for Android builds.
 * Real HCE APDU service is processor-specific and added later.
 */
function withNfcAndroid(config) {
  return withAndroidManifest(config, (cfg) => {
    const manifest = cfg.modResults.manifest;
    if (!manifest['uses-permission']) manifest['uses-permission'] = [];
    const hasNfc = manifest['uses-permission'].some(
      (p) => p.$?.['android:name'] === 'android.permission.NFC',
    );
    if (!hasNfc) {
      manifest['uses-permission'].push({ $: { 'android:name': 'android.permission.NFC' } });
    }
    if (!manifest['uses-feature']) manifest['uses-feature'] = [];
    const hasFeat = manifest['uses-feature'].some(
      (f) => f.$?.['android:name'] === 'android.hardware.nfc',
    );
    if (!hasFeat) {
      manifest['uses-feature'].push({
        $: {
          'android:name': 'android.hardware.nfc',
          'android:required': 'false',
        },
      });
    }
    return cfg;
  });
}

module.exports = withNfcAndroid;
