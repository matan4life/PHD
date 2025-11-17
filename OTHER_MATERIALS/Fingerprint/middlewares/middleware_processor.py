from __future__ import annotations
import uuid
from contextvars import ContextVar
from functools import partial
from typing import TypeVar, Callable, Union, Protocol, Optional

TRequest = TypeVar('TRequest')
TResponse = TypeVar('TResponse')
is_successful = ContextVar('is_successful')
error_severity = ContextVar('error_severity')


class TRequestHandler(Protocol):
    def __call__(self, request: TRequest) -> Union[TResponse, None]: ...


class Middleware(Protocol):
    def __call__(self, request: TRequest, next_delegate: Optional[Middleware, TRequestHandler]) -> Union[TResponse, None]: ...


def middleware_processor(middlewares: list[Middleware]) -> Callable[[TRequestHandler], partial]:
    __middlewares = middlewares

    def create_request_processor(handler: TRequestHandler) -> partial:
        __middlewares.reverse()
        current_func: Union[Middleware, TRequestHandler] = handler
        reduced_func: Union[partial, None] = None
        for middleware in __middlewares:
            reduced_func = partial(middleware, next_delegate=current_func)
            current_func = reduced_func
        return reduced_func

    return create_request_processor
