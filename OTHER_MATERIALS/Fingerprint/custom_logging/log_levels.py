import logging
from enum import Enum


class LogLevels(Enum):
    OK = logging.WARN - 5
