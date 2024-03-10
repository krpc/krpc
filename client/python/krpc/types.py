from __future__ import annotations
from typing import cast, Any, Callable, Iterable, List, Mapping, Type, Optional, TYPE_CHECKING
import collections
from enum import Enum
import krpc.schema.KRPC_pb2 as KRPC
if TYPE_CHECKING:
    from krpc.client import Client


VALUE_TYPES = {
    KRPC.Type.DOUBLE: float,
    KRPC.Type.FLOAT: float,
    KRPC.Type.SINT32: int,
    KRPC.Type.SINT64: int,
    KRPC.Type.UINT32: int,
    KRPC.Type.UINT64: int,
    KRPC.Type.BOOL: bool,
    KRPC.Type.STRING: str,
    KRPC.Type.BYTES: bytes
}

MESSAGE_TYPES = {
    KRPC.Type.EVENT: KRPC.Event,
    KRPC.Type.PROCEDURE_CALL: KRPC.ProcedureCall,
    KRPC.Type.SERVICES: KRPC.Services,
    KRPC.Type.STREAM: KRPC.Stream,
    KRPC.Type.STATUS: KRPC.Status
}

EXCEPTION_TYPES = {
    'InvalidOperationException': RuntimeError,
    'ArgumentException': ValueError,
    'ArgumentNullException': ValueError,
    'ArgumentOutOfRangeException': ValueError
}


def _protobuf_type(code: KRPC.Type.TypeCode,
                   service: str | None = None,
                   name: str | None = None,
                   types: list[KRPC.Type] | None = None) -> KRPC.Type:
    protobuf_type = KRPC.Type()
    protobuf_type.code = code
    if service is not None:
        protobuf_type.service = service
    if name is not None:
        protobuf_type.name = name
    if types is not None:
        protobuf_type.types.extend(types)
    return protobuf_type


