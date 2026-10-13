import os
import socket
from collections.abc import Callable
from typing import Any

import krpc
import krpc.client
import krpc.functioncompiler
import krpc.schema.KRPC_pb2 as KRPC
from krpc.client import Client
from krpc.functioncompiler import NO_VALUE, Parameters

_compile_function_with_type = krpc.functioncompiler.compile_function_with_type


def _checked_compile_function_with_type(
    client: Client,
    func: Callable,
    streamed: bool = False,  # type: ignore[type-arg]
) -> tuple[Any, KRPC.Type | None, Parameters]:
    """Compile a function, and check that the types the compiler tracks for its
    value and its parameters are the types the server gives them. The client
    decodes the value and encodes the arguments with the compiler's types, so
    the two must agree on where a value can be None."""
    function, ptype, parameters = _compile_function_with_type(client, func, streamed)
    if ptype is NO_VALUE:
        if client._expression_return_type(function, cache=False) is not None:
            raise AssertionError("The compiler gives a function with a value no type")
    elif ptype is not None:
        server_ptype = client._expression_return_type(function, cache=False)
        if server_ptype != ptype:
            raise AssertionError(
                "The compiler gives the function the type\n%s\nand the server\n%s"
                % (ptype, server_ptype)
            )
    if parameters.ptypes:
        server_ptypes = client._parameter_types(function)
        if server_ptypes != parameters.ptypes:
            raise AssertionError(
                "The compiler gives the parameters the types\n%s\nand the server\n%s"
                % (parameters.ptypes, server_ptypes)
            )
    return function, ptype, parameters


class ServerTestCase:
    conn: Client = None  # type: ignore[assignment]

    # The wait for a connection to a port nothing is listening on. It is normally refused
    # at once, and where the system drops the attempt instead, this bounds the wait so that
    # the test does not hang.
    CONNECT_TIMEOUT = 10

    @classmethod
    def setUpClass(cls) -> None:
        if cls.conn is None:
            cls.conn = cls.connect()
        # Every function a test compiles and runs is checked
        krpc.client.compile_function_with_type = (  # type: ignore[attr-defined]
            _checked_compile_function_with_type
        )

    @staticmethod
    def connect(
        name: str = "python_client_test",
        rpc: str = "rpc",
        stream: str | None = "stream",
        use_pregenerated_stubs: bool = True,
    ) -> Client:
        """Connect over whichever transport the harness started the server with, which
        it tells us about by port or by socket path. The rpc and stream arguments name
        which of the server's two endpoints each connection should go to, so a test can
        deliberately connect them the wrong way round."""
        rpc_path = os.getenv("RPC_PATH")
        if rpc_path:
            paths = {"rpc": rpc_path, "stream": os.getenv("STREAM_PATH")}
            return krpc.connect_local(
                name=name,
                rpc_path=paths[rpc],  # type: ignore[arg-type]
                stream_path=paths[stream] if stream is not None else None,
                use_pregenerated_stubs=use_pregenerated_stubs,
            )
        ports = {
            "rpc": ServerTestCase.rpc_port(),
            "stream": ServerTestCase.stream_port(),
        }
        return krpc.connect(
            name=name,
            address="localhost",
            rpc_port=ports[rpc],
            stream_port=ports[stream] if stream is not None else None,
            use_pregenerated_stubs=use_pregenerated_stubs,
        )

    @staticmethod
    def unused_port() -> int:
        """A port nothing is listening on, for the tests that connect to the wrong one.
        Binding a port and giving it straight back leaves one that a connection is refused
        on, and leaves it in the range the system hands out. A port derived from the
        server's own can land anywhere, including on a low one, and a connection to those
        is dropped rather than refused on Windows, which leaves the client waiting."""
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.bind(("localhost", 0))
            return int(sock.getsockname()[1])

    @staticmethod
    def rpc_port() -> int:
        return int(os.getenv("RPC_PORT", "50000"))

    @staticmethod
    def stream_port() -> int:
        return int(os.getenv("STREAM_PORT", "50001"))
