"""A minimal conformance testee that speaks the protocol over websockets.

There is no websockets client library in this repository. This testee proves the conformance
suite over websockets, for a websockets client written elsewhere. It connects, closes, and calls
procedures that take and return scalar values. It replies UNSUPPORTED to every other command,
which skips the test.
"""

from __future__ import annotations

import json
import math
import sys
from typing import IO, Any, NoReturn

import testee_pb2 as pb
import websocket
from google.protobuf import json_format
from google.protobuf.message import DecodeError

import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.types import Types, ValueType

DEFAULT_ADDRESS = "127.0.0.1"
DEFAULT_PORT = 50000
DEFAULT_HANDSHAKE_TIMEOUT = 5
# The time closing waits for the server's close frame
CLOSE_TIMEOUT = 1


class Unsupported(Exception):
    """The testee offers no way to do what a command asks."""


class ConnectionFailed(Exception):
    """The connection failed, or is closed."""

    def __init__(self, message: str, status: str = "", reason: str = "") -> None:
        super().__init__(message)
        self.status = status
        self.reason = reason


class EncodingFailed(Exception):
    """A value failed to encode or decode."""


class RPCFailed(Exception):
    """A procedure threw an exception. The message starts with ``Service.Name:`` for an
    exception with a type, as for a client with no error types."""

    def __init__(self, error: KRPC.Error) -> None:
        message = error.description
        if error.name:
            message = "%s.%s: %s" % (error.service, error.name, message)
        if error.stack_trace:
            message += "\nServer stack trace:\n" + error.stack_trace
        super().__init__(message)
        self.service = error.service
        self.name = error.name