class Types:
    """ A type store. Used to obtain type objects from protocol buffer type
        strings, and stores python types for services and service defined
        class and enumeration types. """

    def __init__(self) -> None:
        # Mapping from protobuf type strings to type objects
        self._types: dict[bytes, TypeBase] = {}
        self._exception_types: dict[tuple[str, str], Type[Exception]] = {}

    def register_class_type(self, service: str, name: str, python_type: type) -> None:
        protobuf_type = _protobuf_type(KRPC.Type.CLASS, service, name)
        key = protobuf_type.SerializeToString()
        assert key not in self._types
        self._types[key] = ClassType(protobuf_type, None, python_type)

    def register_enum_type(self, service: str, name: str, python_type: type) -> None:
        protobuf_type = _protobuf_type(KRPC.Type.ENUMERATION, service, name)
        key = protobuf_type.SerializeToString()
        assert key not in self._types
        self._types[key] = EnumerationType(protobuf_type, None, python_type)

    def as_type(self, protobuf_type: KRPC.Type, doc: str | None = None) -> TypeBase:
        """ Return a type object given a protocol buffer type """

        # Get cached type
        key = protobuf_type.SerializeToString()
        if key in self._types:
            return self._types[key]

        typ: TypeBase
        if protobuf_type.code in VALUE_TYPES:
            typ = ValueType(protobuf_type)
        elif protobuf_type.code == KRPC.Type.CLASS:
            typ = ClassType(protobuf_type, doc)
        elif protobuf_type.code == KRPC.Type.ENUMERATION:
            typ = EnumerationType(protobuf_type, doc)
        elif protobuf_type.code == KRPC.Type.TUPLE:
            typ = TupleType(protobuf_type, self)
        elif protobuf_type.code == KRPC.Type.LIST:
            typ = ListType(protobuf_type, self)
        elif protobuf_type.code == KRPC.Type.SET:
            typ = SetType(protobuf_type, self)
        elif protobuf_type.code == KRPC.Type.DICTIONARY:
            typ = DictionaryType(protobuf_type, self)
        elif protobuf_type.code in MESSAGE_TYPES:
            typ = MessageType(protobuf_type)
        else:
            raise ValueError('Invalid type')

        self._types[key] = typ
        return typ

    @classmethod
    def is_none_type(cls, protobuf_type: KRPC.Type) -> bool:
        return protobuf_type.code == KRPC.Type.NONE

    @property
    def double_type(self) -> ValueType:
        """ Get a double value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.DOUBLE)))

    @property
    def float_type(self) -> ValueType:
        """ Get a float value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.FLOAT)))

    @property
    def sint32_type(self) -> ValueType:
        """ Get an sint32 value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.SINT32)))

    @property
    def sint64_type(self) -> ValueType:
        """ Get an sint64 value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.SINT64)))

    @property
    def uint32_type(self) -> ValueType:
        """ Get a uint32 value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.UINT32)))

    @property
    def uint64_type(self) -> ValueType:
        """ Get a uint64 value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.UINT64)))

    @property
    def bool_type(self) -> ValueType:
        """ Get a bool value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.BOOL)))

    @property
    def string_type(self) -> ValueType:
        """ Get a string value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.STRING)))

    @property
    def bytes_type(self) -> TypeBase:
        """ Get a bytes value type """
        return cast(ValueType, self.as_type(_protobuf_type(KRPC.Type.BYTES)))

    def class_type(self, service: str, name: str, doc: Optional[str] = None) -> ClassType:
        """ Get a class type """
        return cast(
            ClassType,
            self.as_type(_protobuf_type(KRPC.Type.CLASS, service, name), doc=doc)
        )

    def enumeration_type(self, service: str, name: str,
                         doc: Optional[str] = None) -> EnumerationType:
        """ Get an enumeration type """
        return cast(
            EnumerationType,
            self.as_type(_protobuf_type(KRPC.Type.ENUMERATION, service, name), doc=doc)
        )

    def exception_type(self, service: str, name: str, doc: Optional[str] = None) -> Type[Exception]:
        """ Get an exception type """
        key = (service, name)
        if key not in self._exception_types:
            self._exception_types[key] = _create_exception_type(
                service, name, doc)
        return self._exception_types[key]

    def tuple_type(self, *value_types: TypeBase) -> TupleType:
        """ Get a tuple type """
        return cast(TupleType,
                    self.as_type(
                        _protobuf_type(KRPC.Type.TUPLE, None, None,
                                       [t.protobuf_type for t in value_types])))

    def list_type(self, value_type: TypeBase) -> ListType:
        """ Get a list type """
        return cast(ListType, self.as_type(
            _protobuf_type(KRPC.Type.LIST, None, None,
                           [value_type.protobuf_type])))

    def set_type(self, value_type: TypeBase) -> SetType:
        """ Get a set type """
        return cast(SetType, self.as_type(
            _protobuf_type(KRPC.Type.SET, None, None,
                           [value_type.protobuf_type])))

    def dictionary_type(self, key_type: TypeBase, value_type: TypeBase) -> DictionaryType:
        """ Get a dictionary type """
        return cast(DictionaryType, self.as_type(
            _protobuf_type(KRPC.Type.DICTIONARY, None, None,
                           [key_type.protobuf_type, value_type.protobuf_type])))

    @property
    def event_type(self) -> MessageType:
        """ Get an Event message type """
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.EVENT)))

    @property
    def procedure_call_type(self) -> MessageType:
        """ Get a ProcedureCall message type """
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.PROCEDURE_CALL)))

    @property
    def services_type(self) -> MessageType:
        """ Get a Services message type """
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.SERVICES)))

    @property
    def stream_type(self) -> MessageType:
        """ Get a Stream message type """
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.STREAM)))

    @property
    def status_type(self) -> MessageType:
        """ Get a Status message type """
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.STATUS)))

    def coerce_to(self, value: object, typ: TypeBase) -> object:
        """ Coerce a value to the specified type (specified by a type object).
            Raises ValueError if the coercion is not possible. """
        if isinstance(value, typ.python_type):
            return value
        # A NoneType can be coerced to a ClassType
        if isinstance(typ, ClassType) and value is None:
            return None
        # Coerce identical class types from different client connections
        if isinstance(typ, ClassType) and isinstance(value, ClassBase):
            value_type = type(value)
            if (
                typ.python_type._service_name ==  # type: ignore[attr-defined]
                value_type._service_name and  # type: ignore[attr-defined]
                typ.python_type._class_name ==  # type: ignore[attr-defined]
                value_type._class_name  # type: ignore[attr-defined]
            ):
                return typ.python_type(value._client, value._object_id)
        # Collection types
        try:
            # Coerce tuples to lists
            if isinstance(value, collections.abc.Iterable) and \
               isinstance(typ, ListType):
                return typ.python_type(
                    self.coerce_to(x, typ.value_type) for x in value)
            # Coerce lists (with appropriate number of elements) to tuples
            if isinstance(value, collections.abc.Iterable) and \
               isinstance(typ, TupleType):
                if len(value) != len(typ.value_types):  # type: ignore[arg-type]
                    raise ValueError
                return typ.python_type(
                    [self.coerce_to(x, typ.value_types[i])
                     for i, x in enumerate(value)])
        except ValueError as exn:
            raise ValueError('Failed to coerce value ' + str(value) +
                             ' of type ' + str(type(value)) +
                             ' to type ' + str(typ)) from exn
        # Numeric types
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int)
        if isinstance(value, bool) or \
           not any(isinstance(value, t) for t in numeric_types) or \
           typ.python_type not in numeric_types:
            raise ValueError('Failed to coerce value ' + str(value) +
                             ' of type ' + str(type(value)) +
                             ' to type ' + str(typ))
        if typ.python_type == float:
            return float(value)  # type: ignore[arg-type]
        return int(value)  # type: ignore[call-overload]


