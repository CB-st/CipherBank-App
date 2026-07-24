/**
 * Selector contract for Expo Cora.
 * Scaffold names (tid.*) map onto real RN `testID`s where they exist.
 * Unmapped strings are targets — add matching testIDs in the app before enabling those stories.
 */
export const tid = {
  navigation: {
    home: 'tab-home',
    wallets: 'tab-home',
    pay: 'tab-pay',
    activity: 'tab-home',
    profile: 'tab-profile',
    convert: 'tab-convert',
    send: 'tab-send',
    receive: 'tab-receive',
  },
  account: {
    /** Expo: Welcome is the create-account entry (no email form). */
    welcomeScreen: 'welcome-screen',
    createSubmit: 'welcome-create',
    recoverEntry: 'welcome-returning',
    recoveryMaterial: 'keys-screen',
    keysContinue: 'keys-continue',
    quizScreen: 'quiz-screen',
    quizContinue: 'quiz-continue',
    setPinScreen: 'set-pin-screen',
    pinInput: 'pin-input',
    pinConfirm: 'pin-confirm',
    pinFinish: 'pin-finish',
    createSuccess: 'home-screen',
    setupPrompt: 'home-setup-prompt',
  },
  home: {
    screen: 'home-screen',
    setupPrompt: 'home-setup-prompt',
  },
  wallets: {
    page: 'wallets-page',
    create: 'create-wallet-button',
    typeUser: 'wallet-type-user',
    typeCipherbank: 'wallet-type-cipherbank',
    receiveAddress: 'wallet-receive-address',
    receiveQr: 'wallet-receive-qr',
  },
  cards: {
    create: 'create-prepaid-card-button',
    item: 'prepaid-card-item',
  },
  payments: {
    page: 'pay-page',
    status: 'payment-status',
  },
  market: {
    page: 'market-data-page',
    currentPrice: 'market-current-price',
    chart: 'market-price-chart',
  },
} as const;
