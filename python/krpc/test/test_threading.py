#!/usr/bin/env python2

import unittest
import subprocess
import threading
import time
import krpc
import krpc.test.Test as TestSchema

def worker_thread(tid, conn):
    for i in range(1000):
        conn.krpc.get_status()

class TestClient(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50001'])
        time.sleep(0.25)

    def setUp(self):
        self.conn = krpc.connect(name='TestClient', port=50001)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_thread_safe_connection(self):
        thread0 = threading.Thread(target=worker_thread, args=(0, self.conn))
        thread1 = threading.Thread(target=worker_thread, args=(1, self.conn))
        thread0.start()
        thread1.start()
        thread0.join()
        thread1.join()

if __name__ == '__main__':
    unittest.main()
