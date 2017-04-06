import re
import collections
import importlib
from enum import Enum
from krpc.attributes import Attributes
from krpc.utils import split_type_string

PROTOBUF_VALUE_TYPES = ['double', 'float', 'int32', 'int64', 'uint32',
                        'uint64', 'bool', 'string', 'bytes']
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
PROTOBUF_TO_MESSAGE_TYPE = {}


def _load_types(package):
    """ Load all message and enum types from the given package,
        and populate PROTOBUF_TO_MESSAGE_TYPE """
    if package in _load_types.loaded:
        return
    _load_types.loaded.add(package)
    try:
        module = importlib.import_module('krpc.schema.' + package + '_pb2')
        if hasattr(module, 'DESCRIPTOR'):
            for name in module.DESCRIPTOR.message_types_by_name.keys():
                PROTOBUF_TO_MESSAGE_TYPE[package + '.' + name] = getattr(
                    module, name)
    except (KeyError, ImportError, AttributeError, ValueError):
        pass


_load_types.loaded = set()


class Types(object):
    """ A type store. Used to obtain type objects from protocol buffer type
        strings, and stores python types for services and service defined
        class and enumeration types. """

    def __init__(self):
        # Mapping from protobuf type strings to type objects
        self._types = {}

    def as_type(self, type_string, doc=None):
        """ Return a type object given a protocol buffer type string """
        if type_string in self._types:
            return self._types[type_string]

        # TODO: add enumeration types
        # Update kRPC server to attach type attributes to parameters/return
        # types etc. that are of type KRPCEnum
        # Will allow proper type checking of enum values passed to procedures
        # pylint: disable=redefined-variable-type
        if type_string in PROTOBUF_VALUE_TYPES:
            typ = ValueType(type_string)
        elif type_string.startswith('Class(') or type_string == 'Class':
            typ = ClassType(type_string, doc)
        elif type_string.startswith('Enum(') or type_string == 'Enum':
            typ = EnumType(type_string, doc)
        elif type_string.startswith('List(') or type_string == 'List':
            typ = ListType(type_string, self)
        elif (type_string.startswith('Dictionary(') or
              type_string == 'Dictionary'):
            typ = DictionaryType(type_string, self)
        elif type_string.startswith('Set(') or type_string == 'Set':
            typ = SetType(type_string, self)
        elif type_string.startswith('Tuple(') or type_string == 'Tuple':
            typ = TupleType(type_string, self)
        else:
            # A message type
            if not re.match(r'^[A-Za-z0-9_\.]+$', type_string):
                raise ValueError(
                    '\'%s\' is not a valid type string' % type_string)
            package, _, _ = type_string.rpartition('.')
            _load_types(package)
            if type_string in PROTOBUF_TO_MESSAGE_TYPE:
                typ = MessageType(type_string)
            else:
                raise ValueError(
                    '\'%s\' is not a valid type string' % type_string)
        # pylint: enable=redefined-variable-type

        self._types[type_string] = typ
        return typ

    def get_parameter_type(self, pos, typ, attrs):
        """ Return a type object for a parameter at the given
            position, with the given protocol buffer type and attributes """
        attrs = Attributes.get_parameter_type_attrs(pos, attrs)
        for attr in attrs:
            try:
                return self.as_type(attr)
            except ValueError:
                pass
        return self.as_type(typ)

    def get_return_type(self, typ, attrs):
        """ Return a type object for the return value with the given
            protocol buffer type and procedure attributes """
        attrs = Attributes.get_return_type_attrs(attrs)
        for attr in attrs:
            try:
                return self.as_type(attr)
            except ValueError:
                pass
        return self.as_type(typ)

    def coerce_to(self, value, typ):
        """ Coerce a value to the specified type (specified by a type object).
            Raises ValueError if the coercion is not possible. """
        if isinstance(value, typ.python_type):
            return value
        # A unicode type can be coerced to a string
        if typ.python_type == str and isinstance(value, unicode):
            return value
        # A NoneType can be coerced to a ClassType
        if isinstance(typ, ClassType) and value is None:
            return None
        # Coerce identical class types from different client connections
        if isinstance(typ, ClassType) and isinstance(value, ClassBase):
            value_type = type(value)
            if typ.python_type._service_name == value_type._service_name and \
               typ.python_type._class_name == value_type._class_name:
                return typ.python_type(value._object_id)
        # Collection types
        try:
            # Coerce tuples to lists
            if isinstance(value, collections.Iterable) and \
               isinstance(typ, ListType):
                return typ.python_type(
                    self.coerce_to(x, typ.value_type) for x in value)
            # Coerce lists (with appropriate number of elements) to tuples
            if isinstance(value, collections.Iterable) and \
               isinstance(typ, TupleType):
                if len(value) != len(typ.value_types):
                    raise ValueError
                return typ.python_type(
                    [self.coerce_to(x, typ.value_types[i])
                     for i, x in enumerate(value)])
        except ValueError:
            raise ValueError('Failed to coerce value ' + str(value) +
                             ' of type ' + str(type(value)) +
                             ' to type ' + str(typ))
        # Numeric types
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int, long)
        if isinstance(value, bool) or \
           not any(isinstance(value, t) for t in numeric_types) or \
           typ.python_type not in numeric_types:
            raise ValueError('Failed to coerce value ' + str(value) +
                             ' of type ' + str(type(value)) +
                             ' to type ' + str(typ))
        if typ.python_type == float:
            return float(value)
        elif typ.python_type == int:
            return int(value)
        else:
            return long(value)


