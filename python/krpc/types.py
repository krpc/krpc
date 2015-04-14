import re
import collections
import krpc.schema
from krpc.attributes import _Attributes
import importlib

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
PROTOBUF_TO_MESSAGE_TYPE = {}
PROTOBUF_TO_ENUM_TYPE = {}

_packages_loaded = set()
_package_search_paths = ['krpc.schema']

def add_search_path(path):
    """ Add a python package to the list of search locations for finding Protocol Buffer message and enum types """
    _package_search_paths.append(path)

def _load_types(package):
    """ Load all message and enum types from the given package,
        and populate PROTOBUF_TO_MESSAGE_TYPE and PROTOBUF_TO_ENUM_TYPE """
    if package in _packages_loaded:
        return
    _packages_loaded.add(package)
    for path in _package_search_paths:
        try:
            module = importlib.import_module(path + '.' + package)
            if hasattr(module, 'DESCRIPTOR'):
               for name in module.DESCRIPTOR.message_types_by_name.keys():
                   PROTOBUF_TO_MESSAGE_TYPE[package+'.'+name] = getattr(module, name)
               for name in module.DESCRIPTOR.enum_types_by_name.keys():
                   PROTOBUF_TO_ENUM_TYPE[package+'.'+name] = getattr(module, name)
        except (KeyError, ImportError, AttributeError, ValueError):
            pass

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
        elif type_string.startswith('Class(') or type_string == 'Class':
            typ = _ClassType(type_string)
        elif type_string.startswith('List(') or type_string == 'List':
            typ = _ListType(type_string, self)
        elif type_string.startswith('Dictionary(') or type_string == 'Dictionary':
            typ = _DictionaryType(type_string, self)
        elif type_string.startswith('Set(') or type_string == 'Set':
            typ = _SetType(type_string, self)
        elif type_string.startswith('Tuple(') or type_string == 'Tuple':
            typ = _TupleType(type_string, self)
        else:
            if not re.match(r'^[A-Za-z0-9_\.]+$', type_string):
                raise ValueError('\'%s\' is not a valid type string' % type_string)
            package,_,_ = type_string.rpartition('.')
            _load_types(package)
            if type_string in PROTOBUF_TO_MESSAGE_TYPE:
                typ = _MessageType(type_string)
            elif type_string in PROTOBUF_TO_ENUM_TYPE:
                typ = _EnumType(type_string)
            else:
                raise ValueError('\'%s\' is not a valid type string' % type_string)

        self._types[type_string] = typ
        return typ

    def get_parameter_type(self, pos, typ, attrs):
        """ Return a type object for a parameter at the given
            position, protocol buffer type, and procedure attributes """
        attrs = _Attributes.get_parameter_type_attrs(pos, attrs)
        for attr in attrs:
            try:
                return self.as_type(attr)
            except ValueError:
                pass
        return self.as_type(typ)

    def get_return_type(self, typ, attrs):
        """ Return a type object for a return value with the given
            protocol buffer type and procedure attributes """
        attrs = _Attributes.get_return_type_attrs(attrs)
        for attr in attrs:
            try:
                return self.as_type(attr)
            except ValueError:
                pass
        return self.as_type(typ)

    def coerce_to(self, value, typ):
        """ Coerce a value to the specified type. Raises ValueError if the coercion is not possible. """
        # A NoneType can be coerced to a _ClassType
        if isinstance(typ, _ClassType) and value is None:
            return None
        # Collection types
        try:
            # Coerce tuples to lists
            if isinstance(value, collections.Iterable) and isinstance(typ, _ListType):
                return typ.python_type(self.coerce_to(x, typ.value_type) for x in value)
            # Coerce lists (with appropriate number of elements) to tuples
            if isinstance(value, collections.Iterable) and isinstance(typ, _TupleType):
                if len(value) != len(typ.value_types):
                    raise ValueError
                return typ.python_type([self.coerce_to(x, typ.value_types[i]) for i,x in enumerate(value)])
        except ValueError:
            raise ValueError('Failed to coerce value ' + str(value) + ' of type ' + str(type(value)) + ' to type ' + str(typ))
        # Numeric types
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int, long)
        if type(value) not in numeric_types or typ.python_type not in numeric_types:
            raise ValueError('Failed to coerce value ' + str(value) + ' of type ' + str(type(value)) + ' to type ' + str(typ))
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

    def __str__(self):
        return '<pbtype: \'' + self.protobuf_type + '\'>'


