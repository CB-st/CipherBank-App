# Build Fixes and Static Analysis Cleanup

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Get the CipherBank MAUI app building successfully and resolve all static analysis warnings/errors.

**Architecture:** Fix compilation errors first (API incompatibilities, missing namespaces), then address security analyzer errors (CA5394), then configure .editorconfig to suppress style rules that conflict with project conventions, and finally fix remaining code quality warnings file-by-file.

**Tech Stack:** .NET 10, MAUI, StyleCop.Analyzers, Microsoft.CodeAnalysis.NetAnalyzers

---

## Current Error/Warning Summary

**Build errors (blocking compilation):**
- CS0266 (2): `AddStandardResilienceHandler()` returns `IHttpStandardResiliencePipelineBuilder`, not `IHttpClientBuilder`
- CS0103 (2): `DelayBackoffType` not in scope — needs `using Polly;`
- CS0246 (2): `Platforms` namespace not found in `PlatformHttpHandlerFactory.cs`
- CA5394 (22): `Random` is insecure — treated as error via `WarningsAsErrors`

**Style warnings to suppress (conflict with project conventions):**
- SA1101 (1356): "Prefix local calls with this" — modern C# omits `this.`
- SA1309 (118): "Field names must not begin with underscore" — project uses `_camelCase` per .editorconfig
- SA1200 (296): "Using directives must be placed within namespace" — conflicts with file-scoped namespaces
- SA0001 (2): XML comment analysis — not all files have XML docs

**Code quality warnings to fix:**
- CA1848 (408) + CA1873 (166): High-performance logging — use `LoggerMessage` source generators
- SA1633 (80): Missing file headers
- SA1503 (60): Braces must not be omitted
- SA1116/SA1117 (162): Parameter must be on own line / should not be on same line
- SA1400 (18): Member must declare access modifier
- SA1518 (16): File must end with single newline
- SA1413 (16): Use trailing comma in multi-line initializers
- SA1208 (14): System using directives must be placed before other using directives
- SA1513 (10): Closing brace must be followed by blank line
- SA1202/SA1201 (22): Member ordering
- SA1203 (18): Constants must appear before fields
- SA1516 (8): Elements must be separated by blank line
- SA1134 (8): Attributes must be on own line
- SA1407 (6): Arithmetic expressions must declare precedence
- SA1204 (6): Static members before instance
- SA1118 (6): Parameter must not span multiple lines
- CA2254 (6): Log template should be static expression
- CA1866 (6): Use char overload of string methods
- CA1852 (10): Type can be sealed
- CA1822 (10): Member does not access instance data — can be static
- CA1310 (10): Specify StringComparison for correctness
- CA1001 (10): Types that own disposable fields should be disposable
- CA1305 (4): Specify IFormatProvider
- SA1316 (4): Tuple element names should use correct casing
- SA1641/SA1636 (4): Company/copyright text mismatch
- CA1711 (2), CA1707 (2), IDE0052 (2): Miscellaneous
- SA1515 (2), SA1508 (2), SA1507 (2), SA1502 (2), SA1122 (2), SA1119 (2), SA1013 (2): Miscellaneous style

---

## Phase 1: Fix Build Errors

### Task 1: Fix HttpClientExtensions.cs API compatibility

**Files:**
- Modify: `CipherBank-app/Extensions/HttpClientExtensions.cs`

**Step 1: Fix the return type and missing namespace**

The `AddStandardResilienceHandler()` method in `Microsoft.Extensions.Http.Resilience` v10.0.0 returns `IHttpStandardResiliencePipelineBuilder`, not `IHttpClientBuilder`. We need to:
1. Add `using Polly;` for `DelayBackoffType`
2. Save the `IHttpClientBuilder` before calling `AddStandardResilienceHandler`, since that method returns a different type

```csharp
// Line 6: Add Polly using
using Polly;

// Lines 26-43: Store builder before resilience call, call resilience separately
var builder = services.AddHttpClient<TClient>((sp, http) =>
{
    var settings = sp.GetRequiredService<ISettingsService>();
    http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
    http.Timeout = TimeSpan.FromSeconds(30);
    http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
    http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
    http.DefaultRequestHeaders.Add("X-Platform", DeviceInfo.Platform.ToString());
#endif
    configure?.Invoke(sp, http);
})
.ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
.AddHttpMessageHandler(sp => new RateLimitingHandler(sp))
.AddHttpMessageHandler(sp => new AuthHeaderHandler(sp));

builder.AddStandardResilienceHandler(ConfigureResilienceOptions);

return builder;
```

**Step 2: Build to verify CS0266, CS0103 are resolved**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep -E 'error CS'`
Expected: No CS errors

### Task 2: Fix PlatformHttpHandlerFactory.cs namespace issue

**Files:**
- Modify: `CipherBank-app/Services/PlatformHttpHandlerFactory.cs`
- Check: `CipherBank-app/Platforms/` directory for actual handler types

**Step 1: Investigate what platform handler types exist**

Check `CipherBank-app/Platforms/iOS/`, `Platforms/Android/`, `Platforms/Windows/` for the certificate pinning handler classes and their actual namespaces.

**Step 2: Add correct using directives or fix namespace references**

The `Platforms.*` types are typically compiled per-target-framework. The error CS0246 suggests the namespace resolution isn't working for maccatalyst. Add proper `#if` guards or namespace usings depending on what's found.

