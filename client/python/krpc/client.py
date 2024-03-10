from __future__ import annotations
from typing import cast, Callable, Generator, Iterable, Iterator, Optional, Type
from types import TracebackType
from contextlib import contextmanager
import sys
import threading
from krpc.connection import Connection
from krpc.error import StreamError
from krpc.event import Event
from krpc.types import Types, TypeBase, DefaultArgument, EXCEPTION_TYPES
from krpc.service import create_service
from krpc.streammanager import StreamManager
from krpc.stream import Stream
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.streammanager
import krpc.schema.KRPC_pb2 as KRPC
import krpc.services


class Client(krpc.services.Client):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects
    to are automatically added. RPCs can be made using
    client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, rpc_connection: Connection, stream_connection: Connection,
                 use_pregenerated_stubs: bool = True) -> None:
        super().__init__()
        self._types = Types()
        self._rpc_connection = rpc_connection
        self._rpc_connection_lock = threading.Lock()
        self._stream_connection = stream_connection
        self._stream_manager = StreamManager(self)

        services = cast(KRPC.Services,
                        self._invoke(
                            'KRPC', 'GetServices', [], [], [],
                            self._types.services_type)).services

        # Load services
        dynamic_services = []
        # Load services with pre-generated stubs first
        # so that class/enum types are loaded if a dynamic service needs them
        for service_info in services:
            service = None
            if use_pregenerated_stubs:
                service = self._services.get(service_info.name)
            if service is not None:
                for name, typ in service._classes.items():  # type: ignore[attr-defined]
                    self._types.register_class_type(service_info.name, name, typ)
                for name, typ in service._enumerations.items():  # type: ignore[attr-defined]
                    self._types.register_enum_type(service_info.name, name, typ)
            else:
                dynamic_services.append(service_info)
        # Then dynamically load services for those without pre-generated stubs
        for service_info in dynamic_services:
            # Dynamically create
            setattr(self, snake_case(service_info.name),
                    create_service(self, service_info))

        # Set up stream update thread
        if stream_connection is not None:
            self._stream_thread_stop = threading.Event()
            self._stream_thread = threading.Thread(
                target=krpc.streammanager.update_thread,
                args=(self._stream_manager, stream_connection,
                      self._stream_thread_stop))
            self._stream_thread.daemon = True
            self._stream_thread.start()
        else:
            self._stream_thread = None

    def close(self) -> None:
        self._rpc_connection.close()
        if self._stream_thread is not None:
            self._stream_thread_stop.set()
            self._stream_thread.join()

    def __enter__(self) -> Client:
        return self

    def __exit__(self,
                 exc_type: Optional[Type[BaseException]],
                 exc_value: Optional[BaseException],
                 exc_tb: Optional[TracebackType]) -> None:
        self.close()

    def add_stream(self, func: Callable,  # type: ignore[type-arg]
                   *args: object, **kwargs: object) -> Stream:
        """ Add a stream to the server """
        if self._stream_connection is None:
            raise StreamError('Not connected to stream server')
        if func == setattr:
            raise StreamError('Cannot stream a property setter')
        return_type = self._get_return_type(func, *args, **kwargs)
        call = self.get_call(func, *args, **kwargs)
        return krpc.stream.Stream.from_call(self, return_type, call)

    @contextmanager
    def stream(self, func: Callable,  # type: ignore[type-arg]
               *args: object, **kwargs: object) -> Iterator[Stream]:
        """ 'with' support for add_stream """
        stream = self.add_stream(func, *args, **kwargs)
        try:
            yield stream
        finally:
            stream.remove()

    @property
    def stream_update_condition(self) -> threading.Condition:
        """ Condition variable that is notified when
            a stream update message has finished being processed. """
        return self._stream_manager.update_condition

    def wait_for_stream_update(self, timeout: Optional[float] = None) -> None:
        """ Wait until the next stream update message or a timeout occurs.
            The condition variable must be locked before calling this method.

            When timeout is not None, it should be a floating point number
            specifying the timeout in seconds for the operation. """
        self._stream_manager.wait_for_update(timeout)

    def add_stream_update_callback(self, callback: Callable[[], None]) -> None:
        """ Add a callback that is invoked whenever
            a stream update message has finished being processed. """
        self._stream_manager.add_update_callback(callback)

    def remove_stream_update_callback(self, callback: Callable[[], None]) -> None:
        """ Remove a stream update callback. """
        self._stream_manager.remove_update_callback(callback)

    @staticmethod
    def get_call(func: Callable,  # type: ignore[type-arg]
                 *args: object, **kwargs: object) -> KRPC.ProcedureCall:
        """ Convert a remote procedure call to a KRPC.ProcedureCall message """
        if func == getattr:
            name = args[1]
            builder = getattr(args[0], '_build_call_' + name)
            args = tuple()
            kwargs = {}
        elif func == setattr:
            raise StreamError('Cannot create a call for a property setter')
        else:
            builder = getattr(
                func.__self__,  # type: ignore[attr-defined]
                '_build_call_' + func.__name__
            )
        return cast(KRPC.ProcedureCall, builder(*args, **kwargs))

    @staticmethod
    def _get_return_type(func: Callable,  # type: ignore[type-arg] # pylint: disable=unused-argument
                         *args: object,
                         **kwargs: object) -> TypeBase:
        """ Get the return type for a remote procedure call """
        if func == getattr:
            name = args[1]
            return_type_fn = getattr(args[0], '_return_type_' + name)
        elif func == setattr:
            raise StreamError('Cannot get return type for a property setter')
        else:
            return_type_fn = getattr(
                func.__self__,  # type: ignore[attr-defined]
                '_return_type_' + func.__name__
            )
        return cast(TypeBase, return_type_fn())

    def _invoke(self, service: str, procedure: str, args: Iterable[object],
                param_names: Iterable[str], param_types: Iterable[TypeBase],
                return_type: Optional[TypeBase]) -> object:
        """ Execute an RPC """

        # Build the request
        call = self._build_call(service, procedure, args,
                                param_names, param_types, return_type)
        request = KRPC.Request()
        request.calls.extend([call])

        # Send the request
        with self._rpc_connection_lock:
            self._rpc_connection.send_message(request)
            response = cast(KRPC.Response, self._rpc_connection.receive_message(KRPC.Response))

        # Check for an error response
        if response.HasField('error'):
            raise self._build_error(response.error)

        # Check for an error in the procedure results
        if response.results[0].HasField('error'):
            raise self._build_error(response.results[0].error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            result = Decoder.decode(
                self, response.results[0].value, return_type
            )
            if isinstance(result, KRPC.Event):
                result = Event(self, result)
        return result

    def _build_call(
            self,
            service: str,
            procedure: str,
            args: Iterable[object],
            param_names: Iterable[str],  # pylint: disable=unused-argument
            param_types: Iterable[TypeBase],
            return_type: Optional[TypeBase]  # pylint: disable=unused-argument
    ) -> KRPC.ProcedureCall:
        """ Build a KRPC.ProcedureCall object """

        call = KRPC.ProcedureCall()
        call.service = service
        call.procedure = procedure

        for i, (value, typ) in enumerate(zip(args, param_types)):
            if isinstance(value, DefaultArgument):
                continue
            if not isinstance(value, typ.python_type):
                try:
                    value = self._types.coerce_to(value, typ)
                except ValueError as exc:
                    raise TypeError(
                        '%s.%s() argument %d must be a %s, got a %s' %
                        (service, procedure, i, typ.python_type, type(value))
                    ) from exc
            call.arguments.add(position=i, value=Encoder.encode(value, typ))

        return call

    def _build_error(self, error: KRPC.Error) -> Exception:
        """ Build an exception from an error message that
            can be thrown to the calling code """
        # TODO: modify the stack trace of the thrown exception so it looks like
        #       it came from the local call
        if error.service and error.name:
            service_name = snake_case(error.service)
            type_name = error.name
            if not hasattr(self, service_name):
                raise RuntimeError(
                    'Error building exception; service \'%s\' not found' %
                    service_name)
            service = getattr(self, service_name)
            if not hasattr(service, type_name):
                raise RuntimeError(
                    'Error building exception; type \'%s.%s\' not found' %
                    (service_name, type_name))
            if error.service == 'KRPC' and error.name in EXCEPTION_TYPES:
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
            msg += '\nServer stack trace:\n' + error.stack_trace
        return msg
