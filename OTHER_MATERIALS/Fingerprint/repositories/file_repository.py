import json
import os.path
import uuid
from typing import Union

from bidict import bidict

from custom_logging.logger_factory import create_logger

logger = create_logger(os.path.basename(__file__).replace("_", " ").title().replace(" ", "").split(".")[0])
file_base_name = "file_base.json"


def retrieve_file_info() -> bidict[str, str]:
    result = bidict()
    if not os.path.exists(os.path.join("upload", file_base_name)):
        logger.warning("Repository was not established.")
        return result
    with open(os.path.join("upload", file_base_name), "r") as file:
        for key, value in dict(json.load(file)).items():
            result[key] = value
    return result


def save_file_info(result: bidict[str, str]):
    serializable = {key: value for key, value in result.items()}
    with open(os.path.join("upload", file_base_name), "w") as file:
        json.dump(serializable, file)


__repository = retrieve_file_info()


def add_file(file_name: str) -> uuid.UUID:
    if file_exists(file_name):
        # logger.warning("File %s already exists", file_name)
        return uuid.UUID(__repository.inverse[file_name])
    __repository.put(str(uuid.uuid4()), file_name)
    save_file_info(__repository)
    return uuid.UUID(__repository.inverse[file_name])


def file_exists(identifier: str) -> bool:
    return identifier in __repository or identifier in __repository.inverse


def get_file(file_id: uuid.UUID) -> str:
    if not file_exists(str(file_id)):
        logger.error("File %s does not exist", file_id)
        raise ValueError(f'{file_id} is not a valid file id.', file_id)
    return __repository[str(file_id)]


def remove_file(file_id: uuid.UUID):
    if not file_exists(str(file_id)):
        logger.error("File %s does not exist", file_id)
        raise ValueError(f'{file_id} is not a valid file id.', file_id)
    __repository.pop(str(file_id), None)
    save_file_info(__repository)
