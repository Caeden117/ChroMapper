#!/usr/bin/env python3
"""Create deterministic ShaderLab ABI snapshots for ChroMapper recovered shaders."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
import re
import shlex
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable

SCHEMA_VERSION = "1.0.0"
SHADER_ROOT = Path("Assets/_Graphics/Shaders")

RECOVERED_REPLACEMENTS = {
    "Assets/_Graphics/Shaders/BloomFog/BloomfogMesh.shader": "Recovered Beat Saber bloom-fog mesh behavior; shader audit comments.",
    "Assets/_Graphics/Shaders/BloomFog/BloomfogSkybox.shader": "Recovered replacement for Custom/BloomSkyboxQuad; shader audit comments.",
    "Assets/_Graphics/Shaders/BloomFog/SkyGradient.shader": "Recovered Beat Saber sky-gradient formulas; shader audit comments.",
    "Assets/_Graphics/Shaders/CloudsLitTransparent.shader": "README mapping for Custom/CloudsLitTransparent and recovered DXBC audit.",
    "Assets/_Graphics/Shaders/CloudsOpaque.shader": "README mapping for Custom/CloudsOpaque and recovered DXBC audit.",
    "Assets/_Graphics/Shaders/Glowing.shader": "README geometry fallback and recovered shader matrix audit.",
    "Assets/_Graphics/Shaders/Lightning.shader": "README replacement mapping for Custom/SimpleLightning; LIGHTNING_REAUDIT.md.",
    "Assets/_Graphics/Shaders/Lit.shader": "README replacement mapping for Custom/SimpleLit; LIT_REAUDIT.md.",
    "Assets/_Graphics/Shaders/Mirror.shader": "README replacement mapping for Custom/Mirror; shader audit comments.",
    "Assets/_Graphics/Shaders/Object/Arc.shader": "README mapping for Custom/SliderNoteCrossedStrips; recovered crossed-strip audit.",
    "Assets/_Graphics/Shaders/Object/Note.shader": "README mapping for Custom/Note and recovered NoteHD/NoteLW contract.",
    "Assets/_Graphics/Shaders/Object/ObstacleDistortion.shader": "README mapping for Custom/ScreenDisplacementHD and recovered ObstacleCoreHD formulas.",
    "Assets/_Graphics/Shaders/ParametricBoxFakeGlow.shader": "README replacement mapping for Custom/ParametricBoxFakeGlow; parametric audit.",
    "Assets/_Graphics/Shaders/ParametricBoxOpaque.shader": "README replacement mapping for Custom/OpaqueNeonLight; parametric audit.",
    "Assets/_Graphics/Shaders/ParametricBoxTransparent.shader": "README replacement mapping for Custom/TransparentNeonLight; parametric audit.",
    "Assets/_Graphics/Shaders/ParametricSliceBillboard.shader": "README replacement mapping for Custom/Parametric3SliceSprite; parametric audit.",
    "Assets/_Graphics/Shaders/Particles.shader": "README replacement mapping for Custom/CustomParticles; PARTICLE_REAUDIT.md.",
    "Assets/_Graphics/Shaders/Post Process/Bloom.shader": "Recovered Beat Saber bloom pass contract and repository bloom findings/history.",
    "Assets/_Graphics/Shaders/Post Process/PostBloom.shader": "Replacement for Beat Saber Hidden/MainEffect; recovered main-effect audit.",
    "Assets/_Graphics/Shaders/Rain.shader": "README replacement mapping for Custom/Rain; recovered formula audit.",
    "Assets/_Graphics/Shaders/SetDepthOnly.shader": "README replacement mapping for Custom/SetDepthOnly; recovered shader audit.",
    "Assets/_Graphics/Shaders/Spectrogram.shader": "README replacement mapping for Custom/Spectrogram; recovered audit.",
    "Assets/_Graphics/Shaders/SpectrogramUnlit.shader": "README replacement mapping for Custom/UnlitSpectrogram; recovered audit.",
    "Assets/_Graphics/Shaders/Stencil.shader": "README replacement mapping for Custom/SimpleStencil; recovered audit.",
    "Assets/_Graphics/Shaders/WaterLit.shader": "README replacement mapping for Custom/WaterLit; WATER_SPECTROGRAM_REAUDIT.md.",
}
RECOVERED_PARTIAL = {
    "Assets/_Graphics/Shaders/Object/ObstacleOutline.shader": "README maps Custom/ParametricBoxFrameHD; file audit identifies an editor adapter, not full ObstacleCore replacement.",
}
RECOVERED_SUPPORT_INCLUDES = {
    f"Assets/_Graphics/Shaders/ShaderLibrary/{name}"
    for name in (
        "Bloom.hlsl",
        "BloomShared.hlsl",
        "Camera.hlsl",
        "Cutout.hlsl",
        "Data.hlsl",
        "Fog.hlsl",
        "Lighting.hlsl",
        "ObjectShared.hlsl",
        "ParametricShared.hlsl",
        "PostProcess.hlsl",
        "Reflection.hlsl",
        "SpectrogramShared.hlsl",
        "Time.hlsl",
        "Tonemapping.hlsl",
    )
}

STATE_NAMES = {
    "AlphaToMask",
    "Blend",
    "BlendOp",
    "ColorMask",
    "Conservative",
    "Cull",
    "Fog",
    "Offset",
    "ZClip",
    "ZTest",
    "ZWrite",
}
STENCIL_NAMES = {
    "Ref",
    "ReadMask",
    "WriteMask",
    "Comp",
    "Pass",
    "Fail",
    "ZFail",
    "CompFront",
    "PassFront",
    "FailFront",
    "ZFailFront",
    "CompBack",
    "PassBack",
    "FailBack",
    "ZFailBack",
}
ENTRY_DIRECTIVES = {"vertex", "fragment", "geometry", "hull", "domain"}
BUILTIN_EXACT = {
    "_WorldSpaceCameraPos",
    "_ProjectionParams",
    "_ScreenParams",
    "_ZBufferParams",
    "_Time",
    "_SinTime",
    "_CosTime",
    "_CameraDepthTexture",
    "_CameraOpaqueTexture",
    "_LightColor0",
    "_WorldSpaceLightPos0",
    "glstate_lightmodel_ambient",
}
TYPE_PATTERN = r"(?:const\s+)?(?:uniform\s+)?(?:row_major\s+|column_major\s+)?(?:bool|int|uint|dword|half|fixed|float|double)(?:[1-4](?:x[1-4])?)?"
RESOURCE_PATTERN = r"(?:sampler(?:1D|2D|3D|CUBE|RECT)?|SamplerState|SamplerComparisonState|Texture(?:1D|2D|3D|Cube)(?:Array)?(?:<[^;>]+>)?|RWTexture(?:1D|2D|3D)(?:Array)?(?:<[^;>]+>)?|StructuredBuffer<[^;>]+>|RWStructuredBuffer<[^;>]+>|ByteAddressBuffer|RWByteAddressBuffer)"
DECL_RE = re.compile(
    rf"^\s*(?P<mod>(?:(?:static|const|uniform|volatile|groupshared|extern|precise)\s+)*)"
    rf"(?P<type>{TYPE_PATTERN}|{RESOURCE_PATTERN})\s+"
    r"(?P<name>[A-Za-z_]\w*)(?P<array>(?:\s*\[[^\]]*\])*)\s*(?::\s*(?P<semantic>[A-Za-z_]\w*(?:\s*\([^)]*\))?))?\s*(?:=[^;]*)?;\s*$"
)
FIELD_RE = re.compile(
    rf"^\s*(?P<mod>(?:(?:nointerpolation|linear|centroid|noperspective|sample|precise|const|static)\s+)*)"
    rf"(?P<type>{TYPE_PATTERN}|[A-Za-z_]\w*(?:\s*<[^>]+>)?)\s+"
    r"(?P<name>[A-Za-z_]\w*)(?P<array>(?:\s*\[[^\]]*\])*)\s*(?::\s*(?P<semantic>[A-Za-z_]\w*(?:\s*\([^)]*\))?))?\s*;\s*$"
)


def posix(path: Path) -> str:
    return path.as_posix()


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def source_record(path: Path, repo: Path) -> dict[str, Any]:
    data = path.read_bytes()
    text = data.decode("utf-8", errors="surrogateescape")
    return {
        "path": posix(path.relative_to(repo)),
        "sha256": sha256_bytes(data),
        "bytes": len(data),
        "line_count": len(text.splitlines()),
        "newline_style": "crlf" if b"\r\n" in data else "lf",
        "ends_with_newline": data.endswith((b"\n", b"\r")),
    }


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def mask_comments_and_strings(text: str) -> str:
    chars = list(text)
    i = 0
    state = "code"
    quote = ""
    while i < len(chars):
        c = text[i]
        n = text[i + 1] if i + 1 < len(chars) else ""
        if state == "code":
            if c == "/" and n == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "line_comment"
                continue
            if c == "/" and n == "*":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "block_comment"
                continue
            if c in ('"', "'"):
                quote = c
                chars[i] = " "
                i += 1
                state = "string"
                continue
        elif state == "line_comment":
            if c == "\n":
                state = "code"
            else:
                chars[i] = " "
        elif state == "block_comment":
            if c == "*" and n == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "code"
                continue
            if c != "\n":
                chars[i] = " "
        elif state == "string":
            if c == "\\" and i + 1 < len(chars):
                chars[i] = " "
                if chars[i + 1] != "\n":
                    chars[i + 1] = " "
                i += 2
                continue
            if c == quote:
                chars[i] = " "
                state = "code"
            elif c != "\n":
                chars[i] = " "
        i += 1
    return "".join(chars)


def matching_brace(masked: str, opening: int) -> int | None:
    depth = 0
    for i in range(opening, len(masked)):
        if masked[i] == "{":
            depth += 1
        elif masked[i] == "}":
            depth -= 1
            if depth == 0:
                return i
    return None


def find_blocks(text: str, masked: str, keyword: str) -> list[dict[str, int]]:
    out: list[dict[str, int]] = []
    for m in re.finditer(rf"\b{re.escape(keyword)}\b", masked, re.I):
        brace = masked.find("{", m.end())
        if brace < 0 or masked[m.end() : brace].strip():
            continue
        end = matching_brace(masked, brace)
        if end is not None:
            out.append({"keyword": m.start(), "open": brace, "close": end})
    return out


def direct_child(
    child: dict[str, int],
    parent: dict[str, int],
    possible_parents: Iterable[dict[str, int]],
) -> bool:
    if not (parent["open"] < child["keyword"] < parent["close"]):
        return False
    return not any(
        p is not parent
        and parent["open"] < p["open"] < child["keyword"] < p["close"] < parent["close"]
        for p in possible_parents
    )


def split_top_level(value: str, delimiter: str = ",") -> list[str]:
    parts: list[str] = []
    start = 0
    round_depth = square_depth = curly_depth = 0
    quote: str | None = None
    escaped = False
    for i, c in enumerate(value):
        if quote:
            if escaped:
                escaped = False
            elif c == "\\":
                escaped = True
            elif c == quote:
                quote = None
            continue
        if c in ('"', "'"):
            quote = c
        elif c == "(":
            round_depth += 1
        elif c == ")":
            round_depth -= 1
        elif c == "[":
            square_depth += 1
        elif c == "]":
            square_depth -= 1
        elif c == "{":
            curly_depth += 1
        elif c == "}":
            curly_depth -= 1
        elif c == delimiter and round_depth == square_depth == curly_depth == 0:
            parts.append(value[start:i])
            start = i + 1
    parts.append(value[start:])
    return parts


def strip_line_comment(line: str) -> str:
    quote: str | None = None
    escaped = False
    i = 0
    while i < len(line) - 1:
        c = line[i]
        if quote:
            if escaped:
                escaped = False
            elif c == "\\":
                escaped = True
            elif c == quote:
                quote = None
        elif c in ('"', "'"):
            quote = c
        elif c == "/" and line[i + 1] == "/":
            return line[:i]
        i += 1
    return line


def parse_property(raw: str, line: int, order: int) -> dict[str, Any] | None:
    syntax = strip_line_comment(raw).strip()
    if not syntax or syntax.startswith("#"):
        return None
    attrs: list[str] = []
    while syntax.startswith("["):
        end = syntax.find("]")
        if end < 0:
            break
        attrs.append(syntax[1:end])
        syntax = syntax[end + 1 :].lstrip()
    m = re.match(
        r"(?P<name>[A-Za-z_]\w*)\s*\((?P<inside>.*)\)\s*=\s*(?P<default>.*)$", syntax
    )
    if not m:
        return None
    inside = split_top_level(m.group("inside"))
    display = inside[0].strip() if inside else ""
    if len(display) >= 2 and display[0] == '"' and display[-1] == '"':
        display_value = display[1:-1]
    else:
        display_value = display
    return {
        "order": order,
        "line": line,
        "raw": raw.strip(),
        "attributes": attrs,
        "name": m.group("name"),
        "display_name_raw": display,
        "display_name": display_value,
        "type": ",".join(inside[1:]).strip(),
        "default": m.group("default").strip(),
    }


def parse_properties(
    text: str, masked: str, warnings: list[str]
) -> list[dict[str, Any]]:
    blocks = find_blocks(text, masked, "Properties")
    if not blocks:
        warnings.append("No Properties block found.")
        return []
    if len(blocks) > 1:
        warnings.append(f"Multiple Properties blocks found: {len(blocks)}.")
    block = blocks[0]
    body = text[block["open"] + 1 : block["close"]]
    base_line = line_number(text, block["open"] + 1)
    entries: list[dict[str, Any]] = []
    pending = ""
    pending_line = 0
    paren = square = 0
    quote: str | None = None
    for idx, raw_line in enumerate(body.splitlines(), base_line):
        clean = strip_line_comment(raw_line).strip()
        if not clean or clean.startswith("//"):
            continue
        if not pending:
            pending_line = idx
        pending = f"{pending}\n{raw_line.strip()}" if pending else raw_line.strip()
        for c in clean:
            if quote:
                if c == quote:
                    quote = None
            elif c in ('"', "'"):
                quote = c
            elif c == "(":
                paren += 1
            elif c == ")":
                paren -= 1
            elif c == "[":
                square += 1
            elif c == "]":
                square -= 1
        if paren <= 0 and square <= 0 and "=" in pending:
            item = parse_property(pending, pending_line, len(entries))
            if item is None:
                warnings.append(
                    f"Unparsed property declaration at line {pending_line}: {pending}"
                )
            else:
                entries.append(item)
            pending = ""
            paren = square = 0
    if pending:
        warnings.append(
            f"Unterminated property declaration at line {pending_line}: {pending}"
        )
    return entries


def block_tags(
    text: str, masked: str, owner: dict[str, int], nested_owners: list[dict[str, int]]
) -> list[dict[str, Any]]:
    tags = []
    for b in find_blocks(text, masked, "Tags"):
        if owner["open"] < b["keyword"] < owner["close"] and not any(
            n["open"] < b["keyword"] < n["close"] for n in nested_owners
        ):
            raw = text[b["open"] + 1 : b["close"]]
            pairs = []
            for order, m in enumerate(
                re.finditer(r'"((?:\\.|[^"\\])*)"\s*=\s*"((?:\\.|[^"\\])*)"', raw)
            ):
                pairs.append(
                    {
                        "order": order,
                        "key": m.group(1),
                        "value": m.group(2),
                        "raw": m.group(0),
                    }
                )
            tags.append(
                {
                    "order": len(tags),
                    "line": line_number(text, b["keyword"]),
                    "raw": text[b["keyword"] : b["close"] + 1].strip(),
                    "pairs": pairs,
                }
            )
    return tags


def line_depths(masked: str, start: int, end: int) -> dict[int, int]:
    depth = 0
    line = line_number(masked, start)
    result = {line: depth}
    for c in masked[start:end]:
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
        elif c == "\n":
            line += 1
            result[line] = depth
    return result


def parse_states(
    text: str, masked: str, owner: dict[str, int], nested: list[dict[str, int]]
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    states: list[dict[str, Any]] = []
    stencils: list[dict[str, Any]] = []
    start, end = owner["open"] + 1, owner["close"]
    depths = line_depths(masked, start, end)
    first_line = line_number(text, start)
    body_lines = text[start:end].splitlines()
    nested_ranges = [(n["keyword"], n["close"]) for n in nested]
    offset = start
    for i, raw in enumerate(body_lines, first_line):
        line_start = offset
        offset += len(raw) + 1
        if any(a <= line_start <= b for a, b in nested_ranges):
            continue
        syntax = strip_line_comment(raw).strip()
        if depths.get(i, 0) == 0:
            state_rx = re.compile(
                r"\b(" + "|".join(sorted(STATE_NAMES, key=len, reverse=True)) + r")\b"
            )
            matches = list(state_rx.finditer(syntax))
            for state_index, match in enumerate(matches):
                segment_end = (
                    matches[state_index + 1].start()
                    if state_index + 1 < len(matches)
                    else len(syntax)
                )
                segment = syntax[match.start() : segment_end].strip()
                states.append(
                    {
                        "order": len(states),
                        "line": i,
                        "command": match.group(1),
                        "arguments": syntax[match.end() : segment_end].strip(),
                        "raw": segment,
                    }
                )
    for sb in find_blocks(text, masked, "Stencil"):
        if owner["open"] < sb["keyword"] < owner["close"] and not any(
            n["open"] < sb["keyword"] < n["close"] for n in nested
        ):
            commands = []
            for j, raw in enumerate(
                text[sb["open"] + 1 : sb["close"]].splitlines(),
                line_number(text, sb["open"] + 1),
            ):
                syntax = strip_line_comment(raw).strip()
                m = re.match(r"([A-Za-z]+)\b(.*)$", syntax)
                if m and m.group(1) in STENCIL_NAMES:
                    commands.append(
                        {
                            "order": len(commands),
                            "line": j,
                            "command": m.group(1),
                            "arguments": m.group(2).strip(),
                            "raw": syntax,
                        }
                    )
            stencils.append(
                {
                    "order": len(stencils),
                    "line": line_number(text, sb["keyword"]),
                    "commands": commands,
                    "raw": text[sb["keyword"] : sb["close"] + 1].strip(),
                }
            )
    return states, stencils


def logical_preprocessor_lines(text: str, base_line: int) -> list[dict[str, Any]]:
    """Return active directives while retaining exact physical source lines."""
    lines = text.splitlines()
    masked_lines = mask_comments_and_strings(text).splitlines()
    out = []
    i = 0
    while i < len(lines):
        masked_line = masked_lines[i] if i < len(masked_lines) else ""
        hash_offset = masked_line.find("#")
        if hash_offset < 0 or masked_line[:hash_offset].strip():
            i += 1
            continue
        raw = lines[i][hash_offset:]
        physical = [raw]
        start = i
        while physical[-1].rstrip().endswith("\\") and i + 1 < len(lines):
            i += 1
            physical.append(lines[i])
        logical = "\n".join(physical)
        normalized = re.sub(r"\\\s*\n\s*", " ", logical).strip()
        out.append(
            {
                "line": base_line + start,
                "physical_lines": physical,
                "raw": logical,
                "normalized": normalized,
            }
        )
        i += 1
    return out


def pragma_details(pp: dict[str, Any], order: int) -> dict[str, Any] | None:
    m = re.match(r"#\s*pragma\s+(\S+)(?:\s+(.*))?$", pp["normalized"])
    if not m:
        return None
    directive, args_raw = m.group(1), (m.group(2) or "")
    try:
        args = shlex.split(args_raw, posix=True)
    except ValueError:
        args = args_raw.split()
    if directive in ENTRY_DIRECTIVES:
        classification = "entry_point"
    elif directive == "target":
        classification = "target_profile"
    elif directive.startswith("multi_compile_instancing"):
        classification = "instancing_variants"
    elif directive.startswith("multi_compile_fog"):
        classification = "fog_variants"
    elif directive.startswith("multi_compile"):
        classification = "multi_compile"
    elif directive.startswith("shader_feature"):
        classification = "shader_feature"
    else:
        classification = "compiler_control"
    locality = "local" if "_local" in directive else "global"
    stage = (
        "vertex"
        if directive.endswith("_vertex")
        else "fragment"
        if directive.endswith("_fragment")
        else "all"
    )
    keywords = args if classification in {"multi_compile", "shader_feature"} else []
    return {
        "order": order,
        "line": pp["line"],
        "raw": pp["raw"],
        "normalized": pp["normalized"],
        "directive": directive,
        "arguments_raw": args_raw,
        "arguments": args,
        "classification": classification,
        "locality": locality,
        "stage_scope": stage,
        "keywords": keywords,
    }


def project_unity_version(repo: Path) -> str | None:
    version_file = repo / "ProjectSettings/ProjectVersion.txt"
    if not version_file.is_file():
        return None
    match = re.search(
        r"^m_EditorVersion:\s*(\S+)",
        version_file.read_text(encoding="utf-8", errors="replace"),
        re.M,
    )
    return match.group(1) if match else None


def unity_builtin_include_root(repo: Path) -> Path | None:
    configured = os.environ.get("UNITY_BUILTIN_INCLUDE_ROOT")
    if not configured:
        return None
    candidate = Path(configured).expanduser().resolve()
    return candidate if candidate.is_dir() else None


def display_resolved_path(
    path: Path, repo: Path, unity_root: Path | None
) -> tuple[str, str]:
    try:
        return posix(path.resolve().relative_to(repo.resolve())), "repository"
    except ValueError:
        pass
    if unity_root:
        try:
            return "@unity_builtin/" + posix(
                path.resolve().relative_to(unity_root.resolve())
            ), "unity_builtin"
        except ValueError:
            pass
    return posix(path.resolve()), "external"


def resolve_include(
    target: str, including: Path, repo: Path, shader_root: Path
) -> Path | None:
    candidates = []
    p = Path(target)
    if target.startswith("Assets/") or target.startswith("Packages/"):
        candidates.append(repo / p)
    candidates += [including.parent / p, shader_root / p, repo / p]
    unity_root = unity_builtin_include_root(repo)
    if unity_root:
        candidates.append(unity_root / p)
    for candidate in candidates:
        try:
            resolved = candidate.resolve()
        except OSError:
            continue
        if resolved.is_file():
            return resolved
    return None


def parse_include_directives(
    text: str, base_line: int, path: Path, repo: Path, shader_root: Path
) -> list[dict[str, Any]]:
    out = []
    raw_lines = text.splitlines()
    masked_lines = mask_comments_and_strings(text).splitlines()
    for offset, raw_line in enumerate(raw_lines):
        masked_line = masked_lines[offset] if offset < len(masked_lines) else ""
        hash_offset = masked_line.find("#")
        if hash_offset < 0 or masked_line[:hash_offset].strip():
            continue
        raw = raw_line[hash_offset:]
        m = re.match(r'#\s*include\s*([<"])([^>"]+)[>"]', raw)
        if not m:
            continue
        i = base_line + offset
        resolved = resolve_include(m.group(2), path, repo, shader_root)
        rec: dict[str, Any] = {
            "order": len(out),
            "line": i,
            "raw": raw.strip(),
            "target": m.group(2),
            "delimiter": "angle" if m.group(1) == "<" else "quote",
            "resolved_path": None,
            "sha256": None,
            "bytes": None,
        }
        if resolved:
            data = resolved.read_bytes()
            resolved_path, scope = display_resolved_path(
                resolved, repo, unity_builtin_include_root(repo)
            )
            rec.update(
                {
                    "resolved_path": resolved_path,
                    "resolved_scope": scope,
                    "sha256": sha256_bytes(data),
                    "bytes": len(data),
                }
            )
        else:
            rec["resolved_scope"] = None
        out.append(rec)
    return out


def recursive_include_graph(
    path: Path, repo: Path, shader_root: Path
) -> list[dict[str, Any]]:
    graph: list[dict[str, Any]] = []
    visiting: set[Path] = set()
    visited: set[Path] = set()

    def visit(current: Path, depth: int) -> None:
        if current in visited or current in visiting:
            return
        visiting.add(current)
        text = current.read_text(encoding="utf-8", errors="surrogateescape")
        for inc in parse_include_directives(text, 1, current, repo, shader_root):
            item = dict(inc)
            item["including_path"], item["including_scope"] = display_resolved_path(
                current, repo, unity_builtin_include_root(repo)
            )
            item["depth"] = depth
            item["order"] = len(graph)
            graph.append(item)
            resolved = resolve_include(inc["target"], current, repo, shader_root)
            if resolved:
                visit(resolved, depth + 1)
        visiting.remove(current)
        visited.add(current)

    visit(path, 0)
    return graph


def parse_structs(
    program_text: str, program_masked: str, base_line: int
) -> list[dict[str, Any]]:
    structs = []
    for m in re.finditer(r"\bstruct\s+([A-Za-z_]\w*)\s*\{", program_masked):
        opening = program_masked.find("{", m.start())
        close = matching_brace(program_masked, opening)
        if close is None:
            continue
        body = program_text[opening + 1 : close]
        fields = []
        records = []
        continuing_preprocessor = False
        for idx, raw in enumerate(
            body.splitlines(), base_line + program_text.count("\n", 0, opening + 1)
        ):
            syntax = strip_line_comment(raw).strip()
            if not syntax:
                continue
            if continuing_preprocessor:
                records.append(
                    {"kind": "preprocessor_continuation", "line": idx, "raw": syntax}
                )
                continuing_preprocessor = syntax.endswith("\\")
                continue
            fm = FIELD_RE.match(syntax)
            if fm:
                item = {
                    "order": len(fields),
                    "line": idx,
                    "raw": syntax,
                    "interpolation_modifiers": fm.group("mod").split(),
                    "type": fm.group("type"),
                    "name": fm.group("name"),
                    "array": (fm.group("array") or "").strip(),
                    "semantic": fm.group("semantic"),
                }
                fields.append(item)
                records.append({"kind": "field", **item})
            elif syntax.startswith("#"):
                records.append({"kind": "preprocessor", "line": idx, "raw": syntax})
                continuing_preprocessor = syntax.endswith("\\")
            elif re.match(
                r"(?:UNITY_|DECLARE_|V2F_)[A-Za-z0-9_]*(?:\s*\(.*\))?$", syntax
            ):
                records.append({"kind": "macro_field", "line": idx, "raw": syntax})
            else:
                records.append({"kind": "unparsed", "line": idx, "raw": syntax})
        structs.append(
            {
                "order": len(structs),
                "line": base_line + program_text.count("\n", 0, m.start()),
                "name": m.group(1),
                "fields": fields,
                "body_records": records,
                "raw": program_text[m.start() : close + 1].strip(),
            }
        )
    return structs


def program_line_depths(text: str) -> list[int]:
    masked = mask_comments_and_strings(text)
    depths = []
    depth = 0
    for line in masked.splitlines():
        depths.append(depth)
        depth += line.count("{") - line.count("}")
    return depths


def parse_bindings(program_text: str, base_line: int) -> list[dict[str, Any]]:
    bindings = []
    lines = program_text.splitlines()
    depths = program_line_depths(program_text)
    cbuffer: str | None = None
    for i, raw in enumerate(lines):
        syntax = strip_line_comment(raw).strip()
        lineno = base_line + i
        cm = re.match(
            r"(?:CBUFFER_START|UNITY_INSTANCING_BUFFER_START)\s*\(\s*([^),]+)", syntax
        )
        if cm:
            cbuffer = cm.group(1).strip()
            continue
        if re.match(r"(?:CBUFFER_END|UNITY_INSTANCING_BUFFER_END)", syntax):
            cbuffer = None
            continue
        macro = re.match(
            r"(UNITY_DECLARE_[A-Za-z0-9_]+|TEXTURE[A-Za-z0-9_]*|SAMPLER|RW_TEXTURE[A-Za-z0-9_]*)\s*\((.*)\)\s*;?",
            syntax,
        )
        if macro:
            bindings.append(
                {
                    "order": len(bindings),
                    "line": lineno,
                    "raw": syntax,
                    "kind": "resource_macro",
                    "macro": macro.group(1),
                    "arguments": [x.strip() for x in split_top_level(macro.group(2))],
                    "buffer": cbuffer,
                }
            )
            continue
        if depths[i] != 0 and cbuffer is None:
            continue
        dm = DECL_RE.match(syntax)
        if dm:
            typ = dm.group("type")
            kind = (
                "texture_or_sampler"
                if re.match(RESOURCE_PATTERN + r"$", typ)
                else "uniform"
            )
            bindings.append(
                {
                    "order": len(bindings),
                    "line": lineno,
                    "raw": syntax,
                    "kind": kind,
                    "modifiers": dm.group("mod").split(),
                    "type": typ,
                    "name": dm.group("name"),
                    "array": (dm.group("array") or "").strip(),
                    "semantic": dm.group("semantic"),
                    "buffer": cbuffer,
                }
            )
    return bindings


def parse_instancing(program_text: str, base_line: int) -> list[dict[str, Any]]:
    out = []
    for i, raw in enumerate(program_text.splitlines(), base_line):
        syntax = strip_line_comment(raw).strip()
        for kind, rx in (
            ("buffer_start", r"UNITY_INSTANCING_BUFFER_START\s*\(\s*([^)]+)\)"),
            ("property", r"UNITY_DEFINE_INSTANCED_PROP\s*\(\s*([^,]+),\s*([^)]+)\)"),
            ("buffer_end", r"UNITY_INSTANCING_BUFFER_END\s*\(\s*([^)]+)\)"),
        ):
            m = re.search(rx, syntax)
            if not m:
                continue
            item = {"order": len(out), "line": i, "kind": kind, "raw": syntax}
            if kind == "property":
                item.update({"type": m.group(1).strip(), "name": m.group(2).strip()})
            else:
                item["buffer"] = m.group(1).strip()
            out.append(item)
    return out


def parse_builtins(program_text: str, base_line: int) -> list[dict[str, Any]]:
    out = []
    token_re = re.compile(
        r"\b(?:UNITY_[A-Za-z0-9_]+|Unity[A-Z][A-Za-z0-9_]*|unity_[A-Za-z0-9_]+|glstate_[A-Za-z0-9_]+|_[A-Za-z][A-Za-z0-9_]*)\b"
    )
    for i, raw in enumerate(program_text.splitlines(), base_line):
        syntax = strip_line_comment(raw)
        for m in token_re.finditer(syntax):
            token = m.group(0)
            if (
                token.startswith(("UNITY_", "Unity", "unity_", "glstate_"))
                or token in BUILTIN_EXACT
                or token.startswith(
                    (
                        "_WorldSpace",
                        "_ProjectionParams",
                        "_ScreenParams",
                        "_ZBufferParams",
                        "_CameraDepthTexture",
                    )
                )
            ):
                out.append(
                    {
                        "order": len(out),
                        "line": i,
                        "token": token,
                        "raw_line": raw.strip(),
                    }
                )
    return out


def parse_outputs(
    program_text: str,
    base_line: int,
    structs: list[dict[str, Any]],
    fragment_entries: list[str],
) -> list[dict[str, Any]]:
    """Capture explicit function outputs and output-struct fields, not vertex COLOR inputs."""
    out = []
    output_semantic = r"(?:SV_Target\d*|SV_Depth\w*|COLOR\d*)"
    returned_structs: set[str] = set()
    lines = program_text.splitlines()
    for i, raw in enumerate(lines, base_line):
        syntax = strip_line_comment(raw).strip()
        signature = re.search(
            rf"(?:^|\s)(?P<return>[A-Za-z_]\w*(?:[1-4](?:x[1-4])?)?)\s+"
            rf"(?P<name>[A-Za-z_]\w*)\s*\([^;{{}}]*\)\s*(?::\s*(?P<semantic>{output_semantic}))?",
            syntax,
            re.I,
        )
        if not signature:
            continue
        semantic = signature.group("semantic")
        if semantic:
            out.append(
                {
                    "order": len(out),
                    "line": i,
                    "kind": "function_return",
                    "symbol": signature.group("name"),
                    "semantic": semantic,
                    "raw": syntax,
                }
            )
        elif signature.group("name") in fragment_entries:
            returned_structs.add(signature.group("return"))
        # Preserve explicit output parameters when present on a single-line signature.
        params = (
            syntax[syntax.find("(") + 1 : syntax.rfind(")")]
            if "(" in syntax and ")" in syntax
            else ""
        )
        for pm in re.finditer(
            rf"\b(?:out|inout)\s+[^,()]*?\b([A-Za-z_]\w*)\s*:\s*({output_semantic})\b",
            params,
            re.I,
        ):
            out.append(
                {
                    "order": len(out),
                    "line": i,
                    "kind": "output_parameter",
                    "symbol": pm.group(1),
                    "semantic": pm.group(2),
                    "raw": syntax,
                }
            )
    for struct in structs:
        if struct["name"] not in returned_structs:
            continue
        for field in struct["fields"]:
            if field["semantic"] and re.fullmatch(
                output_semantic, field["semantic"], re.I
            ):
                out.append(
                    {
                        "order": len(out),
                        "line": field["line"],
                        "kind": "returned_struct_field",
                        "struct": struct["name"],
                        "symbol": field["name"],
                        "semantic": field["semantic"],
                        "raw": field["raw"],
                    }
                )
    out.sort(key=lambda x: (x["line"], x["order"]))
    for i, item in enumerate(out):
        item["order"] = i
    return out


def find_programs(text: str, masked: str) -> list[dict[str, Any]]:
    starts = []
    start_re = re.compile(r"\b(CGPROGRAM|HLSLPROGRAM|CGINCLUDE|HLSLINCLUDE)\b")
    for m in start_re.finditer(masked):
        token = m.group(1)
        end_token = "ENDCG" if token.startswith("CG") else "ENDHLSL"
        em = re.search(rf"\b{end_token}\b", masked[m.end() :])
        if em:
            end_start = m.end() + em.start()
            end_end = m.end() + em.end()
            starts.append(
                {
                    "token": token,
                    "start": m.start(),
                    "content_start": m.end(),
                    "end_start": end_start,
                    "end": end_end,
                }
            )
    return starts


def parse_program(
    program: dict[str, Any],
    text: str,
    source_path: Path,
    repo: Path,
    shader_root: Path,
    sub_index: int | None,
    pass_index: int | None,
) -> dict[str, Any]:
    content = text[program["content_start"] : program["end_start"]]
    base_line = line_number(text, program["content_start"])
    pp = logical_preprocessor_lines(content, base_line)
    pragmas = [x for i, p in enumerate(pp) if (x := pragma_details(p, i)) is not None]
    for i, p in enumerate(pragmas):
        p["order"] = i
    includes = parse_include_directives(
        content, base_line, source_path, repo, shader_root
    )
    structs = parse_structs(content, mask_comments_and_strings(content), base_line)
    entry_points = [
        {
            "order": i,
            "stage": p["directive"],
            "function": p["arguments"][0] if p["arguments"] else None,
            "line": p["line"],
            "pragma_order": p["order"],
        }
        for i, p in enumerate(p for p in pragmas if p["directive"] in ENTRY_DIRECTIVES)
    ]
    targets = [
        {
            "order": i,
            "profile": p["arguments_raw"],
            "line": p["line"],
            "pragma_order": p["order"],
        }
        for i, p in enumerate(p for p in pragmas if p["directive"] == "target")
    ]
    profiles = [
        {
            "order": i,
            "directive": p["directive"],
            "arguments_raw": p["arguments_raw"],
            "line": p["line"],
            "pragma_order": p["order"],
        }
        for i, p in enumerate(
            p
            for p in pragmas
            if p["directive"]
            in {"target", "require", "only_renderers", "exclude_renderers"}
        )
    ]
    return {
        "order": 0,
        "kind": program["token"],
        "line_start": line_number(text, program["start"]),
        "line_end": line_number(text, program["end"]),
        "subshader_index": sub_index,
        "pass_index": pass_index,
        "entry_points": entry_points,
        "all_pragmas": pragmas,
        "keyword_groups": [
            p
            for p in pragmas
            if p["classification"]
            in {
                "multi_compile",
                "shader_feature",
                "instancing_variants",
                "fog_variants",
            }
        ],
        "explicit_targets": targets,
        "profile_constraints": profiles,
        "includes": includes,
        "instancing_declarations": parse_instancing(content, base_line),
        "bindings": parse_bindings(content, base_line),
        "structs": structs,
        "unity_builtins": parse_builtins(content, base_line),
        "output_targets": parse_outputs(
            content,
            base_line,
            structs,
            [
                e["function"]
                for e in entry_points
                if e["stage"] == "fragment" and e["function"]
            ],
        ),
    }


def parse_scope_commands(
    text: str,
    masked: str,
    owner: dict[str, int] | None,
    nested: list[dict[str, int]],
    names: set[str],
) -> list[dict[str, Any]]:
    """Parse line-oriented ShaderLab commands at one lexical scope."""
    start = owner["open"] + 1 if owner else 0
    end = owner["close"] if owner else len(text)
    nested_ranges = [
        (n.get("keyword", n.get("start")), n.get("close", n.get("end"))) for n in nested
    ]
    out = []
    offset = start
    for lineno, raw in enumerate(
        text[start:end].splitlines(), line_number(text, start)
    ):
        line_start = offset
        offset += len(raw) + 1
        if any(a <= line_start <= b for a, b in nested_ranges):
            continue
        syntax = strip_line_comment(raw).strip()
        m = re.match(r"([A-Za-z]+)\b(.*)$", syntax)
        if m and m.group(1) in names:
            out.append(
                {
                    "order": len(out),
                    "line": lineno,
                    "command": m.group(1),
                    "arguments": m.group(2).strip(),
                    "raw": syntax,
                }
            )
    return out


def parse_shader(path: Path, repo: Path) -> tuple[dict[str, Any], list[str]]:
    warnings: list[str] = []
    text = path.read_text(encoding="utf-8", errors="surrogateescape")
    masked = mask_comments_and_strings(text)
    sm = re.search(r'\bShader\s+"((?:\\.|[^"\\])*)"', text)
    shader_name = sm.group(1) if sm else None
    if shader_name is None:
        warnings.append("Shader declaration name not found.")
    sub_blocks = find_blocks(text, masked, "SubShader")
    pass_blocks = find_blocks(text, masked, "Pass")
    program_blocks = find_programs(text, masked)
    subshaders = []
    pass_context: dict[int, tuple[int, int]] = {}
    for si, sb in enumerate(sub_blocks):
        passes = [
            pb for pb in pass_blocks if direct_child(pb, sb, sub_blocks + pass_blocks)
        ]
        pass_items = []
        for pi, pb in enumerate(passes):
            nested = []
            states, stencils = parse_states(text, masked, pb, nested)
            name_match = re.search(
                r'\bName\s+"((?:\\.|[^"\\])*)"', text[pb["open"] + 1 : pb["close"]]
            )
            item = {
                "order": pi,
                "line": line_number(text, pb["keyword"]),
                "name": name_match.group(1) if name_match else None,
                "tags": block_tags(text, masked, pb, []),
                "render_states": states,
                "stencil_blocks": stencils,
            }
            pass_items.append(item)
            pass_context[id(pb)] = (si, pi)
        states, stencils = parse_states(text, masked, sb, passes)
        subshaders.append(
            {
                "order": si,
                "line": line_number(text, sb["keyword"]),
                "tags": block_tags(text, masked, sb, passes),
                "commands": parse_scope_commands(
                    text, masked, sb, passes + find_programs(text, masked), {"LOD"}
                ),
                "render_states": states,
                "stencil_blocks": stencils,
                "passes": pass_items,
            }
        )
    programs = []
    for order, prog in enumerate(program_blocks):
        sub_idx = pass_idx = None
        enclosing_pass = next(
            (pb for pb in pass_blocks if pb["open"] < prog["start"] < pb["close"]), None
        )
        if enclosing_pass:
            for si, sb in enumerate(sub_blocks):
                passes = [
                    pb
                    for pb in pass_blocks
                    if direct_child(pb, sb, sub_blocks + pass_blocks)
                ]
                if enclosing_pass in passes:
                    sub_idx, pass_idx = si, passes.index(enclosing_pass)
                    break
        else:
            for si, sb in enumerate(sub_blocks):
                if sb["open"] < prog["start"] < sb["close"]:
                    sub_idx = si
                    break
        item = parse_program(
            prog, text, path, repo, repo / SHADER_ROOT, sub_idx, pass_idx
        )
        item["order"] = order
        programs.append(item)
    include_graph = recursive_include_graph(path, repo, repo / SHADER_ROOT)
    for inc in include_graph:
        if not inc["resolved_path"]:
            warnings.append(
                f"Unresolved include at {inc['including_path']}:{inc['line']}: {inc['target']}"
            )
    rel = posix(path.relative_to(repo))
    category = (
        "recovered_replacement"
        if rel in RECOVERED_REPLACEMENTS
        else "recovered_partial_adapter"
        if rel in RECOVERED_PARTIAL
        else "other_or_uncertain"
    )
    evidence = (
        RECOVERED_REPLACEMENTS.get(rel)
        or RECOVERED_PARTIAL.get(rel)
        or "No repository evidence classified this shader as a recovered Beat Saber replacement."
    )
    abi = {
        "shader_declaration": {
            "name": shader_name,
            "line": line_number(text, sm.start()) if sm else None,
        },
        "shader_commands": parse_scope_commands(
            text,
            masked,
            None,
            sub_blocks + find_blocks(text, masked, "Properties"),
            {"Fallback", "CustomEditor"},
        ),
        "properties": parse_properties(text, masked, warnings),
        "subshaders": subshaders,
        "programs": programs,
    }
    abi_hash = sha256_bytes(
        json.dumps(
            abi, sort_keys=True, separators=(",", ":"), ensure_ascii=False
        ).encode("utf-8")
    )
    snapshot = {
        "schema_version": SCHEMA_VERSION,
        "classification": {"category": category, "evidence": evidence},
        "include_resolution": {
            "project_unity_version": project_unity_version(repo),
            "unity_builtin_include_root_available": unity_builtin_include_root(repo)
            is not None,
        },
        "source": source_record(path, repo),
        "include_graph": include_graph,
        "abi_sha256": abi_hash,
        "abi": abi,
        "warnings": warnings,
    }
    return snapshot, warnings


def safe_snapshot_name(rel: str) -> str:
    stem = rel.removeprefix("Assets/_Graphics/Shaders/").removesuffix(".shader")
    return stem.replace("/", "__").replace(" ", "_") + ".json"


def classify(rel: str, suffix: str) -> tuple[str, str, bool]:
    if rel in RECOVERED_REPLACEMENTS:
        return "recovered_replacement", RECOVERED_REPLACEMENTS[rel], True
    if rel in RECOVERED_PARTIAL:
        return "recovered_partial_adapter", RECOVERED_PARTIAL[rel], True
    if suffix == ".hlsl" and rel in RECOVERED_SUPPORT_INCLUDES:
        return (
            "supporting_recovered_include",
            "Shared include used by recovered/replacement shader behavior; README include ownership and source comments.",
            False,
        )
    return (
        "other_or_uncertain",
        "No repository evidence classified this asset as recovered/replacement Beat Saber behavior.",
        False,
    )


def write_schema(path: Path) -> None:
    path.write_text(
        """# Recovered Shader ABI Snapshot Schema\n\n`schema_version` is `1.0.0`. JSON object member order is stable, and all ordered ShaderLab constructs are arrays with zero-based `order` fields. Paths are repository-relative POSIX paths. Line numbers are one-based. SHA-256 values are lowercase hashes of exact file bytes.\n\n## Aggregate\n\n`abi_before.json` contains `schema_version`, inventory counts, and `shaders`. Each shader item has classification evidence, exact source metadata, an ordered recursive include graph, an ABI hash, parsed ABI data, and warnings. The same item is written under `per_shader/`.\n\n## ABI fields\n\n- `shader_declaration`: exact declared Shader name. `shader_commands` preserves root `Fallback` and `CustomEditor` commands.\n- `properties`: declaration order, raw declaration, attribute order, spelling, display text, type text, and default text.\n- `subshaders`: source order, ordered tags, `LOD` commands, inherited render states, stencil commands, and passes.\n- `passes`: source order, optional name, ordered tags, render states, and stencil blocks.\n- `programs`: CG/HLSL program/include order and ownership (`subshader_index`, `pass_index`).\n- `all_pragmas`: every active pragma, including physical continuation lines and a normalized parse.\n- `keyword_groups`: exact keyword token order plus compile classification, global/local classification, and stage scope. A single `_` or `__` token is retained.\n- `entry_points`, `explicit_targets`, and `profile_constraints`: stage entry names and explicit target/renderer requirements. Absence is represented by an empty array and is significant.\n- `instancing_declarations`: ordered Unity instancing buffer/property macro declarations with exact types and names.\n- `bindings`: top-level scalar/vector/matrix uniforms, texture/sampler resources, resource macros, and cbuffer membership detectable in source.\n- `structs`: ordered declarations. `fields` preserves parsed field type, name, array, semantic, and interpolation modifiers. `body_records` also preserves preprocessor branches, macro fields, and unparsed lines in order.\n- `unity_builtins`: ordered source occurrences of Unity macro/function/global identifiers.\n- `output_targets`: ordered fragment/depth/COLOR outputs from struct fields and function return semantics.\n- `include_graph`: depth-first include declarations, including target spelling, delimiter, order, resolved path, and exact hash when the include exists in the repository.\n\n## Manifest\n\n`manifest.csv` inventories every `.shader`, `.shadergraph`, and `.hlsl` source below `Assets/_Graphics/Shaders`, with exact hashes, classification, and snapshot paths. Only recovered replacements and the explicitly partial recovered adapter have per-shader ABI snapshots.\n\n## Comparator\n\nUse `Tools/compare_recovered_shader_abi.py BEFORE AFTER` to compare the listed ABI fields. It exits 0 for ABI equality, 1 for a listed ABI change, and 2 for read or schema errors. The comparator ignores evidence-only hashes, source positions, and formatting records. Explicit `--relocation OLD=NEW` arguments permit expected repository-relative source and include moves.\n\n## Deliberate limitations\n\nThis is a deterministic source parser, not Unity's ShaderLab/HLSL compiler. It does not preprocess conditional branches, expand macros, compile variants, infer implicit Unity defaults, or inspect generated GPU binaries. It records all conditional declarations it can see. Binding parsing is limited to conventional top-level declarations and known resource macros. ShaderGraph JSON is inventoried and hashed but not expanded into generated ShaderLab. Built-in Unity includes are resolved from the exact `ProjectVersion.txt` editor installation when available. Missing installations leave those includes unresolved and without hashes. See `parse_warnings.md`.\n""",
        encoding="utf-8",
    )


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", type=Path, default=Path.cwd())
    ap.add_argument(
        "--out",
        type=Path,
        default=Path("Assets/_Graphics/Shaders/Audit/Recovered/GeneratedABI"),
    )
    args = ap.parse_args()
    repo, out = args.repo.resolve(), args.out.resolve()
    shader_root = repo / SHADER_ROOT
    out.mkdir(parents=True, exist_ok=True)
    per_dir = out / "per_shader"
    per_dir.mkdir(parents=True, exist_ok=True)
    for old in per_dir.glob("*.json"):
        old.unlink()

    source_files = sorted(
        [
            *shader_root.rglob("*.shader"),
            *shader_root.rglob("*.shadergraph"),
            *shader_root.rglob("*.hlsl"),
        ],
        key=lambda p: posix(p.relative_to(repo)),
    )
    manifest_rows = []
    selected = []
    warnings_by_path: dict[str, list[str]] = {}
    names: dict[str, str | None] = {}
    for path in source_files:
        rel = posix(path.relative_to(repo))
        category, evidence, included = classify(rel, path.suffix.lower())
        record = source_record(path, repo)
        shader_name = None
        if path.suffix.lower() == ".shader":
            text = path.read_text(encoding="utf-8", errors="surrogateescape")
            m = re.search(r'\bShader\s+"((?:\\.|[^"\\])*)"', text)
            shader_name = m.group(1) if m else None
            names[rel] = shader_name
        manifest_rows.append(
            {
                "path": rel,
                "asset_kind": path.suffix.lower().lstrip("."),
                "classification": category,
                "snapshot_included": "true" if included else "false",
                "snapshot_path": f"per_shader/{safe_snapshot_name(rel)}"
                if included
                else "",
                "shader_name": shader_name or "",
                "sha256": record["sha256"],
                "bytes": record["bytes"],
                "evidence": evidence,
            }
        )
        if included:
            snapshot, warnings = parse_shader(path, repo)
            selected.append(snapshot)
            warnings_by_path[rel] = warnings
            (per_dir / safe_snapshot_name(rel)).write_text(
                json.dumps(snapshot, indent=2, ensure_ascii=False) + "\n",
                encoding="utf-8",
            )

    aggregate = {
        "schema_version": SCHEMA_VERSION,
        "include_resolution": {
            "project_unity_version": project_unity_version(repo),
            "unity_builtin_include_root_available": unity_builtin_include_root(repo)
            is not None,
        },
        "inventory_counts": {
            "shader": sum(p.suffix.lower() == ".shader" for p in source_files),
            "shadergraph": sum(
                p.suffix.lower() == ".shadergraph" for p in source_files
            ),
            "hlsl_include": sum(p.suffix.lower() == ".hlsl" for p in source_files),
            "total_source_files": len(source_files),
            "recovered_replacement_snapshots": sum(
                s["classification"]["category"] == "recovered_replacement"
                for s in selected
            ),
            "recovered_partial_adapter_snapshots": sum(
                s["classification"]["category"] == "recovered_partial_adapter"
                for s in selected
            ),
            "total_snapshots": len(selected),
        },
        "shaders": selected,
    }
    (out / "abi_before.json").write_text(
        json.dumps(aggregate, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    with (out / "manifest.csv").open("w", newline="", encoding="utf-8") as f:
        fields = [
            "path",
            "asset_kind",
            "classification",
            "snapshot_included",
            "snapshot_path",
            "shader_name",
            "sha256",
            "bytes",
            "evidence",
        ]
        writer = csv.DictWriter(f, fieldnames=fields, lineterminator="\n")
        writer.writeheader()
        writer.writerows(manifest_rows)
    write_schema(out / "schema.md")
    warning_lines = [
        "# Parse Warnings",
        "",
        f"Parsed {len(selected)} recovered/replacement or partial-adapter ShaderLab files.",
        "",
        "Warnings are deterministic and do not imply a Unity compile error. Unresolved built-in includes are expected when Unity's editor installation is outside the repository.",
        "",
    ]
    total_warnings = sum(len(v) for v in warnings_by_path.values())
    warning_lines += [f"Total warnings: {total_warnings}", ""]
    for rel in sorted(warnings_by_path):
        warning_lines.append(f"## `{rel}`")
        if warnings_by_path[rel]:
            warning_lines += [f"- {w}" for w in warnings_by_path[rel]]
        else:
            warning_lines.append("- None.")
        warning_lines.append("")
    warning_lines += [
        "## Parser limitations",
        "",
        "- The parser does not run Unity's ShaderLab parser or an HLSL preprocessor/compiler.",
        "- Conditional branches and macro field declarations are preserved as source records, not evaluated layouts.",
        "- Built-in Unity includes are hashed from the project-version editor installation when available; unavailable editor includes remain unresolved and have null hashes.",
        "- Binding extraction recognizes conventional top-level declarations and common resource macros; unusual typedef/macro-generated bindings remain represented only by source/include hashes.",
        "- ShaderGraph assets are inventoried and hashed, but generated ShaderLab is not available and is not parsed.",
        "- The separate ABI comparator ignores evidence-only hashes, source positions, and formatting records. It fails when a listed ABI field changes.",
        "",
    ]
    (out / "parse_warnings.md").write_text("\n".join(warning_lines), encoding="utf-8")
    print(json.dumps(aggregate["inventory_counts"], sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
