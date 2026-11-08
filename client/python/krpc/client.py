from __future__ import annotations

import reprlib
import threading
import warnings
from collections.abc import Callable, Iterable
from types import TracebackType
from typing import TYPE_CHECKING, Any, TypeVar, cast

import krpc.schema.KRPC_pb2 as KRPC
import krpc.stream
from krpc import limits
from krpc.connection import Connection
from krpc.decoder import Decoder
from krpc.definitions import register_all
from krpc.encoder import Encoder
from krpc.error import RPCError, StreamError
from krpc.event import Event
from krpc.functioncompiler import (
    NO_VALUE,
    Parameters,
    compile_function_with_type,
)
from krpc.internalprocedures import InternalProcedures
from krpc.service import create_service, service_definitions
from krpc.session import DEFAULT_STALL_TIMEOUT, Session
from krpc.stream import Stream
from krpc.streammanager import StreamManager
from krpc.types import (
    DefaultArgument,
    EnumerationValueError,
    EventType,
    StreamType,
    TypeBase,
    Types,
)
from krpc.utils import snake_case

_ClientT = TypeVar("_ClientT", bound="Client")


def _type_message(remote_type: Any) -> KRPC.Type:
    """The protocol buffer type message describing a KRPC.Type object."""
    typ = KRPC.Type()
    typ.code = remote_type.code.value
    service = remote_type.service
    if service:
        typ.service = service
        typ.name = remote_type.name
    typ.types.extend([_type_message(t) for t in remote_type.types])
    typ.nullable = remote_type.nullable
    return typ


# Shortens a large argument value in an error
_ARGUMENT_REPR = reprlib.Repr()
_ARGUMENT_REPR.maxother = 80

# The smallest and largest value of each integer type
_INTEGER_RANGES = {
    KRPC.Type.SINT32: (limits.SINT32_MIN, limits.SINT32_MAX),
    KRPC.Type.SINT64: (limits.SINT64_MIN, limits.SINT64_MAX),
    KRPC.Type.UINT32: (0, limits.UINT32_MAX),
    KRPC.Type.UINT64: (0, limits.UINT64_MAX),
}


def _check_argument(value: object, ptype: KRPC.Type) -> None:
    """Raises ValueError for a value the encoder would write without an error,
    but which the type cannot hold: an integer out of its range, or a None at a
    position that is not nullable."""
    if value is None:
        if not ptype.nullable:
            raise ValueError("null value")
        return
    bounds = _INTEGER_RANGES.get(ptype.code)
    if bounds is not None and isinstance(value, int):
        if not bounds[0] <= value <= bounds[1]:
            raise ValueError("integer out of range")
    elif ptype.code in (KRPC.Type.LIST, KRPC.Type.SET):
        for element in cast(Iterable[object], value):
            _check_argument(element, ptype.types[0])
    elif ptype.code == KRPC.Type.TUPLE:
        for element, element_ptype in zip(cast(Iterable[object], value), ptype.types):
            _check_argument(element, element_ptype)
    elif ptype.code == KRPC.Type.DICTIONARY:
        for key, element in cast(dict, value).items():
            _check_argument(key, ptype.types[0])
            _check_argument(element, ptype.types[1])


# The services a client has, declared for type checkers by the generated modules. The client
# builds them at connect, so it needs none of the modules to run
if TYPE_CHECKING:
    from krpc.services import Client as _Services
else:
    _Services = object


