from __future__ import annotations

import collections
import copy
import functools
import weakref
from enum import Enum
from typing import (
    TYPE_CHECKING,
    Any,
    Callable,
    Dict,
    Iterable,
    List,
    Mapping,
    Optional,
    Tuple,
    Type,
    cast,
)

import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


class UnknownTypeError(ValueError):
    """Raised when a type in a service definition is one this client cannot use, either
    because its type code is not one this client knows about, or because it names a structure
    whose definition was skipped. Both are what a definition from a newer server looks like.
    """


VALUE_TYPES = {
    KRPC.Type.DOUBLE: float,
    KRPC.Type.FLOAT: float,
    KRPC.Type.SINT32: int,
    KRPC.Type.SINT64: int,
    KRPC.Type.UINT32: int,
    KRPC.Type.UINT64: int,
    KRPC.Type.BOOL: bool,
    KRPC.Type.STRING: str,
    KRPC.Type.BYTES: bytes,
}

MESSAGE_TYPES = {
    KRPC.Type.EVENT: KRPC.Event,
    KRPC.Type.PROCEDURE_CALL: KRPC.ProcedureCall,
    KRPC.Type.SERVICES: KRPC.Services,
    KRPC.Type.STREAM: KRPC.Stream,
    KRPC.Type.STATUS: KRPC.Status,
}

EXCEPTION_TYPES = {
    "InvalidOperationException": RuntimeError,
    "ArgumentException": ValueError,
    "ArgumentNullException": ValueError,
    "ArgumentOutOfRangeException": ValueError,
}


# Every type code a type object can be built for. A definition from a newer server may name
# a type code outside this set, which leaves the definition partly unusable
_KNOWN_TYPE_CODES = (
    set(VALUE_TYPES)
    | set(MESSAGE_TYPES)
    | {
        KRPC.Type.NONE,
        KRPC.Type.CLASS,
        KRPC.Type.ENUMERATION,
        KRPC.Type.STRUCT,
        KRPC.Type.TUPLE,
        KRPC.Type.LIST,
        KRPC.Type.SET,
        KRPC.Type.DICTIONARY,
    }
)


def is_a_known_type(protobuf_type: KRPC.Type) -> bool:
    """Whether a type object can be built for the given protocol buffer type. It cannot when
    the type, or one it contains, has a type code this client does not know about."""
    if protobuf_type.code not in _KNOWN_TYPE_CODES:
        return False
    return all(is_a_known_type(typ) for typ in protobuf_type.types)


def _protobuf_type(
    code: KRPC.Type.TypeCode,
    service: str | None = None,
    name: str | None = None,
    types: list[KRPC.Type] | None = None,
) -> KRPC.Type:
    protobuf_type = KRPC.Type()
    protobuf_type.code = code
    if service is not None:
        protobuf_type.service = service
    if name is not None:
        protobuf_type.name = name
    if types is not None:
        protobuf_type.types.extend(types)
    return protobuf_type


def _nullable_protobuf_type(protobuf_type: KRPC.Type, nullable: bool) -> KRPC.Type:
    """A copy of the given protocol buffer type, marked as the given nullability"""
    result = KRPC.Type()
    result.CopyFrom(protobuf_type)
    result.nullable = nullable
    return result


