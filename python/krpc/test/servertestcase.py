import subprocess
import time
import krpc

class ServerTestCase(object):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50011', '50012'])
        time.sleep(0.25)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def setUp(self):
        self.conn = krpc.connect(name='TestClient', address='localhost', rpc_port=50011, stream_port=50012)

    def tearDown(self):
        self.conn.close()
