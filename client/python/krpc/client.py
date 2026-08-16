from __future__ import annotations
from typing import cast, Callable, Generator, Iterable, Iterator, Optional, Type
from types import TracebackType
from contextlib import contextmanager
import sys
import threading
from krpc.connection import Connection
from krpc.definitions import CLASS, ENUMERATION, Definition, register_all
from krpc.error import StreamError
from krpc.event import Event
from krpc.types import Types, TypeBase, DefaultArgument, EXCEPTION_TYPES
from krpc.service import create_service, service_definitions
from krpc.streammanager import StreamManager
from krpc.stream import Stream
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.streammanager
import krpc.schema.KRPC_pb2 as KRPC
import krpc.services


def _stub_definitions(name: str, service: object) -> Iterator[Definition]:
    """The types of a service whose stubs were generated ahead of time, as records that can be
    registered in a type registry. The stubs already provide the python types, so registering
    them is what lets a dynamically created service use them."""

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

    classes = service._classes  # type: ignore[attr-defined]
    for class_name, python_type in classes.items():
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

        services = cast(
            KRPC.Services,
            self._invoke("KRPC", "GetServices", [], [], [], self._types.services_type),
        ).services

        # Load services
        definitions = []
        dynamic_services = []
        for service_info in services:
            service = None
            if use_pregenerated_stubs:
                service = self._services.get(service_info.name)
            if service is not None:
                definitions.extend(_stub_definitions(service_info.name, service))
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
        if self._stream_thread is not None:
            self._stream_thread_stop.set()
            # Callbacks run on the update thread, so a client closed from one would be
            # joining the thread it is running on, which raises
            if threading.current_thread() is not self._stream_thread:
                self._stream_thread.join()
        # No further updates will arrive, so wake anything waiting for one rather than
        # leaving it blocked for good
        self._stream_manager.notify_closed()

    def __enter__(self) -> Client:
        return self

    def __exit__(
        self,
        exc_type: Optional[Type[BaseException]],
        exc_value: Optional[BaseException],
        exc_tb: Optional[TracebackType],
    ) -> None:
        self.close()

    def add_stream(
        self, func: Callable, *args: object, **kwargs: object  # type: ignore[type-arg]
    ) -> Stream:
        """Add a stream to the server"""
        if self._stream_connection is None:
            raise StreamError("Not connected to stream server")
        if func == setattr:
            raise StreamError("Cannot stream a property setter")
        return_type = self._get_return_type(func, *args, **kwargs)
        call = self.get_call(func, *args, **kwargs)
        return krpc.stream.Stream.from_call(self, return_type, call)

    @contextmanager
    def stream(
        self, func: Callable, *args: object, **kwargs: object  # type: ignore[type-arg]
    ) -> Iterator[Stream]:
        """'with' support for add_stream"""
        stream = self.add_stream(func, *args, **kwargs)
        try:
            yield stream
        finally:
            stream.remove()

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
        param_names: Iterable[str],
        param_types: Iterable[TypeBase],
        return_type: Optional[TypeBase],
    ) -> object:
        """Execute an RPC"""

        # Build the request. A request carries exactly one call, so the call is
        # filled in where it belongs rather than built on its own and copied in
        request = KRPC.Request()
        self._encode_call(request.calls.add(), service, procedure, args, param_types)

        # Send the request
        with self._rpc_connection_lock:
            self._rpc_connection.send_message(request)
            response = cast(
                KRPC.Response, self._rpc_connection.receive_message(KRPC.Response)
            )

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

    def _build_call(
        self,
        service: str,
        procedure: str,
        args: Iterable[object],
        param_names: Iterable[str],  # pylint: disable=unused-argument
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
            # the error itself, named by its type on the server, rather than the failure to
            # build an exception for it, which would say nothing about what went wrong.
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
