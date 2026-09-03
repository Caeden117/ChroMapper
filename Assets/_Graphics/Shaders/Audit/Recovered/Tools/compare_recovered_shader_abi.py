#!/usr/bin/env python3
"""Compare recovered-shader ABI fields and permit explicit path relocation."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


INCIDENTAL_KEYS = {
    "abi_sha256",
    "arguments_raw",
    "bytes",
    "depth",
    "evidence",
    "include_resolution",
    "line",
    "line_count",
    "line_end",
    "line_start",
    "newline_style",
    "normalized",
    "order",
    "physical_lines",
    "raw",
    "raw_line",
    "sha256",
    "warnings",
}


def escape_json_pointer(key: str) -> str:
    """Return one escaped JSON Pointer component."""
    return key.replace("~", "~0").replace("/", "~1")


def parse_relocation(value: str) -> tuple[str, str]:
    """Parse one repository-relative OLD=NEW prefix mapping."""
    try:
        old, new = value.split("=", 1)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("a relocation must use OLD=NEW") from exc
    old = old.strip("/")
    new = new.strip("/")
    if not old or not new or Path(old).is_absolute() or Path(new).is_absolute():
        raise argparse.ArgumentTypeError(
            "relocation prefixes must be repository-relative"
        )
    return old, new


def canonical_path(value: str, relocations: list[tuple[str, str]]) -> str:
    """Map either side of an expected relocation to one stable token."""
    value = value.replace("\\", "/")
    for index, (old, new) in enumerate(relocations):
        for prefix in (old, new):
            if value == prefix:
                return f"@relocation/{index}"
            if value.startswith(prefix + "/"):
                return f"@relocation/{index}/" + value[len(prefix) + 1 :]
    return value


def clean_abi(value: Any) -> Any:
    """Remove source-position and formatting fields from parsed ABI data."""
    if isinstance(value, dict):
        return {
            key: clean_abi(item)
            for key, item in value.items()
            if key not in INCIDENTAL_KEYS
        }
    if isinstance(value, list):
        return [clean_abi(item) for item in value]
    return value


def include_contract(
    program: dict[str, Any], relocations: list[tuple[str, str]]
) -> list[dict[str, Any]]:
    """Return ordered direct-include identities without source-location data."""
    return [
        {
            "delimiter": item.get("delimiter"),
            "resolved_path": canonical_path(
                item.get("resolved_path") or item.get("target", ""), relocations
            ),
            "resolved_scope": item.get("resolved_scope"),
        }
        for item in program.get("includes", [])
    ]


def shader_contract(
    snapshot: dict[str, Any], relocations: list[tuple[str, str]]
) -> dict[str, Any]:
    """Project one parser snapshot to its listed ABI contract."""
    abi = clean_abi(snapshot["abi"])
    for source, cleaned in zip(
        snapshot["abi"].get("programs", []), abi.get("programs", [])
    ):
        cleaned["includes"] = include_contract(source, relocations)
        for source_struct, cleaned_struct in zip(
            source.get("structs", []), cleaned.get("structs", [])
        ):
            cleaned_struct["body_records"] = [
                clean_abi(record)
                if record.get("kind") == "field"
                else {
                    "kind": record.get("kind"),
                    "text": " ".join(record.get("raw", "").split()),
                }
                for record in source_struct.get("body_records", [])
            ]
    return {
        "source_path": canonical_path(snapshot["source"]["path"], relocations),
        "abi": abi,
    }


def document_contract(
    document: dict[str, Any], relocations: list[tuple[str, str]]
) -> dict[str, Any]:
    """Project an aggregate or per-shader document to ABI fields."""
    if "shaders" in document:
        return {
            "schema_version": document.get("schema_version"),
            "shaders": [
                shader_contract(item, relocations) for item in document["shaders"]
            ],
        }
    return {
        "schema_version": document.get("schema_version"),
        "shader": shader_contract(document, relocations),
    }


def differences(old: Any, new: Any, path: str = ""):
    """Yield exact JSON Pointer differences between two contract values."""
    if type(old) is not type(new):
        yield path or "/", "type", old, new
        return
    if isinstance(old, dict):
        for key in sorted(old.keys() - new.keys()):
            yield f"{path}/{escape_json_pointer(str(key))}", "removed", old[key], None
        for key in sorted(new.keys() - old.keys()):
            yield f"{path}/{escape_json_pointer(str(key))}", "added", None, new[key]
        for key in sorted(old.keys() & new.keys()):
            yield from differences(
                old[key], new[key], f"{path}/{escape_json_pointer(str(key))}"
            )
    elif isinstance(old, list):
        common = min(len(old), len(new))
        for index in range(common):
            yield from differences(old[index], new[index], f"{path}/{index}")
        for index in range(common, len(old)):
            yield f"{path}/{index}", "removed", old[index], None
        for index in range(common, len(new)):
            yield f"{path}/{index}", "added", None, new[index]
    elif old != new:
        yield path or "/", "changed", old, new


def compact(value: Any) -> str:
    """Format one value for deterministic diagnostic output."""
    text = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return text if len(text) <= 240 else text[:237] + "..."


def main() -> int:
    """Read two snapshots and return nonzero for listed ABI changes."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument(
        "--relocation",
        action="append",
        default=[],
        type=parse_relocation,
        metavar="OLD=NEW",
        help="permit one expected repository-relative source/include prefix move",
    )
    args = parser.parse_args()
    try:
        before = json.loads(args.before.read_text(encoding="utf-8"))
        after = json.loads(args.after.read_text(encoding="utf-8"))
        old_contract = document_contract(before, args.relocation)
        new_contract = document_contract(after, args.relocation)
    except (KeyError, OSError, TypeError, json.JSONDecodeError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    changes = list(differences(old_contract, new_contract))
    if not changes:
        print("Listed ABI fields are identical.")
        return 0

    print(f"Listed ABI differences: {len(changes)}")
    for path, kind, old, new in changes:
        if kind == "added":
            print(f"ADDED {path}: {compact(new)}")
        elif kind == "removed":
            print(f"REMOVED {path}: {compact(old)}")
        else:
            print(f"{kind.upper()} {path}: {compact(old)} -> {compact(new)}")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
