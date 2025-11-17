from models.minutia import Minutia
from utils.angle_utils import vector_angle, rotation_angle


class MinutiaeTriplet:
    minutiae: list[Minutia]
    distances: list[tuple[int, int, float]]
    alpha_angles: list[tuple[int, int, float]]
    beta_angles: list[tuple[int, int, float]]
    maximum_distance: float
    medium_distance: float
    minimal_distance: float

    def __init__(self, first_minutia: Minutia, second_minutia: Minutia, third_minutia: Minutia):
        self.minutiae = [first_minutia, second_minutia, third_minutia]
        self.distances = []
        self.alpha_angles = []
        self.beta_angles = []
        self._process_distances()
        self._process_alpha_angles()
        self._process_beta_angles()

    def contains_minutia(self, minutia: Minutia) -> bool:
        return minutia in self.minutiae

    def _process_distances(self):
        for i in range(len(self.minutiae)):
            for j in range(i+1, len(self.minutiae)):
                distance = (i, j, self.minutiae[i].distance(self.minutiae[j]))
                self.distances.append(distance)
        sorted_distances = sorted(map(lambda d: d[2], self.distances))
        self.minimal_distance, self.medium_distance, self.maximum_distance = sorted_distances

    def _process_alpha_angles(self):
        for i in range(len(self.minutiae)):
            for j in range(len(self.minutiae)):
                if i == j:
                    continue
                current_vector_angle = vector_angle(
                    self.minutiae[i].x,
                    self.minutiae[i].y,
                    self.minutiae[j].x,
                    self.minutiae[j].y
                )
                alpha_angle = rotation_angle(current_vector_angle, self.minutiae[i].theta)
                self.alpha_angles.append((i, j, alpha_angle))

    def _process_beta_angles(self):
        for i in range(len(self.minutiae)):
            next_index = i+1 if i+1 != len(self.minutiae) else 0
            beta_angle = rotation_angle(self.minutiae[next_index].theta, self.minutiae[i].theta)
            self.beta_angles.append((i, next_index, beta_angle))
