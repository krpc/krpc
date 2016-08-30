import os
import krpc


class ServerTestCase(object):

    conn = None

    @classmethod
    def setUpClass(cls):
        if cls.conn is None:
            cls.conn = cls.connect()

    @staticmethod
    def connect():
        return krpc.connect(name='python_client_test', address='localhost',
                            rpc_port=int(os.getenv('RPC_PORT', 50000)),
                            stream_port=int(os.getenv('STREAM_PORT', 50001)))
