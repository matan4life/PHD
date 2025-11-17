import os
import uuid

import cv2
import numpy as np

from custom_logging.log_levels import LogLevels
from custom_logging.logger_factory import create_logger
from repositories.file_repository import get_file, add_file, file_exists
from utils.image_utils import get_sobel_gradients, get_local_ridge_orientations, get_local_ridge_frequency, get_mask, \
    get_enhanced_image

logger = create_logger(os.path.basename(__file__).replace("_", " ").title().replace(" ", "").split(".")[0])


def enhance_image_handler(image_id: uuid.UUID) -> tuple[uuid.UUID, np.ndarray]:
    file_path = get_file(image_id)
    new_file_path = file_path.replace("cropped", "enhanced")
    image = cv2.imread(file_path, cv2.IMREAD_GRAYSCALE)
    logger.info("Enhancing image %s", file_path)
    gx2, gy2, gxy = get_sobel_gradients(image)
    mask = get_mask(gx2, gy2)
    # if file_exists(new_file_path):
    #     created_id = add_file(new_file_path)
    #     logger.info("Image %s already enhanced", file_path)
    #     return created_id, mask
    created_id = add_file(new_file_path)
    orientations = get_local_ridge_orientations(gx2, gy2, gxy)
    frequency = get_local_ridge_frequency(image)
    enhanced_image = get_enhanced_image(image, mask, orientations, frequency)
    cv2.imwrite(new_file_path, enhanced_image)
    logger.debug("Enhanced image saved to %s", new_file_path)
    return created_id, mask
