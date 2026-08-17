from __future__ import annotations
from typing import cast, Any, Callable, Collection, Iterable, List, Mapping
from enum import Enum
import struct

# pylint: disable=import-error,no-name-in-module
import google.protobuf
from google.protobuf.internal import encoder as protobuf_encoder

# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import wire_format as protobuf_wire_format
from krpc.error import EncodingError
from krpc.platform import bytelength
import krpc.schema.KRPC_pb2 as KRPC
from krpc.types import (
    Types,
    TypeBase,
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)

# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
_pb_VarintEncoder = protobuf_encoder._VarintEncoder()  # type: ignore[attr-defined]  # pylint: disable=invalid-name
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()  # type: ignore[attr-defined]  # pylint: disable=invalid-name

# protobuf's zigzag encoder carries no types, so it is bound here with the type it does
# have rather than described again at every call. A call carrying a collection reaches it
# once per value, so this also saves looking it up on its module that many times
_pb_zigzag_encode: Callable[[int], int] = protobuf_wire_format.ZigZagEncode

# The one byte encoding of every value that fits in one. A varint below 128 is
# the byte itself, and message sizes and the length prefixes of strings and
# bytes almost always are, so the common case is a lookup rather than a loop
# that builds a list and joins it
_SINGLE_BYTE_VARINTS = tuple(bytes((value,)) for value in range(128))


class Encoder:
    """Routines for encoding messages and values in
    the protocol buffer serialization format"""

    _types = Types()

    @classmethod
    def encode(cls, x: object, typ: TypeBase) -> bytes:
        """Encode a message or value of the given protocol buffer type"""
        # The value types come first: they are what most arguments are, and this is
        # on the hot path of every remote procedure call
        if isinstance(typ, ValueType):
            return cls._encode_value(x, typ)
        if isinstance(typ, MessageType):
            return cast(google.protobuf.message.Message, x).SerializeToString()
        if isinstance(typ, EnumerationType):
            return cls._encode_value(cast(Enum, x).value, cls._types.sint32_type)
        if isinstance(typ, ClassType):
            object_id = x._object_id  # type: ignore[attr-defined]
            return cls._encode_value(object_id, cls._types.uint64_type)
        if isinstance(typ, ListType):
            list_msg = KRPC.List()
            encode_item = cls._item_encoder(typ.value_type)
            list_msg.items.extend(
                encode_item(item) for item in cast(Iterable[object], x)
            )
            return list_msg.SerializeToString()
        if isinstance(typ, DictionaryType):
            dict_msg = KRPC.Dictionary()
            entries = []
            dict_obj = cast(Mapping, x)  # type: ignore[type-arg]
            encode_key = cls._item_encoder(typ.key_type)
            encode_value = cls._item_encoder(typ.value_type)
            for key, value in sorted(
                dict_obj.items(), key=lambda i: i[0]
            ):  # type: ignore[no-any-return]
                entry = KRPC.DictionaryEntry()
                entry.key = encode_key(key)
                entry.value = encode_value(value)
                entries.append(entry)
            dict_msg.entries.extend(entries)
            return dict_msg.SerializeToString()
        if isinstance(typ, SetType):
            set_msg = KRPC.Set()
            encode_item = cls._item_encoder(typ.value_type)
            set_msg.items.extend(
                encode_item(item) for item in cast(Iterable[object], x)
            )
            return set_msg.SerializeToString()
        if isinstance(typ, TupleType):
            tuple_msg = KRPC.Tuple()
            tuple_obj = cast(Collection[object], x)
            if len(tuple_obj) != len(typ.value_types):
                raise EncodingError(
                    "Tuple has wrong number of elements. "
                    + "Expected %d, got %d." % (len(typ.value_types), len(tuple_obj))
                )
            tuple_msg.items.extend(
                cls.encode(item, value_type)
                for item, value_type in zip(tuple_obj, typ.value_types)
            )
            return tuple_msg.SerializeToString()
        raise EncodingError("Cannot encode objects of type " + str(type(x)))

    @classmethod
    def encode_message_with_size(
        cls, message: google.protobuf.message.Message
    ) -> bytes:
        """Encode a protobuf message, prepended with its size"""
        data = message.SerializeToString()
        length = len(data)
        if length < 128:
            return _SINGLE_BYTE_VARINTS[length] + data
        size: bytes = protobuf_encoder._VarintBytes(length)  # type: ignore[attr-defined]
        return size + data

    @classmethod
    def _item_encoder(cls, typ: TypeBase) -> Callable[[Any], bytes]:
        """A function that encodes one value of the given type.

        A collection carries many values of the same type, so working out how to encode
        one of them is worth doing once for the collection rather than once for every
        item in it. The types a collection usually holds are answered directly; anything
        else falls back to the full encode.
        """
        if isinstance(typ, ValueType):
            encode = _VALUE_ENCODERS.get(typ.code)
            if encode is None:
                raise EncodingError("Invalid type")
            return encode
        if isinstance(typ, ClassType):
            encode_id = _VALUE_ENCODERS[cls._types.uint64_type.code]
            return lambda x: encode_id(x._object_id)
        if isinstance(typ, EnumerationType):
            encode_value = _VALUE_ENCODERS[cls._types.sint32_type.code]
            return lambda x: encode_value(x.value)
        return lambda x: cls.encode(x, typ)

    @classmethod
    def _encode_value(cls, value: object, typ: TypeBase) -> bytes:
        encode = _VALUE_ENCODERS.get(typ.code)
        if encode is None:
            raise EncodingError("Invalid type")
        return encode(value)