class Types:
    """A type store. Used to obtain type objects from protocol buffer type
    strings, and stores python types for services and service defined
    class and enumeration types."""

    # The value types are held one attribute each, so that naming one costs an
    # attribute lookup on the hot path of every remote procedure call
    # pylint: disable=too-many-instance-attributes

    def __init__(self) -> None:
        # Mapping from protobuf type strings to type objects
        self._types: dict[bytes, TypeBase] = {}
        self._exception_types: dict[tuple[str, str], Type[Exception]] = {}
        # Type objects keyed by the arguments they were asked for, rather than by
        # the serialized protobuf type that keys _types. The generated service
        # stubs name their parameter and return types on every call, so this is on
        # the hot path of every remote procedure call; a lookup here costs a tuple
        # hash instead of building and serializing a protobuf message.
        self._type_cache: dict[tuple[object, ...], TypeBase] = {}

        # The value types are the same objects for the lifetime of the store, so
        # they are resolved once here rather than on every access
        self.double_type = self._value_type(KRPC.Type.DOUBLE)
        self.float_type = self._value_type(KRPC.Type.FLOAT)
        self.sint32_type = self._value_type(KRPC.Type.SINT32)
        self.sint64_type = self._value_type(KRPC.Type.SINT64)
        self.uint32_type = self._value_type(KRPC.Type.UINT32)
        self.uint64_type = self._value_type(KRPC.Type.UINT64)
        self.bool_type = self._value_type(KRPC.Type.BOOL)
        self.string_type = self._value_type(KRPC.Type.STRING)
        self.bytes_type = self._value_type(KRPC.Type.BYTES)
        self.event_type = cast(
            MessageType, self.as_type(_protobuf_type(KRPC.Type.EVENT))
        )

    def _value_type(self, code: int) -> ValueType:
        return cast(ValueType, self.as_type(_protobuf_type(code)))

    def register_class_type(self, service: str, name: str, python_type: type) -> None:
        protobuf_type = _protobuf_type(KRPC.Type.CLASS, service, name)
        key = protobuf_type.SerializeToString()
        assert key not in self._types
        typ = ClassType(protobuf_type, None, python_type)
        self._types[key] = typ
        self._type_cache[(KRPC.Type.CLASS, service, name)] = typ

    def register_enum_type(self, service: str, name: str, python_type: type) -> None:
        protobuf_type = _protobuf_type(KRPC.Type.ENUMERATION, service, name)
        key = protobuf_type.SerializeToString()
        assert key not in self._types
        typ = EnumerationType(protobuf_type, None, python_type)
        self._types[key] = typ
        self._type_cache[(KRPC.Type.ENUMERATION, service, name)] = typ

    def as_type(self, protobuf_type: KRPC.Type, doc: str | None = None) -> TypeBase:
        """Return a type object given a protocol buffer type"""

        # Get cached type
        key = protobuf_type.SerializeToString()
        if key in self._types:
            return self._types[key]

        # A nullable type is built from the type it is the nullable form of, so that the two
        # share a python type
        if protobuf_type.nullable:
            non_nullable = _nullable_protobuf_type(protobuf_type, False)
            typ = self.as_type(non_nullable, doc)._as_nullable()
            self._types[key] = typ
            return typ

        typ: TypeBase
        if protobuf_type.code in VALUE_TYPES:
            typ = ValueType(protobuf_type)
        elif protobuf_type.code == KRPC.Type.CLASS:
            typ = ClassType(protobuf_type, doc)
        elif protobuf_type.code == KRPC.Type.ENUMERATION:
            typ = EnumerationType(protobuf_type, doc)
        elif protobuf_type.code == KRPC.Type.STRUCT:
            typ = StructType(protobuf_type, doc)
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
            raise UnknownTypeError("Unknown type code %d" % protobuf_type.code)

        self._types[key] = typ
        return typ

    @classmethod
    def is_none_type(cls, protobuf_type: KRPC.Type) -> bool:
        return protobuf_type.code == KRPC.Type.NONE

    def nullable(self, typ: TypeBase) -> TypeBase:
        """Get the given type at a position that can hold null"""
        if typ.nullable:
            return typ
        # The nullable form is held on the type itself, so that naming one costs an
        # attribute lookup on the hot path of every remote procedure call
        nullable_type = typ._nullable_type
        if nullable_type is None:
            nullable_type = typ._as_nullable()
            self._types.setdefault(
                nullable_type.protobuf_type.SerializeToString(), nullable_type
            )
        return nullable_type

    def class_type(
        self, service: str, name: str, doc: Optional[str] = None
    ) -> ClassType:
        """Get a class type"""
        key = (KRPC.Type.CLASS, service, name)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(_protobuf_type(KRPC.Type.CLASS, service, name), doc=doc)
            self._type_cache[key] = typ
        return cast(ClassType, typ)

    def enumeration_type(
        self, service: str, name: str, doc: Optional[str] = None
    ) -> EnumerationType:
        """Get an enumeration type"""
        key = (KRPC.Type.ENUMERATION, service, name)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(
                _protobuf_type(KRPC.Type.ENUMERATION, service, name), doc=doc
            )
            self._type_cache[key] = typ
        return cast(EnumerationType, typ)

    def struct_type(
        self, service: str, name: str, doc: Optional[str] = None
    ) -> StructType:
        """Get a structure type"""
        key = (KRPC.Type.STRUCT, service, name)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(_protobuf_type(KRPC.Type.STRUCT, service, name), doc=doc)
            self._type_cache[key] = typ
        return cast(StructType, typ)

    def exception_type(
        self, service: str, name: str, doc: Optional[str] = None
    ) -> Type[Exception]:
        """Get an exception type"""
        key = (service, name)
        if key not in self._exception_types:
            self._exception_types[key] = _create_exception_type(service, name, doc)
        return self._exception_types[key]

    def tuple_type(self, *value_types: TypeBase) -> TupleType:
        """Get a tuple type"""
        key: tuple[object, ...] = (KRPC.Type.TUPLE,) + value_types
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(
                _protobuf_type(
                    KRPC.Type.TUPLE, None, None, [t.protobuf_type for t in value_types]
                )
            )
            self._type_cache[key] = typ
        return cast(TupleType, typ)

    def list_type(self, value_type: TypeBase) -> ListType:
        """Get a list type"""
        key = (KRPC.Type.LIST, value_type)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(
                _protobuf_type(KRPC.Type.LIST, None, None, [value_type.protobuf_type])
            )
            self._type_cache[key] = typ
        return cast(ListType, typ)

    def set_type(self, value_type: TypeBase) -> SetType:
        """Get a set type"""
        key = (KRPC.Type.SET, value_type)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(
                _protobuf_type(KRPC.Type.SET, None, None, [value_type.protobuf_type])
            )
            self._type_cache[key] = typ
        return cast(SetType, typ)

    def dictionary_type(
        self, key_type: TypeBase, value_type: TypeBase
    ) -> DictionaryType:
        """Get a dictionary type"""
        key = (KRPC.Type.DICTIONARY, key_type, value_type)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(
                _protobuf_type(
                    KRPC.Type.DICTIONARY,
                    None,
                    None,
                    [key_type.protobuf_type, value_type.protobuf_type],
                )
            )
            self._type_cache[key] = typ
        return cast(DictionaryType, typ)

    @property
    def procedure_call_type(self) -> MessageType:
        """Get a ProcedureCall message type"""
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.PROCEDURE_CALL)))

    @property
    def services_type(self) -> MessageType:
        """Get a Services message type"""
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.SERVICES)))

    @property
    def stream_type(self) -> MessageType:
        """Get a Stream message type"""
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.STREAM)))

    @property
    def status_type(self) -> MessageType:
        """Get a Status message type"""
        return cast(MessageType, self.as_type(_protobuf_type(KRPC.Type.STATUS)))

    def coerce_to(self, value: object, typ: TypeBase) -> object:
        """Coerce a value to the specified type (specified by a type object).
        Raises ValueError if the coercion is not possible."""
        # A null stands at a position that can hold one, whatever the type there is
        if typ.nullable and value is None:
            return None
        if isinstance(value, typ.python_type):
            return value
        # Coerce identical class types from different client connections
        if isinstance(typ, ClassType) and isinstance(value, ClassBase):
            value_type = type(value)
            if (
                typ.python_type._service_name  # type: ignore[attr-defined]
                == value_type._service_name  # type: ignore[attr-defined]
                and typ.python_type._class_name  # type: ignore[attr-defined]
                == value_type._class_name  # type: ignore[attr-defined]
            ):
                return typ.python_type(value._client, value._object_id)
        # Collection types
        try:
            # Coerce tuples to lists
            if isinstance(value, collections.abc.Iterable) and isinstance(
                typ, ListType
            ):
                return typ.python_type(self.coerce_to(x, typ.value_type) for x in value)
            # Coerce lists (with appropriate number of elements) to tuples
            if isinstance(value, collections.abc.Iterable) and isinstance(
                typ, TupleType
            ):
                if len(value) != len(typ.value_types):  # type: ignore[arg-type]
                    raise ValueError
                return typ.python_type(
                    [self.coerce_to(x, typ.value_types[i]) for i, x in enumerate(value)]
                )
            # Coerce tuples and lists (with the right number of elements) to structures,
            # taking their elements as the fields in order
            if isinstance(value, collections.abc.Iterable) and isinstance(
                typ, StructType
            ):
                values = list(value)
                if len(values) != len(typ.field_types):
                    raise ValueError
                return typ.python_type(
                    *[
                        self.coerce_to(x, typ.field_types[i])
                        for i, x in enumerate(values)
                    ]
                )
        except ValueError as exn:
            raise ValueError(
                "Failed to coerce value "
                + str(value)
                + " of type "
                + str(type(value))
                + " to type "
                + str(typ)
            ) from exn
        # Numeric types
        # See http://docs.python.org/2/reference/datamodel.html#coercion-rules
        numeric_types = (float, int)
        if (
            isinstance(value, bool)
            or not any(isinstance(value, t) for t in numeric_types)
            or typ.python_type not in numeric_types
        ):
            raise ValueError(
                "Failed to coerce value "
                + str(value)
                + " of type "
                + str(type(value))
                + " to type "
                + str(typ)
            )
        if typ.python_type == float:
            return float(value)  # type: ignore[arg-type]
        return int(value)  # type: ignore[call-overload]


