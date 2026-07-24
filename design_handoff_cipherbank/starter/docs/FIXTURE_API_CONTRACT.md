# Optional Cipherbank test-fixture API contract

Mirrored from the Playwright scaffold. Expo Cora **does not** call this endpoint yet — clean-install and funded scenarios use:

- Playwright storage reset (`e2e/fixtures/onboarding.ts`)
- In-process mocks (`src/mocks/handlers.ts`)
- Env flags (`EXPO_PUBLIC_SEED_DEMO`, `EXPO_PUBLIC_MOCK_HAS_WALLET`)

When a test-only fixture service exists, `FixtureApi.ensure()` will send:

```http
POST ${CB_FIXTURE_API_URL}
Authorization: Bearer ${CB_FIXTURE_API_TOKEN}
Content-Type: application/json

{ "fixture": "funded-user-wallet" }
```

Supported fixture names:

- `funded-user-wallet`
- `funded-cb-wallet`
- `funded-prepaid-card`
- `recoverable-account`
- `market-data`

Recommended response fields are additive; tests use only the fields relevant to the scenario:

```json
{
  "fixture": "funded-user-wallet",
  "userId": "usr_e2e",
  "walletId": "wallet_e2e",
  "walletLabel": "E2E Funded Vault",
  "network": "bitcoin",
  "availableBalance": "0.01000000"
}
```

Recovery fixture:

```json
{
  "fixture": "recoverable-account",
  "identifier": "recoverable@example.test",
  "recoverySecret": "test-only-secret"
}
```

Prepaid-card fixture:

```json
{
  "fixture": "funded-prepaid-card",
  "cardId": "card_e2e",
  "cardToken": "card_test_4242",
  "availableBalance": "100.00"
}
```

## Required behavior

The fixture service should be test-environment-only, authenticated, idempotent, and capable of cleaning or namespacing records by Playwright worker. It must never be deployed on a production route.

## Expo note

Until `CB_FIXTURE_API_URL` is wired, do not block stories on this contract. Prefer UI-driven setup for CB-ACCOUNT-001 and mock handlers for market/portfolio reads.
