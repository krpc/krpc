from krpc.types import _Types
from krpc.service import BaseService, _create_service, _to_snake_case
from krpc.encoder import _Encoder
from krpc.decoder import _Decoder
from krpc.attributes import _Attributes
from krpc.error import RPCError
import krpc.stream
import krpc.schema.KRPC
from contextlib import contextmanager
import threading


class KRPCService(BaseService):
    """ Core kRPC service, e.g. for querying for the available services """

    def __init__(self, client):
        super(KRPCService, self).__init__(client, 'KRPC')

    def get_status(self):
        """ Get status message from the server, including the version number  """
        return self._invoke('GetStatus', return_type=self._client._types.as_type('KRPC.Status'))

    def get_services(self):
        """ Get available services and procedures """
        return self._invoke('GetServices', return_type=self._client._types.as_type('KRPC.Services'))

    def add_stream(self, request):
        """ Add a streaming request. Returns its identifier. """
        return self._invoke('AddStream', args=[request],
                            param_names=['request'], param_types=[self._client._types.as_type('KRPC.Request')],
                            return_type=self._client._types.as_type('uint32'))

    def remove_stream(self, stream_id):
        """ Remove a streaming request """
        return self._invoke('RemoveStream', args=[stream_id],
                            param_names=['id'], param_types=[self._client._types.as_type('uint32')])


class Client(object):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects to are automatically added.
    RPCs can be made using client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, rpc_connection, stream_connection):
        self._rpc_connection = rpc_connection
        self._rpc_connection_lock = threading.Lock()
        self._stream_connection = stream_connection
        self._types = _Types()
        self._request_type = self._types.as_type('KRPC.Request')
        self._response_type = self._types.as_type('KRPC.Response')

        # Set up the main KRPC service
        self.krpc = KRPCService(self)

        services = self.krpc.get_services().services

        # Create class types
        for service in services:
            for procedure in service.procedures:
                try:
                    name = _Attributes.get_class_name(procedure.attributes)
                    self._types.as_type('Class(' + service.name + '.' + name + ')')
                except ValueError:
                    pass

        # Set up services
        for service in services:
            if service.name != 'KRPC':
                setattr(self, _to_snake_case(service.name), _create_service(self, service))

        # Set up stream update thread
        self._stream_thread = threading.Thread(target=krpc.stream.update_thread, args=(stream_connection,))
        self._stream_thread.daemon = True
        self._stream_thread.start()

    def close(self):
        self._rpc_connection.close()
        self._stream_connection.close()

    def __enter__(self):
        return self

    def __exit__(self, typ, value, traceback):
        self.close()

    def add_stream(self, func, *args, **kwargs):
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
        if response.HasField('error'):
            raise RPCError(response.error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            result = _Decoder.decode(response.return_value, return_type)
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
            return _Encoder.encode(value, typ)

        if len(args) > len(param_types):
            raise TypeError('%s.%s() takes exactly %d arguments (%d given)' % \
                            (service, procedure, len(param_types), len(args)))

        # Encode positional arguments
        arguments = []
        for i,arg in enumerate(args):
            argument = krpc.schema.KRPC.Argument()
            argument.position = i
            argument.value = encode_argument(i, arg)
            arguments.append(argument)

        # Encode keyword arguments
        for key,arg in kwargs.items():
            try:
                i = param_names.index(key)
            except ValueError:
                raise TypeError('%s.%s() got an unexpected keyword argument \'%s\'' % \
                                (service, procedure, key))
            if i < len(args):
                raise TypeError('%s.%s() got multiple values for keyword argument \'%s\'' % \
                                (service, procedure, key))
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
        data = _Encoder.encode_delimited(request, self._request_type)
        self._rpc_connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and decode it into a KRPC.Response object """

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += self._rpc_connection.partial_receive(1)
                size,position = _Decoder.decode_size_and_position(data)
                break
            except IndexError:
                pass

        # Read and decode the response message
        data = self._rpc_connection.receive(size)
        return _Decoder.decode(data, self._response_type)
