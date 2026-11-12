"""Export the shipped PC SimpleLit keyword combinations, without decoding shader code."""
import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path, help="AssetRipper's SimpleLit.json shader export")
parser.add_argument("output", type=Path)
parser.add_argument("--game-version", required=True)
args = parser.parse_args()
source_bytes = args.source.read_bytes()
source = json.loads(source_bytes.decode("utf-8-sig"))
form = source["m_ParsedForm"]
names = form["m_KeywordNames"]
# Camera, lighting, stereo, and instancing variants do not change the authored material request.
runtime_keywords = {
    "ENABLE_BLOOM_FOG", "MAIN_EFFECT_ENABLED", "ACES_TONE_MAPPING", "OVERDRAW_VIEW",
    "INSTANCING_ON", "STEREO_INSTANCING_ON", "BEATGAMES_STEREO_PASS",
    "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "SHADOWS_SHADOWMASK",
    "LIGHTMAP_SHADOW_MIXING", "NOTE_VERTEX_DISTORTION", "UNITY_ASSUME_UNIFORM_SCALING",
}
programs = form["m_SubShaders"][0]["m_Passes"][0]["m_ProgFragment"]["m_PlayerSubPrograms"][3]
variants = []
seen = set()
for program in programs:
    keywords = frozenset(names[index] for index in program["m_KeywordIndices"]) - runtime_keywords
    if keywords not in seen:
        seen.add(keywords)
        variants.append(sorted(keywords))
result = {
    "gameVersion": args.game_version,
    "shader": form["m_Name"],
    "sourceSha256": hashlib.sha256(source_bytes).hexdigest(),
    "sourceFile": args.source.name,
    "platform": "PC D3D11",
    "runtimeKeywords": sorted(runtime_keywords),
    "variants": variants,
}
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(f"Exported {len(variants)} native material keyword combinations to {args.output}")
