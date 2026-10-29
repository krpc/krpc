import subprocess
import sys
import time

import ksatest

import krpc

# Starts a warp, and blocks in the call until killed
WARP_CLIENT = """
import krpc
conn = krpc.connect(name="killed client")
print("connected", flush=True)
conn.ksa.warp_to(conn.ksa.ut + 3600)
"""


class TestServer(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_client_killed_during_call(self):
        client = subprocess.Popen(
            [sys.executable, "-c", WARP_CLIENT], stdout=subprocess.PIPE, text=True
        )
        try:
            assert client.stdout is not None
            self.assertEqual("connected", client.stdout.readline().strip())
            time.sleep(1)
        finally:
            client.kill()
            client.wait()
        time.sleep(1)
        # The server still answers this connection and new ones
        ut = self.ksa.ut
        ksatest.wait_until(lambda: self.ksa.ut > ut)
        with krpc.connect(name="after killed client") as conn:
            ksa = conn.ksa  # pyright: ignore[reportAttributeAccessIssue]
            self.assertIsNotNone(ksa.active_vehicle)


class TestGameCompatibility(ksatest.TestCase):
    def test_no_breakages(self):
        # The game the mod was built against has every member the mod uses
        self.assertEqual([], self.conn.testing_tools.compatibility_breakages)
