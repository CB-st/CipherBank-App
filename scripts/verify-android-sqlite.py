#!/usr/bin/env python3
"""Check published APK/AAB SQLite counts and bytes against the central native pin."""

import argparse
import hashlib
import os
from pathlib import Path
import re
import xml.etree.ElementTree as ET
from zipfile import BadZipFile, ZipFile


ANDROID_RIDS = {
    "armeabi-v7a": "android-arm",
    "arm64-v8a": "android-arm64",
    "x86": "android-x86",
    "x86_64": "android-x64",
}

RID_TO_ABI = {rid: abi for abi, rid in ANDROID_RIDS.items()}
DEFAULT_RIDS = ("android-arm64", "android-x64")


def native_package() -> Path:
    """Use the same SQLite version and NuGet cache as the app restore."""
    props = Path(__file__).resolve().parents[1] / "Directory.Packages.props"
    pin = ET.parse(props).find(".//PackageVersion[@Include='SQLite']")
    if pin is None:
        raise ValueError("Directory.Packages.props must pin the SQLite native package")
    packages = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
    package = packages / "sqlite" / pin.attrib["Version"]
    if not package.is_dir():
        raise ValueError(f"SQLite native package not found: {package}")
    return package


def expected_abis(override: str | None = None, project_path: Path | None = None) -> set[str]:
    """Resolve expected Android ABIs from an override or CipherBank-app.csproj."""
    if override is not None:
        abis = {abi.strip() for abi in override.split(",") if abi.strip()}
        if not abis:
            raise ValueError("No ABIs specified in --expected-abis")
        unknown = sorted(abis - ANDROID_RIDS.keys())
        if unknown:
            raise ValueError(f"Unknown Android ABI in --expected-abis: {', '.join(unknown)}")
        return abis

    if project_path is None:
        project_path = Path(__file__).resolve().parents[1] / "CipherBank-app" / "CipherBank-app.csproj"

    root = ET.parse(project_path).getroot()
    rids: list[str] = []
    for elem in root.iter():
        if elem.tag in ("RuntimeIdentifiers", "RuntimeIdentifier"):
            cond = elem.attrib.get("Condition", "").strip().lower()
            if not cond or "android" in cond:
                for part in (elem.text or "").split(";"):
                    part = part.strip()
                    if part.startswith("android-"):
                        rids.append(part)

    if not rids:
        rids = list(DEFAULT_RIDS)

    abis: set[str] = set()
    for rid in rids:
        if rid not in RID_TO_ABI:
            raise ValueError(f"Unknown Android RID: {rid}")
        abis.add(RID_TO_ABI[rid])
    return abis


def verify(archive: Path, package: Path, expected: set[str]) -> None:
    """Count ZIP entries, including duplicate names, for every shipped native ABI."""
    if archive.suffix not in (".apk", ".aab"):
        raise ValueError(f"{archive}: expected an APK or AAB")
    prefix = "" if archive.suffix == ".apk" else r"(?!BUNDLE-METADATA/|META-INF/)[^/]+/"
    pattern = re.compile(prefix + r"lib/([^/]+)/([^/]+)")
    with ZipFile(archive) as bundle:
        libraries = [(entry, match) for entry in bundle.infolist()
                     if (match := pattern.fullmatch(entry.filename))]
        abis = sorted({match[1] for _, match in libraries})
        expected_set = set(expected)
        actual_set = set(abis)
        if actual_set != expected_set:
            details = []
            missing = sorted(expected_set - actual_set)
            unexpected = sorted(actual_set - expected_set)
            if missing:
                details.append(f"missing: {', '.join(missing)}")
            if unexpected:
                details.append(f"unexpected: {', '.join(unexpected)}")
            raise ValueError(f"{archive}: ABI mismatch: {'; '.join(details)}")
        if not abis:
            raise ValueError(f"{archive}: no shipped native ABIs found")
        for abi in abis:
            sqlite = [entry for entry, match in libraries if match[1] == abi and match[2] == "libe_sqlite3.so"]
            if len(sqlite) != 1:
                raise ValueError(f"{archive}: {abi}: expected exactly one libe_sqlite3.so, found {len(sqlite)}")
            expected_file = package / "runtimes" / ANDROID_RIDS[abi] / "native/libe_sqlite3.so"
            # A count alone also accepts the retired 2.x AAR on the parent revision.
            digest = hashlib.sha256(bundle.read(sqlite[0])).hexdigest()
            if digest != hashlib.sha256(expected_file.read_bytes()).hexdigest():
                raise ValueError(f"{archive}: {abi}: libe_sqlite3.so differs from {expected_file}")
            print(f"{archive}: {abi}: exactly one libe_sqlite3.so, SHA-256 {digest}")


def build_parser() -> argparse.ArgumentParser:
    """Build the command-line argument parser."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--expected-abis",
        help="Comma-separated list of expected Android ABIs (overrides project discovery)",
    )
    parser.add_argument("archives", type=Path, nargs="+", help="Published .apk and/or .aab files")
    return parser


def main() -> None:
    """Verify each published Android archive given on the command line."""
    parser = build_parser()
    args = parser.parse_args()
    try:
        package = native_package()
        expected = expected_abis(args.expected_abis)
        for archive in args.archives:
            verify(archive, package, expected)
    except (OSError, ValueError, KeyError, BadZipFile) as error:
        parser.exit(1, f"Android SQLite packaging check failed: {error}\n")


if __name__ == "__main__":
    main()
