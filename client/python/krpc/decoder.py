# pylint: disable=import-error,no-name-in-module
from google.protobuf.internal import decoder as protobuf_decoder
from krpc.types import Types, ValueType, MessageType, ClassType, EnumType
from krpc.types import ListType, DictionaryType, SetType, TupleType
import krpc.platform
from krpc.platform import hexlify


class Decoder(object):
    """ Routines for decoding messages and values from
        the protocol buffer serialization format """

    OK_LENGTH = 2
    OK_MESSAGE = b'\x4F\x4B'

    GUID_LENGTH = 16

    _types = Types()

    @classmethod
    def guid(cls, data):
        """ Decode a 16-byte GUID into a string """
        return '-'.join((
            hexlify(data[3::-1]), hexlify(data[5:3:-1]), hexlify(data[7:5:-1]),
            hexlify(data[8:10]), hexlify(data[10:16])))

    @classmethod
    def decode(cls, data, typ):
        """ Given a python type, and serialized data, decode the value """
        if isinstance(typ, MessageType):
            return cls._decode_message(data, typ)
        elif isinstance(typ, EnumType):
            value = cls._decode_value(data, cls._types.as_type('int32'))
            return typ.python_type(value)
        elif isinstance(typ, ValueType):
            return cls._decode_value(data, typ)
        elif isinstance(typ, ClassType):
            object_id_typ = cls._types.as_type('uint64')
            object_id = cls._decode_value(data, object_id_typ)
            return typ.python_type(object_id) if object_id != 0 else None
        elif isinstance(typ, ListType):
            if data == b'\x00':
                return None
            msg = cls._decode_message(data, cls._types.as_type('KRPC.List'))
            return [cls.decode(item, typ.value_type) for item in msg.items]
        elif isinstance(typ, DictionaryType):
            if data == b'\x00':
                return None
            msg = cls._decode_message(
                data, cls._types.as_type('KRPC.Dictionary'))
            return dict((cls.decode(entry.key, typ.key_type),
                         cls.decode(entry.value, typ.value_type))
                        for entry in msg.entries)
        elif isinstance(typ, SetType):
            if data == b'\x00':
                return None
            msg = cls._decode_message(data, cls._types.as_type('KRPC.Set'))
            return set(cls.decode(item, typ.value_type) for item in msg.items)
        elif isinstance(typ, TupleType):
            if data == b'\x00':
                return None
            msg = cls._decode_message(data, cls._types.as_type('KRPC.Tuple'))
            return tuple(
                cls.decode(item, value_type)
                for item, value_type in zip(msg.items, typ.value_types))
        else:
            raise RuntimeError('Cannot decode type %s' % str(typ))

    @classmethod
    def decode_size_and_position(cls, data):
        """ Decode a varint and return the (size, position) """
        return protobuf_decoder._DecodeVarint(data, 0)

    @classmethod
    def decode_delimited(cls, data, typ):
        """ Decode a message or value with size information
            (used in a delimited communication stream) """
        size, position = cls.decode_size_and_position(data)
        return cls.decode(data[position:position+size], typ)

    @classmethod
    def _decode_message(cls, data, typ):
        message = typ.python_type()
        message.ParseFromString(data)
        return message

    @classmethod
    def _decode_value(cls, data, typ):
        return getattr(_ValueDecoder, 'decode_' + typ.protobuf_type)(data)


class _ValueDecoder(object):
    """ Routines for encoding values from
        the protocol buffer serialization format """

    @classmethod
    def _decode_signed_varint(cls, data):
        return protobuf_decoder._DecodeSignedVarint(data, 0)[0]

    @classmethod
    def _decode_varint(cls, data):
        return protobuf_decoder._DecodeVarint(data, 0)[0]

    @classmethod
    def decode_int32(cls, data):
        return int(cls._decode_signed_varint(data))

    @classmethod
    def decode_int64(cls, data):
        return cls._decode_signed_varint(data)

    @classmethod
    def decode_uint32(cls, data):
        return cls._decode_varint(data)

    @classmethod
    def decode_uint64(cls, data):
        return cls._decode_varint(data)

    # The code for the following two methods is taken from
    # google.protobuf.internal.decoder._FloatDecoder and _DoubleDecoder
    # Copyright 2008, Google Inc.
    # See protobuf-license.txt distributed with this file

    @classmethod
    def decode_double(cls, data):
        import struct
        local_unpack = struct.unpack

        # We expect a 64-bit value in little-endian byte order.  Bit 1 is the
        # sign bit, bits 2-12 represent the exponent, and bits 13-64 are the
        # significand.
        double_bytes = data[0:8]

        # If this value has all its exponent bits set and at least one
        # significand bit set, it's not a number.  In Python 2.4, struct.unpack
        # will treat it as inf or -inf.  To avoid that, we treat it specially.
        if (double_bytes[7:8] in b'\x7F\xFF') and \
           (double_bytes[6:7] >= b'\xF0') and \
           (double_bytes[0:7] != b'\x00\x00\x00\x00\x00\x00\xF0'):
            return krpc.platform.NAN

        # Note that we expect someone up-stack to catch struct.error and
        # convert it to _DecodeError -- this way we don't have to set up
        # exception-handling blocks every time we parse one value.
        return local_unpack('<d', double_bytes)[0]

    @classmethod
    def decode_float(cls, data):
        import struct
        local_unpack = struct.unpack

        # We expect a 32-bit value in little-endian byte order.  Bit 1 is the
        # sign bit, bits 2-9 represent the exponent, and bits 10-32 are
        # the significand.
        float_bytes = data[0:4]

        # If this value has all its exponent bits set, then it's non-finite.
        # In Python 2.4, struct.unpack will convert it to a finite 64-bit
        # value. To avoid that, we parse it specially.
        if float_bytes[3:4] in b'\x7F\xFF' and float_bytes[2:3] >= b'\x80':
            # If at least one significand bit is set...
            if float_bytes[0:3] != b'\x00\x00\x80':
                return krpc.platform.NAN
            # If sign bit is set...
            if float_bytes[3:4] == b'\xFF':
                return krpc.platform.NEG_INF
            return krpc.platform.POS_INF

        # Note that we expect someone up-stack to catch struct.error and
        # convert it to _DecodeError -- this way we don't have to set up
        # exception-handling blocks every time we parse one value.
        return local_unpack('<f', float_bytes)[0]

    # End of code taken from google.protobuf.internal.decoder._FloatDecoder
    # and google.protobuf.internal.decoder._DoubleDecoder

    @classmethod
    def decode_bool(cls, data):
        return bool(cls._decode_varint(data))

    @classmethod
    def decode_string(cls, data):
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return data[position:position+size].decode('utf-8')

    @classmethod
    def decode_bytes(cls, data):
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return data[position:position+size]
