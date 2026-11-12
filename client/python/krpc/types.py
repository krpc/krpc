from __future__ import annotations

import collections
import copy
import sys
import warnings
from collections.abc import Callable, Iterable, Mapping
from enum import Enum, Flag
from typing import (
    TYPE_CHECKING,
    Any,
    TypeVar,
    cast,
)

import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from typing_extensions import deprecated as deprecated

    from krpc.connection import Connection
else:

    def deprecated(message, /, *, category=None, stacklevel=1):
        """The decorator of PEP 702 for a type checker. The built service warns at run time,
        and the client depends on no typing package."""

        def decorator(member):
            member.__deprecated__ = message
            return member

        return decorator


class UnknownTypeError(ValueError):
    """Raised when a type in a service definition is one this client cannot use, either
    because its type code is not one this client knows about, or because it names a structure
    whose definition was skipped. Both are what a definition from a newer server looks like.
    """


class EnumerationValueError(ValueError):
    """Raised for an enumeration value whose number the server's definition of the enumeration
    does not have."""


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
    KRPC.Type.PROCEDURE_CALL: KRPC.ProcedureCall,
    KRPC.Type.SERVICES: KRPC.Services,
}

# The builtin each .NET exception a server maps onto a kRPC exception stands for, keyed by the
# exception's mapped type. The kRPC exception is a subclass of the builtin. One without a
# builtin is a subclass of RuntimeError
NATIVE_EXCEPTIONS: dict[str, type[Exception]] = {
    "System.InvalidOperationException": RuntimeError,
    "System.ArgumentException": ValueError,
    "System.ArgumentNullException": ValueError,
    "System.ArgumentOutOfRangeException": ValueError,
    "System.NullReferenceException": TypeError,
    "System.Collections.Generic.KeyNotFoundException": KeyError,
    "System.IndexOutOfRangeException": IndexError,
    "System.DivideByZeroException": ZeroDivisionError,
    "System.NotSupportedException": NotImplementedError,
    "System.TimeoutException": TimeoutError,
    "System.OverflowException": OverflowError,
}


def native_exception(mapped_type: str) -> type[Exception]:
    """The builtin a kRPC exception with the given mapped type is a subclass of"""
    return NATIVE_EXCEPTIONS.get(mapped_type, RuntimeError)


