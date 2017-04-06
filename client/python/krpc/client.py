from contextlib import contextmanager
import itertools
import threading
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
        self._request_type = self._types.as_type('KRPC.Request')
        self._response_type = self._types.as_type('KRPC.Response')

        # Get the services
        services = self._invoke('KRPC', 'GetServices', [], [], [],
                                self._types.as_type('KRPC.Services')).services

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
            raise RuntimeError('Not connected to stream server')
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
        request = self._build_request(
            service, procedure, args, param_names, param_types, return_type)

        # Send the request
        with self._rpc_connection_lock:
            self._send_request(request)
            response = self._receive_response()

        # Check for an error response
        if response.has_error:
            raise RPCError(response.error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            result = Decoder.decode(response.return_value, return_type)
        return result

    # pylint: disable=unused-argument
    def _build_request(self, service, procedure, args,
                       param_names, param_types, return_type):
        """ Build a KRPC.Request object """

        request = KRPC.Request(service=service, procedure=procedure)

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
            request.arguments.add(position=i, value=Encoder.encode(value, typ))

        return request

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        data = Encoder.encode_delimited(request, self._request_type)
        self._rpc_connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and
            decode it into a KRPC.Response object """

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += self._rpc_connection.partial_receive(1)
                size, _ = Decoder.decode_size_and_position(data)
                break
            except IndexError:
                pass

        # Read and decode the response message
        data = self._rpc_connection.receive(size)
        return Decoder.decode(data, self._response_type)
