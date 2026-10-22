# Runtime configuration

`appsettings.jsonc` contains non-secret production defaults. The host applies
`appsettings.Development.jsonc` only in debug/development builds and applies
`appsettings.Windows.jsonc` only on Windows, in that order. These are embedded
resources parsed by the standard JSON configuration provider.

| Directory | Section | Controls |
| --- | --- | --- |
| `security/` | `CryptographyOptions` | Custody AES-GCM and PBKDF2 parameters |
| `dispatch/` | `SyncSchedulerOptions` | Sync concurrency and dispatch behavior |
| `persistence/` | `PersistenceOptions` | On-device database naming and initialization |
| `sonar/` | server quality gate | New-code quality thresholds and project assignment contract |
| `ui/` | `CoraOptions`, `CarouselLayoutConfig` | Cora copy and carousel layout defaults |

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `Persistence:DefaultRecipients` list.
Never place secrets, tokens, production certificate pins, mnemonics, or
customer banking coordinates in these files. Invalid required values must fail
options validation during startup.
