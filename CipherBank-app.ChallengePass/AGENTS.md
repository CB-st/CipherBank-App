# ChallengePass module contract

This contract supplements the repository root for `CipherBank-app.ChallengePass`.

## Boundary

- Keep the module UI- and storage-neutral. It depends on Core wire DTOs and custody abstractions, not MAUI or SQLite.
- Define host-owned communication through ports (`ISessionChallengeClient`, `IPqKeyShareClient`, `IPqChannelChallengeSource`). Register implementations at the host composition root.
- Suite composition belongs in `ChallengePassServiceCollectionExtensions`; callers select a suite through typed, validated `ChallengePassOptions`.

## Secret ownership

- Treat every private key, seed, KEM secret, X25519 secret, channel key, and decrypted pass as sensitive.
- Hold sensitive arrays for the shortest practical lifetime and wipe them in `finally` blocks. Cleanup must cover decoding errors, cancellation, and partial construction.
- Copy buffers only when ownership crosses an object boundary. Document which side must clear the copy.
- Objects retaining keys implement a clearing/disposal path and wipe displaced keys before replacement.
- Do not log or serialize private material. Tests inspect absence of seed, mnemonic, PIN, and private-key fields in wire bodies.

## Protocol evolution

- Keep algorithm, template, and structure slots independently versioned.
- Do not change existing suite or wire identifiers in place. Add a new version and retain compatibility tests.
- A2 pass construction uses `BuildSessionOpenBodyWithIdentityAsync` so identity adoption, channel establishment, and account binding share one gate hold.
- New algorithms require known-answer or round-trip tests, malformed-input tests, cancellation tests, and explicit buffer-cleanup tests.

## Configuration and DI

- Non-secret defaults live in `config/challenge-pass/challenge-pass.json` and are explained in the adjacent README.
- Unknown suite IDs fail validation. Never silently downgrade A2 to A1 or vice versa.
- `InMemory*` implementations are stateful development/test fixtures. Use Moq when a test needs only an interaction or failure from a port.
- Package versions remain in the root `Directory.Packages.props`.
