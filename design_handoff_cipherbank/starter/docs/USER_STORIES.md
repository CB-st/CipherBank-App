# Cipherbank procedural user stories (Expo mirror)

Readable counterpart to `e2e/stories/catalog.ts`. Full Draw.io-sourced text matches the scaffold at `UserStories/cipherbank-playwright-scaffold/docs/USER_STORIES.md`.

**ID map:** [`STORY_ID_MAP.md`](./STORY_ID_MAP.md)  
**Config:** [`E2E_CONFIGURABLES.md`](./E2E_CONFIGURABLES.md)

### Expo adaptation notes (summary)

| ID | Expo note |
|----|-----------|
| CB-ACCOUNT-001 | Maps to Welcome → Keys → Quiz → Set PIN → Home. No email/password form — custody is on-device mnemonic + PIN. |
| CB-ACCOUNT-002 | Maps to “Set up this device” returning path + bootstrap pull. |
| CB-WALLET-* | Local derive / hybrid checking not yet full Create-wallet wizard in Expo. |
| CB-FUND-* | Receive tab; network fixture events not wired. |
| CB-CARD-* | Vault cards / guest issuance — lab only. |
| CB-PAY-* | Pay tab + POS lab for prepaid presentment. |
| CB-MARKET-001 | Home history chart + Convert `/iquote` via mock handlers. |
| CB-PREPAID-PLACEHOLDER | Blank source — skipped. |

Every action is executed as a named Playwright `test.step()` in the matching workflow spec when the story is no longer `test.fixme`.

---


## CB-ACCOUNT-001 — Create an account

**Source:** `Create Account.drawio`  
**Actor:** Prospective Cipherbank user  
**Story:** As a prospective cipherbank user, I want to create a Cipherbank account and establish a recoverable encrypted user-data record so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Signed out
- Create-account page available

**Procedure**

1. **`open` — Open the create-account page.**
   - UI assertion: The account form is visible.
   - Service contract: No write request occurs.
2. **`complete-form` — Enter required account information and accept required agreements.**
   - UI assertion: Invalid or missing values are identified before submission.
   - Service contract: No account is created while validation errors remain.
3. **`submit` — Submit the account form.**
   - UI assertion: A single in-progress state is shown.
   - Service contract: Exactly one account-creation request is sent.
4. **`backup` — Complete the recovery-secret or backup step.**
   - UI assertion: Recovery material appears only in the protected flow.
   - Service contract: An encrypted user backup is created.
5. **`complete` — Finish account creation.**
   - UI assertion: Success and an authenticated destination are shown.
   - Service contract: Account and initialized user metadata persist.

**Success criteria**

- Exactly one account exists
- Authenticated session belongs to the new account

**Negative backlog**

- Duplicate identifier
- Weak credentials
- Backup failure
- Network retry is idempotent

## CB-ACCOUNT-002 — Recover an account

**Source:** `Recover Account.drawio`  
**Actor:** Existing Cipherbank user  
**Story:** As a existing cipherbank user, I want to recover an account from recovery material and encrypted backup so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Signed out
- Valid backup exists

**Procedure**

1. **`open` — Open account recovery.**
   - UI assertion: The recovery form is visible without account enumeration.
   - Service contract: No account data is fetched yet.
2. **`enter` — Enter account identifier and recovery material.**
   - UI assertion: Secret format is validated without logging it.
   - Service contract: Recovery material remains protected.
3. **`submit` — Submit recovery.**
   - UI assertion: One recovery attempt is shown as in progress.
   - Service contract: Encrypted backup is requested.
4. **`restore` — Unlock the backup and enroll the device if required.**
   - UI assertion: Restored account information appears only after authorization.
   - Service contract: Wallet references and account state are restored.
5. **`complete` — Enter the recovered account.**
   - UI assertion: Authenticated navigation and restored metadata are visible.
   - Service contract: Recovered session maps to the original account.

**Success criteria**

- Original account is restored
- No replacement wallets are created unexpectedly

**Negative backlog**

- Invalid recovery material
- Rate limiting
- Corrupt backup
- Backup service unavailable

## CB-WALLET-001 — Create a user-controlled wallet

