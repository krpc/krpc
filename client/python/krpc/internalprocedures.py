"""The internal procedures of the KRPC service, which the client library calls to implement
streams, events and server side functions. The generated service leaves them out."""

from __future__ import annotations

import functools
from collections.abc import Iterable
from typing import TYPE_CHECKING, Any, cast

import krpc.schema.KRPC_pb2 as KRPC
from krpc.error import EncodingError

if TYPE_CHECKING:
    from krpc.client import Client
    from krpc.event import Event
    from krpc.types import TypeBase


class InternalProcedures:
    """Calls the internal procedures of the KRPC service for a client"""

    def __init__(self, client: Client) -> None:
        self._client = client
        self._types = client._types

    @functools.cached_property
    def expression_type(self) -> TypeBase:
        """The type of a server side function, a KRPC.Expression object"""
        return self._types.class_type("KRPC", "Expression")

    @functools.cached_property
    def _arguments_type(self) -> TypeBase:
        return self._types.list_type(self._types.nullable(self._types.bytes_type))

    def add_stream(self, call: KRPC.ProcedureCall) -> int:
        """Add a stream of a call, and return its id"""
        result = self._invoke(
            "AddStream",
            [call],
            [self._types.procedure_call_type],
            self._types.stream_type,
        )
        return cast(int, _non_null(result))

    def set_stream_rate(self, stream_id: int, rate: float) -> None:
        self._invoke(
            "SetStreamRate",
            [stream_id, rate],
            [self._types.uint64_type, self._types.float_type],
        )

    def remove_stream(self, stream_id: int) -> None:
        self._invoke("RemoveStream", [stream_id], [self._types.uint64_type])

    def remove_stream_request(self, stream_id: int) -> KRPC.Request:
        """A request to remove a stream, which the session sends itself"""
        request = KRPC.Request()
        self._client._encode_call(
            request.calls.add(),
            "KRPC",
            "RemoveStream",
            [stream_id],
            [self._types.uint64_type],
        )
        return request

    def add_function_stream(self, function: Any) -> int:
        """Add a stream of a server side function, and return its id"""
        result = self._invoke(
            "AddFunctionStream",
            [function],
            [self.expression_type],
            self._types.stream_type,
        )
        return cast(int, _non_null(result))

    def compile_function(self, function: Any) -> None:
        self._invoke("CompileFunction", [function], [self.expression_type])

    def run_function(
        self, function: Any, arguments: list[bytes | None]
    ) -> bytes | None:
        """Run a server side function, and return its encoded value, or None for a null
        value"""
        return cast(
            "bytes | None",
            self._invoke(
                "RunFunction",
                [function, arguments],
                [self.expression_type, self._arguments_type],
                self._types.bytes_type,
            ),
        )

    def add_event(self, function: Any) -> Event:
        """Add an event over a server side function"""
        result = self._invoke(
            "AddEvent", [function], [self.expression_type], self._types.event_type
        )
        return cast("Event", _non_null(result))

    def _invoke(
        self,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
        return_type: TypeBase | None = None,
    ) -> object:
        return self._client._invoke("KRPC", procedure, args, param_types, return_type)


def _non_null(value: object) -> object:
    """A stream id or an event, which cannot be null"""
    if value is None:
        raise EncodingError("A null result for a value that cannot be null")
    return value
