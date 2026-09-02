from __future__ import annotations
from typing import (
    cast,
    Callable,
    Iterable,
    List,
    Mapping,
    Optional,
    Tuple,
    Type,
    TYPE_CHECKING,
)
import struct
import google.protobuf

# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import decoder as protobuf_decoder

# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import wire_format as protobuf_wire_format
from krpc.error import EncodingError
import krpc.platform
from krpc.platform import hexlify
from krpc.types import (
    Types,
    TypeBase,
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    StructType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


# protobuf's varint reader and zigzag decoder are internal and carry no types, so they are
# bound here with the types they do have. A call carrying a collection reaches them once
# per value, so this also saves looking them up on their module that many times
_pb_decode_varint: Callable[[bytes, int], Tuple[int, int]] = (
    protobuf_decoder._DecodeVarint  # type: ignore[attr-defined]
)
_pb_zigzag_decode: Callable[[int], int] = protobuf_wire_format.ZigZagDecode


def _decode_varint(data: bytes) -> Tuple[int, int]:
    """Decode the varint at the start of the data, returning its value and the
    number of bytes it occupies. Raises IndexError if the data does not hold a
    complete varint.

    A varint whose first byte has the top bit clear holds its own value in that
    byte. Message sizes, and the length prefixes of strings and bytes, are
    almost always that, so the common case is an index rather than a call into
    protobuf's varint reader."""
    first = data[0]
    if first < 128:
        return first, 1
    return _pb_decode_varint(data, 0)


class Decoder:
    """Routines for decoding messages and values from
    the protocol buffer serialization format"""

    GUID_LENGTH = 16

    _types = Types()

    @classmethod
    def guid(cls, data: bytes) -> str:
        """Decode a 16-byte GUID into a string"""
        return "-".join(
            (
                hexlify(data[3::-1]),
                hexlify(data[5:3:-1]),
                hexlify(data[7:5:-1]),
                hexlify(data[8:10]),
                hexlify(data[10:16]),
            )
        )

    @classmethod
    def decode(cls, client: Optional[Client], data: bytes, typ: TypeBase) -> object:
        """Given a python type, and serialized data, decode the value.

        This decodes a value at a slot, where a null is carried by the is_null flag of the
        message around it. A value at a position inside another value carries its own
        presence bool, which _item_decoder reads."""
        # The value types come first, as most results are one, and this is on the hot path
        # of every remote procedure call
        if isinstance(typ, ValueType):
            return cls._decode_value(data, typ)
        if isinstance(typ, MessageType):
            return cls.decode_message(data, typ.python_type)
        if isinstance(typ, EnumerationType):
            value = cls._decode_value(data, cls._types.sint32_type)
            return typ.python_type(value)
        if isinstance(typ, ClassType):
            object_id_typ = cls._types.uint64_type
            object_id = cls._decode_value(data, object_id_typ)
            return typ.python_type(client, object_id)
        msg: object
        if isinstance(typ, ListType):
            msg = cast(KRPC.List, cls.decode_message(data, KRPC.List))
            decode_item = cls._item_decoder(client, typ.value_type)
            return [decode_item(item) for item in msg.items]
        if isinstance(typ, DictionaryType):
            msg = cast(KRPC.Dictionary, cls.decode_message(data, KRPC.Dictionary))
            decode_key = cls._item_decoder(client, typ.key_type)
            decode_value = cls._item_decoder(client, typ.value_type)
            return dict(
                (decode_key(entry.key), decode_value(entry.value))
                for entry in msg.entries
            )
        if isinstance(typ, SetType):
            msg = cast(KRPC.Set, cls.decode_message(data, KRPC.Set))
            decode_item = cls._item_decoder(client, typ.value_type)
            return set(decode_item(item) for item in msg.items)
        if isinstance(typ, TupleType):
            msg = cast(KRPC.Tuple, cls.decode_message(data, KRPC.Tuple))
            return tuple(cls._decode_items(client, msg.items, typ.value_types))
        if isinstance(typ, StructType):
            # A structure is encoded as the values of its fields in order, which is the
            # same encoding as a tuple of those values
            msg = cast(KRPC.Tuple, cls.decode_message(data, KRPC.Tuple))
            if len(msg.items) < len(typ.field_types):
                raise EncodingError(
                    "Struct has wrong number of fields. "
                    + "Expected at least %d, got %d."
                    % (len(typ.field_types), len(msg.items))
                )
            return typ.python_type(
                *cls._decode_items(client, msg.items, typ.field_types)
            )
        raise EncodingError("Cannot decode type %s" % str(typ))

    @classmethod
    def decode_message_size(cls, data: bytes) -> int:
        return _decode_varint(data)[0]

    @classmethod
    def decode_size_prefix(cls, data: bytes) -> Tuple[int, int]:
        """Decode the size prefix of a message, returning the size of the message
        and the number of bytes the prefix itself occupies. Raises IndexError if
        the data does not yet hold a complete prefix."""
        return _decode_varint(data)

    @classmethod
    def decode_message(
        cls, data: bytes, typ: Type[google.protobuf.message.Message]
    ) -> google.protobuf.message.Message:
        message = typ()
        message.ParseFromString(data)
        return message

    @classmethod
    def _decode_items(
        cls,
        client: Optional[Client],
        items: Iterable[bytes],
        types: Iterable[TypeBase],
    ) -> List[object]:
        """The decoded value of each item of a tuple or a structure"""
        return [cls._item_decoder(client, typ)(item) for item, typ in zip(items, types)]

    @classmethod
    def _item_decoder(
        cls, client: Optional[Client], typ: TypeBase
    ) -> Callable[[bytes], object]:
        """A function that decodes one value at a position of the given type"""
        decode = cls._value_decoder(client, typ)
        if not typ.nullable:
            return decode

        # The presence bool is one byte, as a bool value always is, and is read from the
        # data directly
        def decode_nullable(data: bytes) -> object:
            if not data:
                raise EncodingError("A nullable value carries no presence bool")
            return decode(data[1:]) if data[0] else None

        return decode_nullable

    @classmethod
    def _value_decoder(
        cls, client: Optional[Client], typ: TypeBase
    ) -> Callable[[bytes], object]:
        """A function that decodes one value of the given type.

        A collection carries many values of the same type, so working out how to decode
        one of them is worth doing once for the collection rather than once for every
        item in it. The types a collection usually holds are answered directly; anything
        else falls back to the full decode."""
        if isinstance(typ, ValueType):
            decode = _VALUE_DECODERS.get(typ.code)
            if decode is None:
                raise EncodingError("Invalid type")
            return decode
        if isinstance(typ, ClassType):
            decode_id = _VALUE_DECODERS[cls._types.uint64_type.code]
            python_type = typ.python_type
            return lambda data: python_type(client, decode_id(data))
        if isinstance(typ, EnumerationType):
            decode_value = _VALUE_DECODERS[cls._types.sint32_type.code]
            enum_type = typ.python_type
            return lambda data: enum_type(decode_value(data))
        return lambda data: cls.decode(client, data, typ)

    @classmethod
    def _decode_value(cls, data: bytes, typ: TypeBase) -> object:
        decode = _VALUE_DECODERS.get(typ.code)
        if decode is None:
            raise EncodingError("Invalid type")
        return decode(data)


class _ValueDecoder:
    """Routines for encoding values from
    the protocol buffer serialization format"""

    @classmethod
    def _decode_signed_varint(cls, data: bytes) -> int:
        # The zigzag payload is an unsigned varint. Reading it as a signed one sign extends
        # it as two's complement first, which corrupts anything from 2**62 up once the
        # payload sets bit 63, where long.MaxValue decodes as -1. The single byte case is
        # read here and not through the general _decode_varint: a collection reaches this
        # once per value it carries, and a value from -64 to 63 is one byte.
        first = data[0]
        if first < 128:
            return _pb_zigzag_decode(first)
        return _pb_zigzag_decode(_pb_decode_varint(data, 0)[0])

    @classmethod
    def _decode_varint(cls, data: bytes) -> int:
        return _decode_varint(data)[0]

    # The code for the following two methods is taken from
    # google.protobuf.internal.decoder._FloatDecoder and _DoubleDecoder
    # Copyright 2008, Google Inc.
    # See protobuf-license.txt distributed with this file

    @classmethod
    def decode_double(cls, data: bytes) -> float:
        local_unpack = struct.unpack

        # We expect a 64-bit value in little-endian byte order.  Bit 1 is the
        # sign bit, bits 2-12 represent the exponent, and bits 13-64 are the
        # significand.
        double_bytes = data[0:8]

        # If this value has all its exponent bits set and at least one
        # significand bit set, it's not a number.  In Python 2.4, struct.unpack
        # will treat it as inf or -inf.  To avoid that, we treat it specially.
        if (
            (double_bytes[7:8] in b"\x7f\xff")
            and (double_bytes[6:7] >= b"\xf0")
            and (double_bytes[0:7] != b"\x00\x00\x00\x00\x00\x00\xf0")
        ):
            return krpc.platform.NAN

        # Note that we expect someone up-stack to catch struct.error and
        # convert it to _DecodeError -- this way we don't have to set up
        # exception-handling blocks every time we parse one value.
        return cast(float, local_unpack("<d", double_bytes)[0])

    @classmethod
    def decode_float(cls, data: bytes) -> float:
        local_unpack = struct.unpack

        # We expect a 32-bit value in little-endian byte order.  Bit 1 is the
        # sign bit, bits 2-9 represent the exponent, and bits 10-32 are
        # the significand.
        float_bytes = data[0:4]

        # If this value has all its exponent bits set, then it's non-finite.
        # In Python 2.4, struct.unpack will convert it to a finite 64-bit
        # value. To avoid that, we parse it specially.
        if float_bytes[3:4] in b"\x7f\xff" and float_bytes[2:3] >= b"\x80":
            # If at least one significand bit is set...
            if float_bytes[0:3] != b"\x00\x00\x80":
                return krpc.platform.NAN
            # If sign bit is set...
            if float_bytes[3:4] == b"\xff":
                return krpc.platform.NEG_INF
            return krpc.platform.POS_INF

        # Note that we expect someone up-stack to catch struct.error and
        # convert it to _DecodeError -- this way we don't have to set up
        # exception-handling blocks every time we parse one value.
        return cast(float, local_unpack("<f", float_bytes)[0])

    # End of code taken from google.protobuf.internal.decoder._FloatDecoder
    # and google.protobuf.internal.decoder._DoubleDecoder

    @classmethod
    def decode_bool(cls, data: bytes) -> bool:
        return bool(cls._decode_varint(data))

    @classmethod
    def decode_string(cls, data: bytes) -> str:
        size, position = _decode_varint(data)
        return data[position : position + size].decode("utf-8")

    @classmethod
    def decode_bytes(cls, data: bytes) -> bytes:
        size, position = _decode_varint(data)
        return data[position : position + size]


# The decoder for each value type, looked up by type code, as this is on the hot path of
# every remote procedure call. The signed and unsigned integer types share a decoder
# apiece, named for what it reads
_VALUE_DECODERS: Mapping[KRPC.Type.TypeCode, Callable[[bytes], object]] = {
    KRPC.Type.SINT32: _ValueDecoder._decode_signed_varint,
    KRPC.Type.SINT64: _ValueDecoder._decode_signed_varint,
    KRPC.Type.UINT32: _ValueDecoder._decode_varint,
    KRPC.Type.UINT64: _ValueDecoder._decode_varint,
    KRPC.Type.DOUBLE: _ValueDecoder.decode_double,
    KRPC.Type.FLOAT: _ValueDecoder.decode_float,
    KRPC.Type.BOOL: _ValueDecoder.decode_bool,
    KRPC.Type.STRING: _ValueDecoder.decode_string,
    KRPC.Type.BYTES: _ValueDecoder.decode_bytes,
}
