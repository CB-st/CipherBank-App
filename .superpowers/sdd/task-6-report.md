# Task 6 report — P2 hydrate and P1 chart write-through

**Status:** DONE_WITH_CONCERNS  
**Implementation commit:** `4863ac3` (`feat: hydrate rates from SQLite and persist chart OHLC`)
**Review fix commit:** `77e948d` (`fix: hydrate rates when portfolio loading fails`)

## Delivered

- `CipherBank-app/ViewModels/HomeViewModel.cs`
  - Enqueues P2 `p2-rates` after every Home load attempt, including offline or failed portfolio requests.
  - Loads local wallets before the remote portfolio request so the failed-request path supplies the best available held symbols; existing in-memory holdings are retained.
  - Intersects held symbols with enabled currencies and calls the rate bootstrap work through `ISyncJobQueue`.
  - Enqueues P1 `p1-ohlc-{symbol}` after every history response, including range changes, to persist its points with `IMarketRepository`.
- `CipherBank-app.Core/Persist/MarketBootstrap.cs`
  - Reads SQLite rates, skips refresh only when the requested cache is complete and fresh for 15 minutes, otherwise gets one-unit USD inverse quotes and upserts them.
  - Maps a `PublicQuote` to `RateRow` without logging sensitive data.
- `CipherBank-app.Tests/Persist/MarketBootstrapTests.cs`
  - Covers the `PublicQuote` to `RateRow` mapping.

## Verification

- Focused: `dotnet test CipherBank-app.Tests --filter FullyQualifiedName~MarketBootstrapTests -p:CollectCoverage=false` — 1/1 passed.
- Full: `dotnet test CipherBank-app.Tests -p:CollectCoverage=false` — 243/243 passed.
- IDE diagnostics: no errors in changed files.
- No targeted `HomeViewModel` unit test was added: the unit-test project references Core and ChallengePass only, while this MAUI view model is not part of that target and would require a UI-specific harness.

## Concerns

- Android app build could not run in this environment because a Java SDK is unavailable (`XA5300`), after supplying the installed Android SDK path. Unit tests compile the Core projects but do not compile the MAUI `HomeViewModel` target.
- Existing NU1608 dependency-version warnings remain during test/build restore.