class TypeBase:
    """ Base class for all type objects """

    def __init__(self, protobuf_type: KRPC.Type, python_type: type, string: str) -> None:
        self._protobuf_type = protobuf_type
        self._python_type = python_type
        self._string = string

    @property
    def protobuf_type(self) -> KRPC.Type:
        """ Get the protocol buffer type string for the type """
        return self._protobuf_type

    @property
    def python_type(self) -> type:
        """ Get the python type """
        return self._python_type

    def __str__(self) -> str:
        return '<type: ' + str(self._string) + '>'


class ValueType(TypeBase):
    """ A protocol buffer value type """

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code not in VALUE_TYPES:
            raise ValueError('Not a value type')
        name = KRPC.Type.TypeCode.Name(protobuf_type.code)  # type: ignore[attr-defined]
        super().__init__(
            protobuf_type, VALUE_TYPES[protobuf_type.code], name.lower())


class ClassType(TypeBase):
    """ A class type, represented by a uint64 identifier """

    def __init__(self, protobuf_type: KRPC.Type, doc: Optional[str],
                 typ: Optional[type] = None) -> None:
        if protobuf_type.code != KRPC.Type.CLASS:
            raise ValueError('Not a class type')
        if not protobuf_type.service:
            raise ValueError('Class type has no service name')
        if not protobuf_type.name:
            raise ValueError('Class type has no class name')
        if typ is None:
            typ = _create_class_type(protobuf_type.service, protobuf_type.name, doc)
        string = 'Class(%s.%s)' % (protobuf_type.service, protobuf_type.name)
        super().__init__(protobuf_type, typ, string)


class EnumerationType(TypeBase):
    """ An enumeration type, represented by an sint32 value """

    def __init__(self, protobuf_type: KRPC.Type, doc: Optional[str],
                 typ: Optional[type] = None) -> None:
        if protobuf_type.code != KRPC.Type.ENUMERATION:
            raise ValueError('Not an enum type')
        if not protobuf_type.service:
            raise ValueError('Enum type has no service name')
        if not protobuf_type.name:
            raise ValueError('Enum type has no class name')
        self._service_name = protobuf_type.service
        self._enum_name = protobuf_type.name
        self._doc = doc
        string = 'Enum(%s.%s)' % (protobuf_type.service, protobuf_type.name)
        # When typ in None, set_values must
        # be called to set the python_type
        super().__init__(protobuf_type, cast(type, typ), string)

    def set_values(self, values: Mapping[str, Mapping[str, object]]) -> None:
        """ Set the python type. Creates an Enum class
            using the given values. """
        assert self._python_type is None
        self._python_type = _create_enum_type(
            self._enum_name, values, self._doc)


class TupleType(TypeBase):
    """ A tuple collection type """

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.TUPLE:
            raise ValueError('Not a tuple type')
        if len(protobuf_type.types) < 1:
            raise ValueError('Wrong number of sub-types for tuple type')
        self.value_types = [types.as_type(t) for t in protobuf_type.types]
        string = 'Tuple(%s)' % ','.join(t._string for t in self.value_types)
        super().__init__(protobuf_type, tuple, string)


class ListType(TypeBase):
    """ A list collection type """

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.LIST:
            raise ValueError('Not a list type')
        if len(protobuf_type.types) != 1:
            raise ValueError('Wrong number of sub-types for list type')
        self.value_type = types.as_type(protobuf_type.types[0])
        string = 'List(%s)' % self.value_type._string
        super().__init__(protobuf_type, list, string)


class SetType(TypeBase):
    """ A set collection type """

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.SET:
            raise ValueError('Not a set type')
        if len(protobuf_type.types) != 1:
            raise ValueError('Wrong number of sub-types for set type')
        self.value_type = types.as_type(protobuf_type.types[0])
        string = 'Set(%s)' % self.value_type._string
        super().__init__(protobuf_type, set, string)


class DictionaryType(TypeBase):
    """ A dictionary collection type """

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.DICTIONARY:
            raise ValueError('Not a dictionary type')
        if len(protobuf_type.types) != 2:
            raise ValueError('Wrong number of sub-types for dictionary type')
        self.key_type = types.as_type(protobuf_type.types[0])
        self.value_type = types.as_type(protobuf_type.types[1])
        string = 'Dict(%s,%s)' % \
                 (self.key_type._string, self.value_type._string)
        super().__init__(protobuf_type, dict, string)


