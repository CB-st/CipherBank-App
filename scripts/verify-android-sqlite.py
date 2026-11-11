#!/usr/bin/env python3
"""Check published APK/AAB SQLite counts and bytes against the central native pin."""

import argparse
import hashlib
import os
from pathlib import Path
import re
import xml.etree.ElementTree as ET
from zipfile import BadZipFile, ZipFile, ZipInfo


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


def _override_abis(text: str) -> set[str]:
    """Resolve expected Android ABIs from an override string."""
    abis = {abi.strip() for abi in text.split(",") if abi.strip()}
    if not abis:
        raise ValueError("No ABIs specified in --expected-abis")
    unknown = sorted(abis - ANDROID_RIDS.keys())
    if unknown:
        raise ValueError(f"Unknown Android ABI in --expected-abis: {', '.join(unknown)}")
    return abis


def _elem_rids(elem: ET.Element) -> list[str]:
    """Extract android-* RIDs from a single csproj XML element."""
    if elem.tag not in ("RuntimeIdentifiers", "RuntimeIdentifier"):
        return []
    cond = elem.attrib.get("Condition", "").strip().lower()
    if cond and "android" not in cond:
        return []
    rids: list[str] = []
    for part in (elem.text or "").split(";"):
        part = part.strip()
        if part.startswith("android-"):
            rids.append(part)
    return rids


def _project_rids(project_path: Path) -> list[str]:
    """Scan csproj RuntimeIdentifier(s) elements for android-* RIDs."""
    root = ET.parse(project_path).getroot()
    rids: list[str] = []
    for elem in root.iter():
        rids.extend(_elem_rids(elem))
    if not rids:
        return list(DEFAULT_RIDS)
    return rids


def _rids_to_abis(rids: list[str]) -> set[str]:
    """Map .NET Runtime Identifiers to Android ABIs."""
    abis: set[str] = set()
    for rid in rids:
        if rid not in RID_TO_ABI:
            raise ValueError(f"Unknown Android RID: {rid}")
        abis.add(RID_TO_ABI[rid])
    return abis


def expected_abis(override: str | None = None, project_path: Path | None = None) -> set[str]:
    """Resolve expected Android ABIs from an override or CipherBank-app.csproj."""
    if override is not None:
        return _override_abis(override)

    if project_path is None:
        project_path = Path(__file__).resolve().parents[1] / "CipherBank-app" / "CipherBank-app.csproj"

    rids = _project_rids(project_path)
    return _rids_to_abis(rids)


def _check_abi_set(archive: Path, found: list[str] | set[str], expected: set[str]) -> None:
    """Ensure the archive contains exactly the expected set of ABIs."""
    expected_set = set(expected)
    actual_set = set(found)
    if actual_set != expected_set:
        details = []
        missing = sorted(expected_set - actual_set)
        unexpected = sorted(actual_set - expected_set)
        if missing:
            details.append(f"missing: {', '.join(missing)}")
        if unexpected:
            details.append(f"unexpected: {', '.join(unexpected)}")
        raise ValueError(f"{archive}: ABI mismatch: {'; '.join(details)}")


def _check_sqlite(
    bundle: ZipFile,
    archive: Path,
    abi: str,
    entries: list[tuple[ZipInfo, re.Match[str]]],
    package: Path,
) -> None:
    """Verify that archive has exactly one matching libe_sqlite3.so for abi."""
    sqlite = [entry for entry, match in entries if match[1] == abi and match[2] == "libe_sqlite3.so"]
    if len(sqlite) != 1:
        raise ValueError(f"{archive}: {abi}: expected exactly one libe_sqlite3.so, found {len(sqlite)}")
    expected_file = package / "runtimes" / ANDROID_RIDS[abi] / "native/libe_sqlite3.so"
    # A count alone also accepts the retired 2.x AAR on the parent revision.
    digest = hashlib.sha256(bundle.read(sqlite[0])).hexdigest()
    if digest != hashlib.sha256(expected_file.read_bytes()).hexdigest():
        raise ValueError(f"{archive}: {abi}: libe_sqlite3.so differs from {expected_file}")
    print(f"{archive}: {abi}: exactly one libe_sqlite3.so, SHA-256 {digest}")


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
        _check_abi_set(archive, abis, expected)
        for abi in abis:
            _check_sqlite(bundle, archive, abi, libraries, package)


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
