# Installs the Spells laser-visibility fixture variations as separate levels in CustomLevels so each can be
# opened directly in ChroMapper. Run from the repository root:
#   python Assets/Tests/Fixtures/install_spells_variations.py
#
# Each variation becomes its own level folder with the variation as its only difficulty:
#   "Spells Laser Variations" - the verbatim map essence (the baseline).
#   "Spells NoFogAnimate"     - the AnimateComponent BloomFogEnvironment events removed.
#   "Spells NoPrestart"       - the pre-start (negative beat) AnimateTrack events removed.
#   "Spells NoFogComponent"   - the [0]Environment BloomFogEnvironment component override removed.
# The original level is untouched; rerunning the script is idempotent.
import json
import os
import shutil

SOURCE_LEVEL = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomLevels\35a0b (Spells - Joetastic & Swifter)"
CUSTOM_LEVELS = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomLevels"
FIXTURES = r"C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts\Assets\Tests\Fixtures"

VARIANTS = [
    ("SpellsLaserWallFixture.json", "Spells Laser Variations", "Baseline"),
    ("SpellsNoFogAnimateFixture.json", "Spells NoFogAnimate", "NoFogAnimate"),
    ("SpellsNoPrestartFixture.json", "Spells NoPrestart", "NoPrestart"),
    ("SpellsNoFogComponentFixture.json", "Spells NoFogComponent", "NoFogComponent"),
]

for fixture_name, level_name, label in VARIANTS:
    level_dir = os.path.join(CUSTOM_LEVELS, level_name)
    os.makedirs(level_dir, exist_ok=True)

    # Carry the song and cover over so the level loads with its audio and art.
    for asset in ("song.egg", "cover.jpg"):
        target = os.path.join(level_dir, asset)
        if not os.path.exists(target):
            shutil.copyfile(os.path.join(SOURCE_LEVEL, asset), target)

    # The variation fixture is the level's only difficulty, stored under the standard ExpertPlus filename.
    shutil.copyfile(os.path.join(FIXTURES, fixture_name), os.path.join(level_dir, "ExpertPlusStandard.dat"))

    # Build the level Info from the source Info with the variation's label and a single difficulty.
    with open(os.path.join(SOURCE_LEVEL, "Info.dat"), encoding="utf-8-sig") as f:
        info = json.load(f)

    info["_songName"] = f"Spells Variations - {label}"
    info["_difficultyBeatmapSets"] = [{
        "_beatmapCharacteristicName": "Standard",
        "_difficultyBeatmaps": [{
            "_difficulty": "ExpertPlus",
            "_difficultyRank": 9,
            "_beatmapFilename": "ExpertPlusStandard.dat",
            "_noteJumpMovementSpeed": 18,
            "_noteJumpStartBeatOffset": 0,
            "_beatmapColorSchemeIdx": 0,
            "_environmentNameIdx": 0,
            "_customData": {
                "_difficultyLabel": label,
                "_editorOffset": 0,
                "_editorOldOffset": 0,
                "_suggestions": ["Chroma"],
            },
        }],
    }]

    with open(os.path.join(level_dir, "Info.dat"), "w", encoding="utf-8") as f:
        json.dump(info, f, indent=2, ensure_ascii=False)
    print(f"installed level '{level_name}' ({len(os.listdir(level_dir))} files)")
