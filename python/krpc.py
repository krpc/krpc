import schema
import os
import glob
import socket
import itertools
try:
    import importlib.import_module as import_module
except ImportError:
    import_module = lambda package: __import__(package, globals(), locals(), [], -1)

# Load all protobuf message types
_modules = glob.glob(os.path.dirname(__file__)+"/schema/*.py")
_modules = filter(lambda f: not os.path.basename(f).startswith('_'), _modules)
_modules = [os.path.basename(f)[:-3] for f in _modules]
for module in _modules:
    import_module('schema.' + module)

# TODO: avoid using internals
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal import decoder as protobuf_decoder

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_PORT = 50000
BUFFER_SIZE = 4096
DEBUG_LOGGING = True


class _Types(object):

    #PROTOBUF_VALUE_TYPES = set(['double', 'float', 'int32', 'int64', 'uint32',
    #                        'uint64', 'bool', 'string', 'bytes'])
    PROTOBUF_VALUE_TYPES = set(['float', 'int32', 'int64', 'bool', 'string'])
    PYTHON_VALUE_TYPES = set([float, int, long, bool, str])

    PYTHON_TO_PROTOBUF_VALUE_TYPE = {
        float: 'float',
        int: 'int32',
        long: 'int64',
        bool: 'bool',
        str: 'string',
        #TODO: complete
    }

    PROTOBUF_TO_PYTHON_VALUE_TYPE = {
        'float': float,
        'int32': int,
        'int64': long,
        'bool': bool,
        'string': str,
        #TODO: complete
    }

    @classmethod
    def is_value_type(cls, typ):
        if type(typ) != type:
            return False
        return typ in cls.PYTHON_VALUE_TYPES

    @classmethod
    def is_message_type(cls, typ):
        import google.protobuf.reflection
        try:
            return typ.__metaclass__ == google.protobuf.reflection.GeneratedProtocolMessageType
        except AttributeError:
            return False

    @classmethod
    def is_valid_type(cls, typ):
        return cls.is_value_type(typ) or cls.is_message_type(typ)

    @classmethod
    def as_python_type(cls, protobuf_type):
        if protobuf_type in cls.PROTOBUF_VALUE_TYPES:
            return cls.PROTOBUF_TO_PYTHON_VALUE_TYPE[protobuf_type]
        if '.' in protobuf_type:
            package, typ = protobuf_type.split('.')
            if package in schema.__dict__ and typ in schema.__dict__[package].__dict__:
                return schema.__dict__[package].__dict__[typ]
        raise TypeError(protobuf_type + ' is not a valid protobuf type')

    @classmethod
    def as_protobuf_type(cls, python_type):
        if cls.is_value_type(python_type):
            return cls.PYTHON_TO_PROTOBUF_VALUE_TYPE[python_type]
        if cls.is_message_type(python_type):
            return python_type.DESCRIPTOR.full_name
        raise TypeError(str(python_type) + ' does not map to a protobuf type')

    @classmethod
    def coerce_to(cls, value, typ):
        """ Coerce a value to the specified type. Raises ValueError if the coercion is not possible. """
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int, long)
        if type(value) not in numeric_types or typ not in numeric_types:
            raise ValueError('Failed to coerce value of type ' + str(type(value)) + ' to type ' + str(typ))
        if typ == float:
            return float(value)
        elif typ == int:
            return int(value)
        else:
            return long(value)


class _Encoder(object):

    @classmethod
    def hello_message(cls, name=None):
        """ Generate a hello message with the given name
            truncated to fit if necessary """
        header = bytearray([0x48,0x45,0x4C,0x4C,0x4F,0xBA,0xDA,0x55])
        identifier = bytearray(32)
        if name is not None:
            name = cls._unicode_truncate(name, 32, 'utf-8')
            name = bytearray(name, 'utf_8')
            for i,x in enumerate(name):
                if i >= 32:
                    raise RuntimeError('Name too long')
                identifier[i] = x
        return header + identifier

    @classmethod
    def _unicode_truncate(cls, string, length, encoding='utf-8'):
        """ Shorten a unicode string so that it's encoding uses at
            most length bytes. """
        encoded = string.encode(encoding=encoding)[:length]
        return encoded.decode(encoding, 'ignore')


    @classmethod
    def encode(cls, x):
        if _Types.is_message_type(type(x)):
            return x.SerializeToString()
        elif _Types.is_value_type(type(x)):
            return cls._encode_value(x)
        else:
            raise RuntimeError ('Cannot encode objects of type ' + str(type(x)))

    @classmethod
    def encode_delimited(cls, x):
        """ Encode a message or value with size information
            (for use in a delimited communication stream) """
        data = cls.encode(x)
        delimiter = protobuf_encoder._VarintBytes(len(data))
        return delimiter + data

    @classmethod
    def _encode_value(cls, value):
        protobuf_type = _Types.as_protobuf_type(type(value))
        encode_fn = _ValueEncoder.__dict__['encode_' + protobuf_type].__func__
        return encode_fn(_ValueEncoder, value)


