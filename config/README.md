# Runtime Configuration

Configuration is separated by operational theme. Files are embedded into Core as
safe defaults and bound to typed options at the MAUI composition root. Environment
or deployment providers may override them after defaults are loaded.

| Directory | Section | Controls |
| --- | --- | --- |
| `security/` | `Cryptography` | Custody AES-GCM and PBKDF2 parameters |
| `dispatch/` | `SyncScheduler` | Sync concurrency and dispatch behavior |
| `persistence/` | `Persistence` | On-device database naming and initialization |
| `sonar/` | server quality gate | New-code quality thresholds and project assignment contract |
| `ui/` | `Cora`, `Carousel` | Cora copy and carousel layout defaults |

Do not place secrets, tokens, production certificate pins, mnemonics, or account
data in these files. Unknown keys are ignored; invalid security values must fail
options validation during startup.
