import json
import sys
from typing import Iterable

import cv2
import numpy as np
from matplotlib import pyplot as plt

from handlers.crop_image_request_handler import crop_image_handler
from handlers.draw_plots_request_handler import draw_plots_handler
from handlers.enhance_image_request_handler import enhance_image_handler
from handlers.extract_minutiae_request_handler import extract_minutiae_handler
from handlers.extract_triplets_request_handler import extract_triplets
from handlers.file_prepare_request_handler import prepare_file
from middlewares.error_middleware import error_middleware
from middlewares.logging_middleware import logging_middleware
from middlewares.middleware_processor import middleware_processor, TRequestHandler
from models.minutia import Minutia
from models.minutia_triplet import MinutiaeTriplet
from repositories.file_repository import get_file
from utils.image_utils import Minutiae
from utils.scan_utils import is_minutia_in_frame


def setup_request_handlers(request_handlers: list[TRequestHandler]):
    middleware_creator = middleware_processor([logging_middleware, error_middleware])
    handlers = []
    for handler in request_handlers:
        handlers.append(middleware_creator(handler))
    return handlers


def draw_triplets(image: np.ndarray, triplets: list[MinutiaeTriplet]):
    res = cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
    for triplet in triplets:
        color = (255, 0, 0)
        for minutia in triplet.minutiae:
            cv2.circle(res, (int(minutia.x), int(minutia.y)), 3, color, 1, cv2.LINE_AA)
        cv2.line(res, (int(triplet.minutiae[0].x), int(triplet.minutiae[0].y)), (int(triplet.minutiae[1].x), int(triplet.minutiae[1].y)), color, 1, cv2.LINE_AA)
        cv2.line(res, (int(triplet.minutiae[1].x), int(triplet.minutiae[1].y)), (int(triplet.minutiae[2].x), int(triplet.minutiae[2].y)), color, 1, cv2.LINE_AA)
        cv2.line(res, (int(triplet.minutiae[2].x), int(triplet.minutiae[2].y)), (int(triplet.minutiae[0].x), int(triplet.minutiae[0].y)), color, 1, cv2.LINE_AA)
    return res


def main(file_name: str):
    (prepare_file_request_handler,
     crop_image_request_handler,
     enhance_image_request_handler,
     extract_minutiae_request_handler,
     draw_request_handler,
     extract_triplets_handler) = setup_request_handlers(
        [
            prepare_file,
            crop_image_handler,
            enhance_image_handler,
            extract_minutiae_handler,
            draw_plots_handler,
            extract_triplets
        ]
    )
    image_id = prepare_file_request_handler(file_name)
    cropped_id, top_shift, left_shift = crop_image_request_handler(image_id)
    enhanced_id, mask = enhance_image_request_handler(cropped_id)
    skeleton_id, minutiae = extract_minutiae_request_handler((enhanced_id, mask))
    draw_request_handler((image_id, cropped_id, enhanced_id, skeleton_id, minutiae))
    m1 = list(map(lambda item: Minutia(item[0], item[1], item[3]), minutiae))
    return json.dumps([{"x": int(m.x + left_shift), "y": int(m.y + top_shift), "theta": float(m.theta)} for m in m1])


if __name__ == '__main__':
    print(main(sys.argv[1]))
