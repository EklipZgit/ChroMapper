"""Freeze runtime geometry matrices as ordinary V3 Chroma environment geometry.

Input is DumpGeometrySnapshot's JSON array, not a screenshot or authored pose.
No approximation is made for sheared matrices: those require a parent hierarchy.
"""

import argparse
import copy
import hashlib
import json
import math
from pathlib import Path


def matrix_pose(values):
    if len(values) != 16 or not all(math.isfinite(value) for value in values):
        raise ValueError("Expected a finite Unity column-major 4x4 matrix")
    columns = [[values[column * 4 + row] for row in range(3)] for column in range(3)]
    scales = [math.sqrt(sum(value * value for value in column)) for column in columns]
    if min(scales) < 1e-10:
        raise ValueError("A zero-scale matrix cannot determine its original rotation")
    rotation = [[columns[column][row] / scales[column] for column in range(3)] for row in range(3)]
    determinant = sum(rotation[0][index] * (
        rotation[1][(index + 1) % 3] * rotation[2][(index + 2) % 3]
        - rotation[1][(index + 2) % 3] * rotation[2][(index + 1) % 3]) for index in range(3))
    if determinant < 0:
        scales[0] *= -1
        for row in rotation:
            row[0] *= -1
    error = max(abs(sum(rotation[row][a] * rotation[row][b] for row in range(3))
                    - (1 if a == b else 0)) for a in range(3) for b in range(3))
    if error > 1e-5:
        raise ValueError(f"Matrix contains shear ({error:g}); refusing to change the shape")
    # Unity applies Euler rotations in Z, X, Y order, including signed scales.
    x = math.asin(max(-1, min(1, -rotation[1][2])))
    if abs(math.cos(x)) > 1e-6:
        y = math.atan2(rotation[0][2], rotation[2][2])
        z = math.atan2(rotation[1][0], rotation[1][1])
    else:
        y = math.atan2(-rotation[2][0], rotation[0][0])
        z = 0
    return values[12:15], [math.degrees(value) for value in (x, y, z)], scales


def export_snapshot(source, snapshot):
    if not str(source.get("version", "")).startswith("3."):
        raise ValueError("The source difficulty must use V3 geometry/material keys")
    custom_data = source["customData"]
    source_by_track = {item["track"]: item for item in custom_data["environment"]
                       if isinstance(item.get("track"), str) and "geometry" in item}
    environment = []
    materials = {}
    for row in snapshot:
        track = row["track"]
        original = source_by_track[track]
        position, rotation, scale = matrix_pose(row["worldMatrix"])
        geometry = copy.deepcopy(row["geometry"])
        material = geometry["material"]
        if isinstance(material, str):
            material = copy.deepcopy(custom_data["materials"][material])
        else:
            material = copy.deepcopy(material)
        material.pop("track", None)
        if "color" in row:
            material["color"] = row["color"]
        material_name = "snapshotMaterial" + str(len(materials))
        materials[material_name] = material
        geometry["material"] = material_name
        item = {"geometry": geometry, "track": track, "position": position,
                "rotation": rotation, "scale": scale}
        if "components" in original:
            item["components"] = copy.deepcopy(original["components"])
        environment.append(item)
    return {"version": "3.3.0", "colorNotes": [], "bombNotes": [], "obstacles": [],
            "basicBeatmapEvents": [], "customData": {"materials": materials, "environment": environment}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--label", required=True, help="Describe the captured state; do not label an upright control as distorted")
    args = parser.parse_args()
    source_bytes = args.source.read_bytes()
    snapshot_bytes = args.snapshot.read_bytes()
    snapshot = json.loads(snapshot_bytes)
    result = export_snapshot(json.loads(source_bytes), snapshot)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    manifest = {"label": args.label, "source": str(args.source.resolve()),
                "snapshot": str(args.snapshot.resolve()),
                "sourceSha256": hashlib.sha256(source_bytes).hexdigest(),
                "snapshotSha256": hashlib.sha256(snapshot_bytes).hexdigest(),
                "geometryCount": len(result["customData"]["environment"]),
                "capturedMaterialColorCount": sum("color" in row for row in snapshot),
                "scope": "Frozen geometry. Runtime material colors are copied when recorded. Lighting, fog and camera are not exported."}
    args.output.with_suffix(".manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Exported {manifest['geometryCount']} pieces to {args.output}")


if __name__ == "__main__":
    main()