class TypeBase:
    """Base class for all type objects"""

    # The protocol buffer type, the python type and the type code are plain
    # attributes rather than properties: naming one is on the hot path of every
    # remote procedure call, where a property would cost a call

    def __init__(
        self, protobuf_type: KRPC.Type, python_type: type, string: str
    ) -> None:
        self.protobuf_type = protobuf_type
        self.python_type = python_type
        # The type code the encoder and decoder select on. Held here as well as in the
        # protocol buffer type, as reading it from there is a protocol buffer field access
        self.code = protobuf_type.code
        # Whether the position a value of this type sits in can hold null
        self.nullable = protobuf_type.nullable
        # The nullable form of this type, built on demand by _as_nullable
        self._nullable_type: Optional[TypeBase] = None
        self._string = string

    def __str__(self) -> str:
        return "<type: " + str(self._string) + ">"

    def _as_nullable(self) -> TypeBase:
        """This type at a position that can hold null.

        A copy of this type, so that the two share a python type and a class has one python
        type however the position that holds it is declared."""
        if self.nullable:
            return self
        if self._nullable_type is None:
            typ = copy.copy(self)
            typ.protobuf_type = _nullable_protobuf_type(self.protobuf_type, True)
            typ.nullable = True
            typ._string = self._string + "?"
            self._nullable_type = typ
        return self._nullable_type