class Connection:
    """A connection to a server, over websockets."""

    def __init__(self, command: pb.Connect) -> None:
        url = "ws://%s:%d/" % (
            command.address or DEFAULT_ADDRESS,
            command.port or DEFAULT_PORT,
        )
        timeout = (
            command.handshake_timeout
            if command.HasField("handshake_timeout")
            else DEFAULT_HANDSHAKE_TIMEOUT
        )
        # The harness has no stall timeout, and checks it as for the handshake timeout
        timeouts = [timeout]
        if command.HasField("stall_timeout"):
            timeouts.append(command.stall_timeout)
        if not all(x > 0 for x in timeouts):
            raise ValueError("A timeout must be positive, or infinite")
        self._ended: ConnectionFailed | None = None
        self._next_id = 1
        try:
            self._ws = websocket.create_connection(
                url, timeout=None if math.isinf(timeout) else timeout
            )
        except (OSError, websocket.WebSocketException) as exn:
            raise ConnectionFailed(str(exn)) from exn
        try:
            self._handshake(command.name)
        except BaseException:
            self._ws.shutdown()
            raise
        self._ws.settimeout(None)
        self._types = Types()
        services = KRPC.Services.FromString(self.call("KRPC", "GetServices", []).value)
        self._procedures = {
            (service.name, procedure.name): procedure
            for service in services.services
            for procedure in service.procedures
        }

    def _handshake(self, name: str) -> None:
        self._send(
            KRPC.Message(
                connection_request=KRPC.ConnectionRequest(
                    client_name=name, protocol_version=2
                )
            )
        )
        try:
            message = self._receive()
        except (OSError, websocket.WebSocketException) as exn:
            raise ConnectionFailed(str(exn)) from exn
        if message is None:
            raise ConnectionFailed("The server closed the connection")
        case = message.WhichOneof("content")
        if case == "disconnect":
            reason = reason_name(message.disconnect.reason)
            raise ConnectionFailed(message.disconnect.message, reason=reason)
        if case != "connection_response":
            raise ConnectionFailed("Expected a connection response, got %s" % case)
        response = message.connection_response
        if response.status != KRPC.ConnectionResponse.OK:
            try:
                status = KRPC.ConnectionResponse.Status.Name(response.status)
            except ValueError:
                status = ""
            raise ConnectionFailed(response.message, status=status)
        if response.protocol_version != 2:
            raise ConnectionFailed(
                "The server accepted protocol version %d" % response.protocol_version
            )

    def close(self) -> None:
        if self._ended is None:
            self._ended = ConnectionFailed("The connection is closed")
            try:
                self._send(
                    KRPC.Message(
                        disconnect=KRPC.Disconnect(reason=KRPC.Disconnect.CLOSED)
                    )
                )
                self._ws.close(timeout=CLOSE_TIMEOUT)
            except (OSError, websocket.WebSocketException):
                pass
        self._ws.shutdown()

    def invoke(self, command: pb.Invoke) -> pb.Value:
        if command.named_arguments:
            raise Unsupported("No named arguments")
        procedure = self._procedures.get((command.service, command.procedure))
        if procedure is None:
            raise Unsupported(
                "No procedure %s.%s" % (command.service, command.procedure)
            )
        if len(command.arguments) > len(procedure.parameters):
            raise Unsupported("Too many arguments")
        arguments = []
        for i, value in enumerate(command.arguments):
            argument = KRPC.Argument(position=i)
            if value.WhichOneof("value") == "null":
                if not procedure.parameters[i].type.nullable:
                    raise ValueError("Parameter %d cannot hold null" % i)
                argument.is_null = True
            else:
                typ = self._scalar(procedure.parameters[i].type)
                argument.value = Encoder.encode(from_value(value), typ)
            arguments.append(argument)
        result = self.call(command.service, command.procedure, arguments)
        if not procedure.HasField("return_type"):
            return pb.Value(null=True)
        if result.is_null:
            if not procedure.return_type.nullable:
                raise EncodingFailed("A null result for a type that cannot hold null")
            return pb.Value(null=True)
        typ = self._scalar(procedure.return_type)
        return to_value(Decoder.decode(None, result.value, typ))

    def call(
        self, service: str, procedure: str, arguments: list[KRPC.Argument]
    ) -> KRPC.ProcedureResult:
        if self._ended is not None:
            raise self._ended
        request = KRPC.Request(id=self._next_id)
        self._next_id += 1
        request.calls.add(service=service, procedure=procedure, arguments=arguments)
        try:
            self._send(KRPC.Message(request=request))
            while True:
                message = self._receive()
                if message is None:
                    self._end(ConnectionFailed("The server closed the connection"))
                case = message.WhichOneof("content")
                if case == "response":
                    if message.response.id != request.id:
                        self._fail("PROTOCOL_ERROR", "A response has an unknown id")
                    break
                if case == "disconnect":
                    reason = reason_name(message.disconnect.reason)
                    self._end(
                        ConnectionFailed(message.disconnect.message, reason=reason)
                    )
                if case in ("connection_request", "request"):
                    self._fail("PROTOCOL_ERROR", "The server sent a %s" % case)
                # A stream update, or a message this testee does not know, is skipped
        except DecodeError:
            self._fail("MALFORMED_MESSAGE", "A message failed to parse")
        except (OSError, websocket.WebSocketException) as exn:
            self._end(ConnectionFailed(str(exn)))
        response = message.response
        if response.HasField("error"):
            raise RPCFailed(response.error)
        result = response.results[0]
        if result.HasField("error"):
            raise RPCFailed(result.error)
        return result

    def _scalar(self, protobuf_type: KRPC.Type) -> ValueType:
        typ = self._types.as_type(protobuf_type)
        if not isinstance(typ, ValueType):
            raise Unsupported("No values of type %s" % type(typ).__name__)
        return typ

    def _fail(self, reason: str, message: str) -> NoReturn:
        """End the connection with a disconnect, for a protocol error."""
        disconnect = KRPC.Disconnect(reason=KRPC.Disconnect.Reason.Value(reason))
        try:
            self._send(KRPC.Message(disconnect=disconnect))
        except (OSError, websocket.WebSocketException):
            pass
        self._end(ConnectionFailed(message, reason=reason))

    def _end(self, error: ConnectionFailed) -> NoReturn:
        self._ended = error
        self._ws.shutdown()
        raise error

    def _send(self, message: KRPC.Message) -> None:
        self._ws.send_binary(message.SerializeToString())

    def _receive(self) -> KRPC.Message | None:
        """The next message, or None once the server closes."""
        try:
            data = self._ws.recv()
        except websocket.WebSocketConnectionClosedException:
            return None
        if not data:
            return None
        if isinstance(data, str):
            raise DecodeError("A text message")
        return KRPC.Message.FromString(data)