class TypeBase(object):
    """ Base class for all type objects """

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

    def __str__(self):
        return '<pbtype: \'' + self.protobuf_type + '\'>'


class ValueType(TypeBase):
    """ A protocol buffer value type """

    def __init__(self, type_string):
        if type_string not in PROTOBUF_TO_PYTHON_VALUE_TYPE:
            raise ValueError(
                '\'%s\' is not a valid type string for a value type' %
                type_string)
        typ = PROTOBUF_TO_PYTHON_VALUE_TYPE[type_string]
        super(ValueType, self).__init__(type_string, typ)


class MessageType(TypeBase):
    """ A protocol buffer message type """

    def __init__(self, type_string):
        package, _, _ = type_string.rpartition('.')
        _load_types(package)
        if type_string not in PROTOBUF_TO_MESSAGE_TYPE:
            raise ValueError(
                '\'%s\' is not a valid type string for a message type' %
                type_string)
        typ = PROTOBUF_TO_MESSAGE_TYPE[type_string]
        super(MessageType, self).__init__(type_string, typ)


class ClassType(TypeBase):
    """ A class type, represented by a uint64 identifier """

    def __init__(self, type_string, doc):
        match = re.match(r'Class\(([^\.]+)\.([^\.]+)\)', type_string)
        if not match:
            raise ValueError(
                '\'%s\' is not a valid type string for a class type' %
                type_string)
        service_name = match.group(1)
        class_name = match.group(2)
        typ = _create_class_type(service_name, class_name, doc)
        super(ClassType, self).__init__(str(type_string), typ)


class EnumType(TypeBase):
    """ An enumeration type, represented by an int32 value """

    def __init__(self, type_string, doc):
        match = re.match(r'Enum\(([^\.]+)\.([^\.]+)\)', type_string)
        if not match:
            raise ValueError(
                '\'%s\' is not a valid type string for an enumeration type' %
                type_string)
        self._service_name = match.group(1)
        self._enum_name = match.group(2)
        self._doc = doc
        # Sets python_type to None, set_values
        # must be called to set the python_type
        super(EnumType, self).__init__(str(type_string), None)

    def set_values(self, values):
        """ Set the python type. Creates an Enum class
            using the given values. """
        self._python_type = _create_enum_type(
            self._enum_name, values, self._doc)


