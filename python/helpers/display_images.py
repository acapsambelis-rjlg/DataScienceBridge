import matplotlib.pyplot as plt
import numpy as np


def display_images(images, cols=4, figsize=None, title=None, titles=None, cmap=None):
    """Display a grid of images using matplotlib.

    Args:
        images: List of PIL Images (None values are skipped).
        cols: Number of columns in the grid (default 4).
        figsize: Optional (width, height) tuple. Auto-calculated if None.
        title: Optional super-title for the entire figure.
        titles: Optional list of titles for each image.
        cmap: Optional colormap (e.g. 'gray' for grayscale).
    """
    imgs = [img for img in images if img is not None]
    if not imgs:
        print("No images to display.")
        return

    n = len(imgs)
    rows = (n + cols - 1) // cols
    if figsize is None:
        figsize = (2.5 * cols, 2.5 * rows)

    fig, axes = plt.subplots(rows, cols, figsize=figsize)
    if rows == 1 and cols == 1:
        axes = [[axes]]
    elif rows == 1:
        axes = [axes]
    elif cols == 1:
        axes = [[ax] for ax in axes]

    for idx in range(rows * cols):
        ax = axes[idx // cols][idx % cols]
        if idx < n:
            arr = np.array(imgs[idx])
            ax.imshow(arr, cmap=cmap)
            if titles and idx < len(titles):
                ax.set_title(titles[idx], fontsize=8)
        ax.axis('off')

    if title:
        plt.suptitle(title, fontsize=12)
    plt.tight_layout()
    plt.show()


def display_image(image, title=None, figsize=None, cmap=None):
    """Display a single image using matplotlib.

    Args:
        image: A PIL Image (or None).
        title: Optional title.
        figsize: Optional (width, height) tuple (default (5, 5)).
        cmap: Optional colormap (e.g. 'gray' for grayscale).
    """
    if image is None:
        print("No image to display.")
        return

    if figsize is None:
        figsize = (5, 5)

    fig, ax = plt.subplots(1, 1, figsize=figsize)
    ax.imshow(np.array(image), cmap=cmap)
    if title:
        ax.set_title(title, fontsize=10)
    ax.axis('off')
    plt.tight_layout()
    plt.show()


def compare_images(images, titles=None, figsize=None, cmap=None):
    """Display images side by side for comparison.

    Args:
        images: List of PIL Images.
        titles: Optional list of titles (one per image).
        figsize: Optional (width, height) tuple.
        cmap: Optional colormap.
    """
    imgs = [img for img in images if img is not None]
    if not imgs:
        print("No images to compare.")
        return

    n = len(imgs)
    if figsize is None:
        figsize = (4 * n, 4)

    fig, axes = plt.subplots(1, n, figsize=figsize)
    if n == 1:
        axes = [axes]

    for idx, ax in enumerate(axes):
        ax.imshow(np.array(imgs[idx]), cmap=cmap)
        if titles and idx < len(titles):
            ax.set_title(titles[idx], fontsize=10)
        ax.axis('off')

    plt.tight_layout()
    plt.show()


def image_stats(images, labels=None):
    """Print RGB channel statistics for a list of images.

    Args:
        images: List of PIL Images.
        labels: Optional list of labels for each image.
    """
    for idx, img in enumerate(images):
        if img is None:
            continue
        arr = np.array(img)
        label = labels[idx] if labels and idx < len(labels) else f"Image {idx + 1}"
        if arr.ndim == 2:
            print(f"{label}: grayscale mean={arr.mean():.1f}, std={arr.std():.1f}")
        elif arr.shape[2] >= 3:
            r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]
            print(f"{label}: R={r.mean():.1f} G={g.mean():.1f} B={b.mean():.1f} "
                  f"(std R={r.std():.1f} G={g.std():.1f} B={b.std():.1f})")