**Source:** `Create Wallet (User).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to create a non-custodial user-controlled savings/vault wallet so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Authenticated
- Supported network enabled

**Procedure**

1. **`open` — Open Wallets and choose Create wallet.**
   - UI assertion: Wallet type selection is visible.
   - Service contract: No wallet exists yet.
2. **`type` — Choose User-controlled / Vault.**
   - UI assertion: Non-custodial behavior is disclosed.
   - Service contract: Custody mode is user-controlled.
3. **`details` — Choose a network and label.**
   - UI assertion: Only supported networks and valid labels continue.
   - Service contract: Creation is scoped to one user and network.
4. **`generate` — Generate the wallet.**
   - UI assertion: Public address and protected recovery step appear.
   - Service contract: Only public metadata/encrypted backup references leave the user boundary.
5. **`confirm` — Confirm recovery material.**
   - UI assertion: Wallet is not ready until confirmation succeeds.
   - Service contract: Private signing material is not server-accessible.
6. **`finish` — Finish.**
   - UI assertion: Wallet card shows label, network, address, zero balance, and User-controlled badge.
   - Service contract: Wallet metadata persists exactly once.

**Success criteria**

- One user-controlled wallet is created
- No private material appears in API payloads

**Negative backlog**

- Abandoned recovery
- Duplicate submission
- Unsupported network

## CB-WALLET-002 — Create a Cipherbank checking wallet

**Source:** `Create Wallet (CB).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to create a Cipherbank checking wallet with hybrid custody so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Authenticated
- Eligible for checking wallet

**Procedure**

1. **`open` — Open Wallets and choose Create wallet.**
   - UI assertion: Wallet type selection is visible.
   - Service contract: No write occurs.
2. **`type` — Choose Cipherbank / Checking.**
   - UI assertion: Hybrid signing behavior is disclosed.
   - Service contract: Custody mode is hybrid.
3. **`details` — Choose a network and label.**
   - UI assertion: Only supported networks can continue.
   - Service contract: The network adapter is selected.
4. **`generate` — Submit wallet creation.**
   - UI assertion: One in-progress state is shown.
   - Service contract: Wallet service creates against the selected node.
5. **`policy` — Complete user-key enrollment or policy approval.**
   - UI assertion: Wallet is not active until policy establishment succeeds.
   - Service contract: Wallet DB stores hybrid policy metadata.
6. **`finish` — Finish.**
   - UI assertion: Wallet card shows Checking and Hybrid badges.
   - Service contract: Node and database wallet identities agree.

**Success criteria**

- One hybrid wallet is created
- Required signing policy is attached

**Negative backlog**

- Node failure
- Policy failure
- Duplicate retry

## CB-FUND-001 — Fund a user-controlled wallet

**Source:** `Fund Wallet (User).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to receive funds into a user-controlled wallet and observe confirmations so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- User wallet exists
- Network fixture available

**Procedure**

1. **`open` — Open the wallet and choose Fund / Receive.**
   - UI assertion: Receive view shows the correct wallet and network.
   - Service contract: Wallet record is loaded.
2. **`address` — Reveal the deposit address.**
   - UI assertion: Address text and QR representation match.
   - Service contract: Address belongs to the selected wallet.
3. **`send` — Send test funds with a network fixture.**
   - UI assertion: A single detected/pending deposit appears.
   - Service contract: The network event is observed.
4. **`confirm` — Advance network confirmations.**
   - UI assertion: Pending balance and confirmation count update deterministically.
   - Service contract: Transaction state transitions persist.
5. **`spendable` — Reach the spendable threshold.**
   - UI assertion: Available balance and one activity row update.
   - Service contract: Confirmed ledger and UI state reconcile.

**Success criteria**

- Pending and available balances are distinct
- Replayed events do not duplicate credit

**Negative backlog**

- Wrong network
- Reorg
- Network outage

## CB-FUND-002 — Fund a Cipherbank checking wallet

**Source:** `Fund Wallet (CB).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to receive funds into a Cipherbank checking wallet so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Checking wallet exists
- Node available

**Procedure**

1. **`open` — Open the checking wallet and choose Fund / Receive.**
   - UI assertion: Receive view identifies Checking / Hybrid custody.
   - Service contract: Wallet record is loaded.
2. **`address` — Reveal the deposit address.**
   - UI assertion: Address and QR match the selected network.
   - Service contract: Wallet module resolves the receiving address.
3. **`send` — Send test funds.**
   - UI assertion: One pending incoming transaction appears.
   - Service contract: Node detects the transaction.
4. **`confirm` — Advance confirmations.**
   - UI assertion: Pending, confirmed, and available values update correctly.
   - Service contract: Canonical states persist.
5. **`activity` — Open Activity.**
   - UI assertion: Deposit row shows amount, network, identifier, timestamp, and status.
   - Service contract: Activity and balance reconcile.

**Success criteria**

- Confirmed amount becomes available
- Duplicate notifications do not duplicate credit

**Negative backlog**

