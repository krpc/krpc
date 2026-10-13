class ConnectionError(RuntimeError):
    """Raised when an error occurs connecting to the server"""


class RPCError(RuntimeError):
    """Raised when an error occurs executing a remote procedure call"""


class StreamError(RuntimeError):
    """Raised when an error occurs in a stream operation"""


class FunctionCompilationError(RuntimeError):
    """Raised when a Python function cannot be compiled into a server side
    function"""


class EncodingError(RuntimeError):
    """Raised when an error occurs encoding or decoding a message"""
