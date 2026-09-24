# Regenerates KamikaziLightArrayFixture.json from the reported map. Run from the repository root:
#   python Assets/Tests/Fixtures/make_kamikazi_light_fixture.py
#
# The fixture keeps the reported map's light-array essence: the verbatim 4070-entry environment array (the
# generated ILightWithId geometry parked at z -10000 that hides the vanilla Environment and GameCore), all 3996
# per-track AnimateTrack custom events that fly the lights in from beat 14, the authored materials, and the
# normal light events that drive the generated lights. Gameplay objects are trimmed to keep the fixture small.
import json
import os

SOURCE = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomWIPLevels\Kamikazi Light Level Repro 2"
DEST = os.path.join(os.path.dirname(__file__), "KamikaziLightArrayFixture.json")

with open(os.path.join(SOURCE, "ExpertPlusStandard.dat"), encoding="utf-8-sig") as f:
    difficulty = json.load(f)
with open(os.path.join(SOURCE, "Info.dat"), encoding="utf-8-sig") as f:
    info = json.load(f)

fixture = {
    "version": difficulty["version"],
    "bpmEvents": difficulty.get("bpmEvents", []),
    "rotationEvents": [],
    "basicBeatmapEvents": difficulty.get("basicBeatmapEvents", []),
    "colorBoostBeatmapEvents": [],
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
        "materials": difficulty["customData"]["materials"],
        "time": 80,
    },
}

fixture["customData"]["time"] = 80  # covers the highest animated beat (214 at 202 BPM ~ 64s)

with open(DEST, "w", encoding="utf-8") as f:
    json.dump(fixture, f, separators=(",", ":"))
print(f"wrote {DEST} ({os.path.getsize(DEST)} bytes)")
print(f"environment entries: {len(fixture['customData']['environment'])}")
print(f"custom events: {len(fixture['customData']['customEvents'])}")
