from __future__ import annotations
import unittest
import threading
from krpc.test.servertestcase import ServerTestCase
from krpc.client import Client


def worker_thread(conn: Client) -> None:
    for _ in range(100):
        conn.krpc.get_status()


def worker_thread2(conn: Client, test: TestThreading) -> None:
    for _ in range(10):
        test.assertEqual(
            '3.14159', conn.test_service.float_to_string(float(3.14159)))
        test.assertEqual(
            '3.14159', conn.test_service.double_to_string(float(3.14159)))
        test.assertEqual(
            '42', conn.test_service.int32_to_string(42))


class TestThreading(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestThreading, cls).setUpClass()

    def test_thread_safe_connection(self) -> None:
        thread0 = threading.Thread(target=worker_thread, args=(self.conn,))
        thread1 = threading.Thread(target=worker_thread, args=(self.conn,))
        thread0.start()
        thread1.start()
        thread0.join()
        thread1.join()

    def test_rpc_interleaving(self) -> None:
        threads = [
            threading.Thread(target=worker_thread2, args=(self.conn, self))
            for _ in range(10)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()


if __name__ == '__main__':
    unittest.main()