class _ValueEncoder(object):

    @classmethod
    def encode_float(cls, value):
        data = []
        def write(x):
            data.append(x)
        #TODO: only handles finite values
        encoder = protobuf_encoder.FloatEncoder(1,False,False)
        encoder(write, value)
        return ''.join(data[1:]) # strips the tag value

    @classmethod
    def _encode_varint(cls, value):
        data = []
        def write(x):
            data.append(x)
        protobuf_encoder._VarintEncoder()(write, value)
        return ''.join(data)

    @classmethod
    def encode_int32(cls, value):
        return cls._encode_varint(value)

    @classmethod
    def encode_int64(cls, value):
        return cls._encode_varint(value)

    @classmethod
    def encode_bool(cls, value):
        return cls._encode_varint(value)

    @classmethod
    def encode_string(cls, value):
        data = []
        def write(x):
            data.append(x)
        encoded = value.encode('utf-8')
        protobuf_encoder._VarintEncoder()(write, len(encoded))
        write(encoded)
        return ''.join(data)


class _Decoder(object):

    @classmethod
    def decode(cls, typ, data):
        """ Given a python type, and serialized data, decode the value """
        if _Types.is_message_type(typ):
            return cls._decode_message(typ, data)
        elif _Types.is_value_type(typ):
            return cls._decode_value(typ, data)
        else:
            raise RuntimeError ('Cannot decode type ' + typ)

    @classmethod
    def decode_delimited(cls, typ, data):
        """ Decode a message or value with size information
            (used in a delimited communication stream) """
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return cls.decode(typ, data[position:position+size])

    @classmethod
    def _decode_message(cls, typ, data):
        message = typ()
        message.ParseFromString(data)
        return message

    @classmethod
    def _decode_value(cls, typ, data):
        protobuf_type = _Types.as_protobuf_type(typ)
        decode_fn = _ValueDecoder.__dict__['decode_' + protobuf_type].__func__
        return decode_fn(_ValueDecoder, data)


class _ValueDecoder(object):

    @classmethod
    def decode_varint(cls, data):
        return protobuf_decoder._DecodeVarint(data, 0)[0]

    @classmethod
    def decode_int32(cls, data):
        return int(cls.decode_varint(data))

    @classmethod
    def decode_int64(cls, data):
        return cls.decode_varint(data)

    @classmethod
    def decode_float(cls, data):
        # The following code is taken from google.protobuf.internal.decoder._FloatDecoder
        # Copyright 2008, Google Inc.
        # See protobuf-license.txt, distributed with this file

        # We expect a 32-bit value in little-endian byte order. Bit 1 is the sign
        # bit, bits 2-9 represent the exponent, and bits 10-32 are the significand.
        float_bytes = data[0:4]

        # If this value has all its exponent bits set, then it's non-finite.
        # In Python 2.4, struct.unpack will convert it to a finite 64-bit value.
        # To avoid that, we parse it specially.
        if ((float_bytes[3] in '\x7F\xFF')
            and (float_bytes[2] >= '\x80')):
          # If at least one significand bit is set...
          if float_bytes[0:3] != '\x00\x00\x80':
            return _NAN
          # If sign bit is set...
          if float_bytes[3] == '\xFF':
            return _NEG_INF
          return _POS_INF

        # Note that we expect someone up-stack to catch struct.error and convert
        # it to _DecodeError -- this way we don't have to set up exception-
        # handling blocks every time we parse one value.
        import struct
        return struct.unpack('<f', float_bytes)[0]

        # End of code taken from google.protobuf.internal.decoder._FloatDecoder

    @classmethod
    def decode_bool(cls, data):
        return bool(cls.decode_varint(data))

    @classmethod
    def decode_string(cls, data):
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return unicode(data[position:position+size], 'utf-8')


