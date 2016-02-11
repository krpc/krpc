import subprocess
import time
import krpc
import sys
import os

class ServerTestCase(object):

    def setUp(self):
        self.conn = self.connect()

    def tearDown(self):
        self.conn.close()

    def connect(self):
        return krpc.connect(name='Python2ClientTest', address='localhost',
                            rpc_port=int(os.getenv('RPC_PORT', 50000)),
                            stream_port=int(os.getenv('STREAM_PORT', 50001)))
