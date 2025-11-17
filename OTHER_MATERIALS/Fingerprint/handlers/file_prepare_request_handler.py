import logging
import os
import shutil
import uuid

from custom_logging.logger_factory import create_logger
from repositories.file_repository import add_file, file_exists

logger = create_logger(os.path.basename(__file__).replace("_", " ").title().replace(" ", "").split(".")[0])


def prepare_file(file_path: str) -> uuid.UUID:
    upload_folder = "upload"
    if not os.path.exists(file_path):
        raise FileNotFoundError(file_path)
    if not os.path.exists(upload_folder):
        logger.log(logging.WARNING, "The upload folder does not exist. Recreating...")
        os.mkdir(upload_folder)
    new_file_path = os.path.join(upload_folder, os.path.basename(file_path))
    if not file_exists(new_file_path):
        add_file(new_file_path)
        shutil.copy(file_path, new_file_path)
    created_id = add_file(new_file_path)
    return created_id
