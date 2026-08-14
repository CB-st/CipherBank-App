#!/usr/bin/env python3
"""Provision the CipherBank Sonar quality gate directly on the server.

Context (PR #33, review thread on config/sonar/quality-gate.yaml):

    Two hand-maintained rulebooks always drift. This script replaces
    config/sonar/quality-gate.yaml and scripts/verify-sonar-quality-gate.py.
    Sonar is now the only place the gate definition lives; there is nothing
    left in the repo for CI to diff against it. This script is how that
    definition gets onto the server -- run by whoever holds an admin token,
    not by the per-PR scanner job -- and it is the versioned, reviewable
    record of what the gate is supposed to be, in the same sense the old
    YAML was, minus the second copy.

    CI's job (.github/workflows/sonar.yml) is unchanged in spirit: run the
    scanner, poll api/ce/task, fetch api/qualitygates/project_status, and
    let Sonar's own PR check gate the merge. It no longer verifies the
    fetched result against a checked-in copy, because there is no longer a
    copy to check it against.

Usage:

    export SONAR_HOST_URL='https://sonar.cipherbank.money'
    export SONAR_ADMIN_TOKEN='...'          # 'Administer Quality Gates' permission
    export SONAR_PROJECT_KEY='CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c'

    python3 scripts/sonar/provision_quality_gate.py --dry-run
    python3 scripts/sonar/provision_quality_gate.py
    python3 scripts/sonar/provision_quality_gate.py --include-deferred

SONAR_ADMIN_TOKEN is deliberately a different secret from the SONAR_TOKEN
used in sonar.yml. That one only needs analysis permission and is scoped to
every PR run; this one needs 'Administer Quality Gates' and should be held
by SRE/admins and run out-of-band (locally, or from a separate protected
admin pipeline), not wired into the per-PR workflow.

Before running against sonar.cipherbank.money for the first time, confirm
this script's request shapes against that instance's own API reference at
`${SONAR_HOST_URL}/web_api/api/qualitygates` -- the endpoints below are the
long-stable Web API surface, but a specific server version is the source of
truth for its own parameter names.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass

GATE_NAME = "CipherBank New Code Gate"

# Metric keys are Sonar's "new_*" family: every condition here evaluates
# against new code, matching sonar.pullrequest.* / sonar.branch.name scoping
# already set up in sonar.yml's scanner-begin step.

# Live on sonar.cipherbank.money as of PR #33. Kept minimal on purpose --
# see config/sonar/README.md for why coverage isn't in this set yet.
LIVE_CONDITIONS: list[dict[str, str]] = [
    {"metric": "new_duplicated_lines_density", "op": "GT", "error": "3"},
    {"metric": "new_violations", "op": "GT", "error": "0"},
]

# Not yet on the server. Pass --include-deferred once the team is ready to
# turn these on; do that in the same change that updates config/sonar/README.md,
# same discipline the old YAML's comments called for.
DEFERRED_CONDITIONS: list[dict[str, str]] = [
    {"metric": "new_coverage", "op": "LT", "error": "80"},
    {"metric": "new_reliability_rating", "op": "GT", "error": "1"},
    {"metric": "new_security_rating", "op": "GT", "error": "1"},
    {"metric": "new_maintainability_rating", "op": "GT", "error": "1"},
    {"metric": "new_security_hotspots_reviewed", "op": "LT", "error": "100"},
    {"metric": "new_blocker_violations", "op": "GT", "error": "0"},
    {"metric": "new_critical_violations", "op": "GT", "error": "0"},
]


@dataclass
class Condition:
    id: str | None
    metric: str
    op: str
    error: str


def _request(host: str, token: str, path: str, params: dict[str, str], *, dry_run: bool) -> dict:
    """POST/GET a Sonar Web API endpoint with basic auth (token as username)."""
    url = f"{host.rstrip('/')}{path}"
    method = "GET" if path.endswith(("/list", "/show")) else "POST"
    if method == "GET":
        url = f"{url}?{urllib.parse.urlencode(params)}"
        body = None
    else:
        body = urllib.parse.urlencode(params).encode("utf-8")

    if method == "POST" and dry_run:
        print(f"  [dry-run] POST {path} {params}")
        return {}

    request = urllib.request.Request(url, data=body, method=method)
    request.add_header("Authorization", "Basic " + _basic_auth(token))
    try:
        with urllib.request.urlopen(request) as response:
            raw = response.read()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"{method} {path} failed: {exc.code} {exc.reason}\n{detail}") from exc


def _basic_auth(token: str) -> str:
    import base64

    return base64.b64encode(f"{token}:".encode("utf-8")).decode("ascii")


def find_or_create_gate(host: str, token: str, *, dry_run: bool) -> None:
    """Ensure GATE_NAME exists; create it if this is the first run."""
    listing = _request(host, token, "/api/qualitygates/list", {}, dry_run=False)
    names = [gate.get("name") for gate in listing.get("qualitygates", [])]
    if GATE_NAME in names:
        print(f"Gate '{GATE_NAME}' already exists.")
        return
    print(f"Creating gate '{GATE_NAME}'.")
    _request(host, token, "/api/qualitygates/create", {"name": GATE_NAME}, dry_run=dry_run)


def fetch_conditions(host: str, token: str) -> dict[str, Condition]:
    """Return the gate's current conditions keyed by metric."""
    payload = _request(host, token, "/api/qualitygates/show", {"name": GATE_NAME}, dry_run=False)
    conditions = {}
    for raw in payload.get("conditions", []):
        metric = raw["metric"]
        conditions[metric] = Condition(
            id=raw.get("id"),
            metric=metric,
            op=raw.get("op", ""),
            error=str(raw.get("error", "")),
        )
    return conditions


