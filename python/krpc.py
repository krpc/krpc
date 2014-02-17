import proto.KRPC
import socket

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_PORT = 50000
BUFFER_SIZE = 4096

class Logger(object):
    @classmethod
    def write(self, *args):
        print ' '.join(str(x) for x in args)

class BaseService(object):
    """ Abstract base class for all services. """
    def __init__(self, client, name):
        self._client = client
        self._name = name

    def _invoke(self, method, parameter = None, return_type = None, **kwargs):
        return self._client._invoke(self._name, method, parameter, return_type, **kwargs)

class KRPCService(BaseService):
    """ Service for server related calls. For example, querying for the available services. """
    def __init__(self, client):
        super(KRPCService, self).__init__(client, 'KRPC')

    def GetServices(self):
        """ Get available services and methods """
        return self._invoke('GetServices', return_type='KRPC.Services')

class Service(BaseService):
    """ A service, created from information received by querying the server """
    def __init__(self, client, service):
        """ Create a service from a KRPC.Service object """
        super(Service, self).__init__(client, service.name)
        self._name = service.name
        for method in service.methods:
            self._add_method(method)

    def _add_method(self, method):
        """ Adds a method to this service, from a KRPC.Method object """
        # TODO: make the callback validate the parameter type
        has_parameter_type = method.HasField('parameter_type')
        has_return_type = method.HasField('return_type')
        if (not has_parameter_type) and (not has_return_type):
            fn = lambda: self._invoke(method.name)
        elif has_parameter_type and (not has_return_type):
            fn = lambda parameter: self._invoke(method.name, parameter=parameter)
        elif (not has_parameter_type) and has_return_type:
            fn = lambda: self._invoke(method.name, return_type=method.return_type)
        else: # method.has_parameter_type and method.has_return_type:
            fn = lambda parameter: self._invoke(method.name, parameter=parameter, return_type=method.return_type)
        self.__dict__[method.name] = fn

class Client(object):
    def __init__(self, connection):
        self._connection = connection
        self.KRPC = KRPCService(self)
        for service in self.KRPC.GetServices().services:
            if service.name != 'KRPC':
                self.__dict__[service.name] = Service(self, service)

    def _invoke(self, service, method, parameter=None, return_type=None, **kwargs):
        request = proto.KRPC.Request()
        request.service = service
        request.method = method
        if parameter != None:
            request.request = parameter.SerializeToString()

        self._send_request(request)
        response = self._receive_response()

        if response.HasField('error'):
            raise RuntimeError(response.error)

        result = None

        if return_type != None:
            package,message_type = return_type.split('.')
            result = proto.__dict__[package].__dict__[message_type]()
            result.ParseFromString(response.response)

        return result

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        from google.protobuf.internal import encoder
        data = request.SerializeToString()
        delimiter = encoder._VarintBytes(len(data))
        self._connection.send(delimiter)
        self._connection.send(data)

    def _receive_response(self):
        import google.protobuf.internal.decoder as decoder
        data = self._connection.recv(BUFFER_SIZE)
        # FIXME: we might not receive all of the data in one go
        (size, position) = decoder._DecodeVarint(data, 0)
        response = proto.KRPC.Response()
        response.ParseFromString(data[position:position+size])
        return response

def connect(address=DEFAULT_ADDRESS, port=DEFAULT_PORT, name=None):
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect((address, port))
    connection.send(bytearray([0x48,0x45,0x4C,0x4C,0x4F,0xBA,0xDA,0x55]))
    name_bytes = bytearray(name, 'utf_8')
    identifier = bytearray(32)
    for i,x in enumerate(name_bytes):
        identifier[i] = x
    # TODO: handle identifiers that are too long
    connection.send(identifier)
    return Client(connection)
