#!/usr/bin/env python3
"""Tests for Android SQLite packaging verifier."""

import importlib.util
from pathlib import Path
import tempfile
import unittest
import warnings
from zipfile import ZipFile

MODULE_PATH = Path(__file__).resolve().parent / "verify-android-sqlite.py"
SPEC = importlib.util.spec_from_file_location("verify_android_sqlite", MODULE_PATH)
if SPEC is None or SPEC.loader is None:
    raise ImportError(f"Cannot load module from {MODULE_PATH}")
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


class VerifyAndroidSqliteTests(unittest.TestCase):
    """Test verification of Android SQLite native assets in APK and AAB packages."""

    def setUp(self) -> None:
        """Create temporary test directories and a fake SQLite native package."""
        self.temp_dir = tempfile.TemporaryDirectory()
        self.base_dir = Path(self.temp_dir.name)
        self.fake_package = self.base_dir / "sqlite_pkg"

        # Populate fake native package runtimes
        self.known_bytes: dict[str, bytes] = {
            "android-arm": b"native-sqlite3-binary-arm",
            "android-arm64": b"native-sqlite3-binary-arm64",
            "android-x86": b"native-sqlite3-binary-x86",
            "android-x64": b"native-sqlite3-binary-x64",
        }
        for rid, content in self.known_bytes.items():
            native_dir = self.fake_package / "runtimes" / rid / "native"
            native_dir.mkdir(parents=True, exist_ok=True)
            (native_dir / "libe_sqlite3.so").write_bytes(content)

    def tearDown(self) -> None:
        """Clean up temporary test files."""
        self.temp_dir.cleanup()

    def _create_archive(self, name: str, entries: list[tuple[str, bytes]]) -> Path:
        """Create a synthetic zip archive (APK or AAB) with specified entries."""
        archive_path = self.base_dir / name
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", UserWarning)
            with ZipFile(archive_path, "w") as bundle:
                for entry_name, content in entries:
                    bundle.writestr(entry_name, content)
        return archive_path

    def test_full_expected_set_passes_apk(self) -> None:
        """Full expected ABI set in an APK passes verification."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("lib/x86_64/libe_sqlite3.so", self.known_bytes["android-x64"]),
            ],
        )
        verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})

    def test_full_expected_set_passes_aab(self) -> None:
        """Full expected ABI set in an AAB passes verification."""
        aab = self._create_archive(
            "app.aab",
            [
                ("base/lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("base/lib/x86_64/libe_sqlite3.so", self.known_bytes["android-x64"]),
            ],
        )
        verifier.verify(aab, self.fake_package, {"arm64-v8a", "x86_64"})

    def test_missing_abi_fails_and_names_it(self) -> None:
        """Missing ABI in archive raises ValueError reporting the missing ABI."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
            ],
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})
        self.assertIn("missing", str(ctx.exception))
        self.assertIn("x86_64", str(ctx.exception))

    def test_unexpected_extra_abi_fails(self) -> None:
        """Unexpected extra ABI in archive raises ValueError reporting the extra ABI."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("lib/x86_64/libe_sqlite3.so", self.known_bytes["android-x64"]),
                ("lib/x86/libe_sqlite3.so", self.known_bytes["android-x86"]),
            ],
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})
        self.assertIn("unexpected", str(ctx.exception))
        self.assertIn("x86", str(ctx.exception))

    def test_duplicate_sqlite_fails(self) -> None:
        """Duplicate libe_sqlite3.so for an ABI raises ValueError."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("lib/x86_64/libe_sqlite3.so", self.known_bytes["android-x64"]),
            ],
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})
        self.assertIn("expected exactly one libe_sqlite3.so", str(ctx.exception))

    def test_wrong_bytes_fail(self) -> None:
        """Mismatched SQLite binary digest raises ValueError."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", b"tampered-bytes"),
                ("lib/x86_64/libe_sqlite3.so", self.known_bytes["android-x64"]),
            ],
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})
        self.assertIn("differs from", str(ctx.exception))

    def test_expected_abis_default_for_real_csproj(self) -> None:
        """expected_abis() defaults to {arm64-v8a, x86_64} for the repository csproj."""
        self.assertEqual(verifier.expected_abis(), {"arm64-v8a", "x86_64"})

    def test_expected_abis_cli_parsing(self) -> None:
        """--expected-abis CLI flag and override parse comma-separated ABIs."""
        parsed = verifier.expected_abis(override="arm64-v8a,x86_64")
        self.assertEqual(parsed, {"arm64-v8a", "x86_64"})

        single = verifier.expected_abis(override="armeabi-v7a")
        self.assertEqual(single, {"armeabi-v7a"})

        parser = verifier.build_parser()
        args = parser.parse_args(["--expected-abis", "arm64-v8a,x86_64", "app.apk"])
        self.assertEqual(args.expected_abis, "arm64-v8a,x86_64")
        self.assertEqual(verifier.expected_abis(args.expected_abis), {"arm64-v8a", "x86_64"})

    def test_expected_abis_empty_override_fails(self) -> None:
        """Empty --expected-abis override raises ValueError."""
        with self.assertRaises(ValueError):
            verifier.expected_abis(override="")
        with self.assertRaises(ValueError):
            verifier.expected_abis(override="   ")

    def test_expected_abis_from_custom_csproj(self) -> None:
        """expected_abis() extracts android-* RIDs from project XML elements."""
        custom_proj = self.base_dir / "Custom.csproj"
        custom_proj.write_text(
            "<Project><PropertyGroup>"
            "<RuntimeIdentifiers>android-arm;android-arm64</RuntimeIdentifiers>"
            "</PropertyGroup></Project>",
            encoding="utf-8",
        )
        self.assertEqual(
            verifier.expected_abis(project_path=custom_proj),
            {"armeabi-v7a", "arm64-v8a"},
        )

    def test_expected_abis_unknown_rid_fails(self) -> None:
        """Unknown android-* RID in project XML raises ValueError."""
        custom_proj = self.base_dir / "UnknownRid.csproj"
        custom_proj.write_text(
            "<Project><PropertyGroup>"
            "<RuntimeIdentifier>android-mips64</RuntimeIdentifier>"
            "</PropertyGroup></Project>",
            encoding="utf-8",
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.expected_abis(project_path=custom_proj)
        self.assertIn("Unknown Android RID", str(ctx.exception))

    def test_missing_and_unexpected_abis_reported_separately(self) -> None:
        """Missing and unexpected ABIs are reported separately when both occur."""
        apk = self._create_archive(
            "app-Signed.apk",
            [
                ("lib/arm64-v8a/libe_sqlite3.so", self.known_bytes["android-arm64"]),
                ("lib/armeabi-v7a/libe_sqlite3.so", self.known_bytes["android-arm"]),
            ],
        )
        with self.assertRaises(ValueError) as ctx:
            verifier.verify(apk, self.fake_package, {"arm64-v8a", "x86_64"})
        message = str(ctx.exception)
        self.assertIn("missing: x86_64", message)
        self.assertIn("unexpected: armeabi-v7a", message)


if __name__ == "__main__":
    unittest.main()