class ValueType(TypeBase):
    """A protocol buffer value type"""

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code not in VALUE_TYPES:
            raise ValueError("Not a value type")
        name = KRPC.Type.TypeCode.Name(protobuf_type.code)  # type: ignore[attr-defined]
        super().__init__(protobuf_type, VALUE_TYPES[protobuf_type.code], name.lower())


class ClassType(TypeBase):
    """A class type, represented by a uint64 identifier"""

    def __init__(
        self, protobuf_type: KRPC.Type, doc: Optional[str], typ: Optional[type] = None
    ) -> None:
        if protobuf_type.code != KRPC.Type.CLASS:
            raise ValueError("Not a class type")
        if not protobuf_type.service:
            raise ValueError("Class type has no service name")
        if not protobuf_type.name:
            raise ValueError("Class type has no class name")
        if typ is None:
            typ = _create_class_type(protobuf_type.service, protobuf_type.name, doc)
        string = "Class(%s.%s)" % (protobuf_type.service, protobuf_type.name)
        super().__init__(protobuf_type, typ, string)


class EnumerationType(TypeBase):
    """An enumeration type, represented by an sint32 value"""

    def __init__(
        self, protobuf_type: KRPC.Type, doc: Optional[str], typ: Optional[type] = None
    ) -> None:
        if protobuf_type.code != KRPC.Type.ENUMERATION:
            raise ValueError("Not an enum type")
        if not protobuf_type.service:
            raise ValueError("Enum type has no service name")
        if not protobuf_type.name:
            raise ValueError("Enum type has no class name")
        self._service_name = protobuf_type.service
        self._enum_name = protobuf_type.name
        self._doc = doc
        string = "Enum(%s.%s)" % (protobuf_type.service, protobuf_type.name)
        # When typ in None, set_values must
        # be called to set the python_type
        super().__init__(protobuf_type, cast(type, typ), string)

    @property
    def has_values(self) -> bool:
        """Whether the values of the enumeration are known, which they are
        only once its definition has been registered by calling set_values"""
        return self.python_type is not None

    def set_values(self, values: Mapping[str, Mapping[str, object]]) -> None:
        """Set the python type. Creates an Enum class
        using the given values."""
        assert self.python_type is None
        self.python_type = _create_enum_type(self._enum_name, values, self._doc)
        if self._nullable_type is not None:
            self._nullable_type.python_type = self.python_type


