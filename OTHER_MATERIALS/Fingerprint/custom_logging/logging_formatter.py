import logging

from custom_logging.log_colors import LogColors
from custom_logging.log_levels import LogLevels
from custom_logging.log_text_styles import LogTextStyles


class LoggingFormatter(logging.Formatter):
    @property
    def _formats(self) -> dict:
        return {
            logging.DEBUG: self._get_colored_format(LogColors.GREY),
            logging.INFO: self._get_colored_format(LogColors.BLUE),
            logging.WARNING: self._get_colored_format(LogColors.YELLOW),
            logging.ERROR: self._get_colored_format(LogColors.RED),
            logging.CRITICAL: self._get_colored_format(LogColors.DARK_RED, LogTextStyles.BOLD),
            LogLevels.OK.value: self._get_colored_format(LogColors.GREEN),
        }

    def format(self, record) -> str:
        log_formatter = logging.Formatter(self._formats[record.levelno], style='{')
        return log_formatter.format(record)

    @staticmethod
    def _get_colored_format(color: LogColors, text_style: LogTextStyles = LogTextStyles.DEFAULT):
        log_format = "{levelname:<8s} {asctime} {name} - {message} ({filename}:{lineno:d})"
        return text_style.value + color.value + log_format + LogColors.DEFAULT.value
