import os
import uuid
from typing import Tuple

import cv2

from custom_logging.log_levels import LogLevels
from custom_logging.logger_factory import create_logger
from repositories.file_repository import get_file, add_file, file_exists
from utils.image_utils import get_sobel_gradients, get_mask, crop_image

logger = create_logger(os.path.basename(__file__).replace("_", " ").title().replace(" ", "").split(".")[0])


def crop_image_handler(file_id: uuid.UUID) -> Tuple[uuid.UUID, int, int]:
    file_path = get_file(file_id)
    file_name, file_ext = os.path.splitext(file_path)
    # if file_exists(f'{file_name}_cropped{file_ext}'):
    #     logger.warning('File %s already cropped', file_name)
    #     created_id = add_file(f'{file_name}_cropped{file_ext}')
    #     return created_id
    created_id = add_file(f'{file_name}_cropped{file_ext}')
    image = cv2.imread(file_path, cv2.IMREAD_GRAYSCALE)
    logger.info("Cropping image %s", file_path)
    gx2, gy2, _ = get_sobel_gradients(image)
    mask = get_mask(gx2, gy2)
    cropped, top_shift, left_shift = crop_image(image, mask)
    logger.info("Storing cropped image %s", file_path)
    cv2.imwrite(f'{file_name}_cropped{file_ext}', cropped)
    logger.debug("Cropped image saved to %s", f'{file_name}_cropped{file_ext}')
    return created_id, top_shift, left_shift