class StructType(TypeBase):
    """A structure type, whose value is the values of its fields"""

    def __init__(
        self, protobuf_type: KRPC.Type, doc: Optional[str], typ: Optional[type] = None
    ) -> None:
        if protobuf_type.code != KRPC.Type.STRUCT:
            raise ValueError("Not a struct type")
        if not protobuf_type.service:
            raise ValueError("Struct type has no service name")
        if not protobuf_type.name:
            raise ValueError("Struct type has no struct name")
        self._service_name = protobuf_type.service
        self._struct_name = protobuf_type.name
        self._doc = doc
        # The names and types of the fields, in the order their values are encoded in. Empty
        # until set_fields is called, as the field list is not carried by the type itself
        self.field_names: List[str] = []
        self.field_types: List[TypeBase] = []
        string = "Struct(%s.%s)" % (protobuf_type.service, protobuf_type.name)
        # When typ is None, set_fields must be called to set the python_type
        super().__init__(protobuf_type, cast(type, typ), string)

    @property
    def has_fields(self) -> bool:
        """Whether the fields of the structure are known, which they are only once its
        definition has been registered by calling set_fields"""
        return self.python_type is not None

    def set_fields(
        self,
        fields: Iterable[Tuple[str, TypeBase]],
        python_type: Optional[type] = None,
    ) -> None:
        """Set the fields of the structure, as pairs of a name and a type in the order the
        structure declares them. Creates a named tuple to represent a value of the
        structure, unless a python type to use instead is given."""
        assert self.python_type is None
        fields = list(fields)
        self.field_names = [name for name, _ in fields]
        self.field_types = [typ for _, typ in fields]
        if python_type is None:
            python_type = _create_struct_type(
                self._struct_name, self.field_names, self._doc
            )
        self.python_type = python_type
        if self._nullable_type is not None:
            self._nullable_type.set_fields(fields, python_type)


class TupleType(TypeBase):
    """A tuple collection type"""

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.TUPLE:
            raise ValueError("Not a tuple type")
        if len(protobuf_type.types) < 1:
            raise ValueError("Wrong number of sub-types for tuple type")
        self.value_types = [types.as_type(t) for t in protobuf_type.types]
        string = "Tuple(%s)" % ",".join(t._string for t in self.value_types)
        super().__init__(protobuf_type, tuple, string)


class ListType(TypeBase):
    """A list collection type"""

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.LIST:
            raise ValueError("Not a list type")
        if len(protobuf_type.types) != 1:
            raise ValueError("Wrong number of sub-types for list type")
        self.value_type = types.as_type(protobuf_type.types[0])
        string = "List(%s)" % self.value_type._string
        super().__init__(protobuf_type, list, string)


