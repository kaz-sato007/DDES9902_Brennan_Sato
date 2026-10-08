"""Render the training map from the same heightmap, woodland mask and feature data
that build the Unity terrain, so map and ground always agree."""
import json
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon, Rectangle, FancyArrow
from scipy.ndimage import gaussian_filter

OUT = "out"
H = np.load(f"{OUT}/H.npy")
wood = np.load(f"{OUT}/woodland.npy")
meta = json.load(open(f"{OUT}/landnav_meta.json"))
SIZE = meta["size"]
xs = np.linspace(0, SIZE, H.shape[0])

FLAG_COL = {"Blue": "#1f5fd6", "Red": "#d62020", "Yellow": "#f2c200", "Green": "#169c3a"}
PAPER = "#fbf8ef"
WOOD = "#c5e3b1"
CONTOUR = "#a0522d"
GRID = "#2c6fb5"

# Figure: 2048 x 2560 px portrait (map square on top, legend panel below)
W_PX, H_PX, DPI = 2048, 2560, 256
fig = plt.figure(figsize=(W_PX / DPI, H_PX / DPI), dpi=DPI, facecolor=PAPER)

# Map frame occupies the top square
mx0, my0, mw = 0.08, 0.255, 0.84
mh = mw * W_PX / H_PX
ax = fig.add_axes([mx0, my0, mw, mh])
ax.set_xlim(0, SIZE); ax.set_ylim(0, SIZE)
ax.set_facecolor(PAPER)
ax.set_xticks([]); ax.set_yticks([])
for s in ax.spines.values():
    s.set_linewidth(2.2)

# Woodland tint
wsm = gaussian_filter(wood.astype(float), 1.5)
ax.contourf(xs, xs, wsm, levels=[0.5, 2], colors=[WOOD], zorder=1)

# Contours: 5 m interval, index contour every 25 m
levels = np.arange(5, H.max() + 5, 5)
minor = [l for l in levels if l % 25]
index = [l for l in levels if l % 25 == 0]
ax.contour(xs, xs, H, levels=minor, colors=CONTOUR, linewidths=0.9, zorder=2)
ci = ax.contour(xs, xs, H, levels=index, colors=CONTOUR, linewidths=2.0, zorder=2)
ax.clabel(ci, fmt="%d", fontsize=7, colors=CONTOUR, inline_spacing=2)

# Spot heights on both knolls (standard map detail, does not name the feature)
for f in meta["features"]:
    if f["id"] in ("knoll", "knoll_east"):
        ax.plot(f["x"], f["z"], marker=".", color="black", ms=4, zorder=4)
        ax.text(f["x"] - 9, f["z"] - 3, f"{f['y']:.0f}", fontsize=7, ha="right", va="top", zorder=4)

# Grid: 100 m squares, numbered like a military grid (eastings along bottom, northings up the side)
for v in range(0, int(SIZE) + 1, 100):
    ax.axvline(v, color=GRID, lw=0.8, zorder=3)
    ax.axhline(v, color=GRID, lw=0.8, zorder=3)
for i, v in enumerate(range(0, int(SIZE) + 1, 100)):
    lab = f"{i:02d}"
    ax.text(v, -12, lab, ha="center", va="top", fontsize=9, color=GRID, clip_on=False)
    ax.text(-12, v, lab, ha="right", va="center", fontsize=9, color=GRID, clip_on=False)


def flag_symbol(x, z, colour, label):
    # circle marks the exact spot; a small pennant makes the colour unmistakable
    ax.add_patch(plt.Circle((x, z), 7.5, fc="white", ec="black", lw=1.2, zorder=6))
    ax.add_patch(plt.Circle((x, z), 5.2, fc=colour, ec="none", zorder=7))
    ax.plot([x, x], [z, z + 24], color="black", lw=1.4, zorder=6)
    ax.add_patch(Polygon([[x, z + 24], [x + 15, z + 19.5], [x, z + 15]], closed=True,
                         fc=colour, ec="black", lw=0.9, zorder=7))
    ax.text(x + 9, z - 4, label, fontsize=8, fontweight="bold", va="top", zorder=8,
            bbox=dict(fc="white", ec="none", pad=0.6, alpha=0.85))


for f in meta["features"]:
    if f["flag"]:
        flag_symbol(f["x"], f["z"], FLAG_COL[f["flag"]], f["flag"].upper())

# Start point
st = meta["start"]
ax.add_patch(Polygon([[st["x"], st["z"] + 11], [st["x"] - 9.5, st["z"] - 6], [st["x"] + 9.5, st["z"] - 6]],
                     closed=True, fc="none", ec="#7a1fa2", lw=2.2, zorder=6))
