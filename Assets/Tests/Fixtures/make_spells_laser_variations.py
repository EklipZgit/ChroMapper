# Regenerates the Spells laser-visibility fixture variations from the reported map. Run from the repository
# root: python Assets/Tests/Fixtures/make_spells_laser_variations.py
#
# The variations isolate which event family hides or truncates the laser wall:
#   SpellsLaserWallFixture.json        - the verbatim map (the baseline).
#   SpellsNoFogAnimateFixture.json     - the AnimateComponent BloomFogEnvironment events removed.
#   SpellsNoPrestartFixture.json       - the pre-start (negative beat) AnimateTrack events removed.
#   SpellsNoFogComponentFixture.json   - the [0]Environment BloomFogEnvironment component override removed.
import json
import os

SOURCE = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomLevels\35a0b (Spells - Joetastic & Swifter)"
DEST_DIR = os.path.dirname(__file__)

with open(os.path.join(SOURCE, "ExpertPlusStandard.dat"), encoding="utf-8-sig") as source_file:
    difficulty = json.load(source_file)
custom_data = difficulty["customData"]


def build_base():
    return {
        "version": difficulty["version"],
        "bpmEvents": difficulty.get("bpmEvents", []),
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
            "environment": custom_data["environment"],
            "customEvents": custom_data["customEvents"],
            "pointDefinitions": custom_data.get("pointDefinitions", {}),
            "materials": custom_data.get("materials", {}),
            "time": 90,
        },
    }


def strip_fog_animate(fixture):
    fixture["customData"]["customEvents"] = [
        entry
        for entry in fixture["customData"]["customEvents"]
        if not (entry.get("t") == "AnimateComponent" and "BloomFogEnvironment" in entry.get("d", {}))
    ]
    return fixture


def strip_prestart(fixture):
    fixture["customData"]["customEvents"] = [
        entry for entry in fixture["customData"]["customEvents"] if (entry.get("b") or 0) >= 0
    ]
    return fixture


def strip_fog_component(fixture):
    for entry in fixture["customData"]["environment"]:
        if entry.get("id") == "[0]Environment" and "components" in entry:
            entry["components"].pop("BloomFogEnvironment", None)
    return fixture


def save(name, fixture):
    path = os.path.join(os.path.dirname(__file__), name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(fixture, f, separators=(",", ":"))
    print(f"wrote {name} ({os.path.getsize(path)} bytes)")


save("SpellsLaserWallFixture.json", build_base())
save("SpellsNoFogAnimateFixture.json", strip_fog_animate(build_base()))
save("SpellsNoPrestartFixture.json", strip_prestart(build_base()))
save("SpellsNoFogComponentFixture.json", strip_fog_component(build_base()))