class _ValueEncoder:
    """Routines for encoding values in the
    protocol buffer serialization format"""

    @classmethod
    def encode_double(cls, value: float) -> bytes:
        return struct.pack("<d", value)

    @classmethod
    def encode_float(cls, value: float) -> bytes:
        return struct.pack("<f", value)

    @classmethod
    def _encode_varint(cls, value: int) -> bytes:
        if value < 128:
            return _SINGLE_BYTE_VARINTS[value]
        data: List[bytes] = []
        _pb_VarintEncoder(data.append, value, True)
        return b"".join(data)

    @classmethod
    def _encode_signed_varint(cls, value: int) -> bytes:
        value = _pb_zigzag_encode(value)
        if value < 128:
            return _SINGLE_BYTE_VARINTS[value]
        data: List[bytes] = []
        _pb_SignedVarintEncoder(data.append, value, True)
        return b"".join(data)

    @classmethod
    def encode_uint32(cls, value: int) -> bytes:
        if value < 0:
            raise EncodingError("Value must be non-negative, got %d" % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_uint64(cls, value: int) -> bytes:
        if value < 0:
            raise EncodingError("Value must be non-negative, got %d" % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_bool(cls, value: bool) -> bytes:
        return cls._encode_varint(value)

    @classmethod
    def encode_string(cls, value: str) -> bytes:
        size = cls._encode_varint(bytelength(value))
        data = value.encode("utf-8")
        return size + data

    @classmethod
    def encode_bytes(cls, value: bytes) -> bytes:
        return b"".join([cls._encode_varint(len(value)), value])


# The encoder for each value type, looked up by type code rather than found by
# comparing against each code in turn, as this is on the hot path of every
# remote procedure call. The signed integer types share an encoder, named for
# what it writes rather than for one of the types that write it
_VALUE_ENCODERS: Mapping[KRPC.Type.TypeCode, Callable[[Any], bytes]] = {
    KRPC.Type.SINT32: _ValueEncoder._encode_signed_varint,
    KRPC.Type.SINT64: _ValueEncoder._encode_signed_varint,
    KRPC.Type.UINT32: _ValueEncoder.encode_uint32,
    KRPC.Type.UINT64: _ValueEncoder.encode_uint64,
    KRPC.Type.DOUBLE: _ValueEncoder.encode_double,
    KRPC.Type.FLOAT: _ValueEncoder.encode_float,
    KRPC.Type.BOOL: _ValueEncoder.encode_bool,
    KRPC.Type.STRING: _ValueEncoder.encode_string,
    KRPC.Type.BYTES: _ValueEncoder.encode_bytes,
}
