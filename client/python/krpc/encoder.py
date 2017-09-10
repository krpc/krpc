# pylint: disable=import-error,no-name-in-module
import google.protobuf
from google.protobuf.internal import encoder as protobuf_encoder
# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import wire_format as protobuf_wire_format
from krpc.error import EncodingError
from krpc.platform import bytelength
import krpc.schema.KRPC_pb2 as KRPC
from krpc.types import \
    Types, ValueType, ClassType, EnumerationType, MessageType, TupleType, \
    ListType, SetType, DictionaryType


# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
# pylint: disable=invalid-name
_pb_VarintEncoder = protobuf_encoder._VarintEncoder()
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()
_pb_DoubleEncoder = protobuf_encoder.DoubleEncoder(1, False, False)
_pb_FloatEncoder = protobuf_encoder.FloatEncoder(1, False, False)
_pb_version = google.protobuf.__version__.split('.')
if int(_pb_version[0]) >= 3 and int(_pb_version[1]) >= 4:
    # protobuf v3.4.0 and above
    def _VarintEncoder(write, value):
        return _pb_VarintEncoder(write, value, True)

    def _SignedVarintEncoder(write, value):
        return _pb_SignedVarintEncoder(write, value, True)

    def _DoubleEncoder(write, value):
        return _pb_DoubleEncoder(write, value, True)

    def _FloatEncoder(write, value):
        return _pb_FloatEncoder(write, value, True)
else:
    # protobuf v3.3.0 and below
    _VarintEncoder = _pb_VarintEncoder
    _SignedVarintEncoder = _pb_SignedVarintEncoder
    _DoubleEncoder = _pb_DoubleEncoder
    _FloatEncoder = _pb_FloatEncoder
# pylint: enable=invalid-name


class Encoder(object):
    """ Routines for encoding messages and values in
        the protocol buffer serialization format """

    _types = Types()

    @classmethod
    def encode(cls, x, typ):
        """ Encode a message or value of the given protocol buffer type """
        if isinstance(typ, MessageType):
            return x.SerializeToString()
        elif isinstance(typ, ValueType):
            return cls._encode_value(x, typ)
        elif isinstance(typ, EnumerationType):
            return cls._encode_value(x.value, cls._types.sint32_type)
        elif isinstance(typ, ClassType):
            object_id = x._object_id if x is not None else 0
            return cls._encode_value(object_id, cls._types.uint64_type)
        elif isinstance(typ, ListType):
            msg = KRPC.List()
            msg.items.extend(cls.encode(item, typ.value_type) for item in x)
            return msg.SerializeToString()
        elif isinstance(typ, DictionaryType):
            msg = KRPC.Dictionary()
            entries = []
            for key, value in sorted(x.items(), key=lambda i: i[0]):
                entry = KRPC.DictionaryEntry()
                entry.key = cls.encode(key, typ.key_type)
                entry.value = cls.encode(value, typ.value_type)
                entries.append(entry)
            msg.entries.extend(entries)
            return msg.SerializeToString()
        elif isinstance(typ, SetType):
            msg = KRPC.Set()
            msg.items.extend(cls.encode(item, typ.value_type) for item in x)
            return msg.SerializeToString()
        elif isinstance(typ, TupleType):
            msg = KRPC.Tuple()
            if len(x) != len(typ.value_types):
                raise EncodingError(
                    'Tuple has wrong number of elements. ' +
                    'Expected %d, got %d.' % (len(typ.value_types), len(x)))
            msg.items.extend(cls.encode(item, value_type)
                             for item, value_type in zip(x, typ.value_types))
            return msg.SerializeToString()
        else:
            raise EncodingError(
                'Cannot encode objects of type ' + str(type(x)))

    @classmethod
    def encode_message_with_size(cls, message):
        """ Encode a protobuf message, prepended with its size """
        data = message.SerializeToString()
        size = protobuf_encoder._VarintBytes(len(data))
        return size + data

    @classmethod
    def _encode_value(cls, value, typ):
        if typ.protobuf_type.code == KRPC.Type.SINT32:
            return _ValueEncoder.encode_sint32(value)
        elif typ.protobuf_type.code == KRPC.Type.SINT64:
            return _ValueEncoder.encode_sint64(value)
        elif typ.protobuf_type.code == KRPC.Type.UINT32:
            return _ValueEncoder.encode_uint32(value)
        elif typ.protobuf_type.code == KRPC.Type.UINT64:
            return _ValueEncoder.encode_uint64(value)
        elif typ.protobuf_type.code == KRPC.Type.DOUBLE:
            return _ValueEncoder.encode_double(value)
        elif typ.protobuf_type.code == KRPC.Type.FLOAT:
            return _ValueEncoder.encode_float(value)
        elif typ.protobuf_type.code == KRPC.Type.BOOL:
            return _ValueEncoder.encode_bool(value)
        elif typ.protobuf_type.code == KRPC.Type.STRING:
            return _ValueEncoder.encode_string(value)
        elif typ.protobuf_type.code == KRPC.Type.BYTES:
            return _ValueEncoder.encode_bytes(value)
        else:
            raise EncodingError('Invalid type')


class _ValueEncoder(object):
    """ Routines for encoding values in the
        protocol buffer serialization format """

    @classmethod
    def encode_double(cls, value):
        data = []

        def write(x):
            data.append(x)

        _DoubleEncoder(write, value)
        return b''.join(data[1:])  # strips the tag value

    @classmethod
    def encode_float(cls, value):
        data = []

        def write(x):
            data.append(x)

        _FloatEncoder(write, value)
        return b''.join(data[1:])  # strips the tag value

    @classmethod
    def _encode_varint(cls, value):
        data = []

        def write(x):
            data.append(x)

        _VarintEncoder(write, value)
        return b''.join(data)

    @classmethod
    def _encode_signed_varint(cls, value):
        value = protobuf_wire_format.ZigZagEncode(value)
        data = []
        _SignedVarintEncoder(data.append, value)
        return b''.join(data)

    @classmethod
    def encode_sint32(cls, value):
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_sint64(cls, value):
        return cls._encode_signed_varint(value)

    @classmethod
    def encode_uint32(cls, value):
        if value < 0:
            raise EncodingError('Value must be non-negative, got %d' % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_uint64(cls, value):
        if value < 0:
            raise EncodingError('Value must be non-negative, got %d' % value)
        return cls._encode_varint(value)

    @classmethod
    def encode_bool(cls, value):
        return cls._encode_varint(value)

    @classmethod
    def encode_string(cls, value):
        size = cls._encode_varint(bytelength(value))
        data = value.encode('utf-8')
        return size + data

    @classmethod
    def encode_bytes(cls, value):
        return b''.join([cls._encode_varint(len(value)), value])
