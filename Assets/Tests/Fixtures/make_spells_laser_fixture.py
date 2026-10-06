# Regenerates SpellsLaserWallFixture.json from the reported map. Run from the repository root:
#   python Assets/Tests/Fixtures/make_spells_laser_fixture.py
#
# The fixture keeps the reported map's laser-wall essence: the verbatim environment array (the rotating
# BottomPairLasers pillar regex lookups with TubeBloomPrePassLight components, the hidden vanilla objects,
# the fogged Environment, and the generated light geometry), all custom events, the authored materials, and
# the normal light events. Gameplay objects are trimmed to keep the fixture small.
import json
import os

SOURCE = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomLevels\35a0b (Spells - Joetastic & Swifter)"
DEST = os.path.join(os.path.dirname(__file__), "SpellsLaserWallFixture.json")

with open(os.path.join(SOURCE, "ExpertPlusStandard.dat"), encoding="utf-8-sig") as f:
    difficulty = json.load(f)

fixture = {
    "version": difficulty["version"],
    "bpmEvents": difficulty.get("bpmEvents", []),
    "rotationEvents": [],
    "basicBeatmapEvents": difficulty.get("basicBeatmapEvents", []),
    "colorBoostBeatmapEvents": difficulty.get("colorBoostBeatmapEvents", []),
    "colorNotes": [],
    "bombNotes": [],
    "obstacles": [],
    "sliders": [],
    "burstSliders": [],
    "waypoints": [],
    "lightColorEventBoxGroups": [],
    "lightRotationEventBoxGroups": [],
    "lightTranslationEventBoxGroups": [],
    "vfxEventBoxGroups": [],
    "_fxEventsCollection": {"floatFx": [], "legacyFloatFx": []},
    "basicEventTypesWithKeywords": difficulty.get("basicEventTypesWithKeywords", {"d": []}),
    "useNormalEventsAsCompatibleEvents": True,
    "customData": {
        "environment": difficulty["customData"]["environment"],
        "customEvents": difficulty["customData"]["customEvents"],
        # The map's AnimateTrack events reference point definitions by name (e.g. the rotating_7/8 pillars'
        # "yeet"), so the fixture must carry the map's pointDefinitions or every referenced property fails.
        "pointDefinitions": difficulty["customData"].get("pointDefinitions", {}),
        "materials": difficulty["customData"].get("materials", {}),
        "time": 90,  # covers the map's animated span at 150 BPM
    },
}

with open(DEST, "w", encoding="utf-8") as f:
    json.dump(fixture, f, separators=(",", ":"))
print(f"wrote {DEST} ({os.path.getsize(DEST)} bytes)")
print(f"environment entries: {len(fixture['customData']['environment'])}")
print(f"custom events: {len(fixture['customData']['customEvents'])}")
