# ChallengePass configuration

`challenge-pass.json` selects the active installed challenge/pass suite. It contains identifiers only; secret keys, seed material, nonces, and credentials must never be stored here.

The file is embedded by `CipherBank-app.ChallengePass` and loaded through `ChallengePassDefaultsConfiguration`. A host may add later configuration providers to override the default before calling `AddChallengePassModule(IConfiguration)`.

Supported values:

- `a1-x25519-chacha-v1`: X25519 sealed challenge/pass flow; current compatibility default.
- `a2-hybrid-pq-channel-v1`: ML-KEM-768 + X25519 channel flow; activate only when the host registers the required key-share, challenge, and custody ports.

Unknown values fail options validation instead of silently falling back.
