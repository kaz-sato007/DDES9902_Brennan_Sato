"""Generate the land-navigation training terrain and matching data.

World frame (matches Unity): x = east, z = north, metres. Terrain is SIZE x SIZE,
origin at the south-west corner. Heights are metres above the terrain base.
"""
import json
import numpy as np
from scipy.ndimage import gaussian_filter

SIZE = 500.0          # metres, east-west and north-south
RES = 513             # heightmap resolution (Unity needs 2^n + 1)
MAX_H = 100.0         # Unity terrain height (metres) for normalising
SEED = 7

xs = np.linspace(0, SIZE, RES)
X, Z = np.meshgrid(xs, xs)  # arrays indexed [z, x]
rng = np.random.default_rng(SEED)


def gauss(cx, cz, sx, sz=None, h=1.0):
    sz = sx if sz is None else sz
    return h * np.exp(-(((X - cx) / sx) ** 2 + ((Z - cz) / sz) ** 2) / 2)


def seg_dist(ax, az, bx, bz):
    """Distance from every grid point to segment AB, and the parameter t along it."""
    dx, dz = bx - ax, bz - az
    t = ((X - ax) * dx + (Z - az) * dz) / (dx * dx + dz * dz)
    t = np.clip(t, 0, 1)
    px, pz = ax + t * dx, az + t * dz
    return np.hypot(X - px, Z - pz), t


# ---- Feature geometry -------------------------------------------------------
KW = (160.0, 335.0, 40.0)   # west knoll (x, z, height)
KE = (355.0, 320.0, 36.0)   # east knoll
SADDLE_T = 0.6              # saddle sits 60% of the way from KW to KE
SADDLE_DIP = 12.0           # metres the crest drops at the saddle

# Base: gentle fall from north to south plus broad undulation
base = 6 + 8 * (Z / SIZE) + 3 * np.sin(X / 90.0) * np.cos(Z / 120.0)

# Knolls: rounded tops
kw = gauss(KW[0], KW[1], 50, 50, KW[2])
ke = gauss(KE[0], KE[1], 46, 46, KE[2])

# Ridge connecting the knolls: crest height interpolated, dipping to a saddle
d, t = seg_dist(KW[0], KW[1], KE[0], KE[1])
crest = (1 - t) * (KW[2] - 5) + t * (KE[2] - 5)
crest -= SADDLE_DIP * np.exp(-((t - SADDLE_T) / 0.13) ** 2)
ridge = crest * np.exp(-(d / 40.0) ** 2 / 2)

# Spurs running south off each knoll frame the valley
d1, t1 = seg_dist(KW[0] + 5, KW[1] - 20, 195, 105)
spur_w = (KW[2] - 8) * (1 - t1) ** 1.1 * np.exp(-(d1 / 44.0) ** 2 / 2)
d2, t2 = seg_dist(KE[0] - 5, KE[1] - 20, 330, 100)
spur_e = (KE[2] - 8) * (1 - t2) ** 1.1 * np.exp(-(d2 / 42.0) ** 2 / 2)

_stack = np.stack([kw, ke, ridge, spur_w, spur_e])
_k = 3.5  # smooth-max softness (metres) so landforms blend without creases
_m = _stack.max(0)
H = base + _m + _k * np.log(np.exp((_stack - _m) / _k).sum(0)) - _k * np.log(len(_stack)) * np.exp(-_m / 6)

# Valley: a winding channel draining south from below the saddle, between the spurs
VALLEY_PATH = [(274, 290), (268, 235), (258, 180), (262, 125), (252, 70), (246, 20)]
dv = np.full(H.shape, 1e9); tv = np.zeros(H.shape)
cum = 0.0
lens = [np.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(VALLEY_PATH, VALLEY_PATH[1:])]
total = sum(lens)
for (a, b), L in zip(zip(VALLEY_PATH, VALLEY_PATH[1:]), lens):
    dd, tt = seg_dist(a[0], a[1], b[0], b[1])
    m = dd < dv
    dv[m] = dd[m]; tv[m] = (cum + tt[m] * L) / total
    cum += L