# Every type code a type object can be built for. A definition from a newer server may name
# a type code outside this set, which leaves the definition partly unusable
_KNOWN_TYPE_CODES = (
    set(VALUE_TYPES)
    | set(MESSAGE_TYPES)
    | {
        KRPC.Type.NONE,
        KRPC.Type.CLASS,
        KRPC.Type.STREAM,
        KRPC.Type.EVENT,
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


def clear_type_ids(services: KRPC.Services) -> KRPC.Services:
    """Clear the ids of the types in the given service definitions, in place. The client names
    types by their names, and a type is compared with the types it builds itself."""
    for service in services.services:
        for procedure in service.procedures:
            for parameter in procedure.parameters:
                _clear_ids(parameter.type)
            # Clearing a field of an unset return type would set it
            if procedure.HasField("return_type"):
                _clear_ids(procedure.return_type)
        for struct in service.structs:
            for field in struct.fields:
                _clear_ids(field.type)
    return services


def _clear_ids(protobuf_type: KRPC.Type) -> None:
    protobuf_type.ClearField("service_id")
    protobuf_type.ClearField("id")
    for typ in protobuf_type.types:
        _clear_ids(typ)


class Types:
    """A type store. Used to obtain type objects from protocol buffer type
    strings, and stores python types for services and service defined
    class and enumeration types."""

    # The value types are held one attribute each, so that naming one costs an
    # attribute lookup on the hot path of every remote procedure call

    def __init__(self) -> None:
        # Mapping from protobuf type strings to type objects
        self._types: dict[bytes, TypeBase] = {}
        self._exception_types: dict[tuple[str, str], type[Exception]] = {}
        # Type objects keyed by the arguments they were asked for, rather than by
        # the serialized protobuf type that keys _types. A lookup here costs a tuple
        # hash instead of building and serializing a protobuf message.
        self._type_cache: dict[tuple[object, ...], TypeBase] = {}

        # The value, stream and event types are the same objects for the lifetime of
        # the store, so they are resolved once here rather than on every access
        self.double_type = self._value_type(KRPC.Type.DOUBLE)
        self.float_type = self._value_type(KRPC.Type.FLOAT)
        self.sint32_type = self._value_type(KRPC.Type.SINT32)
        self.sint64_type = self._value_type(KRPC.Type.SINT64)
        self.uint32_type = self._value_type(KRPC.Type.UINT32)
        self.uint64_type = self._value_type(KRPC.Type.UINT64)
        self.bool_type = self._value_type(KRPC.Type.BOOL)
        self.string_type = self._value_type(KRPC.Type.STRING)
        self.bytes_type = self._value_type(KRPC.Type.BYTES)
        self.stream_type = cast(
            StreamType, self.as_type(_protobuf_type(KRPC.Type.STREAM))
        )
        self.event_type = cast(EventType, self.as_type(_protobuf_type(KRPC.Type.EVENT)))
        self.no_value_type = NoValueType()

    def _value_type(self, code: KRPC.Type.TypeCode) -> ValueType:
        return cast(ValueType, self.as_type(_protobuf_type(code)))

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
        elif protobuf_type.code == KRPC.Type.STREAM:
            typ = StreamType(protobuf_type)
        elif protobuf_type.code == KRPC.Type.EVENT:
            typ = EventType(protobuf_type)
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

    def class_type(self, service: str, name: str, doc: str | None = None) -> ClassType:
        """Get a class type"""
        key = (KRPC.Type.CLASS, service, name)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(_protobuf_type(KRPC.Type.CLASS, service, name), doc=doc)
            self._type_cache[key] = typ
        return cast(ClassType, typ)

    def enumeration_type(
        self, service: str, name: str, doc: str | None = None
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
        self, service: str, name: str, doc: str | None = None
    ) -> StructType:
        """Get a structure type"""
        key = (KRPC.Type.STRUCT, service, name)
        typ = self._type_cache.get(key)
        if typ is None:
            typ = self.as_type(_protobuf_type(KRPC.Type.STRUCT, service, name), doc=doc)
            self._type_cache[key] = typ
        return cast(StructType, typ)

    def exception_type(
        self,
        service: str,
        name: str,
        doc: str | None = None,
        mapped_type: str = "",
        python_type: type[Exception] | None = None,
    ) -> type[Exception]:
        """Get an exception type. Creates one, unless a python type to use instead is given."""
        key = (service, name)
        if key not in self._exception_types:
            if python_type is None:
                python_type = _create_exception_type(service, name, doc, mapped_type)
            self._exception_types[key] = python_type
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

    def coerce_to(self, value: object, typ: TypeBase) -> object:
        """Coerce a value to the specified type (specified by a type object).
        Raises ValueError if the coercion is not possible."""
        # A null stands at a position that can hold one, whatever the type there is
        if typ.nullable and value is None:
            return None
        if isinstance(value, typ.python_type):
            return value
        # An enumeration value of the same service and name, from a definition that differs,
        # converts by its number, as a value inside a collection is sent by its number too
        if (
            isinstance(typ, EnumerationType)
            and isinstance(value, (RemoteEnum, RemoteFlags))
            and _enum_names(type(value)) == (typ._service_name, typ._enum_name)
        ):
            try:
                return cast(type[Enum], typ.python_type)(value.value)
            except ValueError as exn:
                raise EnumerationValueError(
                    "%s is numbered %d, which the server's definition of %s.%s does not have"
                    % (value, value.value, typ._service_name, typ._enum_name)
                ) from exn
        # Coerce identical class types from different client connections
        if isinstance(typ, ClassType) and isinstance(value, RemoteObject):
            value_type = type(value)
            if (
                typ.python_type._service_name == value_type._service_name  # pyright: ignore[reportAttributeAccessIssue]
                and typ.python_type._class_name == value_type._class_name  # pyright: ignore[reportAttributeAccessIssue]
            ):
                return typ.python_type(value._connection, value._object_id)
        # Collection types
        try:
            # Coerce tuples to lists
            if isinstance(value, Iterable) and isinstance(typ, ListType):
                return typ.python_type(self.coerce_to(x, typ.value_type) for x in value)
            # Coerce lists (with appropriate number of elements) to tuples
            if isinstance(value, Iterable) and isinstance(typ, TupleType):
                if len(value) != len(typ.value_types):  # pyright: ignore[reportArgumentType]
                    raise ValueError
                return typ.python_type(
                    [self.coerce_to(x, typ.value_types[i]) for i, x in enumerate(value)]
                )
            # Coerce tuples and lists (with the right number of elements) to structures,
            # taking their elements as the fields in order
            if isinstance(value, Iterable) and isinstance(typ, StructType):
                values = list(value)
                if len(values) != len(typ.field_types):
                    raise ValueError
                return typ.python_type(
                    *[
                        self.coerce_to(x, typ.field_types[i])
                        for i, x in enumerate(values)
                    ]
                )
        except EnumerationValueError:
            raise
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
        if typ.python_type is float:
            return float(value)  # pyright: ignore[reportArgumentType]
        return int(value)  # pyright: ignore[reportArgumentType]


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
        self._nullable_type: TypeBase | None = None
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
        name = KRPC.Type.TypeCode.Name(protobuf_type.code)
        super().__init__(protobuf_type, VALUE_TYPES[protobuf_type.code], name.lower())


class ClassType(TypeBase):
    """A class type, represented by a uint64 identifier"""

    def __init__(
        self, protobuf_type: KRPC.Type, doc: str | None, typ: type | None = None
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
        self, protobuf_type: KRPC.Type, doc: str | None, typ: type | None = None
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

    def set_values(
        self,
        values: Mapping[str, Mapping[str, object]],
        python_type: type | None = None,
        flags: bool = False,
    ) -> None:
        """Set the python type. Creates an Enum class using the given values, or a Flag class
        for a flags enumeration, unless a python type to use instead is given."""
        assert self.python_type is None
        if python_type is None:
            python_type = _create_enum_type(
                self._service_name, self._enum_name, values, self._doc, flags
            )
        self.python_type = python_type
        if self._nullable_type is not None:
            self._nullable_type.python_type = self.python_type


class StructType(TypeBase):
    """A structure type, whose value is the values of its fields"""

    def __init__(
        self, protobuf_type: KRPC.Type, doc: str | None, typ: type | None = None
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
        self.field_names: list[str] = []
        self.field_types: list[TypeBase] = []
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
        fields: Iterable[tuple[str, TypeBase]],
        python_type: type | None = None,
        deprecated_fields: Mapping[str, str] | None = None,
    ) -> None:
        """Set the fields of the structure, as pairs of a name and a type in the order the
        structure declares them. Creates a named tuple to represent a value of the
        structure, unless a python type to use instead is given. Reading a field in
        deprecated_fields warns with the message it maps to."""
        assert self.python_type is None
        fields = list(fields)
        self.field_names = [name for name, _ in fields]
        self.field_types = [typ for _, typ in fields]
        if python_type is None:
            python_type = _create_struct_type(
                self._struct_name, self.field_names, self._doc, deprecated_fields or {}
            )
        self.python_type = python_type
        if isinstance(self._nullable_type, StructType):
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


class StreamType(TypeBase):
    """A stream, represented by a uint64 identifier"""

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code != KRPC.Type.STREAM:
            raise ValueError("Not a stream type")
        super().__init__(protobuf_type, int, "Stream")


class EventType(TypeBase):
    """An event, represented by the uint64 identifier of its stream. The client wraps
    the identifier in an Event."""

    def __init__(self, protobuf_type: KRPC.Type) -> None:
        if protobuf_type.code != KRPC.Type.EVENT:
            raise ValueError("Not an event type")
        super().__init__(protobuf_type, int, "Event")


class NoValueType(TypeBase):
    """The type of a stream of a call or function with no value. Its value is None."""

    def __init__(self) -> None:
        super().__init__(_protobuf_type(KRPC.Type.NONE), type(None), "None")


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
        func: Callable,
        doc: str | None = None,
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
        func: Callable,
        doc: str | None = None,
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
        getter: Callable | None = None,
        setter: Callable | None = None,
        doc: str | None = None,
    ) -> object:
        """Add a property"""
        if getter is None and setter is None:
            raise ValueError("Either getter or setter must be provided")
        prop = property(getter, setter, doc=doc)
        setattr(cls, name, prop)
        return getattr(cls, name)


class RemoteObject(DynamicType):
    """Base class for service-defined class types"""

    def __init__(self, connection: Connection, object_id: int) -> None:
        self._connection = connection
        self._object_id = object_id

    def __eq__(self, other: object) -> bool:
        return isinstance(other, RemoteObject) and self._object_id == other._object_id

    def __ne__(self, other: object) -> bool:
        return (
            not isinstance(other, RemoteObject) or self._object_id != other._object_id
        )

    def __lt__(self, other: object) -> bool:
        if not isinstance(other, RemoteObject):
            raise NotImplementedError
        return self._object_id < other._object_id

    def __le__(self, other: object) -> bool:
        if not isinstance(other, RemoteObject):
            raise NotImplementedError
        return self._object_id <= other._object_id

    def __gt__(self, other: object) -> bool:
        if not isinstance(other, RemoteObject):
            raise NotImplementedError
        return self._object_id > other._object_id

    def __ge__(self, other: object) -> bool:
        if not isinstance(other, RemoteObject):
            raise NotImplementedError
        return self._object_id >= other._object_id

    def __hash__(self) -> int:
        return hash(self._object_id)


class DynamicRemoteObject(RemoteObject):
    # Set on each class created for a service
    _service_name: str
    _class_name: str

    def __repr__(self) -> str:
        return "<%s.%s remote object #%d>" % (
            self._service_name,
            self._class_name,
            self._object_id,
        )


def _create_class_type(service_name: str, class_name: str, doc: str | None) -> type:
    return type(
        str(class_name),
        (DynamicRemoteObject,),
        {"_service_name": service_name, "_class_name": class_name, "__doc__": doc},
    )


def _create_enum_type(
    service_name: str,
    enum_name: str,
    values: Mapping[str, Mapping[str, object]],
    doc: str | None,
    flags: bool = False,
) -> type[Enum]:
    base = RemoteFlags if flags else RemoteEnum
    typ = base(
        enum_name,
        dict((name, x["value"]) for name, x in values.items()),
    )
    typ.__doc__ = doc
    typ._service_name = service_name  # pyright: ignore[reportAttributeAccessIssue]
    typ._class_name = enum_name  # pyright: ignore[reportAttributeAccessIssue]
    for name in values.keys():
        getattr(typ, name).__doc__ = values[name]["doc"]
    for name, value in values.items():
        message = value.get("deprecated")
        if message:
            deprecate_attribute(cast(type, typ), name, cast(str, message))
    return typ  # pyright: ignore[reportReturnType]


def _create_struct_type(
    struct_name: str,
    field_names: list[str],
    doc: str | None,
    deprecated_fields: Mapping[str, str],
) -> type:
    typ = collections.namedtuple(struct_name, field_names)
    typ.__doc__ = doc
    for name, message in deprecated_fields.items():
        deprecate_attribute(typ, name, message)
    return typ


def _create_exception_type(
    service_name: str, class_name: str, doc: str | None, mapped_type: str
) -> type[Exception]:
    return type(
        str(class_name),
        (native_exception(mapped_type),),
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


def deprecation_message(name: str, reason: str) -> str:
    """The text of the warning for a use of a deprecated member"""
    return (
        "%s is deprecated: %s" % (name, reason) if reason else name + " is deprecated"
    )


def warn_deprecated(message: str) -> None:
    """Warn of the use of a deprecated member, at the first caller outside the client, as a
    program can reach the member through several of the client's functions."""
    frame = sys._getframe(0)
    level = 1
    while frame is not None and frame.f_globals.get("__name__", "").startswith("krpc."):
        frame = frame.f_back
        level += 1
    warnings.warn(message, DeprecationWarning, stacklevel=level)


class _DeprecatedAttribute:
    """A class attribute whose reads warn, in place of an enumeration value or a struct field"""

    def __init__(self, value: Any, message: str) -> None:
        self._value = value
        self._message = message

    def __get__(self, obj: object, objtype: type | None = None) -> Any:
        warnings.warn(self._message, DeprecationWarning, stacklevel=2)
        bind = getattr(type(self._value), "__get__", None)
        return self._value if bind is None else bind(self._value, obj, objtype)


def deprecate_attribute(cls: type, name: str, message: str) -> None:
    """Warn on each read of a value of an enumeration or a field of a structure. The client's
    own encoding and decoding read neither by name, so a value the server sends does not warn."""
    # An enumeration refuses a new value for a member through its own __setattr__
    type.__setattr__(cls, name, _DeprecatedAttribute(getattr(cls, name), message))


class DeprecatedDefinition:
    """A class, enumeration, structure or exception on a service, whose reads warn"""

    def __init__(self, name: str, message: str, value: object) -> None:
        self._name = name
        self._message = message
        self.value = value

    def __get__(self, obj: object, objtype: type | None = None) -> Any:
        if obj is None:
            return self
        warnings.warn(self._message, DeprecationWarning, stacklevel=2)
        return self.value

    def __set__(self, obj: object, value: object) -> None:
        raise AttributeError("%s cannot be set" % self._name)


_RemoteEnumT = TypeVar("_RemoteEnumT", bound="RemoteEnum")


def _enum_names(cls: type) -> tuple[object, object] | None:
    """The service and name of an enumeration, which a generated one has once registered and a
    built one has once created"""
    service_name = getattr(cls, "_service_name", None)
    if service_name is None:
        return None
    return service_name, getattr(cls, "_class_name", None)


class RemoteEnum(Enum):
    """The base of a service's enumerations."""

    def __new__(
        cls: type[_RemoteEnumT], value: int, doc: str | None = None
    ) -> _RemoteEnumT:
        self = object.__new__(cls)
        self._value_ = value
        if doc is not None:
            self.__doc__ = doc.strip()
        return self


_RemoteFlagsT = TypeVar("_RemoteFlagsT", bound="RemoteFlags")


class RemoteFlags(Flag):
    """The base of a service's flags enumerations, whose values combine with ``|``. A
    combination of values is a value too, and ``in`` tests whether it holds one."""

    def __new__(
        cls: type[_RemoteFlagsT], value: int, doc: str | None = None
    ) -> _RemoteFlagsT:
        self = object.__new__(cls)
        self._value_ = value
        if doc is not None:
            self.__doc__ = doc.strip()
        return self
