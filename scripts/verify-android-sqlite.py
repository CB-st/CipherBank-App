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


def native_package():
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


def verify(archive, package):
    """Count ZIP entries, including duplicate names, for every shipped native ABI."""
    if archive.suffix not in (".apk", ".aab"):
        raise ValueError(f"{archive}: expected an APK or AAB")
    prefix = "" if archive.suffix == ".apk" else r"(?!BUNDLE-METADATA/|META-INF/)[^/]+/"
    pattern = re.compile(prefix + r"lib/([^/]+)/([^/]+)")
    with ZipFile(archive) as bundle:
        libraries = [(entry, match) for entry in bundle.infolist()
                     if (match := pattern.fullmatch(entry.filename))]
        abis = sorted({match[1] for _, match in libraries})
        if not abis:
            raise ValueError(f"{archive}: no shipped native ABIs found")
        for abi in abis:
            sqlite = [entry for entry, match in libraries if match[1] == abi and match[2] == "libe_sqlite3.so"]
            if len(sqlite) != 1:
                raise ValueError(f"{archive}: {abi}: expected exactly one libe_sqlite3.so, found {len(sqlite)}")
            expected = package / "runtimes" / ANDROID_RIDS[abi] / "native/libe_sqlite3.so"
            # A count alone also accepts the retired 2.x AAR on the parent revision.
            digest = hashlib.sha256(bundle.read(sqlite[0])).hexdigest()
            if digest != hashlib.sha256(expected.read_bytes()).hexdigest():
                raise ValueError(f"{archive}: {abi}: libe_sqlite3.so differs from {expected}")
            print(f"{archive}: {abi}: exactly one libe_sqlite3.so, SHA-256 {digest}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archives", type=Path, nargs="+", help="Published .apk and/or .aab files")
    args = parser.parse_args()
    try:
        package = native_package()
        for archive in args.archives:
            verify(archive, package)
    except (OSError, ValueError, KeyError, BadZipFile) as error:
        parser.exit(1, f"Android SQLite packaging check failed: {error}\n")


if __name__ == "__main__":
    main()
