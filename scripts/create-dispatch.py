#!/usr/bin/env python3
"""Create a bounded CipherBank work packet from the repository dispatch map."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from typing import Any


def repository_root() -> pathlib.Path:
    """Return the nearest parent containing the CipherBank solution."""
    current = pathlib.Path(__file__).resolve().parent
    for candidate in [current, *current.parents]:
        if (candidate / "CipherBank-app.sln").is_file():
            return candidate
    raise FileNotFoundError("Could not locate CipherBank-app.sln")


def load_workflow(root: pathlib.Path, workflow_id: str) -> dict[str, Any]:
    """Load one uniquely named workflow from the repository routing map."""
    path = root / "config" / "agentic" / "dispatch.json"
    document = json.loads(path.read_text(encoding="utf-8"))
    matches = [item for item in document["workflows"] if item.get("id") == workflow_id]
    if len(matches) != 1:
        available = ", ".join(sorted(item["id"] for item in document["workflows"]))
        raise ValueError(f"Unknown workflow {workflow_id!r}. Available: {available}")
    return matches[0]


def slugify(value: str) -> str:
    """Convert a feature label to a safe dispatch identifier."""
    slug = re.sub(r"[^a-z0-9]+", "-", value.strip().lower()).strip("-")
    if not slug:
        raise ValueError("Feature must contain at least one letter or number")
    return slug


def build_packet(workflow: dict[str, Any], feature: str, summary: str) -> dict[str, Any]:
    """Build a secret-free packet containing routing and verification facts."""
    return {
        "schemaVersion": 1,
        "dispatchId": f"{workflow['id']}:{slugify(feature)}",
        "feature": feature.strip(),
        "summary": summary.strip(),
        "workflow": workflow["id"],
        "primarySkill": workflow["skill"],
        "signals": workflow.get("signals", []),
        "templates": workflow.get("templates", []),
        "contracts": workflow.get("references", []),
        "followUps": workflow.get("followUps", []),
        "gates": workflow.get("gates", []),
        "decisionsRequired": [
            "Confirm owning layer and out-of-scope behavior.",
            "List existing shared resources before proposing new ones.",
            "Identify interface, implementation, composition, configuration, and test destinations.",
            "Record security, cancellation, offline, and failure invariants that apply.",
        ],
    }


def parse_args() -> argparse.Namespace:
    """Parse the command-line contract."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workflow", required=True, help="Stable workflow ID from config/agentic/dispatch.json")
    parser.add_argument("--feature", required=True, help="Feature or bounded change name")
    parser.add_argument("--summary", required=True, help="One-sentence requested outcome")
    parser.add_argument("--output", type=pathlib.Path, help="Optional JSON output path; stdout is the default")
    parser.add_argument("--force", action="store_true", help="Allow replacement of the explicit output file")
    return parser.parse_args()


def main() -> int:
    """Create and optionally persist one dispatch packet."""
    args = parse_args()
    try:
        root = repository_root()
        workflow = load_workflow(root, args.workflow)
        packet = build_packet(workflow, args.feature, args.summary)
        rendered = json.dumps(packet, indent=2) + "\n"
        if args.output is None:
            sys.stdout.write(rendered)
            return 0

        output = args.output if args.output.is_absolute() else root / args.output
        if output.exists() and not args.force:
            raise FileExistsError(f"Refusing to overwrite {output}; pass --force to replace it")
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(rendered, encoding="utf-8")
        print(output)
        return 0
    except (FileExistsError, FileNotFoundError, KeyError, ValueError, json.JSONDecodeError) as error:
        print(f"dispatch error: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
