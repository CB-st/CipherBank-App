export type HomeSection = 'cora' | 'balance' | 'quickActions' | 'performance' | 'assets';

export type SendSpeedPref = 'instant' | 'ach';

export type AppearancePref = 'dark' | 'light';

export interface UserPrefs {
  homeOrder: HomeSection[];
  homeVisible: Record<HomeSection, boolean>;
  valuesHiddenOnLaunch: boolean;
  coraEnabled: boolean;
  defaultSendSpeed: SendSpeedPref;
  /** App chrome. Dark is default; light is opt-in. */
  appearance: AppearancePref;
}

export const DEFAULT_PREFS: UserPrefs = {
  homeOrder: ['cora', 'balance', 'quickActions', 'performance', 'assets'],
  homeVisible: {
    cora: true,
    balance: true,
    quickActions: true,
    performance: true,
    assets: true,
  },
  valuesHiddenOnLaunch: false,
  coraEnabled: true,
  defaultSendSpeed: 'instant',
  appearance: 'dark',
};

export const HOME_SECTION_LABELS: Record<HomeSection, string> = {
  cora: 'Cora bar',
  balance: 'Balance hero',
  quickActions: 'Quick actions',
  performance: 'Performance chart',
  assets: 'Asset list',
};
