"""Paint a smooth, UV-preserving beard into the staged base-color texture."""

from pathlib import Path

import cv2
import numpy as np


ROOT = Path(r"C:\Users\Gokturk\Documents\GitHub\Deprem")
SOURCE = ROOT / "ArtDirection/CharacterModels/MeshyFamily/Baba/Extracted/Meshy_AI_Cartoon_boy_in_a_blue_0726155643_texture_fbx/Meshy_AI_Cartoon_boy_in_a_blue_0726155643_texture.png"
UV_DATA = ROOT / ".codex_tmp/abdullah_rebuild/beard_uv.npz"
OUTPUT = ROOT / ".codex_tmp/abdullah_rebuild/output/AbdullahEkinci_BaseColor.png"


image = cv2.imread(str(SOURCE), cv2.IMREAD_COLOR)
if image is None:
    raise RuntimeError(f"Could not read {SOURCE}")
height, width = image.shape[:2]
data = np.load(UV_DATA)


def rasterize(triangles: np.ndarray) -> np.ndarray:
    mask = np.zeros((height, width), dtype=np.uint8)
    for triangle in triangles:
        pixels = np.empty((3, 2), dtype=np.int32)
        pixels[:, 0] = np.clip(np.rint(triangle[:, 0] * (width - 1)), 0, width - 1).astype(np.int32)
        pixels[:, 1] = np.clip(np.rint((1.0 - triangle[:, 1]) * (height - 1)), 0, height - 1).astype(np.int32)
        cv2.fillConvexPoly(mask, pixels, 255, lineType=cv2.LINE_AA)
    return mask


full_beard = rasterize(data["full_beard"])

# Close tiny UV-triangle gaps. A solid short beard is more reliable than a
# triangle-defined mouth opening; the mouth is added as clean geometry afterward.
beard_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9))
full_beard = cv2.morphologyEx(full_beard, cv2.MORPH_CLOSE, beard_kernel, iterations=3)
mask = full_beard
mask = cv2.GaussianBlur(mask, (0, 0), sigmaX=1.15, sigmaY=1.15)

# Dark warm-brown beard color with original luminance retained as subtle surface shading.
source_float = image.astype(np.float32) / 255.0
luminance = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0
base_bgr = np.array([10.0, 16.0, 24.0], dtype=np.float32)
shading = (0.78 + luminance[..., None] * 0.42)
beard = np.clip(base_bgr[None, None, :] * shading, 0.0, 255.0)
alpha = (mask.astype(np.float32) / 255.0)[..., None] * 0.98
result = source_float * 255.0 * (1.0 - alpha) + beard * alpha
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
cv2.imwrite(str(OUTPUT), np.clip(result, 0, 255).astype(np.uint8))
print(f"Painted beard texture to {OUTPUT}")
