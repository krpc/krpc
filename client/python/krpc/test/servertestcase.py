import subprocess
import time
import krpc
import sys

class ServerTestCase(object):

    def setUp(self):
        self.conn = self.connect()

    def tearDown(self):
        self.conn.close()

    def connect(self):
        return krpc.connect(name='TestClient', address='localhost', rpc_port=50010, stream_port=50011)
