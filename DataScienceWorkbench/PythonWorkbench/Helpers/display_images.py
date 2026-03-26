# exports: display_images, display_image, compare_images

import matplotlib.pyplot as _plt
import numpy as _np


def _to_pil(image):
    if image is None:
        return None
    if not isinstance(image, str):
        return image
    import io, base64
    from PIL import Image as _PILImage
    s = image
    if s.startswith('__IMG__:'):
        s = s[7:]
    try:
        b = base64.b64decode(s)
        return _PILImage.open(io.BytesIO(b))
    except Exception:
        return None


def display_images(images, cols=4, figsize=None, title=None, titles=None, cmap=None):
    """Display a grid of images using matplotlib.

    Args:
        images: List of PIL Images or base64 strings (None values are skipped).
        cols: Number of columns in the grid (default 4).
        figsize: Optional (width, height) tuple. Auto-calculated if None.
        title: Optional super-title for the entire figure.
        titles: Optional list of titles for each image.
        cmap: Optional colormap (e.g. 'gray' for grayscale).
    """
    imgs = [_to_pil(img) for img in images]
    imgs = [img for img in imgs if img is not None]
    if not imgs:
        print('No images to display.')
        return
    n = len(imgs)
    rows = (n + cols - 1) // cols
    if figsize is None:
        figsize = (2.5 * cols, 2.5 * rows)
    fig, axes = _plt.subplots(rows, cols, figsize=figsize)
    if rows == 1 and cols == 1:
        axes = [[axes]]
    elif rows == 1:
        axes = [axes]
    elif cols == 1:
        axes = [[ax] for ax in axes]
    for idx in range(rows * cols):
        ax = axes[idx // cols][idx % cols]
        if idx < n:
            ax.imshow(_np.array(imgs[idx]), cmap=cmap)
            if titles and idx < len(titles):
                ax.set_title(titles[idx], fontsize=8)
        ax.axis('off')
    if title:
        _plt.suptitle(title, fontsize=12)
    _plt.tight_layout()
    _plt.show()


def display_image(image, title=None, figsize=None, cmap=None):
    """Display a single image using matplotlib.

    Args:
        image: A PIL Image, base64 string, or None.
        title: Optional title.
        figsize: Optional (width, height) tuple (default (5, 5)).
        cmap: Optional colormap (e.g. 'gray' for grayscale).
    """
    image = _to_pil(image)
    if image is None:
        print('No image to display.')
        return
    if figsize is None:
        figsize = (5, 5)
    fig, ax = _plt.subplots(1, 1, figsize=figsize)
    ax.imshow(_np.array(image), cmap=cmap)
    if title:
        ax.set_title(title, fontsize=10)
    ax.axis('off')
    _plt.tight_layout()
    _plt.show()


def compare_images(images, titles=None, figsize=None, cmap=None):
    """Display images side by side for comparison.

    Args:
        images: List of PIL Images or base64 strings.
        titles: Optional list of titles (one per image).
        figsize: Optional (width, height) tuple.
        cmap: Optional colormap.
    """
    imgs = [_to_pil(img) for img in images]
    imgs = [img for img in imgs if img is not None]
    if not imgs:
        print('No images to compare.')
        return
    n = len(imgs)
    if figsize is None:
        figsize = (4 * n, 4)
    fig, axes = _plt.subplots(1, n, figsize=figsize)
    if n == 1:
        axes = [axes]
    for idx, ax in enumerate(axes):
        ax.imshow(_np.array(imgs[idx]), cmap=cmap)
        if titles and idx < len(titles):
            ax.set_title(titles[idx], fontsize=10)
        ax.axis('off')
    _plt.tight_layout()
    _plt.show()
