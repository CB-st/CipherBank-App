# .NET 10 compliance migration report

- Target: `/home/claude/work/cipherbank/CipherBank-App-prototype-maui-m1a-platform`
- Profile: `desktop`
- Projects: 5
- Findings: 86 (critical 0, high 9, medium 69, low 8)

This is a static inventory, not proof of a defect. Preserve behavior with characterization tests before changing intent-bearing code.

## Project inventory

| Project | SDK | Targets | Test | Project references |
|---|---|---|---:|---:|
| `CipherBank-app/CipherBank-app.csproj` | `Microsoft.NET.Sdk` | `net10.0-android, net10.0-ios, net10.0-maccatalyst` | no | 1 |
| `CipherBank-app.Core/CipherBank-app.Core.csproj` | `Microsoft.NET.Sdk` | `net10.0` | no | 0 |
| `CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj` | `Microsoft.NET.Sdk` | `net10.0` | yes | 1 |
| `CipherBank-app.IntegrationTests/CipherBank-app.IntegrationTests.csproj` | `Microsoft.NET.Sdk` | `net10.0` | yes | 1 |
| `CipherBank-app.Tests/CipherBank-app.Tests.csproj` | `Microsoft.NET.Sdk` | `net10.0` | yes | 1 |

## Findings

### HIGH ASYNC001 — `CipherBank-app/Extensions/HttpClientExtensions.cs`:71

Synchronous blocking on asynchronous work can deadlock or exhaust threads.

**Remediation:** Make the call chain async and await the operation while propagating cancellation.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC001 — `CipherBank-app/Extensions/HttpClientExtensions.cs`:75

Synchronous blocking on asynchronous work can deadlock or exhaust threads.

**Remediation:** Make the call chain async and await the operation while propagating cancellation.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/AssetPickerPage.xaml.cs`:26

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/AssetPickerPage.xaml.cs`:38

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/DashboardPage.xaml.cs`:23

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/PurchasePage.xaml.cs`:24

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/PurchasePage.xaml.cs`:36

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/WalletPage.xaml.cs`:33

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### HIGH ASYNC002 — `CipherBank-app/Views/WalletPage.xaml.cs`:56

async void cannot be awaited and hides failures.

**Remediation:** Return Task unless this is a UI event handler; isolate UI handlers from testable async logic.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Core/Services/RateLimiter.cs`:69

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Core/Services/RateLimiter.cs`:108

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/ApiIntegrationTests.cs`:50

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:69

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:83

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:129

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:130

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:156

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:164

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:165

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:193

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.IntegrationTests/MockServerFixture.cs`:213

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/AuthTokenTests.cs`:16

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/AuthTokenTests.cs`:22

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:19

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:20

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:21

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:35

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:36

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:37

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:51

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:52

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:53

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:67

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:68

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:69

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:83

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:84

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:112

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:128

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/PriceHistoryTests.cs`:129

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/TransactionTests.cs`:80

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/TransactionTests.cs`:102

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/TransactionTests.cs`:124

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/WalletTests.cs`:23

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/WalletTests.cs`:39

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/WalletTests.cs`:55

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Models/WalletTests.cs`:74

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/AuthServiceTests.cs`:23

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/AuthServiceTests.cs`:36

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/AuthServiceTests.cs`:64

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs`:83

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs`:84

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs`:89

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs`:90

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/TransactionServiceTests.cs`:29

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/TransactionServiceTests.cs`:39

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/TransactionServiceTests.cs`:69

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/TransactionServiceTests.cs`:116

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/WalletServiceTests.cs`:22

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/WalletServiceTests.cs`:23

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/WalletServiceTests.cs`:45

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app.Tests/Services/WalletServiceTests.cs`:98

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/AuthService.cs`:100

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Handlers/AuthHeaderHandler.cs`:57

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:69

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:94

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:115

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:147

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockCryptoAPIService.cs`:94

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockCryptoAPIService.cs`:137

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockTransactionService.cs`:101

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockTransactionService.cs`:155

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockTransactionService.cs`:201

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockWalletService.cs`:42

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockWalletService.cs`:50

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockWalletService.cs`:58

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockWalletService.cs`:66

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### MEDIUM TIME001 — `CipherBank-app/Services/Mocks/MockWalletService.cs`:135

Direct wall-clock access makes behavior harder to test and can introduce time-zone errors.

**Remediation:** Inject TimeProvider and use GetUtcNow; convert zones only at explicit boundaries.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app.Core/Services/IAuthService.cs`:18

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app.Core/Services/IAuthService.cs`:20

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/AuthService.cs`:65

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/AuthService.cs`:92

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/IDialogService.cs`:21

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:101

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/Mocks/MockAuthService.cs`:107

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

### LOW CANCEL001 — `CipherBank-app/Services/ShellDialogService.cs`:15

An asynchronous API may not expose cancellation.

**Remediation:** If work performs I/O or can run materially long, accept and propagate CancellationToken.

**Review note:** Review context before changing code; static matching can produce false positives.

## Migration order

1. Make the current build and test status reproducible.
2. Select one vertical behavior and complete the intent worksheet.
3. Add characterization tests around observable behavior.
4. Separate policy, ports, adapters, and composition without changing output.
5. Add unit, integration, contract, and cancellation tests at the appropriate boundaries.
6. Enable enforcement for the migrated scope and clear warnings deliberately.
7. Repeat until repository-wide enforcement succeeds.

Use `.compliance/docs/INTENT-TRANSLATION.md` and `.compliance/docs/TESTING-PLAYBOOK.md` for the detailed workflow.
