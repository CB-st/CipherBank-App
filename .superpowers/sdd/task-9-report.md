# Task 9 report — Wallet + recipient delete management

**Status:** DONE_WITH_CONCERNS  
**Implementation commit:** `5898198` (`feat: delete local wallets and ACH recipients from UI`)

## Delivered

- Added `IRecipientRepository.DeleteAsync(string id)` with parameterized SQLite deletion and a red-green repository test that verifies only the requested recipient is removed.
- Added confirmed removal of the selected saved ACH payee in Send, followed by a refreshed recipient list.
- Added confirmed removal of local wallets from Home in both local-wallet and combined-asset layouts, backed by `WalletRepository.DeleteAsync`.
- Dialog content includes only payee names or wallet labels/symbols; no mnemonic, PIN, or address is exposed.

## Verification

- TDD red: the focused recipient test initially failed to compile because `RecipientRepository.DeleteAsync` did not exist.
- Focused: `$HOME/.local/dotnet/dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj --filter FullyQualifiedName~RecipientRepositoryTests.DeleteAsync_RemovesOnlyRecipientWithMatchingId -p:CollectCoverage=false` — 1/1 passed.
- Full: `$HOME/.local/dotnet/dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -p:CollectCoverage=false` — 248/248 passed.
- IDE diagnostics: no errors in changed files.
- Self-review: scoped diff and whitespace validation found no task-change issues.

## Concerns

- The MAUI Android build could not run because the Android SDK is not configured (`XA5300`), so the XAML target was not compiled in this environment.
- Existing NU1608 package-version and unrelated analyzer warnings remain.
