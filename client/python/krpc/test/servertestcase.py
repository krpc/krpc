import subprocess
import time
import krpc

class ServerTestCase(object):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['mono', 'bin/TestServer/TestServer.exe', '50010', '50011', '--quiet'])
        time.sleep(1)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def setUp(self):
        self.conn = self.connect()

    def tearDown(self):
        self.conn.close()

    def connect(self):
        return krpc.connect(name='TestClient', address='localhost', rpc_port=50010, stream_port=50011)
