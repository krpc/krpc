from contextlib import contextmanager
import itertools
import threading
from krpc.error import StreamError
from krpc.event import Event
from krpc.types import Types, DefaultArgument
from krpc.service import create_service
from krpc.streammanager import StreamManager
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.streammanager
import krpc.schema.KRPC_pb2 as KRPC


class Client(object):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects
    to are automatically added. RPCs can be made using
    client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, rpc_connection, stream_connection):
        self._types = Types()
        self._rpc_connection = rpc_connection
        self._rpc_connection_lock = threading.Lock()
        self._stream_connection = stream_connection
        self._stream_manager = StreamManager(self)

        # Get the services
        services = self._invoke('KRPC', 'GetServices', [], [], [],
                                self._types.services_type).services

        # Set up services
        for service in services:
            setattr(self, snake_case(service.name),
                    create_service(self, service))

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

    def close(self):
        self._rpc_connection.close()
        if self._stream_thread is not None:
            self._stream_thread_stop.set()
            self._stream_thread.join()

    def __enter__(self):
        return self

    def __exit__(self, typ, value, traceback):
        self.close()

    def add_stream(self, func, *args, **kwargs):
        """ Add a stream to the server """
        if self._stream_connection is None:
            raise StreamError('Not connected to stream server')
        if func == setattr:
            raise StreamError('Cannot stream a property setter')
        return_type = self._get_return_type(func, *args, **kwargs)
        call = self.get_call(func, *args, **kwargs)
        return krpc.stream.Stream.from_call(self, return_type, call)

    @contextmanager
    def stream(self, func, *args, **kwargs):
        """ 'with' support for add_stream """
        stream = self.add_stream(func, *args, **kwargs)
        try:
            yield stream
        finally:
            stream.remove()

    @staticmethod
    def get_call(func, *args, **kwargs):
        """ Convert a remote procedure call to a KRPC.ProcedureCall message """
        if func == getattr:
            # A property or class property getter
            attr = func(args[0].__class__, args[1])
            return attr.fget._build_call(args[0])
        elif func == setattr:
            # A property setter
            raise StreamError('Cannot create a call for a property setter')
        elif hasattr(func, '__self__'):
            # A method
            return func._build_call(func.__self__, *args, **kwargs)
        else:
            # A class method
            return func._build_call(*args, **kwargs)

    @staticmethod
    def _get_return_type(func, *args,
                         **kwargs):  # pylint: disable=unused-argument
        """ Get the return type for a remote procedure call """
        if func == getattr:
            # A property or class property getter
            attr = func(args[0].__class__, args[1])
            return attr.fget._return_type
        elif func == setattr:
            # A property setter
            raise StreamError('Cannot get return type for a property setter')
        elif hasattr(func, '__self__'):
            # A method
            return func._return_type
        else:
            # A class method
            return func._return_type

    def _invoke(self, service, procedure, args,
                param_names, param_types, return_type):
        """ Execute an RPC """

        # Build the request
        call = self._build_call(service, procedure, args,
                                param_names, param_types, return_type)
        request = KRPC.Request()
        request.calls.extend([call])

        # Send the request
        with self._rpc_connection_lock:
            self._rpc_connection.send_message(request)
            response = self._rpc_connection.receive_message(KRPC.Response)

        # Check for an error response
        if response.HasField('error'):
            raise self._build_error(response.error)

        # Check for an error in the procedure results
        if response.results[0].HasField('error'):
            raise self._build_error(response.results[0].error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            result = Decoder.decode(response.results[0].value, return_type)
            if isinstance(result, KRPC.Event):
                result = Event(self, result)
        return result

    def _build_call(self, service, procedure, args,
                    param_names, param_types, return_type):
                    # pylint: disable=unused-argument
        """ Build a KRPC.ProcedureCall object """

        call = KRPC.ProcedureCall()
        call.service = service
        call.procedure = procedure

        for i, (value, typ) in enumerate(itertools.izip(args, param_types)):
            if isinstance(value, DefaultArgument):
                continue
            if not isinstance(value, typ.python_type):
                try:
                    value = self._types.coerce_to(value, typ)
                except ValueError:
                    raise TypeError(
                        '%s.%s() argument %d must be a %s, got a %s' %
                        (service, procedure, i, typ.python_type, type(value)))
            call.arguments.add(position=i, value=Encoder.encode(value, typ))

        return call

    def _build_error(self, error):
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
            return getattr(service, type_name)(self._error_message(error))
        return RPCError(self._error_message(error))

    @staticmethod
    def _error_message(error):
        msg = error.description
        if error.stack_trace:
            msg += '\nServer stack trace:\n' + error.stack_trace
        return msg
