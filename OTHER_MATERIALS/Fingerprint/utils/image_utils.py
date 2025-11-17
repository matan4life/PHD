import math
from itertools import groupby
from operator import sub, itemgetter
from typing import Union, Tuple

import cv2
import numpy as np

from utils.angle_utils import crossing_number, next_ridge_directions, get_ridge_angle, angle_abs_difference, angle_mean
from utils.filter_utils import get_gabor_kernel
Minutiae = list[tuple[int, int, bool, float]]


def get_sobel_gradients(image: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    gradient_x, gradient_y = cv2.Sobel(image, cv2.CV_32F, 1, 0), cv2.Sobel(image, cv2.CV_32F, 0, 1)
    return gradient_x ** 2, gradient_y ** 2, gradient_x * gradient_y


def get_mask(gradient_x_squared: np.ndarray, gradient_y_squared: np.ndarray) -> np.ndarray:
    gradient_mean = np.sqrt(gradient_x_squared + gradient_y_squared)
    integral_gradient_mean = cv2.boxFilter(gradient_mean, -1, (25, 25), normalize=False)
    threshold = integral_gradient_mean.max() * 0.2
    return cv2.threshold(integral_gradient_mean, threshold, 255, cv2.THRESH_BINARY)[1].astype(np.uint8)


def get_local_ridge_orientations(gradient_x_squared: np.ndarray, gradient_y_squared: np.ndarray,
                                 gradient_x_y: np.ndarray) -> np.ndarray:
    integral_size = (23, 23)
    integral_gradient_x = cv2.boxFilter(gradient_x_squared, -1, integral_size, normalize=False)
    integral_gradient_y = cv2.boxFilter(gradient_y_squared, -1, integral_size, normalize=False)
    integral_gradient_xy = cv2.boxFilter(gradient_x_y, -1, integral_size, normalize=False)
    integral_gradients_diff = integral_gradient_x - integral_gradient_y
    integral_gradients_xy_doubles = 2 * integral_gradient_xy
    return (cv2.phase(integral_gradients_diff, -integral_gradients_xy_doubles) + np.pi) / 2


def get_local_ridge_frequency(image: np.ndarray) -> np.float64:
    investigated_region = image[10:90, 80:130]
    smoothed_region = cv2.blur(investigated_region, (5, 5), -1)
    smoothed_columns_sum = np.sum(smoothed_region, 1)
    local_maximums = np.nonzero(
        np.r_[False, smoothed_columns_sum[1:] > smoothed_columns_sum[:-1]]
        & np.r_[smoothed_columns_sum[:-1] >= smoothed_columns_sum[1:], False]
    )[0]
    return np.average(local_maximums[1:] - local_maximums[:-1])


def get_enhanced_image(image: np.ndarray, mask: np.ndarray, orientations: np.ndarray,
                       ridge_period: np.float64) -> np.ndarray:
    angle_steps = 8
    gabor_filter = [get_gabor_kernel(ridge_period, angle) for angle in np.arange(0, np.pi, np.pi / angle_steps)]
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


def get_minutiae(mask: np.ndarray, skeleton: np.ndarray, binary_skeleton: np.ndarray) -> Minutiae:
    crossed_numbers_kernel = np.array([[1, 2, 4], [128, 0, 8], [64, 32, 16]])
    chebyshev_neighborhood = [np.array([int(d) for d in f'{x:08b}'])[::-1] for x in range(256)]
    crossing_numbers = np.array([crossing_number(neighbor)
                                 for neighbor
                                 in chebyshev_neighborhood]).astype(np.uint8)
    chebyshev_neighborhood_filtered = cv2.filter2D(
        binary_skeleton,
        -1,
        crossed_numbers_kernel,
        borderType=cv2.BORDER_CONSTANT
    )
    crossing_numbers_transformed = cv2.LUT(chebyshev_neighborhood_filtered, crossing_numbers)
    crossing_numbers_transformed[skeleton == 0] = 0
    minutiae = [(x, y, crossing_numbers_transformed[y, x] == 1)
                for y, x
                in zip(*np.where(np.isin(crossing_numbers_transformed, [1, 3])))]
    mask_distance = cv2.distanceTransform(
        cv2.copyMakeBorder(mask, 1, 1, 1, 1, cv2.BORDER_CONSTANT),
        cv2.DIST_C,
        3)[1:-1, 1:-1]
    filtered_minutiae = list(filter(lambda m: mask_distance[m[1], m[0]] > 10, minutiae))
    sqrt2 = math.sqrt(2)
    euclidean_neighborhood = [(-1, -1, sqrt2),
                              (0, -1, 1),
                              (1, -1, sqrt2),
                              (1, 0, 1),
                              (1, 1, sqrt2),
                              (0, 1, 1),
                              (-1, 1, sqrt2),
                              (-1, 0, 1)]
    next_directions = [[next_ridge_directions(previous_distance, neighbour) for previous_distance in range(9)]
                       for neighbour
                       in chebyshev_neighborhood]
    minutiae_list: Minutiae = []
    for x, y, is_termination in filtered_minutiae:
        angle: Union[float, None] = None
        if is_termination:
            angle = get_ridge_angle(x,
                                    y,
                                    next_directions,
                                    chebyshev_neighborhood_filtered,
                                    crossing_numbers_transformed,
                                    euclidean_neighborhood)
        else:
            directions = next_directions[chebyshev_neighborhood_filtered[y, x]][8]
            if len(directions) == 3:
                angles = [get_ridge_angle(x + euclidean_neighborhood[direction][0],
                                          y + euclidean_neighborhood[direction][1],
                                          next_directions,
                                          chebyshev_neighborhood_filtered,
                                          crossing_numbers_transformed,
                                          euclidean_neighborhood,
                                          direction)
                          for direction in directions]
                if all(a is not None for a in angles):
                    angle1, angle2 = min(((angles[i], angles[(i + 1) % 3]) for i in range(3)),
                                         key=lambda t: angle_abs_difference(t[0], t[1]))
                    angle = angle_mean(angle1, angle2)
        if angle is not None:
            minutiae_list.append((x, y, is_termination, angle))
    return minutiae_list


def get_ranges(inp: np.ndarray) -> list[tuple[int, int]]:
    result = []
    for k, g in groupby(enumerate(inp), lambda x: sub(*x)):
        items = list(map(itemgetter(1), g))
        if len(items) > 1:
            result.append((items[0], items[-1]))
        else:
            result.append((items[0], items[0]))
    return result


def get_start_end_indices(index_range: list[tuple[int, int]], shape: int) -> tuple[int, int]:
    if len(index_range) == 0:
        return 0, shape
    if len(index_range) == 1:
        if index_range[0][0] == 0:
            return index_range[0][1] + 1, shape
        else:
            return 0, index_range[0][0]
    return index_range[0][1] + 1, index_range[-1][0]


def crop_image(img: np.ndarray, mask: np.ndarray) -> Tuple[np.ndarray, int, int]:
    zero_horizontal, zero_vertical = get_ranges(np.where(~mask.any(axis=1))[0]), get_ranges(np.where(~mask.any(axis=0))[0])
    row_start, row_end = get_start_end_indices(zero_horizontal, img.shape[0])
    col_start, col_end = get_start_end_indices(zero_vertical, img.shape[1])
    return img[row_start:row_end, col_start:col_end], row_start, col_start
