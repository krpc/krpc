import struct
import binascii

POS_INF = 1e10000
NEG_INF = -POS_INF
NAN = POS_INF * 0


def bytelength(string: str) -> int:
    """ Get the number of bytes in a string """
    return len(string.encode('utf-8'))


def hexlify(value: bytes) -> str:
    return binascii.hexlify(value).decode()


def unhexlify(data: str) -> bytes:
    value = []
    for i in range(0, len(data), 2):
        x = data[i:i + 2]
        value.append(int(x, 16))
    return struct.pack('%dB' % len(value), *value)
