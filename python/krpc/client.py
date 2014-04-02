from krpc.types import _Types
from krpc.service import BaseService, _create_service, _to_snake_case
from krpc.encoder import _Encoder
from krpc.decoder import _Decoder
from krpc.attributes import _Attributes
import krpc.schema.KRPC


BUFFER_SIZE = 8*1024*1024


class RPCError(RuntimeError):
    """ Error raised when an RPC returns an error response """
    def __init__(self, message):
        super(RPCError, self).__init__(message)


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


class Client(object):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects to are automatically added.
    RPCs can be made using client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, connection):
        self._connection = connection
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

    def _invoke(self, service, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        """ Execute an RPC """

        def encode_argument(i, value):
            typ = param_types[i]
            if type(value) != typ.python_type:
                # Try coercing to the correct type
                try:
                    value = self._types.coerce_to(value, typ)
                except ValueError:
                    raise TypeError('%s.%s() argument %d must be a %s, got a %s' % (service, procedure, i, typ.python_type, type(value)))
            return _Encoder.encode(value, typ)

        if len(args) > len(param_types):
            raise TypeError('%s.%s() takes exactly %d arguments (%d given)' % (service, procedure, len(param_types), len(args)))

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
                raise TypeError('%s.%s() got an unexpected keyword argument \'%s\'' % (service, procedure, key))
            if i < len(args):
                raise TypeError('%s.%s() got multiple values for keyword argument \'%s\'' % (service, procedure, key))
            argument = krpc.schema.KRPC.Argument()
            argument.position = i
            argument.value = encode_argument(i, arg)
            arguments.append(argument)

        # Build the request object
        request = krpc.schema.KRPC.Request()
        request.service = service
        request.procedure = procedure
        request.arguments.extend(arguments)

        # Send the request
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

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        data = _Encoder.encode_delimited(request, self._request_type)
        self._connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and decode it into a KRPC.Response object """
        # FIXME: we might not receive all of the data in one go
        data = self._connection.recv(BUFFER_SIZE)
        return _Decoder.decode_delimited(data, self._response_type)
