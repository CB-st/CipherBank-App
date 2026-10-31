# Unit Tests

**Project**: CipherBank-app.Tests

Tests Core models, persist, V1 clients, and architecture gates. Does not reference the MAUI app.

## Structure

```
CipherBank-app.Tests/
├── Architecture/
├── Persist/
├── V1/
├── Services/
│   ├── CertificatePinningTests.cs
│   ├── CurrencySymbolMapTests.cs
│   ├── HttpRateLimiterFactoryTests.cs
│   └── IndicativeQuoteMapperTests.cs
└── Session/
```

## Dependencies

- **CipherBank-app.Core** – Models, interfaces
- **Moq** – Mocking
- **FluentAssertions** – Assertions
- **xUnit** – Test framework
- **coverlet** – Coverage

## Service Tests

Tests mock focused ports (e.g. `Mock<IProductClient>`) and verify behavior at that seam.

| File | Tests |
|------|-------|
| HttpProductClientTests | Session, 401 refresh retry, portfolio |
| CipherBankCoreDiTests | Core DI does not register `IProductClient` |
| HttpRateLimiterFactoryTests | 61st acquire rejected on a 60-permit fail-fast window |
| CertificatePinningTests | Platform handler construction |

## Coverage Configuration

- **CollectCoverage**: true
- **CoverletOutputFormat**: cobertura
- **CoverletOutput**: ./coverage/
- **Threshold**: 70 (line, branch, method)
- **ExcludeByAttribute**: Obsolete, GeneratedCodeAttribute, CompilerGeneratedAttribute
