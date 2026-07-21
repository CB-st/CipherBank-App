# Task 8 report — Vault card add/delete API + Profile UI

**Status:** DONE_WITH_CONCERNS  
**Implementation commit:** `586da3e` (`feat: vault card add/remove on Profile`)

## Delivered

- Extended `IProductApi` with add/delete vault-card operations.
- Made `MockProductApi` retain vault-card mutations in memory, with unit coverage for add and delete behavior.
- Added the HTTP `POST v1/vault/cards` and `POST v1/vault/cards/{id}/delete` calls; add uses the supplied idempotency key.
- Added Profile controls to add a token-metadata-only demo card and remove the selected card. Removing the POS-active card requires `PosAuthorize` step-up and reassigns or clears the active-card preference after deletion.
- No mnemonic or PIN is logged or included in these new API payloads.

## Verification

- TDD red: the new mock mutation tests initially failed because `AddVaultCardAsync` and `DeleteVaultCardAsync` did not exist.
- Focused: `$HOME/.local/dotnet/dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj --filter FullyQualifiedName~MockVaultCardMutationTests -p:CollectCoverage=false` — 2/2 passed.
- Full: `$HOME/.local/dotnet/dotnet build CipherBank-app.Core/CipherBank-app.Core.csproj --no-restore -p:CollectCoverage=false` and `$HOME/.local/dotnet/dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -p:CollectCoverage=false` — Core build passed; 247/247 tests passed.
- IDE diagnostics: no errors in changed files.

## Concerns

- The MAUI Android build could not run in this environment because no Android SDK is configured (`XA5300`); the Core unit suite does not compile the MAUI XAML target.
- Existing NU1608 dependency-version warnings and unrelated analyzer warnings remain.
