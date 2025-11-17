import math
from typing import Union

import numpy as np


def crossing_number(neighborhood: np.ndarray) -> int:
    return np.count_nonzero(neighborhood < np.roll(neighborhood, -1))


def next_ridge_directions(previous_direction: int, directions: np.ndarray) -> list[int]:
    possible_positions: list[int] = np.argwhere(directions != 0).ravel().tolist()
    if len(possible_positions) > 0 and previous_direction != 8:
        possible_positions.sort(key=lambda direction: 4 - abs(abs(direction - previous_direction) - 4))
        if possible_positions[-1] == (previous_direction + 4) % 8:
            possible_positions = possible_positions[:-1]
    return possible_positions


def get_ridge_angle(x: int,
                    y: int,
                    next_directions: list[list[list[int]]],
                    chebyshev_neighborhood_filtered: np.ndarray,
                    crossing_numbers_transformed: np.ndarray,
                    euclidean_neighborhood: list[tuple[int, int, float]],
                    distance: int = 8) -> Union[float, None]:
    current_x, current_y, current_distance = x, y, distance
    length = 0.0
    while length < 20:
        possible_next_directions = next_directions[chebyshev_neighborhood_filtered[current_y, current_x]][current_distance]
        if len(possible_next_directions) == 0:
            break
        if any(crossing_numbers_transformed[
                   current_y + euclidean_neighborhood[next_direction][1],
                   current_x + euclidean_neighborhood[next_direction][0]] != 2
               for next_direction in possible_next_directions):
            break
        current_distance = possible_next_directions[0]
        delta_x, delta_y, delta_distance = euclidean_neighborhood[current_distance]
        current_x += delta_x
        current_y += delta_y
        length += delta_distance
    return math.atan2(-current_y+y, current_x-x) if length >= 10 else None


def angle_abs_difference(a: float, b: float) -> float:
    return math.pi - abs(abs(a - b) - math.pi)


def angle_mean(a: float, b: float) -> float:
    return math.atan2((math.sin(a) + math.sin(b)) / 2, ((math.cos(a) + math.cos(b)) / 2))


def normalize_angle(angle: float, is_radian: bool = False) -> float:
    if is_radian:
        angle = math.degrees(angle)
    if 0 <= angle < 360:
        return angle
    return angle % 360


def superpose_angle(alpha: float, beta: float) -> float:
    return min(abs(alpha-beta), 360-abs(alpha-beta))


def rotation_angle(alpha: float, beta: float) -> float:
    return beta - alpha if beta > alpha else beta - alpha + 360


def vector_angle(x1: float, y1: float, x2: float, y2: float) -> float:
    return atan2(x1, y1, x2, y2)


def atan2(x1: float, y1: float, x2: float, y2: float, is_clockwise: bool = False, shift: float = 0) -> float:
    delta_x = x2 - x1
    delta_y = y2 - y1
    atan2_value = math.degrees(math.atan2(delta_y, delta_x))
    angle = shift - atan2_value if is_clockwise else atan2_value - shift
    return normalize_angle(angle)


def clockwise_compare(x1: float, y1: float, x2: float, y2: float, x_center: float, y_center: float) -> int:
    first_angle = atan2(x_center, y_center, x1, y1, True, 90)
    second_angle = atan2(x_center, y_center, x2, y2, True, 90)
    return np.sign(first_angle - second_angle)
