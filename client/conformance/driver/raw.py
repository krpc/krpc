"""Calls made over a connection of the driver's own, to see what the server sends without a
client in the way."""

from __future__ import annotations

import socket

import krpc.schema.KRPC_pb2 as KRPC
from driver import localsocket
from driver.framing import PROTOCOL_VERSION, frame, receive_frame
from driver.testee import REPLY_TIMEOUT, Endpoint


def _open(endpoint: Endpoint) -> socket.socket:
    if endpoint.protocol == "localsocket":
        sock = localsocket.connect(endpoint.path)
        sock.settimeout(REPLY_TIMEOUT)
        return sock
    return socket.create_connection(
        (endpoint.address, endpoint.control_port), REPLY_TIMEOUT
    )


def call(
    endpoint: Endpoint, service: str, procedure: str, *arguments: bytes
) -> KRPC.ProcedureResult:
    """Call a procedure on a server, and return its result. The arguments are encoded
    values."""
    with _open(endpoint) as sock:
        _send(
            sock,
            KRPC.Message(
                connection_request=KRPC.ConnectionRequest(
                    client_name="conformance-raw", protocol_version=PROTOCOL_VERSION
                )
            ),
        )
        response = _receive(sock)
        assert response.connection_response.status == KRPC.ConnectionResponse.OK
        request = KRPC.Request(id=1)
        request.calls.add(
            service=service,
            procedure=procedure,
            arguments=[
                KRPC.Argument(position=i, value=x) for i, x in enumerate(arguments)
            ],
        )
        _send(sock, KRPC.Message(request=request))
        while True:
            message = _receive(sock)
            if message.WhichOneof("content") == "response":
                break
        _send(
            sock,
            KRPC.Message(disconnect=KRPC.Disconnect(reason=KRPC.Disconnect.CLOSED)),
        )
        assert not message.response.HasField("error"), message.response.error
        return message.response.results[0]


def _send(sock: socket.socket, message: KRPC.Message) -> None:
    sock.sendall(frame(message.SerializeToString()))


def _receive(sock: socket.socket) -> KRPC.Message:
    data = receive_frame(sock)
    assert data is not None, "The server closed the connection"
    return KRPC.Message.FromString(data)
