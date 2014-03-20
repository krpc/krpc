import schema
import os
import glob
import socket
import itertools
import re
try:
    import importlib.import_module as import_module
except ImportError:
    import_module = lambda package: __import__(package, globals(), locals(), [], -1)

# Load all protocol buffer message types
_modules = glob.glob(os.path.dirname(schema.__file__)+"/*.py")
_modules = filter(lambda f: not os.path.basename(f).startswith('_'), _modules)
_modules = [os.path.basename(f)[:-3] for f in _modules]
for module in _modules:
    schema = import_module('schema.' + module)

# TODO: avoid using internals
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal import decoder as protobuf_decoder

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_PORT = 50000
BUFFER_SIZE = 8*1024*1024
DEBUG_LOGGING = True

PROTOBUF_VALUE_TYPES = ['double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'bool', 'string', 'bytes']
PYTHON_VALUE_TYPES = [float, int, long, bool, str, bytes]
PROTOBUF_TO_PYTHON_VALUE_TYPE = {
    'double': float,
    'float': float,
    'int32': int,
    'int64': long,
    'uint32': int,
    'uint64': long,
    'bool': bool,
    'string': str,
    'bytes': bytes
}


class _Types(object):
    """ For handling conversion between protocol buffer types and
        python types, and storing type objects for class types """

    def __init__(self):
        self._types = {}

    def as_type(self, type_string):
        """ Return a type object given a protocol buffer type string """
        if type_string in self._types:
            return self._types[type_string]
        if type_string in PROTOBUF_VALUE_TYPES:
            typ = _ValueType(type_string)
        elif type_string.startswith('Class('):
            typ = _ClassType(type_string)
        else:
            package, _, message = type_string.rpartition('.')
            if hasattr(schema, package) and hasattr(getattr(schema, package), message):
                typ = _MessageType(type_string)
            else:
                typ = _EnumType(type_string)
        self._types[type_string] = typ
        return typ

    def get_parameter_type(self, pos, typ, attrs):
        """ Return a type object for a parameter at the given
            position, protocol buffer type, and procedure attributes """
        attrs = _Attributes.get_parameter_type_attrs(pos, attrs)
        for attr in attrs:
            match = re.match(r'^Class\([^,\.]+\.[^,\.]+\)$', attr)
            if match:
                return self.as_type(attr)
        return self.as_type(typ)

    def get_return_type(self, typ, attrs):
        """ Return a type object for a return value with the given
            protocol buffer type and procedure attributes """
        attrs = _Attributes.get_return_type_attrs(attrs)
        for attr in attrs:
            match = re.match(r'^Class\([^,\.]+\.[^,\.]+\)$', attr)
            if match:
                return self.as_type(attr)
        return self.as_type(typ)

    def coerce_to(self, value, typ):
        """ Coerce a value to the specified type. Raises ValueError if the coercion is not possible. """
        # A NoneType can be coerced to a _ClassType
        if isinstance(typ, _ClassType) and value is None:
            return None
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int, long)
        if type(value) not in numeric_types or typ.python_type not in numeric_types:
            raise ValueError('Failed to coerce value of type ' + str(type(value)) + ' to type ' + str(typ))
        if typ.python_type == float:
            return float(value)
        elif typ.python_type == int:
            return int(value)
        else:
            return long(value)


class _TypeBase(object):
    """ Abstract base class for all type objects """

    def __init__(self, protobuf_type, python_type):
        self._protobuf_type = protobuf_type
        self._python_type = python_type

    @property
    def protobuf_type(self):
        """ Get the protocol buffer type string for the type """
        return self._protobuf_type

    @property
    def python_type(self):
        """ Get the python type """
        return self._python_type


class _ValueType(_TypeBase):
    """ A protocol buffer value type """

    def __init__(self, type_string):
        typ = PROTOBUF_TO_PYTHON_VALUE_TYPE[type_string]
        super(_ValueType, self).__init__(type_string, typ)


class _MessageType(_TypeBase):
    """ A protocol buffer message type """

    def __init__(self, type_string):
        package, message = type_string.split('.')
        typ = getattr(getattr(schema, package), message)
        super(_MessageType, self).__init__(type_string, typ)


