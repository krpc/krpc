from __future__ import annotations


class ConnectionError(RuntimeError):
    """Raised when an error occurs connecting to the server, or when the connection
    fails or ends. status is the name of the ConnectionResponse status when the server
    refused the connection, such as "DENIED". reason is the name of the Disconnect
    reason when the server ended the connection, such as "SERVER_STOPPED"."""

    def __init__(
        self, message: str = "", status: str | None = None, reason: str | None = None
    ) -> None:
        super().__init__(message)
        self.status = status
        self.reason = reason


class RPCError(RuntimeError):
    """Raised when an error occurs executing a remote procedure call"""


class StreamError(RuntimeError):
    """Raised when an error occurs in a stream operation"""


class FunctionCompilationError(RuntimeError):
    """Raised when a Python function cannot be compiled into a server side
    function"""


class EncodingError(RuntimeError):
    """Raised when an error occurs encoding or decoding a message"""
