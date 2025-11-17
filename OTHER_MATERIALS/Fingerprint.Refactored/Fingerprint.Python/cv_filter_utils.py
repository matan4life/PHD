"""
Module related to filter utilities
"""
import math

import cv2
import numexpr as ne
import numpy as np


def create_gabor_kernel(ridge_period: np.float64, orientation: np.float64) -> np.ndarray:
    def calculate_kernel_size(period: np.float64) -> tuple[int, int]:
        size = int(round(2 * period + 1))
        if size % 2 == 0:
            size += 1
        return size, size

    def calculate_sigma(period: np.float64) -> float:
        return period * 1.5 / ((6 * math.log(10)) ** 0.5)

    kernel = cv2.getGaborKernel(
        calculate_kernel_size(ridge_period),
        calculate_sigma(ridge_period),
        np.pi / 2 - orientation,
        float(ridge_period),
        gamma=1,
        psi=0
    )
    kernel /= kernel.sum()
    kernel -= kernel.mean()
    return kernel


def calculate_image_sobel_gradients(image: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    return cv2.Sobel(image, cv2.CV_32F, 1, 0), cv2.Sobel(image, cv2.CV_32F, 0, 1)


def create_binary_mask(gx2: np.ndarray, gy2: np.ndarray) -> np.ndarray:
    gm = ne.evaluate("sqrt(gx2 + gy2)")
    integral_gm = cv2.boxFilter(gm, -1, (25, 25), normalize=False)
    threshold = integral_gm.max() * 0.2
    return cv2.threshold(integral_gm, threshold, 255, cv2.THRESH_BINARY)[1].astype(np.uint8)


def get_crop_indices(mask: np.ndarray) -> tuple[int, int, int, int]:
    non_zero_points = np.argwhere(mask)
    row_active = non_zero_points.min(axis=0)
    column_active = non_zero_points.max(axis=0)
    return row_active[0], column_active[0] + 1, row_active[-1], column_active[-1] + 1


def get_local_ridge_orientations(gx2: np.ndarray, gy2: np.ndarray, gxy: np.ndarray) -> np.ndarray:
    integral_size = (23, 23)
    igx = cv2.boxFilter(gx2, -1, integral_size, normalize=False)
    igy = cv2.boxFilter(gy2, -1, integral_size, normalize=False)
    igxy = cv2.boxFilter(gxy, -1, integral_size, normalize=False)
    igd = igx - igy
    igm = 2 * igxy
    return (cv2.phase(igd, -igm) + np.pi) / 2


def get_local_ridge_frequency(image: np.ndarray) -> np.float64:
    investigated_region = image[80:160, 80:130]
    smoothed_region = cv2.blur(investigated_region, (5, 5), -1)
    smoothed_columns_sum = np.sum(smoothed_region, 1)
    local_maximums = np.nonzero(
        np.r_[False, smoothed_columns_sum[1:] > smoothed_columns_sum[:-1]]
        & np.r_[smoothed_columns_sum[:-1] >= smoothed_columns_sum[1:], False]
    )[0]
    return np.average(local_maximums[1:] - local_maximums[:-1])


def get_enhanced_image(image: np.ndarray,
                       mask: np.ndarray,
                       orientations: np.ndarray,
                       ridge_period: np.float64) -> np.ndarray:
    angle_steps = 8
    gabor_filter = [create_gabor_kernel(ridge_period, angle) for angle in np.arange(0, np.pi, np.pi / angle_steps)]
    negative_image = 255 - image
    negative_gabor_filtered = np.array([cv2.filter2D(negative_image, cv2.CV_32F, gabor_kernel)
                                        for gabor_kernel
                                        in gabor_filter])
    y_coords, x_coords = np.indices(image.shape)
    gabor_filter_orientation_indices = (np.round(((orientations % np.pi) / np.pi) * angle_steps)
                                        .astype(np.int32) % angle_steps)
    gabor_filtered_image = negative_gabor_filtered[gabor_filter_orientation_indices, y_coords, x_coords]
    return mask & np.clip(gabor_filtered_image, 0, 255).astype(np.uint8)


def get_image_skeletons(enhanced_image: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    _, ridge_lines = cv2.threshold(enhanced_image, 32, 255, cv2.THRESH_BINARY)
    skeleton = cv2.ximgproc.thinning(ridge_lines, thinningType=cv2.ximgproc.THINNING_GUOHALL)
    binary_skeleton = np.where(skeleton != 0, 1, 0).astype(np.uint8)
    return skeleton, binary_skeleton


def crossing_number_kernel():
    return [[1, 2, 4], [128, 0, 8], [64, 32, 16]]


def to_binary_ints(number: int) -> list[int]:
    return [int(digit) for digit in f'{number:08b}']


def chebyshev_kernel() -> list[np.ndarray]:
    return [np.array(to_binary_ints(x))[::-1] for x in range(256)]


def euclidean_kernel():
    sqrt2 = math.sqrt(2)
    return [(-1, -1, sqrt2),
            (0, -1, 1),
            (1, -1, sqrt2),
            (1, 0, 1),
            (1, 1, sqrt2),
            (0, 1, 1),
            (-1, 1, sqrt2),
            (-1, 0, 1)]


def next_ridge_directions(previous_direction: int, directions: np.ndarray) -> list[int]:
    possible_positions = np.argwhere(directions != 0).ravel().tolist()
    if len(possible_positions) > 0 and previous_direction != 8:
        possible_positions.sort(key=lambda direction: 4 - abs(abs(direction - previous_direction) - 4))
        if possible_positions[-1] == (previous_direction + 4) % 8:
            possible_positions = possible_positions[:-1]
    return possible_positions


def angle_abs_difference(a: float, b: float) -> float:
    return math.pi - abs(abs(a - b) - math.pi)


def angle_mean(a: float, b: float) -> float:
    return math.atan2((math.sin(a) + math.sin(b)) / 2, ((math.cos(a) + math.cos(b)) / 2))


def calculate_minutiae(mask: np.ndarray, skeleton: np.ndarray, binary_skeleton: np.ndarray):
    crossed_number = np.array(crossing_number_kernel())
    chebyshev = chebyshev_kernel()
    euclidean = euclidean_kernel()
    crossing_numbers = np.array([np.count_nonzero(n < np.roll(n, -1)) for n in chebyshev]).astype(np.uint8)
    chebyshev_filter_applied = cv2.filter2D(binary_skeleton, -1, crossed_number, borderType=cv2.BORDER_CONSTANT)
    crossing_numbers_transformed = cv2.LUT(chebyshev_filter_applied, crossing_numbers)
    crossing_numbers_transformed[skeleton == 0] = 0
    minutiae = [(x, y, crossing_numbers_transformed[y, x] == 1)
                for y, x
                in zip(*np.where(np.isin(crossing_numbers_transformed, [1, 3])))]
    mask_distance = cv2.distanceTransform(
        cv2.copyMakeBorder(mask, 1, 1, 1, 1, cv2.BORDER_CONSTANT),
        cv2.DIST_C,
        3)[1:-1, 1:-1]
    filtered_minutiae = list(filter(lambda m: mask_distance[m[1], m[0]] > 10, minutiae))
    next_directions = [[next_ridge_directions(previous_distance, neighbour) for previous_distance in range(9)]
                       for neighbour
                       in chebyshev]

    def get_ridge_angle(x, y, direction=8):
        current_x, current_y, current_distance = x, y, direction
        length = 0.0
        while length < 20:
            possible_next_directions = next_directions[chebyshev_filter_applied[current_y, current_x]][
                current_distance]
            if len(possible_next_directions) == 0:
                break
            if any(crossing_numbers_transformed[
                       current_y + euclidean[next_direction][1],
                       current_x + euclidean[next_direction][0]] != 2
                   for next_direction in possible_next_directions):
                break
            current_distance = possible_next_directions[0]
            delta_x, delta_y, delta_distance = euclidean[current_distance]
            current_x += delta_x
            current_y += delta_y
            length += delta_distance
        return math.atan2(-current_y + y, current_x - x) if length >= 10 else None

    def get_angle(x, y, termination):
        if termination:
            return get_ridge_angle(x, y)
        directions = next_directions[chebyshev_filter_applied[y, x]][8]
        if len(directions) != 3:
            return None
        angles = [get_ridge_angle(x, y, direction) for direction in directions]
        if any(a is None for a in angles):
            return None
        angle1, angle2 = min(((angles[i], angles[(i + 1) % 3]) for i in range(3)),
                             key=lambda t: angle_abs_difference(t[0], t[1]))
        return angle_mean(angle1, angle2)

    minutiae_with_angles = map(lambda m: (m[0], m[1], m[2], get_angle(m[0], m[1], m[2])), filtered_minutiae)
    return filter(lambda m: m[3] is not None, minutiae_with_angles)