class _EnumType(_TypeBase):
    """ A protocol buffer enumeration type """

    def __init__(self, type_string):
        super(_EnumType, self).__init__(type_string, int)


class _ClassType(_TypeBase):
    """ A class type, represented by a uint64 identifier """

    def __init__(self, type_string):
        # Create class type
        match = re.match(r'Class\([^\.]+\.([^\.]+)\)', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a class type' % type_string)
        class_name = match.group(1)
        typ = type(str(class_name), (_BaseClass,), dict())

        # Add constructor
        def ctor(s, object_id):
            super(typ, s).__init__(object_id)
        typ.__init__ = ctor

        super(_ClassType, self).__init__(str(type_string), typ)


class _BaseClass(object):
    """ Abstract base class for all class types on the server """

    def __init__(self, object_id):
        """ Create a proxy object, that mirrors an object on
            the server with the given object identifier """
        self._object_id = object_id

    def __eq__(self, other):
        return isinstance(other, _BaseClass) and self._object_id == other._object_id

    def __hash__(self):
        return self._object_id


class _Attributes(object):
    """ Methods for extracting information from procedure attributes """

    @classmethod
    def is_a_procedure(cls, attrs):
        """ Return true if the attributes are for a plain procedure,
            i.e. not a property accessor, class method etc. """
        return not cls.is_a_property_accessor(attrs) and \
               not cls.is_a_class_method(attrs) and \
               not cls.is_a_class_property_accessor(attrs)

    @classmethod
    def is_a_property_accessor(cls, attrs):
        """ Return true if the attributes are for a property getter or setter. """
        return any(attr.startswith('Property.') for attr in attrs)

    @classmethod
    def is_a_property_getter(cls, attrs):
        """ Return true if the attributes are for a property getter. """
        return any(attr.startswith('Property.Get(') for attr in attrs)

    @classmethod
    def is_a_property_setter(cls, attrs):
        """ Return true if the attributes are for a property setter. """
        return any(attr.startswith('Property.Set(') for attr in attrs)

    @classmethod
    def is_a_class_method(cls, attrs):
        """ Return true if the attributes are for a class method. """
        return any(attr.startswith('Class.Method(') for attr in attrs)

    @classmethod
    def is_a_class_property_accessor(cls, attrs):
        """ Return true if the attributes are for a class property getter or setter. """
        return any(attr.startswith('Class.Property.') for attr in attrs)

    @classmethod
    def is_a_class_property_getter(cls, attrs):
        """ Return true if the attributes are for a class property getter. """
        return any(attr.startswith('Class.Property.Get(') for attr in attrs)

    @classmethod
    def is_a_class_property_setter(cls, attrs):
        """ Return true if the attributes are for a class property setter. """
        return any(attr.startswith('Class.Property.Set(') for attr in attrs)

    @classmethod
    def get_property_name(cls, attrs):
        """ Return the name of the property handled by a property getter or setter. """
        if cls.is_a_property_accessor(attrs):
            for attr in attrs:
                match = re.match(r'^Property\.(Get|Set)\((.+)\)$', attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a property accessor')

    @classmethod
    def get_service_name(cls, attrs):
        """ Return the name of the services that a class method or property accessor is part of. """
        if cls.is_a_class_method(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Method\(([^,\.]+)\.[^,]+,[^,]+\)$', attr)
                if match:
                    return match.group(1)
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Property.(Get|Set)\(([^,\.]+)\.[^,]+,[^,]+\)$', attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class method or class property accessor')

    @classmethod
    def get_class_name(cls, attrs):
        """ Return the name of the class that a method or property accessor is part of. """
        if cls.is_a_class_method(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Method\([^,\.]+\.([^,\.]+),[^,]+\)$', attr)
                if match:
                    return match.group(1)
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Property.(Get|Set)\([^,\.]+\.([^,]+),[^,]+\)$', attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class method or class property accessor')

    @classmethod
    def get_class_method_name(cls, attrs):
        """ Return the name of a class mathod. """
        if cls.is_a_class_method(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Method\([^,]+,([^,]+)\)$', attr)
                if match:
                    return match.group(1)
        raise ValueError('Procedure attributes are not a class method accessor')

    @classmethod
    def get_class_property_name(cls, attrs):
        """ Return the name of a class property (for a getter or setter procedure). """
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = re.match(r'^Class\.Property\.(Get|Set)\([^,]+,([^,]+)\)$', attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class property accessor')

    @classmethod
    def get_return_type_attrs(cls, attrs):
        """ Return the attributes for the return type of a procedure. """
        return_type_attrs = []
        for attr in attrs:
            match = re.match(r'^ReturnType.(.+)$', attr)
            if match:
                return_type_attrs.append(match.group(1))
        return return_type_attrs

    @classmethod
    def get_parameter_type_attrs(cls, pos, attrs):
        """ Return the attributes for a specific parameter of a procedure. """
        parameter_type_attrs = []
        for attr in attrs:
            match = re.match(r'^ParameterType\(' + str(pos) + '\).(.+)$', attr)
            if match:
                parameter_type_attrs.append(match.group(1))
        return parameter_type_attrs



class _Encoder(object):
    """ Routines for encoding messages and values in the protocol buffer serialization format """

    @classmethod
    def hello_message(cls, name=None):
        """ Generate a hello message with the given name
            truncated to fit if necessary """
        header = b'\x48\x45\x4C\x4C\x4F\xBA\xDA\x55'
        if name is None:
            name = ''
        else:
            name = cls._unicode_truncate(name, 32, 'utf-8')
        name = name.encode('utf-8')
        identifier = name + (b'\x00' * (32-len(name)))
        return header + identifier

    @classmethod
    def _unicode_truncate(cls, string, length, encoding='utf-8'):
        """ Shorten a unicode string so that it's encoding uses at
            most length bytes. """
        encoded = string.encode(encoding=encoding)[:length]
        return encoded.decode(encoding, 'ignore')


    @classmethod
    def encode(cls, x, typ):
        """ Encode a message or value of the given protocol buffer type """
        if isinstance(typ, _MessageType):
            return x.SerializeToString()
        elif isinstance(typ, _ValueType):
            return cls._encode_value(x, typ)
        elif isinstance(typ, _EnumType):
            return cls._encode_value(x, _Types().as_type('int32'))
        elif isinstance(typ, _ClassType):
            object_id = x._object_id if x is not None else 0
            return cls._encode_value(object_id, _Types().as_type('uint64'))
        else:
            raise RuntimeError ('Cannot encode objects of type ' + str(type(x)))

    @classmethod
    def encode_delimited(cls, x, typ):
        """ Encode a message or value with size information
            (for use in a delimited communication stream) """
        data = cls.encode(x, typ)
        delimiter = protobuf_encoder._VarintBytes(len(data))
        return delimiter + data

    @classmethod
    def _encode_value(cls, value, typ):
        return getattr(_ValueEncoder, 'encode_' + typ.protobuf_type)(value)


class _ValueEncoder(object):
    """ Routines for encoding values in the protocol buffer serialization format """

    @classmethod
    def encode_double(cls, value):
        data = []
        def write(x):
            data.append(x)
        #TODO: only handles finite values
        encoder = protobuf_encoder.DoubleEncoder(1,False,False)
        encoder(write, value)
        return ''.join(data[1:]) # strips the tag value

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
    def _encode_signed_varint(cls, value):
        data = []
        def write(x):
            data.append(x)
        protobuf_encoder._SignedVarintEncoder()(write, value)
        return ''.join(data)

    @classmethod
    def encode_int32(cls, value):
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_int64(cls, value):
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_uint32(cls, value):
        if value < 0:
            raise ValueError('Value must be non-negative, got %d' % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_uint64(cls, value):
        if value < 0:
            raise ValueError('Value must be non-negative, got %d' % value)
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

    @classmethod
    def encode_bytes(cls, value):
        return ''.join([cls._encode_varint(len(value)), value])


class _Decoder(object):
    """ Routines for decoding messages and values from the protocol buffer serialization format """

    @classmethod
    def decode(cls, data, typ):
        """ Given a python type, and serialized data, decode the value """
        if isinstance(typ, _MessageType):
            return cls._decode_message(data, typ)
        elif isinstance(typ, _EnumType):
            return cls._decode_value(data, _Types().as_type('int32'))
        elif isinstance(typ, _ValueType):
            return cls._decode_value(data, typ)
        elif isinstance(typ, _ClassType):
            object_id_typ = _Types().as_type('uint64')
            object_id = cls._decode_value(data, object_id_typ)
            return typ.python_type(object_id) if object_id != 0 else None
        else:
            raise RuntimeError ('Cannot decode type %s' % str(typ))

    @classmethod
    def decode_delimited(cls, data, typ):
        """ Decode a message or value with size information
            (used in a delimited communication stream) """
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return cls.decode(data[position:position+size], typ)

    @classmethod
    def _decode_message(cls, data, typ):
        message = typ.python_type()
        message.ParseFromString(data)
        return message

    @classmethod
    def _decode_value(cls, data, typ):
        return getattr(_ValueDecoder, 'decode_' + typ.protobuf_type)(data)


class _ValueDecoder(object):
    """ Routines for encoding values from the protocol buffer serialization format """

    @classmethod
    def _decode_signed_varint(cls, data):
        return protobuf_decoder._DecodeSignedVarint(data, 0)[0]

    @classmethod
    def _decode_varint(cls, data):
        return protobuf_decoder._DecodeVarint(data, 0)[0]

    @classmethod
    def decode_int32(cls, data):
        return int(cls._decode_signed_varint(data))

    @classmethod
    def decode_int64(cls, data):
        return cls._decode_signed_varint(data)

    @classmethod
    def decode_uint32(cls, data):
        return cls._decode_varint(data)

    @classmethod
    def decode_uint64(cls, data):
        return cls._decode_varint(data)

    # The code for the following two methods is taken from
    # google.protobuf.internal.decoder._FloatDecoder and _DoubleDecoder
    # Copyright 2008, Google Inc.
    # See protobuf-license.txt distributed with this file

    @classmethod
    def decode_double(cls, data):
        # We expect a 64-bit value in little-endian byte order.  Bit 1 is the sign
        # bit, bits 2-12 represent the exponent, and bits 13-64 are the significand.
        double_bytes = data[0:8]

        # If this value has all its exponent bits set and at least one significand
        # bit set, it's not a number.  In Python 2.4, struct.unpack will treat it
        # as inf or -inf.  To avoid that, we treat it specially.
        if ((double_bytes[7] in '\x7F\xFF')
            and (double_bytes[6] >= '\xF0')
            and (double_bytes[0:7] != '\x00\x00\x00\x00\x00\x00\xF0')):
          return _NAN

        # Note that we expect someone up-stack to catch struct.error and convert
        # it to _DecodeError -- this way we don't have to set up exception-
        # handling blocks every time we parse one value.
        import struct
        return struct.unpack('<d', double_bytes)[0]

    @classmethod
    def decode_float(cls, data):
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

    # End of code taken from google.protobuf.internal.decoder._FloatDecoder and _DoubleDecoder

    @classmethod
    def decode_bool(cls, data):
        return bool(cls._decode_varint(data))

    @classmethod
    def decode_string(cls, data):
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return unicode(data[position:position+size], 'utf-8')

    @classmethod
    def decode_bytes(cls, data):
        (size, pos) = protobuf_decoder._DecodeVarint(data, 0)
        return data[pos:pos+size]


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

    def _invoke(self, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        return self._client._invoke(self._name, procedure, args, kwargs, param_names, param_types, return_type)


class KRPCService(BaseService):
    """ Core kRPC service, e.g. for querying for the available services """

    def __init__(self, client):
        super(KRPCService, self).__init__(client, 'KRPC')

    def GetStatus(self):
        """ Get status message from the server, including the version number  """
        return self._invoke('GetStatus', return_type=self._client._types.as_type('KRPC.Status'))

    def GetServices(self):
        """ Get available services and procedures """
        return self._invoke('GetServices', return_type=self._client._types.as_type('KRPC.Services'))


def _create_service(client, service):
    """ Create a new class type for a service and instantiate it """
    cls = type(str('_Service_' + service.name), (_Service,), {})
    return cls(cls, client, service)

class _Service(BaseService):
    """ A dynamically created service, created using information received from the server.
        Should not be instantiated directly. Use _create_service instead. """

    def __init__(self, cls, client, service):
        """ Create a service from the dynamically created class type for the service, the client,
            and a KRPC.Service object received from a call to KRPC.GetServices()
            Should not be instantiated directly. Use _create_service instead. """
        super(_Service, self).__init__(client, service.name)
        self._cls = cls
        self._name = service.name
        self._types = client._types

        # Add class types to service
        for procedure in service.procedures:
            try:
                name = _Attributes.get_class_name(procedure.attributes)
                self._add_class(name)
            except ValueError:
                pass

        # Create plain procedures
        for procedure in service.procedures:
            if _Attributes.is_a_procedure(procedure.attributes):
                self._add_procedure(procedure)

        # Create static service properties
        properties = {}
        for procedure in service.procedures:
            if _Attributes.is_a_property_accessor(procedure.attributes):
                name = _Attributes.get_property_name(procedure.attributes)
                if name not in properties:
                    properties[name] = [None,None]
                if _Attributes.is_a_property_getter(procedure.attributes):
                    properties[name][0] = procedure
                else:
                    properties[name][1] = procedure
        for name, procedures in properties.items():
            self._add_property(name, procedures[0], procedures[1])

        # Create class methods
        for procedure in service.procedures:
            if _Attributes.is_a_class_method(procedure.attributes):
                class_name = _Attributes.get_class_name(procedure.attributes)
                method_name = _Attributes.get_class_method_name(procedure.attributes)
                self._add_class_method(class_name, method_name, procedure)

        # Create class properties
        properties = {}
        for procedure in service.procedures:
            if _Attributes.is_a_class_property_accessor(procedure.attributes):
                class_name = _Attributes.get_class_name(procedure.attributes)
                property_name = _Attributes.get_class_property_name(procedure.attributes)
                key = (class_name, property_name)
                if key not in properties:
                    properties[key] = [None,None]
                if _Attributes.is_a_class_property_getter(procedure.attributes):
                    properties[key][0] = procedure
                else:
                    properties[key][1] = procedure
        for (class_name, property_name), procedures in properties.items():
            self._add_class_property(class_name, property_name, procedures[0], procedures[1])

    def _add_class(self, name):
        """ Add a class type with the given name to this service, and the type store """
        class_type = self._types.as_type('Class(' + self._name + '.' + name + ')')
        setattr(self, name, class_type.python_type)

    def _add_procedure(self, procedure):
        """ Add a plain procedure to this service """
        param_names = [param.name for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        setattr(self, procedure.name,
                lambda *args, **kwargs: self._invoke(
                    procedure.name, args=args, kwargs=kwargs,
                    param_names=param_names, param_types=param_types, return_type=return_type))

    def _add_property(self, name, getter=None, setter=None):
        """ Add a property to the service, with a getter and/or setter procedure """
        fget = fset = None
        if getter:
            self._add_procedure(getter)
            fget = lambda s: getattr(self, getter.name)()
        if setter:
            self._add_procedure(setter)
            fset = lambda s, value: getattr(self, setter.name)(value)
        setattr(self._cls, name, property(fget, fset))

    def _add_class_method(self, class_name, method_name, procedure):
        """ Add a class method to the service """
        cls = getattr(self, class_name)
        param_names = [param.name for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        setattr(cls, method_name,
                lambda s, *args, **kwargs: self._invoke(procedure.name, args=[s] + list(args), kwargs=kwargs,
                                                        param_names=param_names, param_types=param_types,
                                                        return_type=return_type))

    def _add_class_property(self, class_name, property_name, getter=None, setter=None):
        fget = fset = None
        if getter:
            self._add_procedure(getter)
            fget = lambda s: getattr(self, getter.name)(s)
        if setter:
            self._add_procedure(setter)
            fset = lambda s, value: getattr(self, setter.name)(s, value)
        class_type = getattr(self, class_name)
        setattr(class_type, property_name, property(fget, fset))


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
        self._types = _Types()
        self._request_type = self._types.as_type('KRPC.Request')
        self._response_type = self._types.as_type('KRPC.Response')

        # Set up the main KRPC service
        self.KRPC = KRPCService(self)

        services = self.KRPC.GetServices().services

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
                setattr(self, service.name, _create_service(self, service))

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
            argument = schema.KRPC.Argument()
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
            argument = schema.KRPC.Argument()
            argument.position = i
            argument.value = encode_argument(i, arg)
            arguments.append(argument)

        # Build the request object
        request = schema.KRPC.Request()
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
