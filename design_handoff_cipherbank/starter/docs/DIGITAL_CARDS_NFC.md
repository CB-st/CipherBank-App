# Digital cards & NFC presentment (CipherBank lab ↔ Visa / Mastercard)

CipherBank’s POS lab models the **issuer-side / wallet-side** half of contactless pay: unlock custody → fund with crypto/fiat → mint a short-lived **processor token ref** → present `{ sessionId, tokenRef }` over NFC (or Simulate tap). The PAN never leaves the processor vault.

This document maps that lab flow to public Visa and Mastercard digital-enablement docs, and to what a production stack still needs.

## Lab flow (what we ship today)

```
POST /pos/sessions          → pending_auth
POST /pos/authorize         → ephemeral presentment { tokenRef, last4, brand, ttlMs }
POST /pos/confirm           → ready_to_present
NFC / Simulate exchange     → RF or mock APDU stages carry tokenRef only
stream pos.settled          → receipt
```

See [`POS_API.md`](../src/mocks/POS_API.md) and [`TESTING.md`](./TESTING.md).

| Lab concept | Production analogue |
|-------------|---------------------|
| Vault `cardId` + `hardwareTest` | Digitized token on device / cloud (DPAN), never raw PAN in app |
| `ephemeralCardTokenId` / `tokenRef` | Single-use or short-TTL cryptogram / token for this tap |
| `deviceAttestation` | CDCVM / biometrics + wallet unlock |
| Simulate exchange steps | Contactless EMV kernel steps (SELECT → GPO → GENERATE AC) |
| `nfcPresent` NDEF JSON | Stand-in payload; real HCE serves EMV APDUs from SE/TEE |

## Visa — digital card creation & handling

Primary product: **[Visa Token Service (VTS) — Provisioning & Credential Management](https://developer.visa.com/capabilities/token-service-provisioning)**

What Visa documents publicly:

1. **Tokenization** — Replace PAN with a domain-restricted token (same length/format as a PAN for rails). Aligns with **EMVCo payment tokenization**.
2. **Token requestor** — Wallet / merchant / fintech registers with Visa; requests tokens for enrolled accounts.
3. **Issuer APIs** (Visa → issuer during provisioning):
   - Check Eligibility (ID&V, card art, T&Cs, Token Reference ID)
   - Approve Provisioning (approve / decline / step-up)
   - Get Cardholder Verification Methods + Send Passcode (OTP step-up)
   - Token Inquiry / Lifecycle (suspend, resume, delete)
   - PAN Lifecycle + Update Card Metadata
4. **Channels** — e-commerce, m-commerce, in-app, **contactless NFC**.
5. **In-app push provisioning** — [Visa In-App Provisioning](https://developer.visa.com/capabilities/visa-in-app-provisioning/overview) / [Visa Digital Enablement SDK](https://developer.visa.com/capabilities/visa-digital-enablement-sdk/docs-getting-started-dcd) encrypts payload for Apple Pay / Google Pay / Samsung Pay so the banking app does not implement each wallet API raw.

CipherBank mapping:

- Our vault stores **processor display metadata + token ids**, not PAN/CVV (matches VTS “token instead of PAN”).
- Authorize → `presentment.tokenRef` ≈ “credential ready for this device/domain + TTL”.
- Full VTS onboarding requires Visa Ready / account executive — sandbox APIs are gated.

## Mastercard — digital card creation & handling

Primary product: **[Mastercard Digital Enablement Service (MDES)](https://developer.mastercard.com/mdes-digital-enablement/documentation/)**  
Overview: [MDES for Financial Institutions](https://engagepartners.mastercard.com/english/solutions/solution/MDES%20for%20Financial%20Institutions)

What MDES covers:

1. **Digitization** — Deliver tokenized credentials to a device (Secure Element or **HCE**) or cloud.
2. **Tokenization** — PAN → token; MDES validates crypto / domain controls on use.
3. **NFC contactless** — Digitized credentials drive existing contactless POS (plus in-app / browser / COF).
4. **Issuer integration** — Pre-Digitization APIs (portfolio enablement), Customer Service / lifecycle APIs, Payment Account Management (PAM).
5. **Token Connect / push provisioning** — Issuer app pushes eligibility into wallet / merchant token requestors.

CipherBank mapping:

- Android-first `nfc.ts` + HCE path later = MDES/HCE model (iOS consumer HCE is not equivalent; Apple Tap to Pay / Wallet is a separate program).
- Lab `Simulate exchange` stages mirror what an MDES-enabled HCE applet would answer over ISO-DEP — without implementing proprietary Mastercard crypto.

## Contactless exchange (EMV-shaped lab stages)

Real Visa/MC contactless is **ISO 14443 + EMV Contactless** APDUs (AID select, PDOL/GPO, GENERATE AC / cryptogram). Our lab does **not** speak real scheme crypto; it animates the same *phases* and logs the CipherBank NDEF/JSON payload:

| Stage | Lab name | Meaning |
|-------|----------|---------|
| 1 | `rf_field` | Reader field detected / mock RF up |
| 2 | `select_ppse` | SELECT PPSE (Proximity Payment System Environment) |
| 3 | `select_aid` | SELECT Visa/MC AID |
| 4 | `get_processing_options` | GPO — transaction params |
| 5 | `generate_ac` | Application cryptogram / tokenized auth data |
| 6 | `outcome` | APPROVED lab settle with `tokenRef` |

Production: stages 4–5 use keys from the token vault / HCE; CipherBank only forwards `tokenRef` to the processor who holds the real cryptogram material.

## What to build next (production)

1. **Issuer + processor partnership** — VTS and/or MDES as token requestor *or* issuer connector (not both inventively).
2. **Push provisioning** — VDE SDK / MDES Token Connect into Google Wallet (Android) first.
3. **HCE service** — Android `HostApduService` serving EMV APDUs; replace NDEF write in `nfc.ts`.
4. **CDCVM** — Map `unlockLocalCustody` to FIDO / biometrics required by scheme.
5. **Never** send PAN/CVV/mnemonic to CipherBank servers (already in API contract).

## Android demo notes

- Emulator NFC RF is unreliable — use **Simulate exchange** on AVD.
- Physical device + EAS/dev client: **Start NFC** writes NDEF presentment; true HCE is a follow-on.
- Commands: see [`TESTING.md`](./TESTING.md).
