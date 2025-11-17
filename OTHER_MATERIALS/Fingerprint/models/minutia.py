from __future__ import annotations

import numpy as np

from utils.angle_utils import normalize_angle


class Minutia:
    def __init__(self, x: float, y: float, theta: float):
        self.x = x
        self.y = y
        self.theta = theta

    @property
    def x(self) -> float:
        return self._x

    @x.setter
    def x(self, value: float) -> None:
        self._x = value

    @property
    def y(self) -> float:
        return self._y

    @y.setter
    def y(self, value: float) -> None:
        self._y = value

    @property
    def theta(self) -> float:
        return self._theta

    @theta.setter
    def theta(self, value: float) -> None:
        self._theta = normalize_angle(value, True)

    def __eq__(self, other) -> bool:
        return self.x == other.x and self.y == other.y

    def __str__(self) -> str:
        return f'(x={self.x}, y={self.y}, θ={self.theta})'

    def distance(self, other: Minutia) -> float:
        current_coords = np.array([self.x, self.y])
        other_coords = np.array([other.x, other.y])
        return np.linalg.norm(current_coords - other_coords)
