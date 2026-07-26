# Sonar Stage 2 — Structural Plan (SA1402 / SA1649)

**Status: SKELETON ONLY. No file splits have been performed.** This document
inventories every `SA1402` ("file may only contain a single type") and
`SA1649` ("file name should match first type name") issue reported against
`prototype/maui-m1` (PR #20) and `prototype/maui-m2` (PR #21) in
`/tmp/sonar-stack/{m1,m2}/issues.json`, and proposes a split target for each.
Per the Stage 1 spec, **Stage 3 (medium/minor/info) does not start until this
plan is reviewed**, and no code movement should land until each row's
"Proposed path" is turned into a real task with tests.

Source data: `external_roslyn:SA1402` + `external_roslyn:SA1649` entries only.
Type lists are best-effort, extracted from the current tree in
`.worktrees/sonar-stage1` (`prototype/maui-m3`) by regex over `class` /
`interface` / `struct` / `record` / `enum` declarations — private/nested
types are omitted. `*` marks the type Sonar/StyleCop treats as "first" (the
one the filename should match). "Callers" is intentionally left `TBD`; it
requires a real reference search per type before any split executes.

## Legend

- **Breaks** — expected blast radius of the proposed split, not yet verified by build/test.
  - `rename only` — file renamed to match its sole/primary type; no type or namespace change; zero API impact.
  - `split only` — extra types extracted to sibling files in the same namespace; no rename, no API impact.
  - `rename + split` — both of the above.
  - `high churn` — module used widely across `ChallengePass`; coordinate before executing.
- **Annotation needed** — `Yes`: every new/renamed file must get the AGENTS.md copyright header (updated `file="..."` to match new name) and, for any touched function, the mandatory purpose/call-frequency/scope doc comment. This column is about the **cross-PR duplication comments** reviewers will see, not a one-time task: a split lands on `m1` and is then merged up through `m2` → `m3` → `m4`, so the identical header/doc-comment diff reappears on each downstream PR. Review it once on the owning layer and treat the repeats as merge noise.

## Open before Stage 2 approval

- **Callers are still `TBD` on every row.** No reference search has been run, so the `Breaks` column remains an estimate. Stage 2 cannot be approved until each row's callers are filled in; that is step 2 of the execution order below.
- **Shell compile gate.** The `Callers` search must include the MAUI Shell (`CipherBank-app/` — view models, platform services, and DI registration), not just `Core` and `ChallengePass`. Stage 1 already produced one Critical finding of exactly this shape: Core API signatures changed without mapping Shell call sites, and the Shell stopped compiling even though `Core` and the 267-test suite were green. Renames and type moves in Stage 2 have the same blast radius, so **every Stage 2 PR must build `CipherBank-app/CipherBank-app.csproj`, not only run `dotnet test`.** Note the Shell only exists in full from `prototype/maui-m3` upward, so the compile gate runs there even when the split itself lands on `m1`.

## CipherBank-app.Core

| Layer | File | Types (best-effort, `*`=primary) | Callers | Proposed path(s) | Breaks | Annotation needed |
|---|---|---|---|---|---|---|
| Core/Charts | `Charts/ChartMath.cs` | ChartPoint(record), ChartMath(class)*, ChartPathResult(class) | TBD | `Charts/ChartPoint.cs`, `Charts/ChartPathResult.cs` | split only | Yes |
| Core/Custody | `Custody/CustodyService.cs` | CustodyPinChangeResult(enum), ICustodyService(interface), CustodyService(class)* | TBD | `Custody/CustodyPinChangeResult.cs`, `Custody/ICustodyService.cs` | split only | Yes |
| Core/Custody | `Custody/Mnemonic.cs` | MnemonicHelper(class)* | TBD | rename file → `Custody/MnemonicHelper.cs` | rename only | Yes |
| Core/Custody | `Custody/PinChange.cs` | PinChangeStatus(enum)*, PinChangeOutcome(record), PinChangeCoordinator(class) | TBD | rename file → `Custody/PinChangeStatus.cs`; extract `Custody/PinChangeOutcome.cs`, `Custody/PinChangeCoordinator.cs` | rename + split | Yes |
| Core/Custody | `Custody/PinService.cs` | IPinService(interface), PinService(class)* | TBD | `Custody/IPinService.cs` | split only | Yes |
| Core/Persist | `Persist/HoldingVisibility.cs` | HoldingVisibility(class)*, HoldingVisibilityResult(record) | TBD | `Persist/HoldingVisibilityResult.cs` | split only | Yes |
| Core/Persist | `Persist/IRatesCache.cs` | IRatesCache(interface)*, RateRow(record) | TBD | `Persist/RateRow.cs` | split only | Yes |
| Core/Persist | `Persist/LocalDb.cs` | ILocalDb(interface), LocalDb(class)* | TBD | `Persist/ILocalDb.cs` | split only | Yes |
| Core/Persist | `Persist/PrefsStore.cs` | IPrefsStore(interface), UserPrefs(class), PrefsStore(class)* | TBD | `Persist/IPrefsStore.cs`, `Persist/UserPrefs.cs` | split only | Yes |
| Core/Persist | `Persist/RecipientRepository.cs` | IRecipientRepository(interface), AchRecipientRow(record), RecipientRepository(class)* | TBD | `Persist/IRecipientRepository.cs`, `Persist/AchRecipientRow.cs` | split only | Yes |
| Core/Persist | `Persist/WalletRepository.cs` | IWalletRepository(interface), LocalWalletRow(record), WalletRepository(class)* | TBD | `Persist/IWalletRepository.cs`, `Persist/LocalWalletRow.cs` | split only | Yes |
| Core/Pos | `Pos/NfcPresentment.cs` | INfcPresentmentService(interface)*, EmvExchangeSimulator(class), NfcPresentmentPayload(class), NullNfcPresentmentService(class) | TBD | rename file → `Pos/INfcPresentmentService.cs`; extract `Pos/EmvExchangeSimulator.cs`, `Pos/NfcPresentmentPayload.cs`, `Pos/NullNfcPresentmentService.cs` | rename + split | Yes |
| Core/Session | `Session/AppSession.cs` | IAppSession(interface), AppSession(class)* | TBD | `Session/IAppSession.cs` | split only | Yes |
| Core/V1 | `V1/AccountBootstrap.cs` | AccountBootstrapDto(class)*, BootstrapRecipientDto(class) | TBD | `V1/BootstrapRecipientDto.cs` | split only | Yes |
| Core/V1 | `V1/AccountBootstrapService.cs` | IAccountBootstrapService(interface), AccountBootstrapService(class)* | TBD | `V1/IAccountBootstrapService.cs` | split only | Yes |
| Core/V1 | `V1/PrefsSyncService.cs` | IPrefsSyncService(interface), PrefsSyncService(class)* | TBD | `V1/IPrefsSyncService.cs` | split only | Yes |
| Core/V1 | `V1/PrefsWire.cs` | PrefsMerge(class)*, PrefsWireDto(class) | TBD | rename file → `V1/PrefsMerge.cs`; extract `V1/PrefsWireDto.cs` | rename + split | Yes |
| Core/V1 | `V1/SessionChallenge.cs` | ISessionProofBuilder(interface)*, SessionChallengeDto(class), SessionPassDto(class), LabSessionProofBuilder(class) | TBD | rename file → `V1/ISessionProofBuilder.cs`; extract `V1/SessionChallengeDto.cs`, `V1/SessionPassDto.cs`, `V1/LabSessionProofBuilder.cs` | rename + split | Yes |
| Core/V1 | `V1/SessionKeyShareWire.cs` | KeyShareRequestDto(class)*, KeyShareResponseDto(class), CreateWalletRequestDto(class), CreateWalletResultDto(class) | TBD | rename file → `V1/KeyShareRequestDto.cs`; extract `V1/KeyShareResponseDto.cs`, `V1/CreateWalletRequestDto.cs`, `V1/CreateWalletResultDto.cs` | rename + split | Yes |
| Core/V1 | `V1/StreamHub.cs` | IStreamHub(interface), StreamHub(class)* | TBD | `V1/IStreamHub.cs` | split only | Yes |
| Core/V1 | `V1/StreamService.cs` | IStreamService(interface)*, StreamEvent(class), MockStreamService(class), ClientWebSocketStreamService(class) | TBD | rename file → `V1/IStreamService.cs`; extract `V1/StreamEvent.cs`, `V1/MockStreamService.cs`, `V1/ClientWebSocketStreamService.cs` | rename + split | Yes |
| Core/V1 | `V1/WireModels.cs` | PortfolioDto(class)*, HoldingDto, HistoryPointDto, QuoteDto, SessionDto, ReceiveDto, PosSessionDto, VaultCardDto, MoneyMoveDto, VaultBinaryDto (all class) | TBD | rename file → `V1/PortfolioDto.cs`; extract remaining 9 DTOs to their own `V1/<Name>.cs` | rename + split | Yes |
| Core/Wallets | `Wallets/LocalWalletSeeder.cs` | ILocalWalletSeeder(interface), LocalWalletSeeder(class)* | TBD | `Wallets/ILocalWalletSeeder.cs` | split only | Yes |
| Core/Wallets | `Wallets/WalletRegistry.cs` | WalletUiMode(enum), WalletSource(enum), WalletRegistry(class)*, WalletModule(class) | TBD | `Wallets/WalletUiMode.cs`, `Wallets/WalletSource.cs`, `Wallets/WalletModule.cs` | split only | Yes |

## CipherBank-app.ChallengePass

| Layer | File | Types (best-effort, `*`=primary) | Callers | Proposed path(s) | Breaks | Annotation needed |
|---|---|---|---|---|---|---|
| ChallengePass | `ChallengePassSlots.cs` | ISealAlgorithm(interface)*, IChallengeTemplate, IChallengePassStructure, IChallengePassCatalog (interfaces), AccountKeyPair(record), ChallengeBindContext, ParsedChallenge, ChallengePassSuite (class) | TBD | rename file → `ISealAlgorithm.cs`; extract the other 7 types to their own files | high churn | Yes |
| ChallengePass/Crypto | `Crypto/PortableX25519.cs` | PortableX25519(class)*, PortableChaCha20Poly1305(class) | TBD | `Crypto/PortableChaCha20Poly1305.cs` | split only | Yes |
| ChallengePass/Hybrid | `Hybrid/HybridKeyShareModels.cs` | IPqKeyShareClient(interface)*, HybridPublicIdentity, HybridPrivateIdentity, PqKeyShareResponse (class) | TBD | rename file → `Hybrid/IPqKeyShareClient.cs`; extract `Hybrid/HybridPublicIdentity.cs`, `Hybrid/HybridPrivateIdentity.cs`, `Hybrid/PqKeyShareResponse.cs` | rename + split | Yes |
| ChallengePass/Hybrid | `Hybrid/PqSymmetricChannel.cs` | IPqChannel(interface), PqSymmetricChannel(class)* | TBD | `Hybrid/IPqChannel.cs` | split only | Yes |
| ChallengePass/Structures | `Structures/PqChannelChallengePassStructure.cs` | IPqChannelChallengeSource(interface), PqChannelChallengePassStructure(class)*, InMemoryPqChannelChallengeSource(class) | TBD | `Structures/IPqChannelChallengeSource.cs`, `Structures/InMemoryPqChannelChallengeSource.cs` | split only | Yes |

## Totals

- 24 files in `CipherBank-app.Core`, 5 files in `CipherBank-app.ChallengePass` — 29 files, 58 SA1402/SA1649 issues (49 on m1/PR#20, 9 on m2/PR#21).
- No new SA1402/SA1649 issues found beyond these on m1/m2 at time of writing; m3/m4 issue exports were not part of this pass (only `m1`/`m2` existed under `/tmp/sonar-stack`).

## Execution order (once reviewed)

1. Confirm this plan with the team (per spec gate — Stage 3 blocked until then).
2. Real caller search per type (replace `TBD`), starting with `high churn` rows, and including MAUI Shell callers per the Shell compile gate above.
3. Execute renames/splits on `prototype/maui-m1` first (earliest layer touching these files), then merge/cherry-pick up through m2 → m3 → m4, same as Stage 1 mechanical fixes.
4. One PR per layer/folder (e.g. `Core/V1`, `ChallengePass`) to keep diffs reviewable; run the full test suite **and** the Shell build before each push.