class Client(_Services):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects
    to are automatically added. RPCs can be made using
    client.ServiceName.ProcedureName(parameter)
    """

    def __init__(
        self,
        connection: Connection,
        stall_timeout: float | None = DEFAULT_STALL_TIMEOUT,
    ) -> None:
        self._types = Types()
        self._internal = InternalProcedures(self)
        self._connection = connection
        self._session = Session(
            connection, self._internal.remove_stream_request, stall_timeout
        )
        self._stream_manager = StreamManager(self, self._session.dispatch)
        self._session.start(self._stream_manager)
        # The type a server side function returns, and the types of its
        # parameters, by expression object identifier, so that each is
        # introspected over the wire only once
        self._expression_return_types: dict[int, KRPC.Type | None] = {}
        self._expression_parameter_types: dict[int, list[KRPC.Type]] = {}
        # The parameters of a function compiled by compile, which bind a call's
        # arguments to them
        self._function_parameters: dict[int, Parameters] = {}
        # The service definitions the function compiler reads, built on the first
        # compile, and the objects naming a type by their type message. Both cost
        # round trips to build, and every function compiled for this connection
        # shares them
        self._expression_metadata: Any = None
        self._expression_remote_types: dict[bytes, Any] = {}
        try:
            self._load_services()
        except BaseException:
            self.close()
            raise

    def _load_services(self) -> None:
        services = cast(
            KRPC.Services,
            self._invoke("KRPC", "GetServices", [], [], self._types.services_type),
        ).services
        # Register the types of every service before creating any of them: a service's
        # procedures are built from types that any service may define, and a default value
        # cannot be decoded before the definition of the enumeration it belongs to has been
        # registered
        definitions = []
        for service_info in services:
            definitions.extend(service_definitions(service_info))
        register_all(self._types, definitions)
        for service_info in services:
            setattr(
                self, snake_case(service_info.name), create_service(self, service_info)
            )

    def close(self) -> None:
        # The dispatcher thread stops trying to take a condition the caller can hold
        self._stream_manager.request_close()
        self._session.close()
        # Streams end before the join, so a thread waiting on one is woken
        deferred = self._stream_manager.notify_closed(cause=self._session.failure_cause)
        # Callbacks run on the dispatcher thread, so a client closed from one does not
        # join it. A callback can also wait for a condition the caller holds, so the
        # dispatcher thread then ends once the caller releases it.
        self._session.join(not self._stream_manager.holds_a_condition())
        # An exception from an end callback is raised once the client has closed
        if deferred is not None:
            raise deferred

    def __enter__(self: _ClientT) -> _ClientT:
        return self

    def __exit__(
        self,
        exc_type: type[BaseException] | None,
        exc_value: BaseException | None,
        exc_tb: TracebackType | None,
    ) -> None:
        self.close()

    def add_stream(self, func: Any, *args: object, **kwargs: object) -> Stream:
        """Add a stream to the server. Given a remote procedure and its arguments,
        the stream repeats that call. Given a server side function, a
        KRPC.Expression object, the stream evaluates it on each update. The
        function may also be a Python function or lambda taking no arguments,
        compiled with compile on every call. A call or function with no value
        runs on each update for its effects, and the stream's value is None.
        Raises a StreamError for arguments to a function, and for a compiled
        function that takes parameters. Raises TypeError for any other value."""
        if func == setattr or self._is_remote_procedure(func):
            return_type = self._get_return_type(func, *args, **kwargs)
            call = self.get_call(func, *args, **kwargs)
            return krpc.stream.Stream.from_call(self, return_type, call)
        self._check_function(func, "a remote procedure, ")
        if args or kwargs:
            raise StreamError("A server side function is streamed without arguments")
        # Checked before the return type is looked up, which can make remote calls
        if not callable(func):
            self._check_no_parameters(func)
        function, return_type, _ = self._function_and_type(func, streamed=True)
        stream_id = self._internal.add_function_stream(function)
        typ = (
            self._types.no_value_type
            if return_type is None
            else self._types.as_type(return_type)
        )
        return krpc.stream.Stream.from_stream_id(self, stream_id, typ)

    @staticmethod
    def _is_remote_procedure(func: Any) -> bool:
        """Whether a callable is a remote procedure, or getattr naming a property
        of a service or an object."""
        if func == getattr:
            return True
        owner = getattr(func, "__self__", None)
        name = getattr(func, "__name__", None)
        return (
            owner is not None
            and name is not None
            and hasattr(owner, "_build_call__" + name)
        )

    def stream(self, func: Any, *args: object, **kwargs: object) -> Stream:
        """Deprecated. Use add_stream in a with statement."""
        warnings.warn(
            "Client.stream is deprecated: use add_stream in a with statement",
            DeprecationWarning,
            stacklevel=2,
        )
        return self.add_stream(func, *args, **kwargs)

    def compile(self, func: Any) -> Any:
        """Compile a Python function or lambda into a server side function, a
        KRPC.Expression object. Each run makes the function's remote procedure
        calls again, and uses its other values as they were when it was
        compiled. A def can take parameters, each with a type annotation, and
        the run method passes its arguments to them. Compiling takes a round
        trip per node, so compile a function once to use it repeatedly. Can be
        used as a decorator. Given a KRPC.Expression object built with the
        expression API, compiles it on the server and returns it."""
        if isinstance(func, self._internal.expression_type.python_type):
            self._internal.compile_function(func)
            return func
        function, return_type, parameters = compile_function_with_type(self, func)
        self._internal.compile_function(function)
        self._function_parameters[function._object_id] = parameters
        # A type the compiler tracked need not be introspected on the first run
        if return_type is NO_VALUE:
            self._expression_return_types[function._object_id] = None
        elif return_type is not None:
            self._expression_return_types[function._object_id] = return_type
        return function

    def _function_and_type(
        self, function: Any, streamed: bool = False
    ) -> tuple[Any, KRPC.Type | None, Parameters | None]:
        """The server side function for a KRPC.Expression object or a Python
        function, the type of the values it evaluates to, and its parameters.
        The compiler tracks the type of a Python function, so the server is
        asked only for a type the compiler does not know. A function built with
        the expression API has no parameters to bind arguments with."""
        # A Python function is compiled again on each call, as a cached function
        # would keep the values it captured when it was first compiled
        if callable(function):
            function, return_type, parameters = compile_function_with_type(
                self, function, streamed
            )
            if return_type is NO_VALUE:
                return function, None, parameters
            if return_type is not None:
                return function, return_type, parameters
            # A function compiled for one use is not looked up again
            return (
                function,
                self._expression_return_type(function, cache=False),
                parameters,
            )
        return (
            function,
            self._expression_return_type(function),
            self._function_parameters.get(function._object_id),
        )

    def add_event(self, function: Any) -> Event:
        """Create an event from a server side function that evaluates to a
        boolean value. The function may also be a Python function or lambda
        taking no arguments, compiled with compile on every call. Raises a
        StreamError for a compiled function that takes parameters. Raises
        ValueError for a function that does not evaluate to a boolean value, and
        TypeError for a value that is not a function."""
        self._check_function(function)
        if callable(function):
            function, _, _ = compile_function_with_type(self, function, streamed=True)
        self._check_no_parameters(function)
        return self._internal.add_event(function)

    def _check_function(self, function: Any, procedure: str = "") -> None:
        """Raises TypeError for a value that is neither a Python function nor
        a KRPC.Expression object."""
        expression = self._internal.expression_type.python_type
        if callable(function) or isinstance(function, expression):
            return
        raise TypeError(
            "expected %sa KRPC.Expression object, or a Python function or lambda, "
            "got a value of type %s" % (procedure, type(function).__name__)
        )

    def _check_no_parameters(self, function: Any) -> None:
        """Raises a StreamError if a function compiled by compile takes
        parameters, as a stream or an event runs its function with no
        arguments."""
        parameters = self._function_parameters.get(function._object_id)
        if parameters is not None and parameters.ptypes:
            raise StreamError(
                "A function that takes parameters cannot be streamed or used as "
                "an event"
            )

    def run(self, function: Any, *args: object, **kwargs: object) -> Any:
        """Run a function on the server, within a single physics tick, and
        return the value it produces. The value is None when the function has
        no result or its result is null. The function may be given as a
        KRPC.Expression object, or as a Python function or lambda, which is
        compiled using compile on every call. The arguments are passed to the
        function's parameters. A function built with the expression API takes
        positional arguments only. Raises TypeError for a value that is not a
        function, and for an argument its parameter cannot take."""
        self._check_function(function)
        function, return_type, parameters = self._function_and_type(function)
        if parameters is not None:
            values = parameters.bind(args, kwargs)
            parameter_types = parameters.ptypes
        elif kwargs:
            raise TypeError(
                "a function built with the expression API takes positional "
                "arguments only"
            )
        else:
            values = list(args)
            parameter_types = self._parameter_types(function)
        arguments = self._encode_arguments(parameter_types, values)
        data = self._internal.run_function(function, arguments)
        # A null value has no data
        if data is None or return_type is None:
            return None
        return Decoder.decode(self, data, self._types.as_type(return_type))

    def _expression_return_type(
        self, expression: Any, cache: bool = True
    ) -> KRPC.Type | None:
        """Build the protocol buffer type message describing the type of the
        values a server side function evaluates to, by introspecting its
        return type on the server. None for an expression with no value."""
        object_id = expression._object_id
        if object_id in self._expression_return_types:
            return self._expression_return_types[object_id]
        return_type = (
            _type_message(expression.return_type)
            if expression.has_return_type
            else None
        )
        if cache:
            self._expression_return_types[object_id] = return_type
        return return_type

    def _parameter_types(self, expression: Any) -> list[KRPC.Type]:
        """The protocol buffer type messages describing the types of a server
        side function's parameters, introspected on the server."""
        object_id = expression._object_id
        if object_id not in self._expression_parameter_types:
            self._expression_parameter_types[object_id] = [
                _type_message(typ) for typ in expression.parameter_types
            ]
        return self._expression_parameter_types[object_id]

    def _encode_arguments(
        self, parameter_types: list[KRPC.Type], args: list[object]
    ) -> list[bytes | None]:
        """Encode the arguments of a call to a server side function, one per
        parameter. A None is sent as a null argument."""
        if len(args) != len(parameter_types):
            raise TypeError(
                "the function takes %d argument%s, got %d"
                % (
                    len(parameter_types),
                    "" if len(parameter_types) == 1 else "s",
                    len(args),
                )
            )
        return [
            self._encode_argument(position, arg, typ)
            for position, (arg, typ) in enumerate(zip(args, parameter_types), 1)
        ]

    def _encode_argument(
        self, position: int, value: object, ptype: KRPC.Type
    ) -> bytes | None:
        """Encode an argument of a call to a server side function, coerced to
        the type of its parameter as the arguments of a remote procedure call
        are. Raises TypeError naming the argument's position for a value the
        parameter cannot take. A None is sent as a null argument, which the
        server checks."""
        if value is None:
            return None
        typ = self._types.as_type(ptype)
        try:
            value = self._types.coerce_to(value, typ)
            _check_argument(value, ptype)
            return Encoder.encode(value, typ)
        except Exception as exc:
            raise TypeError(
                "run() argument %d must be a %s, got %s"
                % (position, typ, _ARGUMENT_REPR.repr(value))
            ) from exc

    @property
    def stream_update_condition(self) -> threading.Condition:
        """Condition variable that is notified when
        a stream update message has finished being processed."""
        return self._stream_manager.update_condition

    def wait_for_stream_update(self, timeout: float | None = None) -> None:
        """Wait until the next stream update message or a timeout occurs.
        The condition variable must be locked before calling this method.

        When timeout is not None, it should be a floating point number
        specifying the timeout in seconds for the operation."""
        self._stream_manager.wait_for_update(timeout)

    def add_stream_update_callback(self, callback: Callable[[], None]) -> None:
        """Add a callback that is invoked whenever
        a stream update message has finished being processed."""
        self._stream_manager.add_update_callback(callback)

    def remove_stream_update_callback(self, callback: Callable[[], None]) -> None:
        """Remove a stream update callback."""
        self._stream_manager.remove_update_callback(callback)

    @staticmethod
    def get_call(
        func: Callable,
        *args: object,
        **kwargs: object,
    ) -> KRPC.ProcedureCall:
        """Convert a remote procedure call to a KRPC.ProcedureCall message. Pass
        getattr or setattr as the procedure for a property, then the object, the
        property's name and, for setattr, the value."""
        if func == getattr:
            name = cast(str, args[1])
            builder = getattr(args[0], "_build_call__" + name)
            args = tuple()
            kwargs = {}
        elif func == setattr:
            name = cast(str, args[1])
            builder = getattr(args[0], "_build_call_set__" + name)
            args = (args[2],)
            kwargs = {}
        else:
            builder = getattr(
                func.__self__,  # pyright: ignore[reportFunctionMemberAccess]
                "_build_call__" + func.__name__,
            )
        return cast(KRPC.ProcedureCall, builder(*args, **kwargs))

    def _get_return_type(
        self,
        func: Callable,
        *args: object,
        **kwargs: object,
    ) -> TypeBase:
        """Get the return type for a remote procedure call"""
        if func == getattr:
            name = cast(str, args[1])
            return_type_fn = getattr(args[0], "_return_type__" + name)
        elif func == setattr:
            return self._types.no_value_type
        else:
            return_type_fn = getattr(
                func.__self__,  # pyright: ignore[reportFunctionMemberAccess]
                "_return_type__" + func.__name__,
            )
        return_type = cast("TypeBase | None", return_type_fn())
        return self._types.no_value_type if return_type is None else return_type

    def _invoke(
        self,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
        return_type: TypeBase | None,
    ) -> object:
        """Execute an RPC"""

        # Build the request. A request carries exactly one call, so the call is filled in
        # where it belongs
        args = list(args)
        request = KRPC.Request()
        self._encode_call(request.calls.add(), service, procedure, args, param_types)

        # Send the request, and wait for its response. The reader registers a stream
        # or an event the call adds before it reads the stream's first update
        adds_stream = isinstance(return_type, (StreamType, EventType))
        response = self._session.call(request, adds_stream)

        # Check for an error response
        if response.HasField("error"):
            raise self._build_error(response.error)

        # Check for an error in the procedure results
        result = response.results[0]
        if result.HasField("error"):
            raise self._build_error(result.error)

        # Decode the response and return the (optional) result
        if return_type is None or result.is_null:
            return None
        return Decoder.decode(self, result.value, return_type)

    def _build_call(
        self,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
        return_type: TypeBase | None,
    ) -> KRPC.ProcedureCall:
        """Build a KRPC.ProcedureCall object"""

        call = KRPC.ProcedureCall()
        self._encode_call(call, service, procedure, args, param_types)
        return call

    def _encode_call(
        self,
        call: KRPC.ProcedureCall,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
    ) -> None:
        """Fill in a KRPC.ProcedureCall message with a call and its arguments"""

        call.service = service
        call.procedure = procedure
        arguments = call.arguments

        for i, (value, typ) in enumerate(zip(args, param_types)):
            if isinstance(value, DefaultArgument):
                continue
            if value is None:
                # A null argument is signaled out-of-band; the value field is left unset
                arguments.add(position=i, is_null=True)
                continue
            if not isinstance(value, typ.python_type):
                try:
                    value = self._types.coerce_to(value, typ)
                except EnumerationValueError as exc:
                    raise TypeError(
                        "%s.%s() argument %d: %s" % (service, procedure, i, exc)
                    ) from exc
                except ValueError as exc:
                    raise TypeError(
                        "%s.%s() argument %d must be a %s, got a %s"
                        % (service, procedure, i, typ.python_type, type(value))
                    ) from exc
            arguments.add(position=i, value=Encoder.encode(value, typ))

    def _build_error(self, error: KRPC.Error) -> Exception:
        """Build an exception from an error message that
        can be thrown to the calling code"""
        # TODO: modify the stack trace of the thrown exception so it looks like
        #       it came from the local call
        if error.service and error.name:
            service_name = snake_case(error.service)
            type_name = error.name
            # The service is missing if this client does not know it, and the member is an
            # exception class only for an exception the service declares. Report the error
            # itself, named by its type on the server, so that the failure to build an
            # exception for it does not hide it.
            service = getattr(self, service_name, None)
            exception_type = getattr(service, type_name, None)
            if not (
                isinstance(exception_type, type)
                and issubclass(exception_type, Exception)
            ):
                return RPCError(
                    "%s.%s: %s" % (error.service, type_name, self._error_message(error))
                )
            return exception_type(self._error_message(error))
        return RPCError(self._error_message(error))

    @staticmethod
    def _error_message(error: KRPC.Error) -> str:
        msg = error.description
        if error.stack_trace:
            msg += "\nServer stack trace:\n" + error.stack_trace
        return msg
