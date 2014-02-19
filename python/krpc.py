import proto.KRPC
import socket
import importlib

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_PORT = 50000
BUFFER_SIZE = 4096
DEBUG_LOGGING = True


class Logger(object):

    @classmethod
    def debug(cls, *args):
        print ' '.join(str(x) for x in args)


def _load_protobuf_service_types(service):
    """ Import the compiled protobuf message types for the given service """
    try:
        importlib.import_module('proto.' + service)
    except ImportError:
        Logger.debug('Failed to load protobuf message types for service', service)
        pass


def _protobuf_type_exists(typ):
    """ Returns true if the given protobuf message type exists,
        where type is of the form PackageName.TypeName """
    package, typ = typ.split(".")
    return package in proto.__dict__ and typ in proto.__dict__[package].__dict__


class BaseService(object):
    """ Abstract base class for all services """

    def __init__(self, client, name):
        self._client = client
        self._name = name

    def _invoke(self, method, parameter=None, return_type=None, **kwargs):
        return self._client._invoke(self._name, method, parameter, return_type, **kwargs)


class KRPCService(BaseService):
    """ Core kRPC service, e.g. for querying for the available services """

    def __init__(self, client):
        super(KRPCService, self).__init__(client, 'KRPC')

    def GetStatus(self):
        """ Get status message from the server, including the version number  """
        return self._invoke('GetStatus', return_type='KRPC.Status')

    def GetServices(self):
        """ Get available services and methods """
        return self._invoke('GetServices', return_type='KRPC.Services')


class Service(BaseService):
    """ A dynamically created service, created using information received from the server """

    def __init__(self, client, service):
        """ Create a service from a KRPC.Service object received from a call to KRPC.GetServices() """
        super(Service, self).__init__(client, service.name)
        self._name = service.name
        # Load the protobuf message types
        _load_protobuf_service_types(self._name)
        # Add all the methods
        for method in service.methods:
            self._add_method(method)

    def _add_method(self, method):
        """ Add a method to this service, from a KRPC.Method object """
        # TODO: make the callback validate the parameter type
        has_parameter_type = method.HasField('parameter_type')
        has_return_type = method.HasField('return_type')

        if has_parameter_type and not _protobuf_type_exists(method.parameter_type):
            Logger.debug('Failed to ad method', self._name, method.name, '; ' +
                         'protobuf type', method.parameter_type, 'not found.')
            return

        if has_return_type and not _protobuf_type_exists(method.return_type):
            Logger.debug('Failed to add method', self._name, method.name, '; ' +
                         'protobuf type', method.return_type, 'not found.')
            return

        if (not has_parameter_type) and (not has_return_type):
            fn = lambda: self._invoke(method.name)
        elif has_parameter_type and (not has_return_type):
            fn = lambda parameter: self._invoke(method.name, parameter=parameter)
        elif (not has_parameter_type) and has_return_type:
            fn = lambda: self._invoke(method.name, return_type=method.return_type)
        else:  # method.has_parameter_type and method.has_return_type:
            fn = lambda parameter: self._invoke(method.name, parameter=parameter, return_type=method.return_type)
        self.__dict__[method.name] = fn


class RPCError(RuntimeError):
    """ Error raised when an RPC returns an error response """
    def __init__(self, message):
        super(RPCError, self).__init__(message)


class Client(object):
    """
    A kRPC client, through which all Remote Procedure Calls are made.
    Services provided by the server that the client connects to are automatically added.
    RPCs can be made using client.ServiceName.ProcedureName(parameter)
    """

    def __init__(self, connection):
        self._connection = connection
        # Set up the main KRPC service
        self.KRPC = KRPCService(self)
        # Use KRPC.GetServices RPC call to discover and add other services that the server supports
        for service in self.KRPC.GetServices().services:
            if service.name != 'KRPC':
                self.__dict__[service.name] = Service(self, service)

    def _invoke(self, service, method, parameter=None, return_type=None, **kwargs):
        """ Execute an RPC """

        # Build the request object
        # TODO: validate the request object, so we catch it here instead of in an error response from the server
        request = proto.KRPC.Request()
        request.service = service
        request.method = method
        if parameter is not None:
            request.request = parameter.SerializeToString()

        # Send the request
        self._send_request(request)
        response = self._receive_response()

        # Check for an error response
        if response.HasField('error'):
            raise RPCError(response.error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            package, message_type = return_type.split('.')
            result = proto.__dict__[package].__dict__[message_type]()
            result.ParseFromString(response.response)
        return result

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        data = request.SerializeToString()
        # TODO: avoid using protobuf internals
        from google.protobuf.internal import encoder
        delimiter = encoder._VarintBytes(len(data))
        self._connection.send(delimiter)
        self._connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and decode it into a KRPC.Response object """
        data = self._connection.recv(BUFFER_SIZE)
        # FIXME: we might not receive all of the data in one go
        # TODO: avoid using protobuf internals
        import google.protobuf.internal.decoder as decoder
        (size, position) = decoder._DecodeVarint(data, 0)
        response = proto.KRPC.Response()
        response.ParseFromString(data[position:position+size])
        return response


def connect(address=DEFAULT_ADDRESS, port=DEFAULT_PORT, name=None):
    """
    Connect to a kRPC server on the specified IP address and port number,
    and optionally give the kRPC server the supplied name to identify the client
    (up to 32 bytes of UTF-8 encoded text)
    """

    # Establish a TCP connection
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect((address, port))

    # Send hello verification message
    connection.send(bytearray([0x48,0x45,0x4C,0x4C,0x4F,0xBA,0xDA,0x55]))

    # Send the name
    name_bytes = bytearray(name, 'utf_8')
    identifier = bytearray(32)
    for i,x in enumerate(name_bytes):
        if i >= 32:
            raise RuntimeError("Name too long")
        identifier[i] = x
    connection.send(identifier)

    return Client(connection)
