import os
import krpc
from krpc.client import Client


class ServerTestCase:
    conn: Client = None  # type: ignore[assignment]

    @classmethod
    def setUpClass(cls) -> None:
        if cls.conn is None:
            cls.conn = cls.connect()

    @staticmethod
    def connect() -> Client:
        return krpc.connect(name='python_client_test', address='localhost',
                            rpc_port=ServerTestCase.rpc_port(),
                            stream_port=ServerTestCase.stream_port())

    @staticmethod
    def rpc_port() -> int:
        return int(os.getenv('RPC_PORT', '50000'))

    @staticmethod
    def stream_port() -> int:
        return int(os.getenv('STREAM_PORT', '50001'))