**Step 3: Build to verify CS0246 is resolved**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep 'error CS0246'`
Expected: No CS0246 errors

### Task 3: Fix CA5394 — Replace Random with cryptographic RNG in mock services

**Files:**
- Modify: `CipherBank-app/Services/Mocks/MockWalletService.cs`
- Modify: `CipherBank-app/Services/Mocks/MockTransactionService.cs`
- (MockCryptoAPIService.cs already uses `RandomNumberGenerator` for `SimulateNetworkDelayAsync` but still uses `_random` elsewhere)
- Modify: `CipherBank-app/Services/Mocks/MockCryptoAPIService.cs`

**Step 1: In each mock file, replace `Random` field with `RandomNumberGenerator` usage**

Replace `private readonly Random _random = new();` with helper methods using `System.Security.Cryptography.RandomNumberGenerator`:

```csharp
// Remove: private readonly Random _random = new();
// Add using if not present: using System.Security.Cryptography;

// Replace _random.Next(min, max) with:
RandomNumberGenerator.GetInt32(min, max)

// Replace _random.NextDouble() with:
RandomNumberGenerator.GetInt32(0, 10000) / 10000.0
```

Apply to all usages in:
- `MockWalletService.cs`: lines 170, 178, 185, 192, 221
- `MockCryptoAPIService.cs`: lines 120, 154, 160
- `MockTransactionService.cs`: lines 264, 271, 285

**Step 2: Build to verify CA5394 is resolved**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep 'error CA5394'`
Expected: No CA5394 errors

**Step 3: Verify build succeeds**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep -E '(Build SUCCEEDED|Build FAILED)'`
Expected: `Build SUCCEEDED`

**Step 4: Commit**

```bash
git add CipherBank-app/Extensions/HttpClientExtensions.cs CipherBank-app/Services/PlatformHttpHandlerFactory.cs CipherBank-app/Services/Mocks/
git commit -m "fix: resolve build errors - API compatibility, namespace, and security"
```

---

## Phase 2: Configure .editorconfig Suppressions

### Task 4: Suppress conflicting StyleCop rules in .editorconfig

**Files:**
- Modify: `.editorconfig`

**Step 1: Add suppressions for rules that conflict with project conventions**

Add to the `[*.cs]` section:

```
# Suppress StyleCop rules that conflict with modern C# conventions
dotnet_diagnostic.SA1101.severity = none   # Don't require 'this.' prefix
dotnet_diagnostic.SA1309.severity = none   # Allow _camelCase private fields
dotnet_diagnostic.SA1200.severity = none   # Allow usings outside namespace (file-scoped)
dotnet_diagnostic.SA0001.severity = none   # Don't require XML docs on all files
```

**Step 2: Build to verify warning count drops**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep -oE 'warning [A-Z][A-Z0-9]+' | sort | uniq -c | sort -rn`
Expected: SA1101, SA1309, SA1200, SA0001 no longer appear

**Step 3: Commit**

```bash
git add .editorconfig
git commit -m "chore: suppress StyleCop rules that conflict with project conventions"
```

---

## Phase 3: Fix Remaining Warnings

### Task 5: Fix CA1848 + CA1873 — High-performance logging with LoggerMessage source generators

**Files:**
- All files under `CipherBank-app/Services/` and `CipherBank-app/ViewModels/` that use `ILogger`

**Step 1: For each service/viewmodel class, add a static partial class with `[LoggerMessage]` attributes**

For each file, convert `_logger.LogInformation(...)` calls to source-generated methods. Example pattern:

```csharp
// At the bottom of the file, add:
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Returned {Count} wallets")]
    public static partial void ReturnedWallets(ILogger logger, int count);
}
```

Then replace calls: `_logger.LogInformation("Returned {Count} wallets", count)` → `Log.ReturnedWallets(_logger, count)`

This is the largest single task. Do it file by file, building after each file to verify.

### Task 6: Fix SA1633 — Add file headers

**Files:**
- All `.cs` files in `CipherBank-app/` missing the header

**Step 1: Add file header to each file**

```csharp
// <copyright file="FileName.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>
```

### Task 7: Fix SA1503 — Add braces to single-line statements

**Files:**
- All files with single-line `if`, `else`, `for`, etc. without braces

### Task 8: Fix SA1116/SA1117 — Parameter line placement

**Files:**
- Files with multi-line method calls where parameters aren't consistently placed

### Task 9: Fix remaining SA warnings (SA1400, SA1518, SA1413, SA1208, SA1513, SA1202, SA1201, SA1203, SA1516, SA1134, SA1407, SA1204, SA1118, SA1316, SA1641, SA1636, etc.)

**Files:**
- Various files as identified by build output

### Task 10: Fix remaining CA warnings (CA2254, CA1866, CA1852, CA1822, CA1310, CA1001, CA1305, CA1711, CA1707, IDE0052)

**Files:**
- Various files as identified by build output

### Task 11: Final verification

**Step 1: Full build with zero warnings**

Run: `dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-maccatalyst 2>&1 | grep -E '(warning|error)' | wc -l`
Expected: 0

**Step 2: Build Core and Tests projects**

Run: `dotnet build CipherBank-app.Core/ && dotnet build CipherBank-app.Tests/`
Expected: Both succeed

**Step 3: Run tests**

Run: `dotnet test CipherBank-app.Tests/`
Expected: All tests pass

**Step 4: Final commit**

```bash
git add -A
git commit -m "chore: resolve all static analysis warnings"
```
