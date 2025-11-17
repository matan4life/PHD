import numpy as np

from cv_filter_utils import calculate_image_sobel_gradients, create_binary_mask, get_local_ridge_orientations, \
    get_local_ridge_frequency, get_enhanced_image, get_crop_indices


def crop_image(image: np.ndarray):
    gx, gy = calculate_image_sobel_gradients(image)
    mask = create_binary_mask(gx ** 2, gy ** 2)
    r1, w1, r2, w2 = get_crop_indices(mask)
    return image[r1:w1, r2:w2], r1, r2, w1, w2


def create_enhanced_version(image: np.ndarray):
    gx, gy = calculate_image_sobel_gradients(image)
    gx2, gy2, gxy = gx ** 2, gy ** 2, gx * gy
    mask = create_binary_mask(gx2, gy2)
    ro = get_local_ridge_orientations(gx2, gy2, gxy)
    rf = get_local_ridge_frequency(image)
    return get_enhanced_image(image, mask, ro, rf), mask
