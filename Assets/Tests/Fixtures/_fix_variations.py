import re

path = r"Assets\Tests\Fixtures\make_spells_laser_variations.py"
with open(path, encoding="utf-8") as f:
    text = f.read()

# Load the source map before build_base runs, and fix the build() call name.
old = "DEST_DIR = os.path.dirname(__file__)\n"
new = (
    "DEST_DIR = os.path.dirname(__file__)\n\n"
    'with open(os.path.join(SOURCE, "ExpertPlusStandard.dat"), encoding="utf-8-sig") as source_file:\n'
    "    difficulty = json.load(source_file)\n"
    "custom_data = difficulty[\"customData\"]\n"
)
assert text.count(old) == 1
text = text.replace(old, new)
text = text.replace('save("SpellsLaserWallFixture.json", build())', 'save("SpellsLaserWallFixture.json", build_base())')

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("fixed")
