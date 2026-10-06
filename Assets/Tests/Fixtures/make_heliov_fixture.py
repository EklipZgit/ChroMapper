# Rebuild HeliovEnvironmentFixture.json from Swifter's installed Expert Standard map.
# Run from the repository root with Python 3.
#
# The reported intro, valley, mountain, and powerline scenes depend on the full environment
# enhancement array and every preceding custom event: removing earlier track animations changes
# the camera and object state at later seeks. Keep those sections verbatim through beat 525.
import json
from pathlib import Path


SOURCE = Path(
    r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data"
    r"\CustomLevels\27ade (Heliov - Swifter)\ExpertStandard.dat"
)
DEST = Path(__file__).with_name("HeliovEnvironmentFixture.json")

with SOURCE.open(encoding="utf-8-sig") as source_file:
    difficulty = json.load(source_file)

custom = difficulty["_customData"]
fixture = {
    "_version": difficulty["_version"],
    # The first playable notes establish the reported camera arrival. The initial rain is made
    # from authored obstacles, while later scenes are environment-enhancement duplicates.
    "_notes": [note for note in difficulty["_notes"] if note["_time"] <= 10],
    "_obstacles": [wall for wall in difficulty["_obstacles"] if wall["_time"] <= 10],
    "_events": [event for event in difficulty["_events"] if event["_time"] <= 525],
    "_waypoints": [],
    "_customData": {
        "_environment": custom["_environment"],
        "_customEvents": [event for event in custom["_customEvents"] if event["_time"] <= 525],
        # The kept animations reference the map's one named point definition.
        "_pointDefinitions": custom["_pointDefinitions"],
        "_time": 525,
    },
}

with DEST.open("w", encoding="utf-8") as dest_file:
    json.dump(fixture, dest_file, separators=(",", ":"))

print(f"Wrote {DEST} ({DEST.stat().st_size:,} bytes)")
for key, value in fixture["_customData"].items():
    if isinstance(value, (dict, list)):
        print(f"  {key}: {len(value)}")
