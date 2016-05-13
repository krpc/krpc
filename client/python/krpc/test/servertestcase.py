import os
import krpc

class ServerTestCase(object):

    def setUp(self): #pylint: disable=invalid-name
        self.conn = self.connect()

    def tearDown(self): #pylint: disable=invalid-name
        self.conn.close()

    @staticmethod
    def connect():
        return krpc.connect(name='python_client_test', address='localhost',
                            rpc_port=int(os.getenv('RPC_PORT', 50000)),
                            stream_port=int(os.getenv('STREAM_PORT', 50001)))
