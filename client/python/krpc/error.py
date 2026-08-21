class ConnectionError(RuntimeError):  # pylint: disable=redefined-builtin
    """Raised when an error occurs connecting to the server"""


class RPCError(RuntimeError):
    """Raised when an error occurs executing a remote procedure call"""


class StreamError(RuntimeError):
    """Raised when an error occurs in a stream operation"""


class ExpressionCompilationError(RuntimeError):
    """Error raised when a python function cannot be compiled
    into a server side expression"""


class EncodingError(RuntimeError):
    """Raised when an error occurs encoding or decoding a message"""
