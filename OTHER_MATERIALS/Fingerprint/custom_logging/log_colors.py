from enum import Enum


class LogColors(Enum):
    GREY = "\x1b[90m"
    YELLOW = "\x1b[93m"
    RED = "\x1b[31m"
    DARK_RED = "\x1b[91m"
    GREEN = "\x1b[92m"
    BLUE = "\x1b[96m"
    DEFAULT = "\x1b[0m"
