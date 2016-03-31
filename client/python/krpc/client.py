from krpc.types import Types, DefaultArgument
from krpc.service import create_service
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.attributes import Attributes
from krpc.utils import snake_case
from krpc.error import RPCError
import krpc.stream
import krpc.schema.KRPC
from contextlib import contextmanager
import threading

class Client(object):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects to are automatically added.
    RPCs can be made using client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, rpc_connection, stream_connection):
        self._types = Types()
        self._rpc_connection = rpc_connection
        self._rpc_connection_lock = threading.Lock()
        self._stream_connection = stream_connection
        self._request_type = self._types.as_type('KRPC.Request')
        self._response_type = self._types.as_type('KRPC.Response')

        # Get the services
        services = self._invoke('KRPC', 'GetServices', return_type=self._types.as_type('KRPC.Services')).services

        # Set up services
        for service in services:
            setattr(self, snake_case(service.name), create_service(self, service))

        # Set up stream update thread
        if stream_connection is not None:
            self._stream_thread_stop = threading.Event()
            self._stream_thread = threading.Thread(target=krpc.stream.update_thread,
                                                   args=(stream_connection,self._stream_thread_stop))
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
        s = self.add_stream(func, *args, **kwargs)
        try:
            yield s
        finally:
            s.remove()

    def _invoke(self, service, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        """ Execute an RPC """

        # Build the request
        request = self._build_request(service, procedure, args, kwargs, param_names, param_types, return_type)

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

    def _build_request(self, service, procedure, args=[], kwargs={},
                       param_names=[], param_types=[], return_type=None):
        """ Build a KRPC.Request object """

        def encode_argument(i, value):
            typ = param_types[i]
            if type(value) != typ.python_type:
                # Try coercing to the correct type
                try:
                    value = self._types.coerce_to(value, typ)
                except ValueError:
                    raise TypeError('%s.%s() argument %d must be a %s, got a %s' % \
                                    (service, procedure, i, typ.python_type, type(value)))
            return Encoder.encode(value, typ)

        if len(args) > len(param_types):
            raise TypeError('%s.%s() takes exactly %d arguments (%d given)' % \
                            (service, procedure, len(param_types), len(args)))

        arguments = []
        nargs = len(args)
        for i,param in enumerate(param_names):
            add = False
            if i < nargs and not isinstance(args[i], DefaultArgument):
                arg = args[i]
                add = True
            elif param in kwargs:
                arg = kwargs[param]
                add = True
            if add:
                argument = krpc.schema.KRPC.Argument()
                argument.position = i
                argument.value = encode_argument(i, arg)
                arguments.append(argument)

        # Build the request object
        request = krpc.schema.KRPC.Request()
        request.service = service
        request.procedure = procedure
        request.arguments.extend(arguments)
        return request

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        data = Encoder.encode_delimited(request, self._request_type)
        self._rpc_connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and decode it into a KRPC.Response object """

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += self._rpc_connection.partial_receive(1)
                size,position = Decoder.decode_size_and_position(data)
                break
            except IndexError:
                pass

        # Read and decode the response message
        data = self._rpc_connection.receive(size)
        return Decoder.decode(data, self._response_type)
