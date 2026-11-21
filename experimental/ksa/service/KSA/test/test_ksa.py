import struct
import time
from pathlib import Path

import ksatest


class TestKSA(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    def test_bodies(self):
        bodies = self.ksa.bodies
        self.assertIn("Earth", bodies)
        for name, body in bodies.items():
            self.assertEqual(name, body.name)

    def test_vehicles(self):
        names = [vehicle.name for vehicle in self.ksa.vehicles]
        self.assertIn("Gemini7", names)
        self.assertIn("Rocket", names)
        self.assertEqual(len(names), len(set(names)))

    def test_active_vehicle(self):
        active = self.ksa.active_vehicle
        self.assertIsNotNone(active)
        self.assertIn(active, self.ksa.vehicles)

    def test_ut_advances(self):
        ut = self.ksa.ut
        ksatest.wait_until(lambda: self.ksa.ut > ut + 0.1)

    def test_simulation_speed(self):
        try:
            self.ksa.simulation_speed = 2
            self.assertEqual(2, self.ksa.simulation_speed)
        finally:
            self.ksa.simulation_speed = 1
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_negative_simulation_speed(self):
        with self.assertRaises(ValueError):
            self.ksa.simulation_speed = -1
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_pause(self):
        try:
            self.conn.krpc.paused = True
            self.assertTrue(self.conn.krpc.paused)
            self.assertEqual(0, self.ksa.simulation_speed)
        finally:
            self.conn.krpc.paused = False
        self.assertFalse(self.conn.krpc.paused)
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_warp_to(self):
        ut = self.ksa.ut + 120
        self.ksa.warp_to(ut)
        self.assertAlmostEqual(ut, self.ksa.ut, delta=1)
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_warp_to_past(self):
        ut = self.ksa.ut
        self.ksa.warp_to(ut - 10)
        self.assertLess(self.ksa.ut, ut + 1)

    def test_save_and_load(self):
        vehicle = self.ksa.active_vehicle
        name = vehicle.name
        try:
            self.ksa.save("krpctest-save")
            self.assertTrue(self.ksa.save_exists("krpctest-save"))
            vehicle.name = "Renamed"
            self.ksa.load("krpctest-save")
            names = [v.name for v in self.ksa.vehicles]
            self.assertIn(name, names)
            self.assertNotIn("Renamed", names)
        finally:
            self.load_save()

    def test_load_counts_game_state(self):
        loads = self.conn.krpc.game_state_loads
        self.load_save()
        self.assertEqual(loads + 1, self.conn.krpc.game_state_loads)

    def test_load_missing_save(self):
        self.assertFalse(self.ksa.save_exists("krpctest-missing"))
        with self.assertRaises(ValueError):
            self.ksa.load("krpctest-missing")

    def test_message(self):
        self.ksa.show_message("krpctest message", 3)
        self.assertIn("krpctest message", self.conn.testing_tools.messages)
        self.ksa.show_message("krpctest warning", warning=True)
        self.assertIn("krpctest warning", self.conn.testing_tools.messages)

    def test_message_expires(self):
        self.ksa.show_message("krpctest short message", 0)
        self.assertIn("krpctest short message", self.conn.testing_tools.messages)
        ksatest.wait_until(
            lambda: "krpctest short message" not in self.conn.testing_tools.messages
        )


class TestScreenshot(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    def screenshot(self, *args, **kwargs):
        path = Path(self.ksa.take_screenshot(*args, **kwargs))
        self.addCleanup(path.unlink, missing_ok=True)
        self.assertTrue(path.is_file())
        self.assertEqual(".png", path.suffix)
        with open(path, "rb") as image:
            header = image.read(24)
        self.assertEqual(b"\x89PNG\r\n\x1a\n", header[:8])
        width, height = struct.unpack(">II", header[16:24])
        return width, height

    def test_screenshot(self):
        width, height = self.screenshot()
        self.assertGreater(width, 0)
        self.assertGreater(height, 0)
        # The file name has the time to the second
        time.sleep(1)
        # The game reduces the scale to fit the graphics memory
        scaled = self.screenshot(2)
        self.assertIn(scaled, [(width, height), (2 * width, 2 * height)])
        time.sleep(1)
        self.assertEqual((width, height), self.screenshot(ui=True))

    def test_invalid_scale(self):
        with self.assertRaises(ValueError):
            self.ksa.take_screenshot(0)
        with self.assertRaises(ValueError):
            self.ksa.take_screenshot(9)
