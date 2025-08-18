import numpy as np
from cv_filter_utils import calculate_image_sobel_gradients, create_binary_mask, get_local_ridge_orientations, \
    get_local_ridge_frequency, get_enhanced_image, get_crop_indices

def crop_image(image: np.ndarray) -> tuple[np.ndarray, int, int, int, int]:
    """Crop fingerprint to active region. Reusable for dataset/input."""
    gx, gy = calculate_image_sobel_gradients(image)
    mask = create_binary_mask(gx ** 2, gy ** 2)
    r1, w1, r2, w2 = get_crop_indices(mask)
    return image[r1:w1, r2:w2], r1, r2, w1, w2

def create_enhanced_version(image: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Enhance image with Gabor filters. Vectorized and jitted for speedup."""
    gx, gy = calculate_image_sobel_gradients(image)
    gx2, gy2, gxy = gx ** 2, gy ** 2, gx * gy
    mask = create_binary_mask(gx2, gy2)
    ro = get_local_ridge_orientations(gx2, gy2, gxy)
    rf = get_local_ridge_frequency(image)
    return get_enhanced_image(image, mask, ro, rf), mask

def get_centroid(minutiae: List[Tuple[int, int, bool, float]]) -> Tuple[float, float]:
    """Calculate the center of mass (centroid) for minutiae points.
    
    Reuse: For dataset or query images to define center for comparison.
    Optimizations: Uses NumPy for vectorized calculation.
    Publication: Based on .NET GetSquares for consistent minutiae alignment.
    
    Args:
        minutiae: List of (x, y, is_termination, theta).
    
    Returns:
        (center_x, center_y).
    """
    if not minutiae:
        return 0.0, 0.0
    
    points = np.array([(x, y) for x, y, _, _ in minutiae])
    center_x, center_y = np.mean(points, axis=0)
    return float(center_x), float(center_y)