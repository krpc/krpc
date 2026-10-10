import math
import unittest
import krpctest


class TestOrbitPath(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.space_center = cls.conn.space_center
        cls.kerbin = cls.space_center.bodies["Kerbin"]
        cls.set_circular_orbit("Kerbin", 100000)

    def create_orbit(self, semi_major_axis, eccentricity):
        return self.space_center.Orbit.create_from_orbital_elements(
            self.kerbin, semi_major_axis, eccentricity, 0, 0, 0, 0, self.space_center.ut
        )

    def test_vessel_orbit(self):
        orbit = self.space_center.active_vessel.orbit
        path = self.drawing.add_orbit(orbit)
        self.assertEqual(orbit, path.orbit)
        self.assertTrue(path.visible)
        self.assertTrue(path.whole_orbit)
        self.assertEqual((1, 1, 1), path.color)
        self.assertAlmostEqual(0.1, path.thickness)
        self.assertFalse(path.start_arrow)
        self.assertFalse(path.end_arrow)
        path.end_arrow = True
        self.wait()
        path.remove()
        self.assertRaises(self.destroyed, path.remove)

    def test_segment(self):
        path = self.drawing.add_orbit(self.space_center.active_vessel.orbit)
        path.start_true_anomaly = 1
        self.assertFalse(path.whole_orbit)
        path.end_true_anomaly = -1
        path.end_arrow = True
        self.wait()
        self.assertAlmostEqual(1, path.start_true_anomaly)
        self.assertAlmostEqual(-1, path.end_true_anomaly)
        path.whole_orbit = True
        self.wait()
        self.assertTrue(path.whole_orbit)
        path.remove()

    def test_large_true_anomalies(self):
        # A segment of a closed orbit is drawn as at most one turn
        path = self.drawing.add_orbit(self.space_center.active_vessel.orbit)
        path.start_true_anomaly = 1e12
        path.end_true_anomaly = 0
        self.wait()
        path.start_true_anomaly = 0
        path.end_true_anomaly = 1e12
        self.wait()
        self.assertAlmostEqual(1e12, path.end_true_anomaly)
        path.remove()

    def test_hyperbolic_orbit(self):
        orbit = self.create_orbit(-1000000, 1.5)
        path = self.drawing.add_orbit(orbit)
        path.start_arrow = True
        path.end_arrow = True
        self.wait()
        # A segment beyond the asymptotes is clamped to the part of the orbit inside the
        # sphere of influence
        path.start_true_anomaly = -math.pi
        path.end_true_anomaly = math.pi
        self.wait()
        path.remove()
        orbit.remove()

    def test_removed_orbit(self):
        orbit = self.create_orbit(2000000, 0.2)
        path = self.drawing.add_orbit(orbit)
        self.wait()
        orbit.remove()
        self.wait()
        # The path stays, and can be given another orbit
        self.assertTrue(path.visible)
        path.orbit = self.space_center.active_vessel.orbit
        self.wait()
        path.remove()


if __name__ == "__main__":
    unittest.main()
