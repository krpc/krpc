from __future__ import annotations

import reprlib
import threading
import warnings
from collections.abc import Callable, Iterable, Iterator
from contextlib import contextmanager
from types import TracebackType
from typing import Any, cast

import krpc.schema.KRPC_pb2 as KRPC
import krpc.services
import krpc.stream
from krpc import limits
from krpc.connection import Connection
from krpc.decoder import Decoder
from krpc.definitions import CLASS, ENUMERATION, STRUCT, Definition, register_all
from krpc.encoder import Encoder
from krpc.error import RPCError, StreamError
from krpc.event import Event
from krpc.functioncompiler import (
    NO_VALUE,
    Parameters,
    compile_function_with_type,
)
from krpc.service import (
    create_service,
    extended_stub_classes,
    merge_service,
    service_definitions,
)
from krpc.session import Session
from krpc.stream import Stream
from krpc.streammanager import StreamManager
from krpc.types import DefaultArgument, TypeBase, Types
from krpc.utils import snake_case


def _client_class(
    client: Client, service_name: str, class_name: str, python_type: type
) -> type:
    """One client's subclass of a pre-generated class, for the members it adds to it.

    The subclass names the class it stands for, which is what coerces an object between two
    connections that each have a subclass of their own. It also carries the client, so a
    static method reached through an object's class uses that object's connection."""
    return type(
        python_type.__name__,
        (python_type,),
        {
            "_client": client,
            "_service_name": service_name,
            "_class_name": class_name,
            "__doc__": python_type.__doc__,
            "__module__": python_type.__module__,
            "__qualname__": python_type.__qualname__,
        },
    )


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


def _stub_definitions(
    client: Client, service_info: KRPC.Service, service: object
) -> Iterator[Definition]:
    """The types of a service whose stubs were generated ahead of time, as records that can be
    registered in a type registry. The stubs already provide the python types, so registering
    them is what lets a dynamically created service use them."""
    name = service_info.name

    def register_class(class_name: str, python_type: type) -> Callable[[Types], None]:
        def register(types: Types) -> None:
            types.register_class_type(name, class_name, python_type)

        return register

    def register_enumeration(
        enum_name: str, python_type: type
    ) -> Callable[[Types], None]:
        def register(types: Types) -> None:
            types.register_enum_type(name, enum_name, python_type)

        return register

    def register_struct(
        struct: KRPC.Struct, python_type: type
    ) -> Callable[[Types], None]:
        field_names = python_type._fields
        build_as: type | None = python_type
        if len(struct.fields) < len(field_names):
            # Fields are only ever appended to a structure, so a server that declares fewer
            # of them than the stubs is an older one. Its values cannot be built as the stub
            # type, so use a plain named tuple of the fields it does declare
            warnings.warn(
                "Reading the struct %s.%s as a plain named tuple, as the server declares "
                "fewer fields for it than this client was generated against"
                % (name, struct.name),
                stacklevel=2,
            )
            field_names = field_names[: len(struct.fields)]
            build_as = None

        def register(types: Types) -> None:
            fields = [
                (field_name, types.as_type(field.type))
                for field_name, field in zip(field_names, struct.fields)
            ]
            types.struct_type(name, struct.name).set_fields(fields, build_as)

        return register

    # A pre-generated class is shared by every client in the process, so a member built for
    # one client cannot go on it. The client's own subclass is what carries the members that
    # only the server's definition declares
    extended = extended_stub_classes(service_info, service)
    classes = service._classes  # pyright: ignore[reportAttributeAccessIssue]
    for class_name, python_type in classes.items():
        if class_name in extended:
            python_type = _client_class(client, name, class_name, python_type)
        yield Definition(
            CLASS, name, class_name, [], register_class(class_name, python_type)
        )
    enumerations = service._enumerations  # pyright: ignore[reportAttributeAccessIssue]
    for enum_name, python_type in enumerations.items():
        yield Definition(
            ENUMERATION,
            name,
            enum_name,
            [],
            register_enumeration(enum_name, python_type),
        )
    # The field types of a structure come from the service definition, so that the same code
    # decodes a structure whether its stubs were generated ahead of time or not. Only the
    # type a value is built as comes from the stubs
    structs = service._structs  # pyright: ignore[reportAttributeAccessIssue]
    for struct in service_info.structs:
        python_type = structs.get(struct.name)
        if python_type is None:
            continue
        yield Definition(
            STRUCT,
            name,
            struct.name,
            [field.type for field in struct.fields],
            register_struct(struct, python_type),
        )


