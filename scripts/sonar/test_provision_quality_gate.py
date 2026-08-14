#!/usr/bin/env python3
"""Unit tests for first-run dry-run provisioning."""

from __future__ import annotations

import unittest

from provision_quality_gate import GATE_NAME, gate_exists_on_server, should_fetch_conditions


class DryRunFirstRunTests(unittest.TestCase):
    def test_missing_gate_is_detected(self):
        self.assertFalse(gate_exists_on_server(["Sonar way"], GATE_NAME))

    def test_dry_run_skips_show_when_gate_missing(self):
        self.assertFalse(should_fetch_conditions(dry_run=True, gate_exists=False))

    def test_live_run_still_fetches_when_gate_missing(self):
        self.assertTrue(should_fetch_conditions(dry_run=False, gate_exists=False))

    def test_existing_gate_always_fetches(self):
        self.assertTrue(should_fetch_conditions(dry_run=True, gate_exists=True))


if __name__ == "__main__":
    unittest.main()
