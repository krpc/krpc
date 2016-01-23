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
        if sys.version_info < (3, 0):
            return krpc.connect(name='Python2ClientTest', address='localhost', rpc_port=50010, stream_port=50011)
        else:
            return krpc.connect(name='Python3ClientTest', address='localhost', rpc_port=50012, stream_port=50013)