class Logger(object):

    @classmethod
    def info(cls, *args):
        print ' '.join(str(x) for x in args)

    @classmethod
    def debug(cls, *args):
        if DEBUG_LOGGING:
            print ' '.join(str(x) for x in args)


class BaseService(object):
    """ Abstract base class for all services """

    def __init__(self, client, name):
        self._client = client
        self._name = name

    def _invoke(self, procedure, parameters=[], parameter_types=[], return_type=None, **kwargs):
        return self._client._invoke(self._name, procedure, parameters, parameter_types, return_type, **kwargs)


class KRPCService(BaseService):
    """ Core kRPC service, e.g. for querying for the available services """

    def __init__(self, client):
        super(KRPCService, self).__init__(client, 'KRPC')

    def GetStatus(self):
        """ Get status message from the server, including the version number  """
        return self._invoke('GetStatus', return_type=schema.KRPC.Status)

    def GetServices(self):
        """ Get available services and procedures """
        return self._invoke('GetServices', return_type=schema.KRPC.Services)


class Service(BaseService):
    """ A dynamically created service, created using information received from the server """

    def __init__(self, client, service):
        """ Create a service from a KRPC.Service object received from a call to KRPC.GetServices() """
        super(Service, self).__init__(client, service.name)
        self._name = service.name
        for procedure in service.procedures:
            self._add_procedure(procedure)

    def _add_procedure(self, procedure):
        """ Add a procedure to this service, from a KRPC.Procedure object """
        parameter_types = []
        for parameter_type in procedure.parameter_types:
            try:
                parameter_types.append(_Types.as_python_type(parameter_type))
            except TypeError:
                Logger.info('Failed to add procedure', self._name, procedure.name, '; ' +
                             'protobuf type', parameter_type, 'not found.')
                return

        return_type = None
        if procedure.HasField('return_type'):
            try:
                return_type = _Types.as_python_type(procedure.return_type)
            except TypeError:
                Logger.info('Failed to add procedure', self._name, procedure.name, '; ' +
                             'protobuf type', procedure.return_type, 'not found.')
                return

        fn = lambda *parameters: self._invoke(
            procedure.name, parameters=parameters,
            parameter_types=parameter_types, return_type=return_type)
        self.__dict__[procedure.name] = fn


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

    def _invoke(self, service, procedure, parameters=[], parameter_types=[], return_type=None, **kwargs):
        """ Execute an RPC """

        # Validate the parameter types
        validated_parameters = []
        if len(parameters) != len(parameter_types):
           raise RPCError(
               'Wrong number of parameters for ' + procedure + '. ' +
               'Expected ' + len(parameter_types) + ', got ' + len(parameters) + '.')
        for i, (value, typ) in enumerate(itertools.izip(parameters, parameter_types)):
            if type(value) != typ:
                # Try coercing to the correct type
                try:
                    value = _Types.coerce_to(value, typ)
                except ValueError:
                    raise ValueError(
                        'Incorrect type for parameter ' + str(i) + ' of ' + procedure + '. ' +
                        'Expected ' + str(typ) + ', got ' + str(type(value)) + '.')
            validated_parameters.append(value)

        # Build the request object
        request = schema.KRPC.Request()
        request.service = service
        request.procedure = procedure
        for parameter in validated_parameters:
            request.parameters.append(_Encoder.encode(parameter))

        # Send the request
        self._send_request(request)
        response = self._receive_response()

        # Check for an error response
        if response.HasField('error'):
            raise RPCError(response.error)

        # Decode the response and return the (optional) result
        result = None
        if return_type is not None:
            result = _Decoder.decode(return_type, response.return_value)

        return result

    def _send_request(self, request):
        """ Send a KRPC.Request object to the server """
        data = _Encoder.encode_delimited(request)
        self._connection.send(data)

    def _receive_response(self):
        """ Receive data from the server and decode it into a KRPC.Response object """
        # FIXME: we might not receive all of the data in one go
        data = self._connection.recv(BUFFER_SIZE)
        return _Decoder.decode_delimited(schema.KRPC.Response, data)


def connect(address=DEFAULT_ADDRESS, port=DEFAULT_PORT, name=None):
    """
    Connect to a kRPC server on the specified IP address and port number,
    and optionally give the kRPC server the supplied name to identify the client
    (up to 32 bytes of UTF-8 encoded text)
    """
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect((address, port))
    connection.send(_Encoder.hello_message(name))
    return Client(connection)