ax.text(st["x"] + 12, st["z"] - 2, "START", color="#7a1fa2", fontsize=8, fontweight="bold", zorder=6)

# Title
fig.text(0.5, 0.988, "TERRAIN IDENTIFICATION TRAINING AREA", ha="center", va="top",
         fontsize=15, fontweight="bold", family="DejaVu Sans")
fig.text(0.5, 0.963, "Sheet 1 of 1  |  Grid squares 100 m  |  Contour interval 5 m  |  Heights in metres",
         ha="center", va="top", fontsize=8.5)

# ---- Legend panel -----------------------------------------------------------
lg = fig.add_axes([0.06, 0.02, 0.88, 0.2])
lg.set_xlim(0, 100); lg.set_ylim(0, 40); lg.axis("off")
lg.add_patch(Rectangle((0, 0), 100, 40, fc="white", ec="black", lw=1.5))

lg.text(3, 37, "TASK", fontsize=10, fontweight="bold", va="top")
lg.text(3, 32.5, "Walk to each coloured flag marked on this map and\nplant that flag on the feature at that spot.\n"
        "Read the contours to recognise the feature\nbefore you plant.", fontsize=7.5, va="top", linespacing=1.4)
yy = 15.5
for i, c in enumerate(["Blue", "Red", "Yellow", "Green"]):
    x = 4 + i * 11
    lg.add_patch(plt.Circle((x, yy), 1.6, fc=FLAG_COL[c], ec="black", lw=0.8))
    lg.text(x + 2.3, yy, c, va="center", fontsize=7.5)
lg.text(3, 9, "Flag colours are not ordered: plant them in any sequence.", fontsize=7, style="italic", va="center")

# map symbols legend
lx = 55
lg.text(lx, 37, "LEGEND", fontsize=10, fontweight="bold", va="top")
rows = [
    ("wood", "Woodland"),
    ("open", "Open ground"),
    ("cont", "Contour (5 m)"),
    ("index", "Index contour (25 m)"),
    ("grid", "Grid line (100 m)"),
    ("start", "Start point"),
]
for i, (k, t) in enumerate(rows):
    y = 31 - i * 4.6
    if k == "wood":
        lg.add_patch(Rectangle((lx, y - 1.3), 6, 2.6, fc=WOOD, ec="#888", lw=0.5))
    elif k == "open":
        lg.add_patch(Rectangle((lx, y - 1.3), 6, 2.6, fc=PAPER, ec="#888", lw=0.5))
    elif k == "cont":
        lg.plot([lx, lx + 6], [y, y], color=CONTOUR, lw=0.9)
    elif k == "index":
        lg.plot([lx, lx + 6], [y, y], color=CONTOUR, lw=2.0)
    elif k == "grid":
        lg.plot([lx, lx + 6], [y, y], color=GRID, lw=0.8)
    elif k == "start":
        lg.add_patch(Polygon([[lx + 3, y + 1.6], [lx + 1.4, y - 1.2], [lx + 4.6, y - 1.2]], closed=True,
                             fc="none", ec="#7a1fa2", lw=1.5))
    lg.text(lx + 8, y, t, va="center", fontsize=7.5)

# North arrow and scale bar (inside legend panel, right side)
nx, ny = 92, 26
lg.add_patch(FancyArrow(nx, ny - 6, 0, 10, width=1.2, head_width=3.4, head_length=3.2, fc="black", ec="black"))
lg.text(nx, ny + 8, "N", ha="center", fontsize=11, fontweight="bold")
# 100 m scale bar in data units of the legend axes: map is 500 m across mw of figure;
# legend axes spans same width (100 units) so 100 m = 100/500*100*(mw/0.88) units
unit = 100.0 / SIZE * 100.0 * (mw / 0.88)
sx, sy = 97, 6
for i in range(2):
    lg.add_patch(Rectangle((sx - unit + i * unit / 2, sy), unit / 2, 1.2,
                           fc="black" if i % 2 == 0 else "white", ec="black", lw=0.8))
lg.text(sx - unit, sy + 2.3, "0", ha="center", fontsize=7)
lg.text(sx, sy + 2.3, "100 m", ha="center", fontsize=7)

fig.savefig(f"{OUT}/TrainingMap.png", dpi=DPI, facecolor=PAPER)
print("map saved")

# Record where the map square sits in the image so Unity can place the "you are here"
# option later if wanted (normalised UV of the map frame, origin bottom-left)
json.dump(dict(u0=mx0, v0=my0, u1=mx0 + mw, v1=my0 + mh), open(f"{OUT}/map_frame_uv.json", "w"))
