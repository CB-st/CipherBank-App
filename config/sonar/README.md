# Sonar quality-gate policy

`quality-gate.yaml` is the checked-in contract for the live gate on
https://sonar.cipherbank.money. CI runs `scripts/verify-sonar-quality-gate.py`
after each scan and fails if fetched conditions and this file disagree in either
direction.

## Live set (PR #33)

Coverage, duplicated-line density, and violations on new code. This matches the
Sonar CAYC gate currently assigned to the project.

## Deferred expansion

Ratings (reliability / security / maintainability worse than A), security-hotspot
review at 100%, and blocker / critical issue counts are commented in
`quality-gate.yaml`. Restore them only when the same conditions exist on the
server:

1. Add the six conditions to **CipherBank New Code Gate** on Sonar.
2. Uncomment the matching YAML block in the same change.
3. Confirm `verify-sonar-quality-gate.py` against a fetched `quality-gate.json`.

Do not uncomment YAML first. A thinned live gate then fails the verifier, which
is the intended fail-closed behavior.