class SetType(TypeBase):
    """A set collection type"""

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.SET:
            raise ValueError("Not a set type")
        if len(protobuf_type.types) != 1:
            raise ValueError("Wrong number of sub-types for set type")
        self.value_type = types.as_type(protobuf_type.types[0])
        string = "Set(%s)" % self.value_type._string
        super().__init__(protobuf_type, set, string)


class DictionaryType(TypeBase):
    """A dictionary collection type"""

    def __init__(self, protobuf_type: KRPC.Type, types: Types) -> None:
        if protobuf_type.code != KRPC.Type.DICTIONARY:
            raise ValueError("Not a dictionary type")
        if len(protobuf_type.types) != 2:
            raise ValueError("Wrong number of sub-types for dictionary type")
        self.key_type = types.as_type(protobuf_type.types[0])
        self.value_type = types.as_type(protobuf_type.types[1])
        string = "Dict(%s,%s)" % (self.key_type._string, self.value_type._string)
        super().__init__(protobuf_type, dict, string)


class MessageType(TypeBase):
    """A protocol buffer message type"""

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code not in MESSAGE_TYPES:
            raise ValueError("Not a message type")
        typ = MESSAGE_TYPES[protobuf_type.code]
        super().__init__(protobuf_type, typ, typ.__name__)


def check_type_is_known(typ: TypeBase) -> None:
    """Raise an UnknownTypeError if the given type, or a type it contains, is a structure
    whose definition was skipped and whose fields are therefore not known. Whatever names
    such a type cannot be encoded or decoded, and is skipped in turn."""
    if isinstance(typ, StructType):
        if not typ.has_fields:
            raise UnknownTypeError(
                "The definition of the struct %s.%s was skipped"
                % (typ.protobuf_type.service, typ.protobuf_type.name)
            )
        for field_type in typ.field_types:
            check_type_is_known(field_type)
    elif isinstance(typ, TupleType):
        for value_type in typ.value_types:
            check_type_is_known(value_type)
    elif isinstance(typ, (ListType, SetType)):
        check_type_is_known(typ.value_type)
    elif isinstance(typ, DictionaryType):
        check_type_is_known(typ.key_type)
        check_type_is_known(typ.value_type)


class DynamicType:
    @classmethod
    def _add_method(
        cls,
        name: str,
        func: Callable,  # type: ignore[type-arg]
        doc: Optional[str] = None,
    ) -> object:
        """Add a method"""
        func.__name__ = name
        func.__doc__ = doc
        setattr(cls, name, func)
        return getattr(cls, name)

    @classmethod
    def _add_class_method(
        cls,
        name: str,
        func: Callable,  # type: ignore[type-arg]
        doc: Optional[str] = None,
    ) -> object:
        """Add a static method"""
        func.__name__ = name
        func.__doc__ = doc
        static_func = classmethod(func)
        setattr(cls, name, static_func)
        return getattr(cls, name)

    @classmethod
    def _add_property(
        cls,
        name: str,
        getter: Optional[Callable] = None,  # type: ignore[type-arg]
        setter: Optional[Callable] = None,  # type: ignore[type-arg]
        doc: Optional[str] = None,
    ) -> object:
        """Add a property"""
        if getter is None and setter is None:
            raise ValueError("Either getter or setter must be provided")
        prop = property(getter, setter, doc=doc)
        setattr(cls, name, prop)
        return getattr(cls, name)


class ClassBase(DynamicType):
    """Base class for service-defined class types"""

    def __init__(self, client: Client, object_id: int) -> None:
        self._client = client
        self._object_id = object_id

    def __eq__(self, other: object) -> bool:
        return isinstance(other, ClassBase) and self._object_id == other._object_id

    def __ne__(self, other: object) -> bool:
        return not isinstance(other, ClassBase) or self._object_id != other._object_id

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


class DynamicClassBase(ClassBase):
    def __repr__(self) -> str:
        return "<%s.%s remote object #%d>" % (
            self._service_name,
            self._class_name,
            self._object_id,
        )  # type: ignore[attr-defined]


def _create_class_type(service_name: str, class_name: str, doc: Optional[str]) -> type:
    return type(
        str(class_name),
        (DynamicClassBase,),
        {"_service_name": service_name, "_class_name": class_name, "__doc__": doc},
    )


