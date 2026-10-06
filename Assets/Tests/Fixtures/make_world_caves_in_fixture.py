# Fixture source: "As The World Caves In", mapped by Mawntee & Fatalution (BeatSaver ID: 210e3).
import json
from collections import defaultdict
from pathlib import Path

src = r"C:\Users\tdrak\BSManager\BSInstances\1.44.1\Beat Saber_Data\CustomLevels\210e3 (As The World Caves In - Mawntee & Fatalution)"
dst = r"C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts\Assets\Tests\Fixtures\WorldCavesInEnvironmentEssence.json"

with open(src + r"\ExpertPlusStandard.dat", "r", encoding="utf-8") as f:
    m = json.load(f)

# --- notes: intro run + windows covering the reported beats (104 movement, 161/165 smoke+rings, 266 kaleidoscope)
def keep_note(n):
    t = n["_time"]
    return t < 24 or 96 <= t <= 112 or 156 <= t <= 170 or 258 <= t <= 276

notes = [n for n in m["_notes"] if keep_note(n)]
print("notes kept:", len(notes))

# --- events: laser speed (2/3) drive the spinning pair lasers, ring rotation (8) / zoom (9) drive the rings.
# Keep those entirely; keep early light events (types 0/1) so the intro constructs render lit like the map.
# Keep ALL type 4 events: the beat-168..172 runway/trapezoid regression test needs the authored _lightID
# streams verbatim (residual state at the assertion beats depends on every earlier type-4 event).
def keep_event(e):
    t = e["_time"]
    return e["_type"] in (2, 3, 8, 9) or e["_type"] == 4 or (e["_type"] in (0, 1) and t <= 24)

events = [e for e in m["_events"] if keep_event(e)]
print("events kept:", len(events))
by_type = defaultdict(int)
for e in events:
    by_type[e["_type"]] += 1
print("by type:", dict(sorted(by_type.items())))

cd = m["_customData"]
fixture = {
    "_version": m["_version"],
    "_notes": notes,
    "_obstacles": [],
    "_events": events,
    "_waypoints": [],
    "_sliders": [],
    "_specialEventsKeywordFilters": {},
    "_customData": {
        "_pointDefinitions": cd.get("_pointDefinitions", []),
        "_environment": cd["_environment"],
        "_customEvents": cd["_customEvents"],
    },
}

text = json.dumps(fixture, indent=0, separators=(",", ":"), ensure_ascii=False)
with open(dst, "w", encoding="utf-8") as f:
    f.write(text)
print("wrote", dst, len(text), "bytes")
