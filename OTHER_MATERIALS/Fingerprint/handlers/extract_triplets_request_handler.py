from functools import cmp_to_key

import numpy as np

from models.minutia import Minutia
from models.minutia_triplet import MinutiaeTriplet
from utils.angle_utils import clockwise_compare


def extract_triplets(minutiae: list[Minutia]) -> list[MinutiaeTriplet]:
    triplets: list[MinutiaeTriplet] = []
    for minutia in minutiae:
        if has_already_added_minutia(triplets, minutia):
            continue
        ordered_neighbours = list(
            sorted(
                filter(
                    lambda neighbour: neighbour != minutia and not has_already_added_minutia(triplets, neighbour),
                    minutiae
                ),
                key=lambda neighbour: neighbour.distance(minutia)
            )
        )[:4]
        if len(ordered_neighbours) < 2:
            continue
        m1, m2 = ordered_neighbours[:2]
        m1, m2, m3 = order_minutiae_clockwise([m1, m2, minutia])
        triplets.append(MinutiaeTriplet(m1, m2, m3))
    return triplets


def has_already_added_minutia(triplets: list[MinutiaeTriplet], minutia: Minutia) -> bool:
    return any(map(lambda triplet: triplet.contains_minutia(minutia), triplets))


def order_minutiae_clockwise(minutiae: list[Minutia]) -> list[Minutia]:
    x_center, y_center = np.array(list(map(lambda m: np.array([m.x, m.y]), minutiae))).mean(axis=0)
    return list(sorted(minutiae, key=cmp_to_key(
        lambda m1, m2: clockwise_compare(m1.x, m1.y, m2.x, m2.y, x_center, y_center))))