- Node outage
- Reorg/replacement

## CB-CARD-001 — Create a prepaid card from an account

**Source:** `Create Prepaid Card (Account).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to create and fund a prepaid card from a wallet so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Authenticated
- Eligible funded wallet exists

**Procedure**

1. **`open` — Open Cards and choose Create prepaid card.**
   - UI assertion: Card creation form is visible.
   - Service contract: No card exists yet.
2. **`details` — Choose source wallet, asset, card currency, and amount.**
   - UI assertion: Balance and limits are visible.
   - Service contract: Wallet availability is verified.
3. **`quote` — Request a quote.**
   - UI assertion: Rate, fees, card value, crypto amount, and expiry are visible.
   - Service contract: Cached/provider market quote is obtained.
4. **`confirm` — Confirm before quote expiry.**
   - UI assertion: One processing state is shown.
   - Service contract: Funds are reserved or debited once.
5. **`step-up` — Complete any step-up/signing approval.**
   - UI assertion: Card issuance cannot continue before required approval.
   - Service contract: Policy result attaches to the transaction.
6. **`issued` — Wait for issuance.**
   - UI assertion: Masked card details, status, and funded balance appear.
   - Service contract: Card network and user-card records update.
7. **`activity` — Open Activity.**
   - UI assertion: One card creation/funding row is shown.
   - Service contract: Interaction and transaction history records persist.

**Success criteria**

- Source wallet debited once
- Card value reconciles to quote and fees

**Negative backlog**

- Expired quote
- Insufficient balance
- Card-network failure
- Duplicate submit
- Security/compliance review

## CB-CARD-002 — Create a prepaid card as a guest

**Source:** `Create Prepaid Card (Guest).drawio`  
**Actor:** Guest user  
**Story:** As a guest user, I want to exchange supported cryptocurrency for a guest prepaid card so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Signed out
- Guest card flow enabled

**Procedure**

1. **`open` — Open guest card creation.**
   - UI assertion: Guest limitations are disclosed.
   - Service contract: Temporary guest transaction identity is created.
2. **`details` — Choose source asset, card currency, and amount.**
   - UI assertion: Supported assets and limits are visible.
   - Service contract: No persistent account is created.
3. **`quote` — Request a quote.**
   - UI assertion: Rate, fees, amount due, card value, and expiry are visible.
   - Service contract: Market quote is obtained.
4. **`payment` — Confirm and obtain payment instructions.**
   - UI assertion: Address and QR match the selected network and quote.
   - Service contract: Temporary receiving path is prepared.
5. **`confirmations` — Send crypto and advance network state.**
   - UI assertion: Status moves awaiting \u2192 detected \u2192 confirmed.
   - Service contract: Selected network confirms payment.
6. **`issued` — Receive the card.**
   - UI assertion: Masked card, balance, status, and guest retrieval mechanism appear.
   - Service contract: One guest card is issued.

**Success criteria**

- No persistent account is created
- Exactly one card is issued

**Negative backlog**

- Under/overpayment
- Late payment
- Wrong network
- Expired quote
- Reload/resume
- Issuance failure

## CB-PAY-001 — Pay a merchant from a user-controlled wallet

**Source:** `Pay Merchent (User Wallet).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to pay a merchant from a user-controlled wallet so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Authenticated
- Funded user wallet exists

**Procedure**

1. **`start` — Start merchant payment.**
   - UI assertion: Merchant, amount, currency, and source are visible.
   - Service contract: Canonical payment transaction is created.
2. **`source` — Select the user-controlled wallet.**
   - UI assertion: Balance and custody mode are shown.
   - Service contract: Selected wallet is resolved.
3. **`quote` — Request or receive a conversion quote.**
   - UI assertion: Crypto debit, merchant amount, rate, fees, and expiry are visible.
   - Service contract: Market data is obtained.
4. **`authorize` — Authorize with the user signing method.**
   - UI assertion: Payment cannot continue without valid authorization.
   - Service contract: Private signing material remains user-controlled.
5. **`submit` — Submit the authorized payment.**
   - UI assertion: One processing state is shown.
   - Service contract: Network broadcast and merchant settlement are initiated.
6. **`result` — Receive the outcome.**
   - UI assertion: A canonical outcome is displayed.
   - Service contract: Transaction reaches a defined state.
7. **`activity` — Open Activity.**
   - UI assertion: One reconciled merchant payment row appears.
   - Service contract: Transaction history updates.

**Success criteria**

- Wallet debit and merchant settlement reconcile
- Signing secret remains private

**Negative backlog**

