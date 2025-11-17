import logging
import traceback
import uuid
from contextvars import ContextVar

from custom_logging.logger_factory import create_logger
from middlewares.middleware_processor import is_successful, error_severity


def error_middleware(request, next_delegate):
    try:
        response = next_delegate(request)
        is_successful.set(True)
        if response is not None:
            return response
    except Exception as e:
        logger = create_logger("ErrorHandler")
        logger.log(logging.CRITICAL, "%s", {"trace_id": trace_id, "error": e})
        is_successful.set(False)
        error_severity.set(logging.ERROR)