depth = 6.0 * np.clip(tv * 4, 0, 1) * (1 - 0.4 * tv)
width = 16 + 14 * tv
H -= depth * np.exp(-(dv / width) ** 2 / 2)

# Northern fall-away so the knolls read as high ground from every side
H -= 10 * np.clip((Z - 400) / 100, 0, 1) ** 2

# Small-scale roughness so the ground does not look sculpted
noise = gaussian_filter(rng.standard_normal(H.shape), 6) * 6
H += noise * 0.6
H = gaussian_filter(H, 1.2)
H -= H.min() - 2.0  # lowest ground at 2 m

# ---- Feature points (for flags and feedback) --------------------------------
def height_at(x, z):
    i = int(round(z / SIZE * (RES - 1)))
    j = int(round(x / SIZE * (RES - 1)))
    return float(H[i, j])


def crest_point(tt):
    x = KW[0] + tt * (KE[0] - KW[0])
    z = KW[1] + tt * (KE[1] - KW[1])
    # snap to the local high point across the ridge line
    best = None
    nx, nz = -(KE[1] - KW[1]), (KE[0] - KW[0])
    n = np.hypot(nx, nz); nx, nz = nx / n, nz / n
    for o in np.linspace(-15, 15, 31):
        h = height_at(x + nx * o, z + nz * o)
        if best is None or h > best[0]:
            best = (h, x + nx * o, z + nz * o)
    return best[1], best[2]


def local_extreme(x, z, r, fn):
    pts = [(x + dx, z + dz) for dx in np.arange(-r, r + 1, 2) for dz in np.arange(-r, r + 1, 2)]
    return fn(pts, key=lambda p: height_at(*p))


kw_top = local_extreme(KW[0], KW[1], 20, max)
ke_top = local_extreme(KE[0], KE[1], 20, max)

# Saddle: lowest crest point between the knolls
crest_samples = [crest_point(tt) for tt in np.linspace(0.3, 0.85, 56)]
saddle = min(crest_samples, key=lambda p: height_at(*p))
ridge_pt = crest_point(0.3)

# Valley: lowest point across the channel at a set distance down it
vz = 185.0
valley = min([(x, vz) for x in np.arange(220, 310, 1.0)], key=lambda p: height_at(*p))

features = [
    # id, name, x, z, radius, flag colour (or None for distractor zones)
    dict(id="knoll", name="Knoll", x=kw_top[0], z=kw_top[1], r=18, flag="Blue",
         teach="A knoll is a rounded hilltop: ground falls away in every direction. On the map it is a small closed contour ring."),
    dict(id="ridge", name="Ridgeline", x=ridge_pt[0], z=ridge_pt[1], r=16, flag="Red",
         teach="A ridgeline is a line of high ground. Ground falls away on both sides but stays level or rises along it. Contours form long U or V shapes pointing away from high ground."),
    dict(id="saddle", name="Saddle", x=saddle[0], z=saddle[1], r=16, flag="Yellow",
         teach="A saddle is the dip on a ridge between two high points. Ground rises in two directions (to each knoll) and falls in the other two. Contours look like an hourglass."),
    dict(id="valley", name="Valley", x=valley[0], z=valley[1], r=20, flag="Green",
         teach="A valley is low ground between two spurs or ridges, usually with a watercourse. Contours form U or V shapes pointing uphill, towards higher ground."),
    dict(id="knoll_east", name="Knoll (the eastern one)", x=ke_top[0], z=ke_top[1], r=18, flag=None,
         teach="This is the other knoll on the map. Check which closed contour ring your flag is marked on."),
]
for f in features:
    f["y"] = height_at(f["x"], f["z"])

START = dict(x=95.0, z=55.0)
START["y"] = height_at(START["x"], START["z"])


# ---- Slope check (walkability) ---------------------------------------------
cell = SIZE / (RES - 1)
gz, gx = np.gradient(H, cell)
slope = np.degrees(np.arctan(np.hypot(gx, gz)))

