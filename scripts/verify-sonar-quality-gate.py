#!/usr/bin/env python3
"""Fail if fetched Sonar gate conditions disagree with config/sonar/quality-gate.yaml."""

from __future__ import annotations

import json
import sys
from pathlib import Path

OPERATORS = {
    "greater_than": "GT",
    "less_than": "LT",
    "equals": "EQ",
    "not_equals": "NE",
}

METRIC_ALIASES = {
    "reliability_rating": ("reliability_rating", "new_reliability_rating"),
    "security_rating": ("security_rating", "new_security_rating"),
    "maintainability_rating": ("maintainability_rating", "new_maintainability_rating"),
    "security_hotspots_reviewed": (
        "security_hotspots_reviewed",
        "new_security_hotspots_reviewed",
    ),
    "coverage": ("coverage", "new_coverage"),
    "duplicated_lines_density": (
        "duplicated_lines_density",
        "new_duplicated_lines_density",
    ),
    "violations": ("violations", "new_violations"),
    "blocker_issues": (
        "blocker_issues",
        "blocker_violations",
        "new_blocker_violations",
    ),
    "critical_issues": (
        "critical_issues",
        "critical_violations",
        "new_critical_violations",
    ),
}


def parse_gate_yaml(path: Path) -> dict[str, dict[str, str]]:
    conditions: dict[str, dict[str, str]] = {}
    current: str | None = None
    for raw in path.read_text(encoding="utf-8").splitlines():
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        if raw.startswith("  ") and not raw.startswith("    ") and raw.rstrip().endswith(":"):
            current = raw.strip()[:-1]
            conditions[current] = {}
            continue
        if raw.startswith("    ") and current is not None and ":" in raw:
            key, value = raw.strip().split(":", 1)
            conditions[current][key.strip()] = value.strip()
    return conditions


def aliases_for(name: str) -> tuple[str, ...]:
    return METRIC_ALIASES.get(name, (name, f"new_{name}"))


def normalize_threshold(value: object) -> str:
    text = str(value).strip()
    try:
        number = float(text)
    except ValueError:
        return text
    if number.is_integer():
        return str(int(number))
    return str(number)


def yaml_match_for(metric_key: str, expected: dict[str, dict[str, str]]) -> tuple[str, dict[str, str]] | None:
    for name, spec in expected.items():
        if metric_key in aliases_for(name):
            return name, spec
    return None


def verify(fetched_path: Path, yaml_path: Path) -> list[str]:
    payload = json.loads(fetched_path.read_text(encoding="utf-8"))
    conditions = payload.get("conditions") or []
    expected = parse_gate_yaml(yaml_path)
    errors: list[str] = []

    if not conditions:
        errors.append(f"{fetched_path} has no quality-gate conditions to compare.")
        return errors

    for condition in conditions:
        metric = condition.get("metricKey") or ""
        match = yaml_match_for(metric, expected)
        if match is None:
            errors.append(
                f"fetched `{metric}` is not declared in {yaml_path.name}; "
                "the live gate and the checked-in policy have split."
            )
            continue
        name, spec = match
        expected_op = OPERATORS.get(spec.get("operator", ""), spec.get("operator", ""))
        actual_op = condition.get("comparator") or ""
        if expected_op and actual_op != expected_op:
            errors.append(
                f"`{metric}` operator is {actual_op}, yaml `{name}` expects {expected_op}."
            )
        expected_threshold = normalize_threshold(spec.get("error_threshold", ""))
        actual_threshold = normalize_threshold(condition.get("errorThreshold", ""))
        if expected_threshold and actual_threshold != expected_threshold:
            errors.append(
                f"`{metric}` threshold is {actual_threshold}, yaml `{name}` expects {expected_threshold}."
            )

    return errors


def main(argv: list[str]) -> int:
    if len(argv) != 3:
        print(
            f"usage: {Path(argv[0]).name} <quality-gate.json> <quality-gate.yaml>",
            file=sys.stderr,
        )
        return 2
    fetched = Path(argv[1])
    yaml_path = Path(argv[2])
    errors = verify(fetched, yaml_path)
    if errors:
        print("Quality-gate policy mismatch:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1
    print(f"Fetched Sonar conditions match {yaml_path}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
