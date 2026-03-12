# snippet: Display Images
from DotNetData import customers
  import matplotlib.pyplot as plt
  import numpy as np
  from PIL import Image

  # Find the first image column in a dataset
  dataset = customers
  img_col = None
  for col in dataset.columns:
      val = dataset[col].dropna().iloc[0] if len(dataset[col].dropna()) > 0 else None
      if isinstance(val, Image.Image):
          img_col = col
          break

  if img_col is None:
      print('No image columns found in dataset.')
  elif len(dataset[img_col].dropna()) == 0:
      print('Image column found but contains no data.')
  else:
      imgs = dataset[img_col].dropna().reset_index(drop=True)
      n = min(16, len(imgs))
      cols = 4
      rows = (n + cols - 1) // cols

      fig, axes = plt.subplots(rows, cols, figsize=(8, 2 * rows))
      if rows == 1:
          axes = [axes]
      for idx in range(rows * cols):
          ax = axes[idx // cols][idx % cols] if cols > 1 else axes[idx // cols]
          if idx < n:
              img = imgs.iloc[idx]
              ax.imshow(np.array(img))
              ax.set_title(f"Image {idx+1}", fontsize=8)
          ax.axis('off')
      plt.suptitle(img_col + ' images')
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
  