# ---- Vegetation -------------------------------------------------------------
# Woodland density: more trees low down and in the valley, open knoll tops and crest
wood = gaussian_filter(rng.standard_normal(H.shape), 18)
wood = (wood - wood.mean()) / wood.std()
hn = (H - H.min()) / (H.max() - H.min())
wood += 0.9 - 1.6 * hn
for f in features[:4] + features[4:]:
    wood -= 2.2 * np.exp(-((X - f["x"]) ** 2 + (Z - f["z"]) ** 2) / (2 * 22 ** 2))
wood -= 2.5 * np.exp(-((X - START["x"]) ** 2 + (Z - START["z"]) ** 2) / (2 * 25 ** 2))
woodland = wood > 0.35  # boolean mask used for map tint and tree placement

trees = []
for _ in range(9000):
    x, z = rng.uniform(5, SIZE - 5, 2)
    i, j = int(z / SIZE * (RES - 1)), int(x / SIZE * (RES - 1))
    p = 0.85 if woodland[i, j] else 0.025
    if rng.random() < p * 0.55:
        trees.append((round(float(x), 2), round(float(z), 2),
                      round(float(rng.uniform(0.8, 1.35)), 2), int(rng.integers(0, 2))))
# keep a minimum spacing
trees.sort(key=lambda t: (t[0], t[1]))
kept = []
grid = {}
for t in trees:
    key = (int(t[0] // 4), int(t[1] // 4))
    if any(grid.get((key[0] + a, key[1] + b)) for a in (-1, 0, 1) for b in (-1, 0, 1)):
        continue
    grid[key] = True
    kept.append(t)
trees = kept

# ---- Splat (texture weights) -----------------------------------------------
# layers: 0 lush grass, 1 dry grass, 2 forest floor, 3 rocky dirt
SPLAT_RES = 512
from scipy.ndimage import zoom
sl = zoom(slope, SPLAT_RES / RES, order=1)
hh = zoom(hn, SPLAT_RES / RES, order=1)
wd = zoom(gaussian_filter(woodland.astype(float), 3), SPLAT_RES / RES, order=1)
nz = zoom(gaussian_filter(rng.standard_normal(H.shape), 4), SPLAT_RES / RES, order=1)
w0 = np.clip(1.0 - hh * 0.8 + nz * 0.6, 0, None)
w1 = np.clip(hh * 1.2 - 0.2 + -nz * 0.6, 0, None)
w2 = np.clip(wd * 1.6, 0, None)
w3 = np.clip((sl - 22) / 8, 0, None) * 1.5
W = np.stack([w0, w1, w2, w3], -1) + 1e-4
W /= W.sum(-1, keepdims=True)

if __name__ == "__main__":
    out = "out"
    # Heights: float32 little-endian, row-major [z][x], metres
    H.astype("<f4").tofile(f"{out}/heights_f32.bytes")
    W.astype("<f4").tofile(f"{out}/splat_f32.bytes")
    with open(f"{out}/trees.csv", "w") as fh:
        for t in trees:
            fh.write(f"{t[0]},{t[1]},{t[2]},{t[3]}\n")
    meta = dict(size=SIZE, res=RES, maxHeight=MAX_H, splatRes=SPLAT_RES,
                start=START, features=features)
    json.dump(meta, open(f"{out}/landnav_meta.json", "w"), indent=2)
    np.save(f"{out}/H.npy", H)
    np.save(f"{out}/woodland.npy", woodland)
    print("height range", H.min(), H.max())
    print("max slope", slope.max(), "p99", np.percentile(slope, 99), ">40deg cells", (slope > 40).sum())
    print("trees", len(trees))
    for f in features:
        print(f["id"], round(f["x"], 1), round(f["z"], 1), round(f["y"], 1))
    print("start", START)
    import itertools
    for a, b in itertools.combinations(features, 2):
        print(a["id"], b["id"], round(np.hypot(a["x"] - b["x"], a["z"] - b["z"]), 1))
