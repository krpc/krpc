from __future__ import annotations
from typing import cast, Any, Callable, Generator, Iterable, Iterator, Optional, Type
from types import TracebackType
from contextlib import contextmanager
import reprlib
import sys
import threading
import warnings
from krpc import limits
from krpc.connection import Connection
from krpc.definitions import CLASS, ENUMERATION, STRUCT, Definition, register_all
from krpc.error import StreamError
from krpc.event import Event
from krpc.types import Types, TypeBase, DefaultArgument, EXCEPTION_TYPES
from krpc.service import (
    create_service,
    extended_stub_classes,
    merge_service,
    service_definitions,
)
from krpc.streammanager import StreamManager
from krpc.stream import Stream
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.streammanager
import krpc.schema.KRPC_pb2 as KRPC
import krpc.services


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
        for key, element in cast(dict, value).items():  # type: ignore[type-arg]
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
        field_names = python_type._fields  # type: ignore[attr-defined]
        build_as: Optional[type] = python_type
        if len(struct.fields) < len(field_names):
            # Fields are only ever appended to a structure, so a server that declares fewer
            # of them than the stubs is an older one. Its values cannot be built as the stub
            # type, so use a plain named tuple of the fields it does declare
            warnings.warn(
                "Reading the struct %s.%s as a plain named tuple, as the server declares "
                "fewer fields for it than this client was generated against"
                % (name, struct.name)
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
    classes = service._classes  # type: ignore[attr-defined]
    for class_name, python_type in classes.items():
        if class_name in extended:
            python_type = _client_class(client, name, class_name, python_type)
        yield Definition(
            CLASS, name, class_name, [], register_class(class_name, python_type)
        )
    enumerations = service._enumerations  # type: ignore[attr-defined]
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
    structs = service._structs  # type: ignore[attr-defined]
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
        rpc_connection: Connection,
        stream_connection: Connection,
        use_pregenerated_stubs: bool = True,
    ) -> None:
        super().__init__()
        self._types = Types()
        self._rpc_connection = rpc_connection
        self._rpc_connection_lock = threading.Lock()
        self._stream_connection = stream_connection
        self._stream_manager = StreamManager(self)
        # The type a server side function returns, and the types of its
        # parameters, by expression object identifier, so that each is
        # introspected over the wire only once
        self._expression_return_types: dict[int, Optional[KRPC.Type]] = {}
        self._expression_parameter_types: dict[int, list[KRPC.Type]] = {}

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

        # Set up stream update thread
        if stream_connection is not None:
            self._stream_thread_stop = threading.Event()
            self._stream_thread = threading.Thread(
                target=krpc.streammanager.update_thread,
                args=(
                    self._stream_manager,
                    stream_connection,
                    self._stream_thread_stop,
                ),
            )
            self._stream_thread.daemon = True
            self._stream_thread.start()
        else:
            self._stream_thread = None

    def close(self) -> None:
        self._rpc_connection.close()
        # Streams end before the join, as the caller can hold a stream's condition, which
        # the update thread then stops trying to take
        self._stream_manager.request_close()
        deferred = self._stream_manager.notify_closed()
        if self._stream_thread is not None:
            self._stream_thread_stop.set()
            # Callbacks run on the update thread, so a client closed from one would join the
            # thread it is running on, which raises. A callback can also wait for a
            # condition the caller holds, so the update thread then ends once the caller
            # releases it.
            if (
                threading.current_thread() is not self._stream_thread
                and not self._stream_manager.holds_a_condition()
            ):
                self._stream_thread.join()
        # An exception from an end callback is raised once the client has closed
        if deferred is not None:
            raise deferred

    def __enter__(self) -> Client:
        return self

    def __exit__(
        self,
        exc_type: Optional[Type[BaseException]],
        exc_value: Optional[BaseException],
        exc_tb: Optional[TracebackType],
    ) -> None:
        self.close()

    def add_stream(self, func: Any, *args: object, **kwargs: object) -> Stream:
        """Add a stream to the server. Given a remote procedure and its arguments,
        the stream repeats that call. Given a server side function, a
        KRPC.Expression object, the stream evaluates it on each update. Raises
        a StreamError without a stream connection, for a property setter, for
        arguments to a function, and for a function with no value. Raises
        TypeError for any other value."""
        if self._stream_connection is None:
            raise StreamError("Not connected to stream server")
        if func == setattr:
            raise StreamError("Cannot stream a property setter")
        if self._is_remote_procedure(func):
            return_type = self._get_return_type(func, *args, **kwargs)
            call = self.get_call(func, *args, **kwargs)
            return krpc.stream.Stream.from_call(self, return_type, call)
        self._check_function(func, "a remote procedure, ")
        if args or kwargs:
            raise StreamError("A server side function is streamed without arguments")
        return_type = self._expression_return_type(func)
        if return_type is None:
            raise StreamError("The function does not evaluate to a value")
        stream = self.krpc.add_function_stream(func)
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

    def add_event(self, function: Any) -> Event:
        """Create an event from a server side function that evaluates to a
        boolean value. Raises a StreamError without a stream connection,
        ValueError for a function that does not evaluate to a boolean value,
        and TypeError for a value that is not a function."""
        if self._stream_connection is None:
            raise StreamError("Not connected to stream server")
        self._check_function(function)
        return cast(Event, self.krpc.add_event(function))

    def _check_function(self, function: Any, procedure: str = "") -> None:
        """Raises TypeError for a value that is not a KRPC.Expression object."""
        expression = self._types.class_type("KRPC", "Expression").python_type
        if isinstance(function, expression):
            return
        raise TypeError(
            "expected %sa KRPC.Expression object, got a value of type %s"
            % (procedure, type(function).__name__)
        )

    def run(self, function: Any, *args: object) -> Any:
        """Run a function on the server, within a single physics tick, and
        return the value it produces. The value is None when the function has
        no result or its result is null. The function is a KRPC.Expression
        object, and the arguments are passed to its parameters. Raises
        TypeError for a value that is not a function, and for an argument its
        parameter cannot take."""
        self._check_function(function)
        return_type = self._expression_return_type(function)
        parameter_types = self._parameter_types(function)
        arguments = self._encode_arguments(parameter_types, list(args))
        data = self.krpc.run_function(function, arguments)
        # A null value has no data
        if data is None or return_type is None:
            return None
        return Decoder.decode(self, data, self._types.as_type(return_type))

    def _expression_return_type(
        self, expression: Any, cache: bool = True
    ) -> Optional[KRPC.Type]:
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
    ) -> list[Optional[bytes]]:
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
    ) -> Optional[bytes]:
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
        except Exception as exc:  # pylint: disable=broad-exception-caught
            raise TypeError(
                "run() argument %d must be a %s, got %s"
                % (position, typ, _ARGUMENT_REPR.repr(value))
            ) from exc

    @property
    def stream_update_condition(self) -> threading.Condition:
        """Condition variable that is notified when
        a stream update message has finished being processed."""
        return self._stream_manager.update_condition

    def wait_for_stream_update(self, timeout: Optional[float] = None) -> None:
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
        func: Callable, *args: object, **kwargs: object  # type: ignore[type-arg]
    ) -> KRPC.ProcedureCall:
        """Convert a remote procedure call to a KRPC.ProcedureCall message"""
        if func == getattr:
            name = args[1]
            builder = getattr(args[0], "_build_call_" + name)
            args = tuple()
            kwargs = {}
        elif func == setattr:
            raise StreamError("Cannot create a call for a property setter")
        else:
            builder = getattr(
                func.__self__,  # type: ignore[attr-defined]
                "_build_call_" + func.__name__,
            )
        return cast(KRPC.ProcedureCall, builder(*args, **kwargs))

    @staticmethod
    def _get_return_type(
        func: Callable,  # type: ignore[type-arg] # pylint: disable=unused-argument
        *args: object,
        **kwargs: object,
    ) -> TypeBase:
        """Get the return type for a remote procedure call"""
        if func == getattr:
            name = args[1]
            return_type_fn = getattr(args[0], "_return_type_" + name)
        elif func == setattr:
            raise StreamError("Cannot get return type for a property setter")
        else:
            return_type_fn = getattr(
                func.__self__,  # type: ignore[attr-defined]
                "_return_type_" + func.__name__,
            )
        return cast(TypeBase, return_type_fn())

    def _invoke(
        self,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
        return_type: Optional[TypeBase],
    ) -> object:
        """Execute an RPC"""

        # Build the request. A request carries exactly one call, so the call is filled in
        # where it belongs
        args = list(args)
        request = KRPC.Request()
        self._encode_call(request.calls.add(), service, procedure, args, param_types)

        # Send the request
        with self._rpc_connection_lock:
            self._rpc_connection.send_message(request)
            response = cast(
                KRPC.Response, self._rpc_connection.receive_message(KRPC.Response)
            )
            self._note_stream_id(response, return_type)
            if (
                service == "KRPC"
                and procedure == "RemoveStream"
                and not response.HasField("error")
                and response.results
                and not response.results[0].HasField("error")
            ):
                self._stream_manager.note_removed(cast(int, args[0]))

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

    def _note_stream_id(
        self, response: KRPC.Response, return_type: Optional[TypeBase]
    ) -> None:
        """Note the id of a stream or event the server returned. Called in the order the
        replies arrive, so the stream manager keeps the latest value and any ending sent
        for the stream before it is registered."""
        if return_type is None or return_type.python_type not in (
            KRPC.Stream,
            KRPC.Event,
        ):
            return
        if response.HasField("error") or not response.results:
            return
        result = response.results[0]
        if result.HasField("error") or result.is_null:
            return
        message = return_type.python_type()
        message.ParseFromString(result.value)
        stream = message if isinstance(message, KRPC.Stream) else message.stream
        self._stream_manager.note_issued(stream.id)

    def _build_call(
        self,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_types: Iterable[TypeBase],
        return_type: Optional[TypeBase],  # pylint: disable=unused-argument
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
            if error.service == "KRPC" and error.name in EXCEPTION_TYPES:
                # Use a built-in exception type if it's in the mapping
                cls = EXCEPTION_TYPES[type_name]
            else:
                cls = getattr(service, type_name)
            return cls(self._error_message(error))
        return RPCError(self._error_message(error))

    @staticmethod
    def _error_message(error: KRPC.Error) -> str:
        msg = error.description
        if error.stack_trace:
            msg += "\nServer stack trace:\n" + error.stack_trace
        return msg
