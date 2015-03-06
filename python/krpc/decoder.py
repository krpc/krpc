# TODO: avoid using internals
from google.protobuf.internal import decoder as protobuf_decoder
from krpc.types import _Types, _ValueType, _MessageType, _ClassType, _EnumType, _ListType, _DictionaryType, _SetType, _TupleType
import itertools


class _Decoder(object):
    """ Routines for decoding messages and values from the protocol buffer serialization format """

    OK_LENGTH = 2
    OK_MESSAGE = b'\x4F\x4B'

    GUID_LENGTH = 16

    @classmethod
    def guid(cls, data):
        """ Decode a 16-byte GUID into a string """
        # TODO: test this
        chunks = [4,2,2,2,6]
        reverse_chunk = [True,True,True,False,False]
        result = ''
        offset = 0
        for c,chunk in enumerate(chunks):
            poss = range(offset,offset+chunk)
            if reverse_chunk[c]:
                poss = reversed(poss)
            for pos in poss:
                result += '%02x' % ord(data[pos])
            offset += chunk
            result += '-'
        return result[:-1]

    @classmethod
    def decode(cls, data, typ):
        """ Given a python type, and serialized data, decode the value """
        if isinstance(typ, _MessageType):
            return cls._decode_message(data, typ)
        elif isinstance(typ, _EnumType):
            return cls._decode_value(data, _Types().as_type('int32'))
        elif isinstance(typ, _ValueType):
            return cls._decode_value(data, typ)
        elif isinstance(typ, _ClassType):
            object_id_typ = _Types().as_type('uint64')
            object_id = cls._decode_value(data, object_id_typ)
            return typ.python_type(object_id) if object_id != 0 else None
        elif isinstance(typ, _ListType):
            msg = cls._decode_message(data, _Types().as_type('KRPC.List'))
            return [cls.decode(item, typ.value_type) for item in msg.items]
        elif isinstance(typ, _DictionaryType):
            msg = cls._decode_message(data, _Types().as_type('KRPC.Dictionary'))
            return dict((cls.decode(entry.key, typ.key_type), cls.decode(entry.value, typ.value_type)) for entry in msg.entries)
        elif isinstance(typ, _SetType):
            msg = cls._decode_message(data, _Types().as_type('KRPC.Set'))
            return set(cls.decode(item, typ.value_type) for item in msg.items)
        elif isinstance(typ, _TupleType):
            msg = cls._decode_message(data, _Types().as_type('KRPC.Tuple'))
            return tuple(cls.decode(item, value_type) for item,value_type in itertools.izip(msg.items,typ.value_types))
        else:
            raise RuntimeError ('Cannot decode type %s' % str(typ))

    @classmethod
    def decode_size_and_position(cls, data):
        """ Decode a varint and return the (size, position) """
        return protobuf_decoder._DecodeVarint(data, 0)

    @classmethod
    def decode_delimited(cls, data, typ):
        """ Decode a message or value with size information
            (used in a delimited communication stream) """
        (size, position) = cls.decode_size_and_position(data)
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
    """ Routines for encoding values from the protocol buffer serialization format """

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
    _POS_INF = 1e10000
    _NEG_INF = -_POS_INF
    _NAN = _POS_INF * 0

    @classmethod
    def decode_double(cls, data):
        # We expect a 64-bit value in little-endian byte order.  Bit 1 is the sign
        # bit, bits 2-12 represent the exponent, and bits 13-64 are the significand.
        double_bytes = data[0:8]

        # If this value has all its exponent bits set and at least one significand
        # bit set, it's not a number.  In Python 2.4, struct.unpack will treat it
        # as inf or -inf.  To avoid that, we treat it specially.
        if ((double_bytes[7] in '\x7F\xFF')
            and (double_bytes[6] >= '\xF0')
            and (double_bytes[0:7] != '\x00\x00\x00\x00\x00\x00\xF0')):
          return cls._NAN

        # Note that we expect someone up-stack to catch struct.error and convert
        # it to _DecodeError -- this way we don't have to set up exception-
        # handling blocks every time we parse one value.
        import struct
        return struct.unpack('<d', double_bytes)[0]

    @classmethod
    def decode_float(cls, data):
        # We expect a 32-bit value in little-endian byte order. Bit 1 is the sign
        # bit, bits 2-9 represent the exponent, and bits 10-32 are the significand.
        float_bytes = data[0:4]

        # If this value has all its exponent bits set, then it's non-finite.
        # In Python 2.4, struct.unpack will convert it to a finite 64-bit value.
        # To avoid that, we parse it specially.
        if ((float_bytes[3] in '\x7F\xFF')
            and (float_bytes[2] >= '\x80')):
          # If at least one significand bit is set...
          if float_bytes[0:3] != '\x00\x00\x80':
            return cls._NAN
          # If sign bit is set...
          if float_bytes[3] == '\xFF':
            return cls._NEG_INF
          return cls._POS_INF

        # Note that we expect someone up-stack to catch struct.error and convert
        # it to _DecodeError -- this way we don't have to set up exception-
        # handling blocks every time we parse one value.
        import struct
        return struct.unpack('<f', float_bytes)[0]

    # End of code taken from google.protobuf.internal.decoder._FloatDecoder and _DoubleDecoder

    @classmethod
    def decode_bool(cls, data):
        return bool(cls._decode_varint(data))

    @classmethod
    def decode_string(cls, data):
        (size, position) = protobuf_decoder._DecodeVarint(data, 0)
        return unicode(data[position:position+size], 'utf-8')

    @classmethod
    def decode_bytes(cls, data):
        (size, pos) = protobuf_decoder._DecodeVarint(data, 0)
        return data[pos:pos+size]
