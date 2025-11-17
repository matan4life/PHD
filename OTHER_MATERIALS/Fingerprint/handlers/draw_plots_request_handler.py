import math
import os
import uuid

import cv2
import matplotlib.pyplot as plt

from repositories.file_repository import get_file
from utils.image_utils import Minutiae


def draw_minutiae(fingerprint, minutiae, termination_color=(255, 0, 0), bifurcation_color=(0, 0, 255)):
    res = cv2.cvtColor(fingerprint, cv2.COLOR_GRAY2BGR)
    for x, y, t, *d in minutiae:
        color = termination_color if t else bifurcation_color
        if len(d) == 0:
            cv2.drawMarker(res, (x, y), color, cv2.MARKER_CROSS, 8)
        else:
            d = d[0]
            ox = int(round(math.cos(d) * 7))
            oy = int(round(math.sin(d) * 7))
            cv2.circle(res, (x, y), 3, color, 1, cv2.LINE_AA)
            cv2.line(res, (x, y), (x + ox, y - oy), color, 1, cv2.LINE_AA)
    return res


def draw_plots_handler(plots: tuple[uuid.UUID, uuid.UUID, uuid.UUID, uuid.UUID, Minutiae]) -> None:
    original_id, cropped_id, enhanced_id, skeleton_id, minutiae = plots
    file_name = get_file(original_id)
    file_path, file_ext = os.path.splitext(file_name)
    new_file_name = f'{file_path}_plots.png'
    figure, axis = plt.subplots(2, 3)
    figure.set_size_inches(15, 10)
    axis[0, 0].imshow(cv2.imread(get_file(cropped_id), cv2.IMREAD_GRAYSCALE), cmap='gray')
    axis[0, 1].imshow(cv2.imread(get_file(enhanced_id), cv2.IMREAD_GRAYSCALE), cmap='gray')
    axis[0, 2].imshow(cv2.imread(get_file(skeleton_id), cv2.IMREAD_GRAYSCALE), cmap='gray')
    axis[1, 0].imshow(draw_minutiae(cv2.imread(get_file(cropped_id), cv2.IMREAD_GRAYSCALE), minutiae), cmap='gray')
    axis[1, 1].imshow(draw_minutiae(cv2.imread(get_file(enhanced_id), cv2.IMREAD_GRAYSCALE), minutiae), cmap='gray')
    axis[1, 2].imshow(draw_minutiae(cv2.imread(get_file(skeleton_id), cv2.IMREAD_GRAYSCALE), minutiae), cmap='gray')
    for i in range(0, 1):
        for j in range(0, 2):
            axis[i, j].set_axis_off()
    figure.savefig(new_file_name)
