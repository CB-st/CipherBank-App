# Runtime configuration

Non-secret defaults live in `appsettings.jsonc` (plus `appsettings.Development.jsonc` /
`appsettings.Windows.jsonc` overlays) and are embedded into Core. Remaining theme
directories keep files that are not folded into those overlays. Environment or
deployment providers may override values after defaults are loaded. The host
applies Development only for debug/development builds and Windows only on
Windows, in that order.

| File / directory | Section | Controls |
| --- | --- | --- |
| `appsettings.jsonc` | `CryptographyOptions`, `PersistenceOptions`, `SyncSchedulerOptions`, `UserPreferenceDefaultsOptions`, `HostBehaviorOptions`, `CoraOptions`, `CarouselLayoutConfig` | Custody AES-GCM/PBKDF2, on-device DB name, sync dispatch, first-run preferences, host behavior, Cora copy, carousel layout |
| `challenge-pass/` | `ChallengePass` | Installed session suite selection (non-secret identifiers only) |
| `network/` | `Network` | Product API and WebSocket endpoints by environment |
| `sonar/` | server quality gate | New-code quality thresholds and project assignment contract |

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `PersistenceOptions:DefaultRecipients` list.
Never place secrets, tokens, production certificate pins, mnemonics, or
customer banking coordinates in these files. Invalid required security,
persistence, or suite values must fail options validation during startup.

The Development and Windows overlays retain their distinct database filenames.
Stored user preferences win over configured defaults; missing wire fields use
validated `UserPreferenceDefaultsOptions`. Production service selection comes
from `HostBehaviorOptions`; development indicators enable the existing local
mock-service preference override.