def reason_name(reason: int) -> str:
    """The name of a disconnect reason, or its number for a reason the schema does not
    name."""
    if reason in KRPC.Disconnect.Reason.values():
        return KRPC.Disconnect.Reason.Name(reason)
    return str(reason)


def from_value(value: pb.Value) -> Any:
    case = value.WhichOneof("value")
    if case not in (
        "double",
        "float",
        "sint32",
        "sint64",
        "uint32",
        "uint64",
        "bool",
        "string",
        "bytes",
    ):
        raise Unsupported("No %s values" % case)
    return getattr(value, case)


def to_value(value: object) -> pb.Value:
    if isinstance(value, bool):
        return pb.Value(bool=value)
    if isinstance(value, int):
        # The driver compares integers by value, whatever their type
        return pb.Value(sint64=value) if value < 0 else pb.Value(uint64=value)
    if isinstance(value, float):
        return pb.Value(double=value)
    if isinstance(value, str):
        return pb.Value(string=value)
    if isinstance(value, bytes):
        return pb.Value(bytes=value)
    raise Unsupported("No conformance value for %s" % type(value))


def to_error(exn: Exception) -> pb.Error:
    error = pb.Error(message=str(exn))
    if isinstance(exn, Unsupported):
        error.kind = pb.Error.UNSUPPORTED
    elif isinstance(exn, ConnectionFailed):
        error.kind = pb.Error.CONNECTION
        error.status = exn.status
        error.reason = exn.reason
    elif isinstance(exn, RPCFailed):
        error.kind = pb.Error.RPC
        error.service = exn.service
        error.name = exn.name
    elif isinstance(exn, EncodingFailed):
        error.kind = pb.Error.ENCODING
    elif isinstance(exn, (TypeError, ValueError)):
        error.kind = pb.Error.ARGUMENT
    return error


class Testee:
    def __init__(self) -> None:
        self._conn: Connection | None = None

    def run(self, command: pb.Command) -> pb.Reply:
        reply = pb.Reply(id=command.id, thread="main")
        try:
            case = command.WhichOneof("command")
            if case == "connect":
                self.close()
                self._conn = Connection(command.connect)
                reply.value.SetInParent()
            elif case == "close":
                self.close()
                reply.value.SetInParent()
            elif case == "invoke":
                if command.invoke.HasField("through"):
                    raise Unsupported("A static method is called through its class")
                if self._conn is None:
                    raise ConnectionFailed("Not connected")
                reply.value.CopyFrom(self._conn.invoke(command.invoke))
            else:
                raise Unsupported("No %s command" % case)
        except Exception as exn:  # noqa: BLE001
            reply.ClearField("result")
            reply.error.CopyFrom(to_error(exn))
        return reply

    def close(self) -> None:
        if self._conn is not None:
            self._conn.close()


def write(stream: IO[bytes], message: pb.Output) -> None:
    data = json.dumps(json_format.MessageToDict(message), separators=(",", ":"))
    stream.write(data.encode() + b"\n")
    stream.flush()


def main() -> int:
    # The protocol owns standard output, so anything else the process prints goes to standard
    # error
    stdout = sys.stdout.buffer
    sys.stdout = sys.stderr
    write(
        stdout,
        pb.Output(
            hello=pb.Hello(
                client="websockets",
                profiles=["core"],
                protocols=["websockets"],
            )
        ),
    )
    testee = Testee()
    for line in sys.stdin.buffer:
        command = json_format.Parse(line.rstrip(b"\r\n"), pb.Command())
        write(stdout, pb.Output(reply=testee.run(command)))
    testee.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