def _create_enum_type(
    enum_name: str, values: Mapping[str, Mapping[str, object]], doc: Optional[str]
) -> Enum:
    typ = Enum(
        enum_name,
        dict((name, x["value"]) for name, x in values.items()),  # type: ignore[misc]
    )
    setattr(typ, "__doc__", doc)
    for name in values.keys():
        setattr(getattr(typ, name), "__doc__", values[name]["doc"])
    return typ  # type: ignore[return-value]


def _create_struct_type(
    struct_name: str, field_names: List[str], doc: Optional[str]
) -> type:
    typ = collections.namedtuple(struct_name, field_names)  # type: ignore[misc]
    setattr(typ, "__doc__", doc)
    return typ


def _create_exception_type(
    service_name: str, class_name: str, doc: Optional[str]
) -> Type[Exception]:
    if service_name == "KRPC" and class_name in EXCEPTION_TYPES:
        return EXCEPTION_TYPES[class_name]
    return type(
        str(class_name),
        (RuntimeError,),
        {"_service_name": service_name, "_class_name": class_name, "__doc__": doc},
    )


class DefaultArgument:
    """A sentinel value for default arguments"""

    def __init__(self, value: str) -> None:
        self._value = value

    def __str__(self) -> str:
        return self._value

    def __repr__(self) -> str:
        return self._value


# Per-client subclasses of wrapped classes, keyed weakly by client then by the shared class. With
# pre-generated stubs the class object is shared across every client, so binding a client to it must
# not be done by writing _client onto the shared class (which multiple clients would overwrite for
# each other). Each client gets its own subclass carrying its own _client instead.
_wrapped_subclasses: "weakref.WeakKeyDictionary[object, Dict[type, type]]" = (
    weakref.WeakKeyDictionary()
)


def _wrapped_subclass(client: object, class_type: type) -> type:
    """Return the given client's subclass of class_type, carrying that client as _client, creating
    and caching it on first use."""
    cache = _wrapped_subclasses.setdefault(client, {})
    subclass = cache.get(class_type)
    if subclass is None:
        subclass = type(class_type.__name__, (class_type,), {"_client": client})
        cache[class_type] = subclass
    return subclass


class WrappedClass:
    """Wraps a class type accessed through a service, binding it to the client it was accessed
    through so that its static methods use that client."""

    def __init__(self, client: Client, class_type: type) -> None:
        self._client = client
        self._class_type = _wrapped_subclass(client, class_type)
        self.__doc__ = class_type.__doc__

    def __call__(self, *args: object, **kwargs: object) -> object:
        return self._class_type(*args, **kwargs)

    def __getattr__(self, name: str) -> object:
        return getattr(self._class_type, name)

    def __dir__(self) -> List[str]:
        return dir(self._class_type)


class StaticMethod:
    """Descriptor for static methods.

    Like @classmethod, but also works when called on an instance: if the class does not already
    have _client set, the instance's _client is injected onto it first. A class accessed through a
    service is a per-client subclass (see WrappedClass) that already carries its own _client, so
    this only applies to static methods called through an instance."""

    def __init__(self, func: Callable) -> None:  # type: ignore[type-arg]
        self._func = func

    def __get__(  # type: ignore[type-arg]
        self, obj: object, objtype: Optional[type] = None
    ) -> Callable:
        if objtype is None:
            objtype = type(obj)
        if obj is not None and not hasattr(objtype, "_client"):
            objtype._client = obj._client  # type: ignore[attr-defined]

        @functools.wraps(self._func)
        def bound(*args: object, **kwargs: object) -> object:
            return self._func(objtype, *args, **kwargs)

        bound.__self__ = objtype  # type: ignore[attr-defined]
        return bound


class DocEnum(Enum):
    def __new__(cls, value: int, doc: Optional[str] = None):  # type: ignore[no-untyped-def]
        self = object.__new__(cls)
        self._value_ = value
        if doc is not None:
            self.__doc__ = doc.strip()
        return self