class MessageType(TypeBase):
    """ A protocol buffer message type """

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code not in MESSAGE_TYPES:
            raise ValueError('Not a message type')
        typ = MESSAGE_TYPES[protobuf_type.code]
        super().__init__(protobuf_type, typ, typ.__name__)


class DynamicType:
    @classmethod
    def _add_method(cls,
                    name: str,
                    func: Callable,  # type: ignore[type-arg]
                    doc: Optional[str] = None) -> object:
        """ Add a method """
        func.__name__ = name
        func.__doc__ = doc
        setattr(cls, name, func)
        return getattr(cls, name)

    @classmethod
    def _add_class_method(cls,
                          name: str,
                          func: Callable,  # type: ignore[type-arg]
                          doc: Optional[str] = None) -> object:
        """ Add a static method """
        func.__name__ = name
        func.__doc__ = doc
        static_func = classmethod(func)
        setattr(cls, name, static_func)
        return getattr(cls, name)

    @classmethod
    def _add_property(cls,
                      name: str,
                      getter: Optional[Callable] = None,  # type: ignore[type-arg]
                      setter: Optional[Callable] = None,  # type: ignore[type-arg]
                      doc: Optional[str] = None) -> object:
        """ Add a property """
        if getter is None and setter is None:
            raise ValueError('Either getter or setter must be provided')
        prop = property(getter, setter, doc=doc)
        setattr(cls, name, prop)
        return getattr(cls, name)


class ClassBase:
    """ Base class for service-defined class types """
    def __init__(self,
                 client: Client,
                 object_id: int) -> None:
        self._client = client
        self._object_id = object_id

    def __eq__(self, other: object) -> bool:
        return isinstance(other, ClassBase) and \
            self._object_id == other._object_id

    def __ne__(self, other: object) -> bool:
        return not isinstance(other, ClassBase) or \
            self._object_id != other._object_id

    def __lt__(self, other: object) -> bool:
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id < other._object_id

    def __le__(self, other: object) -> bool:
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id <= other._object_id

    def __gt__(self, other: object) -> bool:
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id > other._object_id

    def __ge__(self, other: object) -> bool:
        if not isinstance(other, ClassBase):
            raise NotImplementedError
        return self._object_id >= other._object_id

    def __hash__(self) -> int:
        return hash(self._object_id)


class DynamicClassBase(ClassBase, DynamicType):
    def __repr__(self) -> str:
        return '<%s.%s remote object #%d>' % \
            (self._service_name, self._class_name, self._object_id)  # type: ignore[attr-defined]


def _create_class_type(service_name: str, class_name: str, doc: Optional[str]) -> type:
    return type(str(class_name), (DynamicClassBase,),
                {'_service_name': service_name,
                 '_class_name': class_name,
                 '__doc__': doc})


def _create_enum_type(enum_name: str, values: Mapping[str, Mapping[str, object]],
                      doc: Optional[str]) -> Enum:
    typ = Enum(enum_name, dict((name, x['value'])  # type: ignore[misc]
                               for name, x in values.items()))
    setattr(typ, '__doc__', doc)
    for name in values.keys():
        setattr(getattr(typ, name), '__doc__', values[name]['doc'])
    return typ  # type: ignore[return-value]


def _create_exception_type(service_name: str, class_name: str,
                           doc: Optional[str]) -> Type[Exception]:
    if service_name == 'KRPC' and class_name in EXCEPTION_TYPES:
        return EXCEPTION_TYPES[class_name]
    return type(str(class_name), (RuntimeError,),
                {'_service_name': service_name,
                 '_class_name': class_name,
                 '__doc__': doc})


class DefaultArgument:
    """ A sentinel value for default arguments """

    def __init__(self, value: str) -> None:
        self._value = value

    def __str__(self) -> str:
        return self._value

    def __repr__(self) -> str:
        return self._value


class WrappedClass:
    """ Wraps a class type, to allow injection of a client object
        for static method calls """

    def __init__(self, client: Client, class_type: type) -> None:
        self._client = client
        self._class_type = class_type
        self.__doc__ = class_type.__doc__

    def __call__(self, *args: object, **kwargs: object) -> object:
        return self._class_type(*args, **kwargs)

    def __getattr__(self, name: str) -> object:
        # FIXME: is this the best place to set it?
        # Might be better to dynamically create a type that derives from _class_type
        # and adds the _client field
        self._class_type._client = self._client  # type: ignore[attr-defined]
        return getattr(self._class_type, name)

    def __dir__(self) -> List[str]:
        return dir(self._class_type)


class DocEnum(Enum):
    def __new__(cls, value: int, doc: Optional[str] = None):  # type: ignore[no-untyped-def]
        self = object.__new__(cls)
        self._value_ = value
        if doc is not None:
            self.__doc__ = doc.strip()
        return self
