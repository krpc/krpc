class ConnectionError(RuntimeError):
    """ Raised when an error occurs connecting to the server """
    def __init__(self, message):
        super(ConnectionError, self).__init__(message)


class RPCError(RuntimeError):
    """ Raised when an error occurs executing a remote procedure call """
    def __init__(self, message):
        super(RPCError, self).__init__(message)


class StreamError(RuntimeError):
    """ Raised when an error occurs in a stream operation """
    def __init__(self, message):
        super(StreamError, self).__init__(message)


class EncodingError(RuntimeError):
    """ Raised when an error occurs encoding or decoding a message """
    def __init__(self, message):
        super(EncodingError, self).__init__(message)