def reconcile(
    host: str,
    token: str,
    declared: list[dict[str, str]],
    fetched: dict[str, Condition],
    *,
    dry_run: bool,
) -> None:
    """Push declared conditions to the server; remove fetched ones that aren't declared.

    This is the same bidirectional check the old verify-sonar-quality-gate.py
    did read-only -- it just acts on the server now instead of failing a CI
    step when the two disagreed.
    """
    declared_metrics = {c["metric"] for c in declared}

    for condition in declared:
        metric, op, error = condition["metric"], condition["op"], condition["error"]
        existing = fetched.get(metric)
        if existing is None:
            print(f"  + create {metric} {op} {error}")
            _request(
                host,
                token,
                "/api/qualitygates/create_condition",
                {"gateName": GATE_NAME, "metric": metric, "op": op, "error": error},
                dry_run=dry_run,
            )
        elif existing.op != op or existing.error != error:
            print(f"  ~ update {metric}: {existing.op} {existing.error} -> {op} {error}")
            _request(
                host,
                token,
                "/api/qualitygates/update_condition",
                {"id": existing.id or "", "metric": metric, "op": op, "error": error},
                dry_run=dry_run,
            )
        else:
            print(f"  = {metric} already matches")

    for metric, existing in fetched.items():
        if metric not in declared_metrics:
            print(f"  - delete {metric} (not declared)")
            _request(
                host,
                token,
                "/api/qualitygates/delete_condition",
                {"id": existing.id or ""},
                dry_run=dry_run,
            )


def assign_to_project(host: str, token: str, project_key: str, *, dry_run: bool) -> None:
    print(f"Assigning '{GATE_NAME}' to project '{project_key}'.")
    _request(
        host,
        token,
        "/api/qualitygates/select",
        {"gateName": GATE_NAME, "projectKey": project_key},
        dry_run=dry_run,
    )


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--include-deferred", action="store_true", help="Also provision the deferred conditions.")
    parser.add_argument("--dry-run", action="store_true", help="Print planned changes without calling the API.")
    args = parser.parse_args(argv[1:])

    host = os.environ.get("SONAR_HOST_URL")
    token = os.environ.get("SONAR_ADMIN_TOKEN")
    project_key = os.environ.get("SONAR_PROJECT_KEY")
    if not host or not token or not project_key:
        print(
            "Set SONAR_HOST_URL, SONAR_ADMIN_TOKEN, and SONAR_PROJECT_KEY before running.",
            file=sys.stderr,
        )
        return 2

    declared = list(LIVE_CONDITIONS)
    if args.include_deferred:
        declared += DEFERRED_CONDITIONS

    find_or_create_gate(host, token, dry_run=args.dry_run)
    fetched = fetch_conditions(host, token)
    reconcile(host, token, declared, fetched, dry_run=args.dry_run)
    assign_to_project(host, token, project_key, dry_run=args.dry_run)

    print("Done." if not args.dry_run else "Dry run complete; no changes were made.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
