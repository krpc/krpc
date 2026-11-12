from __future__ import annotations


class RemoteError(RuntimeError):
    """The base of every error the client raises of its own. A server exception with a
    mapped type, such as ArgumentException, raises the builtin it stands for, and the client's
    argument errors are the builtin ValueError and TypeError."""


class ConnectionError(RemoteError):
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


class ServerError(RemoteError):
    """Raised for an error the server reports, through a call, a stream or an event, that has
    no builtin type. The exception classes of the services derive it."""


class StreamError(RemoteError):
    """Raised for a stream or event that has been removed, or that cannot be added."""


class EncodingError(RemoteError):
    """Raised for a value of the right type that fails to encode, or a value that fails to
    decode."""


class CompilationError(RemoteError):
    """Raised when a Python function cannot be compiled into a server side function or
    expression."""
