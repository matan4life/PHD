import os
import uuid

import cv2
import numpy as np

from custom_logging.log_levels import LogLevels
from custom_logging.logger_factory import create_logger
from repositories.file_repository import get_file, file_exists, add_file
from utils.image_utils import get_image_skeletons, Minutiae, get_minutiae
logger = create_logger(os.path.basename(__file__).replace("_", " ").title().replace(" ", "").split(".")[0])


def extract_minutiae_handler(input_tuple: tuple[uuid.UUID, np.ndarray]) -> tuple[uuid.UUID, Minutiae]:
    image_id, mask = input_tuple
    file_path = get_file(image_id)
    enhanced_image = cv2.imread(file_path, cv2.IMREAD_GRAYSCALE)
    skeleton, binary_skeleton = get_image_skeletons(enhanced_image)
    minutiae = get_minutiae(mask, skeleton, binary_skeleton)
    new_file_path = file_path.replace("enhanced", "skeleton")
    # if file_exists(new_file_path):
    #     created_id = add_file(new_file_path)
    #     logger.info("Image %s already skeletoned", file_path)
    #     return created_id, minutiae
    created_id = add_file(new_file_path)
    cv2.imwrite(new_file_path, skeleton)
    logger.debug("Skeleton image saved to %s", new_file_path)
    return created_id, minutiae
