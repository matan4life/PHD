import json
import logging
import sys
from logging import Logger, getLogger

import seqlog

from custom_logging.log_levels import LogLevels
from custom_logging.logging_formatter import LoggingFormatter


def __add_logging_level(level_name, level_num):
    logging.addLevelName(level_num, level_name)


def create_logger(name: str) -> Logger:
    logger = getLogger(name)
    return logger
