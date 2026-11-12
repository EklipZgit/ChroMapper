"""Restore native note-spawn intensity after an external environment JSON export."""

import argparse
import json
from pathlib import Path
import re


def read_native_intensities(native_assets):
    fog_assets = {}
    for path in native_assets.rglob("*FogParams.asset"):
        match = re.search(r"^  noteSpawnIntensity: ([^\r\n]+)", path.read_text(encoding="utf-8"), re.MULTILINE)
        if match is None:
            continue

        guid = re.search(r"^guid: (\w+)", path.with_suffix(".asset.meta").read_text(encoding="utf-8"), re.MULTILINE)
        fog_assets[guid.group(1)] = (float(match.group(1)), path)

    result = {}
    for scene in native_assets.rglob("*Environment.unity"):
        references = re.findall(r"^  _fogParams: \{[^}]*guid: (\w+)", scene.read_text(encoding="utf-8"), re.MULTILINE)
        matches = [fog_assets[guid] for guid in references if guid in fog_assets]
        if len({value for value, _ in matches}) == 1:
            result[scene.stem] = (matches[0][0], scene, matches[0][1])

    return result


def patch_fog_params(text, value):
    block = re.search(r'("fogParams"\s*:\s*\{)([^}]*)(\})', text)
    if block is None:
        raise ValueError("Environment export has no fogParams object")

    body = block.group(2)
    formatted = json.dumps(value)
    existing = re.search(r'("noteSpawnIntensity"\s*:\s*)[-+\deE.]+', body)
    if existing is not None:
        body = body[:existing.start()] + existing.group(1) + formatted + body[existing.end():]
    else:
        indentation = re.search(r'\n(\s*)"', body).group(1)
        trailing = body[len(body.rstrip()):]
        newline = "\r\n" if "\r\n" in text else "\n"
        body = body.rstrip() + ',' + newline + indentation + '"noteSpawnIntensity": ' + formatted + trailing

    return text[:block.start(2)] + body + text[block.end(2):]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native-assets-root", required=True, type=Path, help="Beat Saber 1.44.1 AssetRipper ExportedProject/Assets")
    parser.add_argument("--environment-data-root", type=Path, default=Path(__file__).resolve().parents[1] / "Assets/__Scenes/Environments/Data")
    parser.add_argument("--report", type=Path, help="Write the exact scene and fog asset provenance as JSON")
    parser.add_argument("--check", action="store_true", help="Report missing or incorrect fields without writing")
    args = parser.parse_args()
    native = read_native_intensities(args.native_assets_root)
    report = []
    pending = []
    for path in sorted(args.environment_data_root.glob("*Environment.json")):
        if path.stem not in native:
            raise ValueError(f"No unambiguous native fog reference for {path.stem}")

        value, scene, fog = native[path.stem]
        text = path.read_bytes().decode("utf-8")
        original = json.loads(text)
        changed = original["environmentData"]["fogParams"].get("noteSpawnIntensity") != value
        if changed:
            patched = patch_fog_params(text, value)
            expected = json.loads(text)
            expected["environmentData"]["fogParams"]["noteSpawnIntensity"] = value
            if json.loads(patched) != expected:
                raise ValueError(f"Patch changed unrelated environment data in {path}")

            pending.append((path, patched))

        report.append({"environment": path.stem, "noteSpawnIntensity": value, "nativeScene": str(scene), "nativeFogAsset": str(fog), "changed": changed})

    if not args.check:
        for path, text in pending:
            path.write_bytes(text.encode("utf-8"))

    if args.report is not None:
        args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    print(f"Resolved {len(report)} environments; {len(pending)} {'require changes' if args.check else 'updated'}.")
    return int(args.check and bool(pending))


if __name__ == "__main__":
    raise SystemExit(main())
