# Task 4 Report: IRatesCache + MarketRepository

**Date:** 2026-07-20  
**Branch:** `feat/cora-redesign-maui`  
**Status:** Complete

## Summary

Implemented the rates snapshot cache and OHLC market repository against
`ILocalDb.Open()`. Both use SQLite upserts, normalize symbols to uppercase,
propagate cancellation tokens through asynchronous database operations, and
register as singletons in `MauiProgram`.

## Files Changed

- Created `CipherBank-app.Core/Persist/IRatesCache.cs`
- Created `CipherBank-app.Core/Persist/RatesCache.cs`
- Created `CipherBank-app.Core/Persist/IMarketRepository.cs`
- Created `CipherBank-app.Core/Persist/MarketRepository.cs`
- Created `CipherBank-app.Tests/Persist/RatesCacheTests.cs`
- Created `CipherBank-app.Tests/Persist/MarketRepositoryTests.cs`
- Updated `CipherBank-app/MauiProgram.cs`

## TDD Evidence

1. Added tests for filtered rate retrieval, rate replacement, OHLC replacement,
   timestamp filtering, and ascending timestamp order.
2. Verified the tests failed to compile because `RatesCache`, `RateRow`, and
   `MarketRepository` did not exist.
3. Implemented the interfaces and repositories, then verified both tests pass.

## Verification

```bash
PATH="$HOME/.local/dotnet:$PATH" dotnet test \
  CipherBank-app.Tests/CipherBank-app.Tests.csproj -p:CollectCoverage=false
```

Result: 240 passed, 0 failed, 0 skipped.

The focused Task 4 run passed 2 tests. IDE lint diagnostics reported no errors
in changed files.

An Android app build was attempted to validate `MauiProgram`, but the machine
does not have an Android SDK configured (`XA5300`). The test and Core builds
completed successfully. Existing `NU1608` dependency warnings remain.

## Commit

`feat: rates cache and OHLC market repository`
