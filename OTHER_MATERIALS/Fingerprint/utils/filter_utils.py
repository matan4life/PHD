import math

import cv2
import numpy as np


def get_gabor_kernel(ridge_period: np.float64, orientation: np.float64) -> np.ndarray:
    def get_gabor_size(period: np.float64) -> tuple[int, int]:
        size = int(round(2 * period + 1))
        if size % 2 == 0:
            size += 1
        return size, size

    def get_gabor_sigma(period: np.float64) -> float:
        return period * 1.5 / ((6 * math.log(10)) ** 0.5)

    kernel = cv2.getGaborKernel(
        get_gabor_size(ridge_period),
        get_gabor_sigma(ridge_period),
        np.pi / 2 - orientation,
        float(ridge_period),
        gamma=1,
        psi=0
    )
    kernel /= kernel.sum()
    kernel -= kernel.mean()
    return kernel
