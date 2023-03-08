from __future__ import annotations
from typing import cast, Callable, Collection, Iterable, List, Mapping
from enum import Enum
# pylint: disable=import-error,no-name-in-module
import google.protobuf
from google.protobuf.internal import encoder as protobuf_encoder
# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import wire_format as protobuf_wire_format
from krpc.error import EncodingError
from krpc.platform import bytelength
import krpc.schema.KRPC_pb2 as KRPC
from krpc.types import (
    Types, TypeBase, ValueType, ClassType, EnumerationType, MessageType, TupleType,
    ListType, SetType, DictionaryType
)


# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
_pb_VarintEncoder = protobuf_encoder._VarintEncoder()  # type: ignore[attr-defined]
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()  # type: ignore[attr-defined]
_pb_DoubleEncoder = protobuf_encoder.DoubleEncoder(1, False, False)  # type: ignore[arg-type]
_pb_FloatEncoder = protobuf_encoder.FloatEncoder(1, False, False)  # type: ignore[arg-type]


class Encoder:
    """ Routines for encoding messages and values in
        the protocol buffer serialization format """

    _types = Types()

    @classmethod
    def encode(cls, x: object, typ: TypeBase) -> bytes:
        """ Encode a message or value of the given protocol buffer type """
        if isinstance(typ, MessageType):
            return cast(google.protobuf.message.Message, x).SerializeToString()
        if isinstance(typ, ValueType):
            return cls._encode_value(x, typ)
        if isinstance(typ, EnumerationType):
            return cls._encode_value(cast(Enum, x).value, cls._types.sint32_type)
        if isinstance(typ, ClassType):
            object_id = x._object_id if x is not None else 0  # type: ignore[attr-defined]
            return cls._encode_value(object_id, cls._types.uint64_type)
        if isinstance(typ, ListType):
            list_msg = KRPC.List()
            list_msg.items.extend(
                cls.encode(item, typ.value_type) for item in cast(Iterable[object], x)
            )
            return list_msg.SerializeToString()
        if isinstance(typ, DictionaryType):
            dict_msg = KRPC.Dictionary()
            entries = []
            dict_obj = cast(Mapping, x)  # type: ignore[type-arg]
            for key, value in sorted(
                    dict_obj.items(), key=lambda i: i[0]):  # type: ignore[no-any-return]
                entry = KRPC.DictionaryEntry()
                entry.key = cls.encode(key, typ.key_type)
                entry.value = cls.encode(value, typ.value_type)
                entries.append(entry)
            dict_msg.entries.extend(entries)
            return dict_msg.SerializeToString()
        if isinstance(typ, SetType):
            set_msg = KRPC.Set()
            set_msg.items.extend(cls.encode(item, typ.value_type)
                                 for item in cast(Iterable[object], x))
            return set_msg.SerializeToString()
        if isinstance(typ, TupleType):
            tuple_msg = KRPC.Tuple()
            tuple_obj = cast(Collection[object], x)
            if len(tuple_obj) != len(typ.value_types):
                raise EncodingError(
                    'Tuple has wrong number of elements. ' +
                    'Expected %d, got %d.' % (len(typ.value_types), len(tuple_obj)))
            tuple_msg.items.extend(cls.encode(item, value_type)
                                   for item, value_type in zip(tuple_obj, typ.value_types))
            return tuple_msg.SerializeToString()
        raise EncodingError(
                'Cannot encode objects of type ' + str(type(x)))

    @classmethod
    def encode_message_with_size(cls, message: google.protobuf.message.Message) -> bytes:
        """ Encode a protobuf message, prepended with its size """
        data = message.SerializeToString()
        size: bytes = protobuf_encoder._VarintBytes(len(data))  # type: ignore[attr-defined]
        return size + data

    @classmethod
    def _encode_value(cls, value: object, typ: TypeBase) -> bytes:
        if typ.protobuf_type.code == KRPC.Type.SINT32:
            return _ValueEncoder.encode_sint32(cast(int, value))
        if typ.protobuf_type.code == KRPC.Type.SINT64:
            return _ValueEncoder.encode_sint64(cast(int, value))
        if typ.protobuf_type.code == KRPC.Type.UINT32:
            return _ValueEncoder.encode_uint32(cast(int, value))
        if typ.protobuf_type.code == KRPC.Type.UINT64:
            return _ValueEncoder.encode_uint64(cast(int, value))
        if typ.protobuf_type.code == KRPC.Type.DOUBLE:
            return _ValueEncoder.encode_double(cast(float, value))
        if typ.protobuf_type.code == KRPC.Type.FLOAT:
            return _ValueEncoder.encode_float(cast(float, value))
        if typ.protobuf_type.code == KRPC.Type.BOOL:
            return _ValueEncoder.encode_bool(cast(bool, value))
        if typ.protobuf_type.code == KRPC.Type.STRING:
            return _ValueEncoder.encode_string(cast(str, value))
        if typ.protobuf_type.code == KRPC.Type.BYTES:
            return _ValueEncoder.encode_bytes(cast(bytes, value))
        raise EncodingError('Invalid type')


class _ValueEncoder:
    """ Routines for encoding values in the
        protocol buffer serialization format """

    @classmethod
    def encode_double(cls, value: float) -> bytes:
        data: List[bytes] = []

        def write(x: bytes) -> None:
            data.append(x)

        _pb_DoubleEncoder(write, value, True)  # type: ignore[operator]
        return b''.join(data[1:])  # strips the tag value

    @classmethod
    def encode_float(cls, value: float) -> bytes:
        data: List[bytes] = []

        def write(x: bytes) -> None:
            data.append(x)

        _pb_FloatEncoder(write, value, True)  # type: ignore[operator]
        return b''.join(data[1:])  # strips the tag value

    @classmethod
    def _encode_varint(cls, value: int) -> bytes:
        data: List[bytes] = []

        def write(x: bytes) -> None:
            data.append(x)

        _pb_VarintEncoder(write, value, True)
        return b''.join(data)

    @classmethod
    def _encode_signed_varint(cls, value: int) -> bytes:
        value = protobuf_wire_format.ZigZagEncode(value)  # type: ignore[no-untyped-call]
        data: List[bytes] = []
        _pb_SignedVarintEncoder(data.append, value, True)
        return b''.join(data)

    @classmethod
    def encode_sint32(cls, value: int) -> bytes:
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_sint64(cls, value: int) -> bytes:
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_uint32(cls, value: int) -> bytes:
        if value < 0:
            raise EncodingError('Value must be non-negative, got %d' % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_uint64(cls, value: int) -> bytes:
        if value < 0:
            raise EncodingError('Value must be non-negative, got %d' % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_bool(cls, value: bool) -> bytes:
        return cls._encode_varint(value)

    @classmethod
    def encode_string(cls, value: str) -> bytes:
        size = cls._encode_varint(bytelength(value))
        data = value.encode('utf-8')
        return size + data

    @classmethod
    def encode_bytes(cls, value: bytes) -> bytes:
        return b''.join([cls._encode_varint(len(value)), value])