class Client(krpc.services.Client):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects
    to are automatically added. RPCs can be made using
    client.ServiceName.ProcedureName(parameter)
    """

    def __init__(
        self,
        connection: Connection,
        use_pregenerated_stubs: bool = True,
    ) -> None:
        super().__init__()
        self._types = Types()
        self._connection = connection
        self._session = Session(connection, self._remove_stream_request)
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
            self._load_services(use_pregenerated_stubs)
        except BaseException:
            self.close()
            raise

    def _load_services(self, use_pregenerated_stubs: bool) -> None:
        services = cast(
            KRPC.Services,
            self._invoke("KRPC", "GetServices", [], [], self._types.services_type),
        ).services

        # Load services
        definitions = []
        dynamic_services = []
        stub_services = []
        for service_info in services:
            service = None
            if use_pregenerated_stubs:
                service = self._services.get(service_info.name)
            if service is not None:
                definitions.extend(_stub_definitions(self, service_info, service))
                stub_services.append((service_info, service))
            else:
                dynamic_services.append(service_info)
                definitions.extend(service_definitions(service_info))
        # Register the types of every service, whether its stubs were pre-generated or not,
        # before creating any of them: a service's procedures are built from types that any
        # service may define, and a default value cannot be decoded before the definition of
        # the enumeration it belongs to has been registered
        register_all(self._types, definitions)
        # Then dynamically create services for those without pre-generated stubs
        for service_info in dynamic_services:
            setattr(
                self, snake_case(service_info.name), create_service(self, service_info)
            )
        # Add the class members that only the server's definition declares
        for service_info, service in stub_services:
            merge_service(self, service_info, service)

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

    def __enter__(self) -> Client:
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
        compiled with compile on every call. Raises a StreamError for a property
        setter, for arguments to a function, for a function with no value, and
        for a compiled function that takes parameters. Raises TypeError for any
        other value."""
        if func == setattr:
            raise StreamError("Cannot stream a property setter")
        if self._is_remote_procedure(func):
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
        if return_type is None:
            raise StreamError("The function does not evaluate to a value")
        stream = self.krpc.add_function_stream(function)
        return krpc.stream.Stream.from_stream_id(
            self, stream.id, self._types.as_type(return_type)
        )

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
            and hasattr(owner, "_build_call_" + name)
        )

    @contextmanager
    def stream(self, func: Any, *args: object, **kwargs: object) -> Iterator[Stream]:
        """'with' support for add_stream"""
        stream = self.add_stream(func, *args, **kwargs)
        try:
            yield stream
        finally:
            stream.remove()

    def compile(self, func: Callable) -> Any:
        """Compile a Python function or lambda into a server side function, a
        KRPC.Expression object. Each run makes the function's remote procedure
        calls again, and uses its other values as they were when it was
        compiled. A def can take parameters, each with a type annotation, and
        the run method passes its arguments to them. Compiling takes a round
        trip per node, so compile a function once to use it repeatedly. Can be
        used as a decorator."""
        function, return_type, parameters = compile_function_with_type(self, func)
        self.krpc.compile_function(function)
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
        return cast(Event, self.krpc.add_event(function))

    def _check_function(self, function: Any, procedure: str = "") -> None:
        """Raises TypeError for a value that is neither a Python function nor
        a KRPC.Expression object."""
        expression = self._types.class_type("KRPC", "Expression").python_type
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
        data = self.krpc.run_function(function, arguments)
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
        """Convert a remote procedure call to a KRPC.ProcedureCall message"""
        if func == getattr:
            name = cast(str, args[1])
            builder = getattr(args[0], "_build_call_" + name)
            args = tuple()
            kwargs = {}
        elif func == setattr:
            raise StreamError("Cannot create a call for a property setter")
        else:
            builder = getattr(
                func.__self__,  # pyright: ignore[reportFunctionMemberAccess]
                "_build_call_" + func.__name__,
            )
        return cast(KRPC.ProcedureCall, builder(*args, **kwargs))

    @staticmethod
    def _get_return_type(
        func: Callable,
        *args: object,
        **kwargs: object,
    ) -> TypeBase:
        """Get the return type for a remote procedure call"""
        if func == getattr:
            name = cast(str, args[1])
            return_type_fn = getattr(args[0], "_return_type_" + name)
        elif func == setattr:
            raise StreamError("Cannot get return type for a property setter")
        else:
            return_type_fn = getattr(
                func.__self__,  # pyright: ignore[reportFunctionMemberAccess]
                "_return_type_" + func.__name__,
            )
        return cast(TypeBase, return_type_fn())

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
        stream_type = None
        if return_type is not None and return_type.python_type in (
            KRPC.Stream,
            KRPC.Event,
        ):
            stream_type = return_type.python_type
        response = self._session.call(request, stream_type)

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
        value = Decoder.decode(self, result.value, return_type)
        if isinstance(value, KRPC.Event):
            value = Event(self, value)
        return value

    def _remove_stream_request(self, stream_id: int) -> KRPC.Request:
        """A request to remove a stream on the server."""
        request = KRPC.Request()
        self._encode_call(
            request.calls.add(),
            "KRPC",
            "RemoveStream",
            [stream_id],
            [self._types.uint64_type],
        )
        return request

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
            # The service is missing here if it is not one this client knows about, and the
            # type is missing if it is not one the service declares as an exception. Report
            # the error itself, named by its type on the server, so that the failure to build
            # an exception for it does not hide it.
            if not hasattr(self, service_name):
                return RPCError(
                    "%s.%s: %s" % (error.service, type_name, self._error_message(error))
                )
            service = getattr(self, service_name)
            if not hasattr(service, type_name):
                return RPCError(
                    "%s.%s: %s" % (error.service, type_name, self._error_message(error))
                )
            return getattr(service, type_name)(self._error_message(error))
        return RPCError(self._error_message(error))

    @staticmethod
    def _error_message(error: KRPC.Error) -> str:
        msg = error.description
        if error.stack_trace:
            msg += "\nServer stack trace:\n" + error.stack_trace
        return msg
