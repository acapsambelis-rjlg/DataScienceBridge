
from IntelliSEM import images
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image

imgs = list()
for i in range(16):
    imgs.append(images[1].LoadedImage)

n = min(16, len(imgs))
cols = 4
rows = (n + cols - 1) // cols

fig, axes = plt.subplots(rows, cols, figsize=(8, 2 * rows))
if rows == 1:
    axes = [axes]
for idx in range(rows * cols):
    ax = axes[idx // cols][idx % cols] if cols > 1 else axes[idx // cols]
    if idx < n:
        img = imgs[idx]
        ax.imshow(np.array(img))
        ax.set_title(f"Image {idx+1}", fontsize=8)
    ax.axis('off')
plt.suptitle('loaded images')
plt.tight_layout()
plt.show()

# Compute average color across all images
r_avg, g_avg, b_avg = [], [], []
for img in imgs:
    arr = np.array(img)
    r_avg.append(arr[:,:,0].mean())
    g_avg.append(arr[:,:,1].mean())
    b_avg.append(arr[:,:,2].mean())

print(f'Average R: {np.mean(r_avg):.1f}')
print(f'Average G: {np.mean(g_avg):.1f}')
print(f'Average B: {np.mean(b_avg):.1f}')
