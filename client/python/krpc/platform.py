POS_INF = 1e10000
NEG_INF = -POS_INF
NAN = POS_INF * 0


def bytelength(string: str) -> int:
    """Get the number of bytes in a string"""
    return len(string.encode("utf-8"))