class _ValueType(_TypeBase):
    """ A protocol buffer value type """

    def __init__(self, type_string):
        typ = PROTOBUF_TO_PYTHON_VALUE_TYPE[type_string]
        super(_ValueType, self).__init__(type_string, typ)


class _MessageType(_TypeBase):
    """ A protocol buffer message type """

    def __init__(self, type_string):
        package,_,_ = type_string.rpartition('.')
        _load_types(package)
        if type_string not in PROTOBUF_TO_MESSAGE_TYPE:
            raise ValueError('\'%s\' is not a valid type string for a message type' % type_string)
        typ = PROTOBUF_TO_MESSAGE_TYPE[type_string]
        super(_MessageType, self).__init__(type_string, typ)


class _EnumType(_TypeBase):
    """ A protocol buffer enumeration type """

    def __init__(self, type_string):
        package,_,_ = type_string.rpartition('.')
        _load_types(package)
        if type_string not in PROTOBUF_TO_ENUM_TYPE:
            raise ValueError('\'%s\' is not a valid type string for an enum type' % type_string)
        super(_EnumType, self).__init__(type_string, int)


class _ClassType(_TypeBase):
    """ A class type, represented by a uint64 identifier """

    def __init__(self, type_string):
        # Create class type
        match = re.match(r'Class\(([^\.]+)\.([^\.]+)\)', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a class type' % type_string)
        service_name = match.group(1)
        class_name = match.group(2)
        typ = type(str(class_name), (_BaseClass,), dict())

        # Add constructor
        def ctor(s, object_id):
            super(typ, s).__init__(object_id)
        typ.__init__ = ctor

        # Add cmp method
        def cmp_(s, other):
            if not hasattr(other, '_object_id'):
                return -1
            return s._object_id.__cmp__(other._object_id)
        typ.__cmp__ = cmp_

        # Add hash method
        def hash_(s):
            return hash(s._object_id)
        typ.__hash__ = hash_

        # Add repr method
        def repr_(s):
            return '<%s.%s object #%d>' % (service_name, class_name, s._object_id)
        typ.__repr__ = repr_

        super(_ClassType, self).__init__(str(type_string), typ)


def _parse_type_string(typ):
    """ Given a string, extract a substring up to the first comma. Parses parnetheses.
        Multiple calls can be used to separate a string by commas. """
    if typ == None:
        raise ValueError
    result = ''
    level = 0
    for x in typ:
        if level == 0 and x == ',':
            break
        if x == '(':
            level += 1
        if x == ')':
            level -= 1
        result += x
    if level != 0:
        raise ValueError
    if result == typ:
        return result, None
    if typ[len(result)] != ',':
        raise ValueError
    return result, typ[len(result)+1:]


class _ListType(_TypeBase):
    """ A list collection type, represented by a protobuf message """

    def __init__(self, type_string, type_store):
        match = re.match(r'^List\((.+)\)$', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a list type' % type_string)

        self.value_type = type_store.as_type(match.group(1))

        super(_ListType, self).__init__(str(type_string), list)


class _DictionaryType(_TypeBase):
    """ A dictionary collection type, represented by a protobuf message """

    def __init__(self, type_string, type_store):
        match = re.match(r'^Dictionary\((.+)\)$', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a dictionary type' % type_string)

        typ = match.group(1)

        try:
            key_string, typ = _parse_type_string(typ)
            value_string, typ = _parse_type_string(typ)
            if typ != None:
                raise ValueError
            self.key_type = type_store.as_type(key_string)
            self.value_type = type_store.as_type(value_string)
        except ValueError:
            raise ValueError('\'%s\' is not a valid type string for a dictionary type' % type_string)

        super(_DictionaryType, self).__init__(str(type_string), dict)


class _SetType(_TypeBase):
    """ A set collection type, represented by a protobuf message """

    def __init__(self, type_string, type_store):
        match = re.match(r'^Set\((.+)\)$', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a set type' % type_string)

        self.value_type = type_store.as_type(match.group(1))

        super(_SetType, self).__init__(str(type_string), set)


class _TupleType(_TypeBase):
    """ A tuple collection type, represented by a protobuf message """

    def __init__(self, type_string, type_store):
        match = re.match(r'^Tuple\((.+)\)$', type_string)
        if not match:
            raise ValueError('\'%s\' is not a valid type string for a set type' % type_string)

        self.value_types = []
        typ = match.group(1)
        while typ != None:
            value_type, typ = _parse_type_string(typ)
            self.value_types.append(type_store.as_type(value_type))

        super(_TupleType, self).__init__(str(type_string), tuple)


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
