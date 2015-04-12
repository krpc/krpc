import sys
import struct

PY2 = sys.version_info[0] < 3
POS_INF = 1e10000
NEG_INF = -POS_INF
NAN = POS_INF * 0

def bytelength(s):
    """ Get the number of bytes in a string """
    if PY2 and type(s) == str:
        return len(s)
    else:
        return len(s.encode('utf-8'))

def hexlify(value):
    if PY2:
        return ''.join('%02x' % ord(x) for x in value)
    else:
        return ''.join('%02x' % x for x in value)

def unhexlify(data):
    value = []
    for i in range(0, len(data), 2):
        x = data[i:i+2]
        value.append(int(x, 16))
    return struct.pack('%dB' % len(value), *value)
