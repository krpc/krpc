import re
import krpc.schema
from krpc.attributes import _Attributes

try:
    import importlib.import_module as import_module
except ImportError:
    import_module = lambda package: __import__(package, globals(), locals(), [], -1)

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
            typ = None
            package, _, message = type_string.rpartition('.')
            try:
                module = import_module('krpc.schema.' + package)
                if hasattr(getattr(module.schema, package), message):
                    typ = _MessageType(type_string)
            except:
                pass
            if typ is None:
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
        typ = getattr(getattr(krpc.schema, package), message)
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
