"""Tileable terrain textures and a grass-blade detail texture (all procedurally made,
so the project carries no third-party art)."""
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

rng = np.random.default_rng(11)
N = 512
OUT = "out"


def tnoise(sigma, amp=1.0):
    n = gaussian_filter(rng.standard_normal((N, N)), sigma, mode="wrap")
    return amp * (n - n.mean()) / (n.std() + 1e-9)


def save(arr, name):
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).save(f"{OUT}/{name}")


def blend(base, var, noise):
    base, var = np.array(base, float), np.array(var, float)
    t = (noise[..., None] + 3) / 6
    return base * (1 - t) + var * t


# Lush grass
n = tnoise(1.2) * 0.6 + tnoise(6) * 0.4 + tnoise(30) * 0.5
img = blend((58, 92, 34), (112, 150, 62), n)
save(img, "Tex_GrassLush.png")

# Dry grass (higher, exposed ground)
n = tnoise(1.0) * 0.6 + tnoise(5) * 0.4 + tnoise(25) * 0.6
img = blend((112, 112, 58), (170, 160, 92), n)
save(img, "Tex_GrassDry.png")

# Forest floor: dark earth with leaf-litter specks
n = tnoise(1.5) * 0.5 + tnoise(8) * 0.5
img = blend((52, 40, 26), (104, 82, 52), n)
specks = rng.random((N, N)) > 0.985
img[specks] = (img[specks] * 0.5 + np.array([150, 110, 50]) * 0.5)
img = np.stack([gaussian_filter(img[..., c], 0.6, mode="wrap") for c in range(3)], -1)
save(img, "Tex_ForestFloor.png")

# Rocky dirt (steeper slopes)
n = tnoise(0.8) * 0.5 + tnoise(4) * 0.5 + np.abs(tnoise(14)) * 0.6
img = blend((86, 76, 64), (150, 138, 120), n)
save(img, "Tex_RockDirt.png")

# Grass blades for terrain detail (RGBA, bottom of image = root)
G = 256
a = np.zeros((G, G), float)
rgb = np.zeros((G, G, 3), float)
for _ in range(14):
    x0 = rng.uniform(30, G - 30)
    h = rng.uniform(0.55, 0.98) * G
    lean = rng.uniform(-40, 40)
    w0 = rng.uniform(5, 9)
    for yy in range(int(h)):
        f = yy / h
        cx = x0 + lean * f ** 2
        w = w0 * (1 - f) + 0.6
        y = G - 1 - yy
        xs = np.arange(int(cx - w - 1), int(cx + w + 2))
        xs = xs[(xs >= 0) & (xs < G)]
        cov = np.clip(w - np.abs(xs - cx) + 0.5, 0, 1)
        a[y, xs] = np.maximum(a[y, xs], cov)
        shade = 0.55 + 0.45 * f
        rgb[y, xs] = np.array([95, 150, 60]) * shade
img = np.dstack([rgb, a * 255])
Image.fromarray(img.astype(np.uint8), "RGBA").save(f"{OUT}/Tex_GrassBlade.png")
print("textures done")
