import logging
import uuid
from contextvars import ContextVar

from custom_logging.log_levels import LogLevels
from custom_logging.logger_factory import create_logger
from middlewares.middleware_processor import is_successful, error_severity

logger = ContextVar('logger')
logger.set(create_logger("Middleware"))


def logging_middleware(request, next_delegate):
    current_logger = logger.get()
    current_logger.log(logging.INFO, "Request start", request)
    response = next_delegate(request)
    if not is_successful.get():
        current_logger.log(error_severity.get(), "Request finished with error")
        return
    elif response is not None:
        current_logger.log(LogLevels.OK.value, "Request finished", response)
        return response
    else:
        current_logger.log(LogLevels.OK.value, "Request finished")
