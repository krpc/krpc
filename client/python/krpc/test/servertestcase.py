import os
import socket
from typing import Optional
import krpc
from krpc.client import Client


class ServerTestCase:
    conn: Client = None  # type: ignore[assignment]

    @classmethod
    def setUpClass(cls) -> None:
        if cls.conn is None:
            cls.conn = cls.connect()

    @staticmethod
    def connect(
        name: str = "python_client_test",
        rpc: str = "rpc",
        stream: Optional[str] = "stream",
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