class ListType(TypeBase):
    """ A list collection type, represented by a protobuf message """

    def __init__(self, type_string, types):
        if not (type_string.startswith('List(') and type_string.endswith(')')):
            raise ValueError(
                '\'%s\' is not a valid type string for a list type' %
                type_string)

        self.value_type = types.as_type(type_string[5:-1])

        super(ListType, self).__init__(str(type_string), list)


class DictionaryType(TypeBase):
    """ A dictionary collection type, represented by a protobuf message """

    def __init__(self, type_string, types):
        if not (type_string.startswith('Dictionary(') and
                type_string.endswith(')')):
            raise ValueError(
                '\'%s\' is not a valid type string for a dictionary type' %
                type_string)

        type_strings = split_type_string(type_string[11:-1])
        if len(type_strings) != 2:
            raise ValueError(
                '\'%s\' is not a valid type string for a dictionary type' %
                type_string)
        self.key_type = types.as_type(type_strings[0])
        self.value_type = types.as_type(type_strings[1])

        super(DictionaryType, self).__init__(str(type_string), dict)


class SetType(TypeBase):
    """ A set collection type, represented by a protobuf message """

    def __init__(self, type_string, types):
        if not (type_string.startswith('Set(') and type_string.endswith(')')):
            raise ValueError(
                '\'%s\' is not a valid type string for a set type' %
                type_string)

        self.value_type = types.as_type(type_string[4:-1])

        super(SetType, self).__init__(str(type_string), set)


class TupleType(TypeBase):
    """ A tuple collection type, represented by a protobuf message """

    def __init__(self, type_string, types):
        if not (type_string.startswith('Tuple(') and
                type_string.endswith(')')):
            raise ValueError(
                '\'%s\' is not a valid type string for a tuple type' %
                type_string)

        self.value_types = [types.as_type(typ)
                            for typ in split_type_string(type_string[6:-1])]

        super(TupleType, self).__init__(str(type_string), tuple)


class DynamicType(object):
    @classmethod
    def _add_method(cls, name, func, doc=None):
        """ Add a method """
        func.__name__ = name
        func.__doc__ = doc
        setattr(cls, name, func)
        return getattr(cls, name)

    @classmethod
    def _add_static_method(cls, name, func, doc=None):
        """ Add a static method """
        func.__name__ = name
        func.__doc__ = doc
        func = staticmethod(func)
        setattr(cls, name, func)
        return getattr(cls, name)

    @classmethod
    def _add_property(cls, name, getter=None, setter=None, doc=None):
        """ Add a property """
        if getter is None and setter is None:
            raise ValueError('Either getter or setter must be provided')
        prop = property(getter, setter, doc=doc)
        setattr(cls, name, prop)
        return getattr(cls, name)


class ClassBase(DynamicType):
    """ Base class for service-defined class types """

    _client = None

    def __init__(self, object_id):
        self._object_id = object_id

    def __eq__(self, other):
        return isinstance(other, ClassBase) and \
            self._object_id == other._object_id

    def __ne__(self, other):
        return not isinstance(other, ClassBase) or \
            self._object_id != other._object_id

    def __lt__(self, other):
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id < other._object_id

    def __le__(self, other):
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id <= other._object_id

    def __gt__(self, other):
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id > other._object_id

    def __ge__(self, other):
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id >= other._object_id

    def __hash__(self):
        return hash(self._object_id)

    def __repr__(self):
        return '<%s.%s remote object #%d>' % \
            (self._service_name, self._class_name, self._object_id)


def _create_class_type(service_name, class_name, doc):
    return type(str(class_name), (ClassBase,),
                {'_service_name': service_name,
                 '_class_name': class_name,
                 '__doc__': doc})


def _create_enum_type(enum_name, values, doc):
    typ = Enum(str(enum_name), dict((name, x['value'])
                                    for name, x in values.items()))
    setattr(typ, '__doc__', doc)
    for name in values.keys():
        setattr(getattr(typ, name), '__doc__', values[name]['doc'])
    return typ


class DefaultArgument(object):
    """ A sentinel value for default arguments """

    def __init__(self, value):
        self._value = value

    def __str__(self):
        return self._value

    def __repr__(self):
        return self._value