- Expired quote
- Insufficient balance
- Invalid signature
- Compliance denial
- Broadcast failure
- Reload during processing

## CB-PAY-002 — Pay a merchant from a Cipherbank checking wallet

**Source:** `Pay Merchent (CB Wallet).drawio`  
**Actor:** Authenticated Cipherbank user  
**Story:** As a authenticated cipherbank user, I want to pay a merchant under the hybrid user-and-Cipherbank signing policy so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Authenticated
- Funded checking wallet exists
- Co-signing path healthy

**Procedure**

1. **`start` — Start merchant payment.**
   - UI assertion: Merchant, amount, currency, and source are visible.
   - Service contract: Canonical payment is created.
2. **`source` — Select the Cipherbank checking wallet.**
   - UI assertion: Checking / Hybrid custody and balance are shown.
   - Service contract: Wallet and key policy are resolved.
3. **`quote` — Review the quote.**
   - UI assertion: Rate, expiry, debit, merchant amount, and fees are shown.
   - Service contract: Market data is obtained.
4. **`user-approval` — Approve the user side.**
   - UI assertion: Final approval is not displayed prematurely.
   - Service contract: User approval attaches to the transaction.
5. **`cb-approval` — Complete Cipherbank co-sign/policy.**
   - UI assertion: Step-up, review, denial, or approval is visible.
   - Service contract: Cipherbank policy validates and co-signs.
6. **`settle` — Broadcast and settle.**
   - UI assertion: One processing transaction is shown.
   - Service contract: Node and merchant settlement paths execute.
7. **`result` — Receive result and open Activity.**
   - UI assertion: Receipt and activity reconcile.
   - Service contract: Wallet, history, backup metadata, and tracking update.

**Success criteria**

- Both authorization sides precede settlement
- Wallet is debited once

**Negative backlog**

- Missing user approval
- Co-sign unavailable
- Expired quote
- Settlement failure

## CB-PAY-003 — Pay a merchant with a prepaid card

**Source:** `Pay Merchent (Prepayed).drawio`  
**Actor:** Prepaid card holder  
**Story:** As a prepaid card holder, I want to pay a merchant at POS using a prepaid card so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Active funded prepaid card exists
- POS fixture available

**Procedure**

1. **`present` — Present the card through the supported POS/RFID path.**
   - UI assertion: Merchant and purchase amount are displayed.
   - Service contract: One authorization request is received.
2. **`authorize` — Submit authorization.**
   - UI assertion: Request enters pending once.
   - Service contract: Card status and available balance are validated.
3. **`verify` — Apply required card verification.**
   - UI assertion: Invalid, blocked, expired, or insufficient cards fail distinctly.
   - Service contract: Card controls are enforced.
4. **`result` — Receive authorization result.**
   - UI assertion: POS and Cipherbank views agree.
   - Service contract: Approved balance update is atomic.
5. **`receipt` — View card balance and receipt.**
   - UI assertion: Balance decreases exactly once and one receipt appears.
   - Service contract: Transaction persists once.

**Success criteria**

- Purchase is authorized once
- POS result and card ledger agree

**Negative backlog**

- Insufficient balance
- Blocked/expired card
- Duplicate authorization
- Timeout/reconciliation

## CB-MARKET-001 — View current and historical price data

**Source:** `View Price Data.drawio`  
**Actor:** Cipherbank visitor or user  
**Story:** As a cipherbank visitor or user, I want to view fresh current and historical cryptocurrency market data so that I can complete the Cipherbank workflow safely and verifiably.

**Preconditions**

- Market page available
- Provider or cache data exists

**Procedure**

1. **`open` — Open the market-data page.**
   - UI assertion: Asset, quote currency, current price, chart, and freshness controls are visible.
   - Service contract: Current data is requested.
2. **`select` — Select asset and quote currency.**
   - UI assertion: Selection is reflected in UI state.
   - Service contract: Correct market pair is requested.
3. **`current` — View the current price.**
   - UI assertion: Price, source, and as-of timestamp are shown.
   - Service contract: Cache/provider data is served.
4. **`history` — Select a historical range.**
   - UI assertion: Chart/table updates without mixing assets.
   - Service contract: Historical database supplies the series.
5. **`freshness` — Refresh or exceed freshness threshold.**
   - UI assertion: Fresh, cached, stale, and unavailable states are distinguishable.
   - Service contract: Provider fallback follows cache policy.

**Success criteria**

- Values match selected pair/range
- Source time is not misrepresented

**Negative backlog**

- Provider failover
- All providers unavailable
- Historical gaps
- Rapid asset switching race
