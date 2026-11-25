#!/usr/bin/env bash
# Stamp a current+backup SPKI pin-set into Android network_security_config.xml.
# Use: Low (Appium pin-path tests or CI release). Scope: that XML file only.
#
# Skirt (default): leave the committed file alone — system CAs, no pin-set.
# Appium: export ANDROID_CERT_PINS=current,backup before scripts/e2e-android.sh
#   so the Debug APK exercises pinning against a lab/live leaf + backup.
# CI: map GitHub secret ANDROID_CERT_PINS into the same env var before a
#   Release stamp. Do not invent hashes; require both current and backup.
#
# Usage: ANDROID_CERT_PINS=<b64>,<b64> ./scripts/stamp-android-cert-pins.sh [xml-path]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
XML="${1:-$ROOT/CipherBank-app/Platforms/Android/Resources/xml/network_security_config.xml}"
EXPIRY="${ANDROID_CERT_PIN_EXPIRY:-2027-12-31}"

die() { echo "ERROR: $*" >&2; exit 1; }

[[ -f "$XML" ]] || die "network_security_config.xml not found: $XML"
[[ -n "${ANDROID_CERT_PINS:-}" ]] || die "ANDROID_CERT_PINS is unset — refuse to invent pins. Leave the skirt (system CAs) or pass current,backup SPKI hashes."

python3 - "$XML" "$EXPIRY" <<'PY'
import base64, os, re, sys

xml_path, expiry = sys.argv[1], sys.argv[2]
raw = os.environ["ANDROID_CERT_PINS"].strip()
parts = [p.strip() for p in re.split(r"[\s,;]+", raw) if p.strip()]
if len(parts) != 2:
    sys.exit("ANDROID_CERT_PINS must be exactly two SHA-256 SPKI hashes (current,backup)")
for pin in parts:
    if "REPLACE_WITH" in pin.upper() or pin.lower().startswith("todo"):
        sys.exit("ANDROID_CERT_PINS contains a placeholder — refuse to stamp fake pins")
    try:
        decoded = base64.b64decode(pin, validate=True)
    except Exception as exc:
        sys.exit(f"ANDROID_CERT_PINS is not Base64: {exc}")
    if len(decoded) != 32:
        sys.exit("each ANDROID_CERT_PINS value must be a SHA-256 digest (32 bytes)")
if not re.fullmatch(r"\d{4}-\d{2}-\d{2}", expiry):
    sys.exit("ANDROID_CERT_PIN_EXPIRY must be YYYY-MM-DD")

pin_set = (
    f'    <pin-set expiration="{expiry}">\n'
    f'      <pin digest="SHA-256">{parts[0]}</pin>\n'
    f'      <pin digest="SHA-256">{parts[1]}</pin>\n'
    f"    </pin-set>\n"
)

text = open(xml_path, encoding="utf-8").read()
text = re.sub(r"\n[ \t]*<pin-set\b[\s\S]*?</pin-set>", "", text)
needle = (
    '    <domain includeSubdomains="true">api.dev.cipherbank.money</domain>\n'
    '    <trust-anchors>\n'
    '      <certificates src="system" />\n'
    "    </trust-anchors>\n"
    "  </domain-config>"
)
if needle not in text:
    sys.exit("could not find the product domain-config block to stamp")
stamped = needle.replace(
    "    </trust-anchors>\n  </domain-config>",
    "    </trust-anchors>\n" + pin_set + "  </domain-config>",
)
open(xml_path, "w", encoding="utf-8").write(text.replace(needle, stamped, 1))
print(f"stamped pin-set (expiry {expiry}) into {xml_path}", file=sys.stderr)
PY
