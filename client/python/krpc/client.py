from contextlib import contextmanager
import itertools
import threading
from krpc.error import StreamError
from krpc.types import Types, DefaultArgument
from krpc.service import create_service
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.stream
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
        self._stream_cache = {}
        self._stream_cache_lock = threading.Lock()

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
                target=krpc.stream.update_thread,
                args=(stream_connection, self._stream_thread_stop,
                      self._stream_cache, self._stream_cache_lock))
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
        if self._stream_connection is None:
            raise StreamError('Not connected to stream server')
        return krpc.stream.add_stream(self, func, *args, **kwargs)

    @contextmanager
    def stream(self, func, *args, **kwargs):
        """ 'with' support """
        stream = self.add_stream(func, *args, **kwargs)
        try:
            yield stream
        finally:
            stream.remove()

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
        if len(error.service) > 0 and len(error.name) > 0:
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
        if len(error.stack_trace) > 0:
            msg += '\nServer stack trace:\n' + error.stack_trace
        return msg
