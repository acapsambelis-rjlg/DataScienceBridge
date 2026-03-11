# exports: image_stats

import numpy as _np


def image_stats(images, labels=None):
    """Print RGB channel statistics for a list of images.

    Args:
        images: List of PIL Images.
        labels: Optional list of labels for each image.
    """
    for idx, img in enumerate(images):
        if img is None:
            continue
        arr = _np.array(img)
        label = labels[idx] if labels and idx < len(labels) else f'Image {idx + 1}'
        if arr.ndim == 2:
            print(f'{label}: grayscale mean={arr.mean():.1f}, std={arr.std():.1f}')
        elif arr.shape[2] >= 3:
            r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]
            print(f'{label}: R={r.mean():.1f} G={g.mean():.1f} B={b.mean():.1f} '
                  f'(std R={r.std():.1f} G={g.std():.1f} B={b.std():.1f})')
