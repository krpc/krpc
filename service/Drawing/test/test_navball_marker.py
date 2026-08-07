import unittest
import krpctest


class TestNavballMarker(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.connect().drawing
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ref = cls.vessel.reference_frame

    def add_marker(self):
        return self.drawing.add_navball_marker((0, 1, 0), self.ref, False)

    def test_navball_marker(self):
        marker = self.drawing.add_navball_marker((1, 2, 3), self.ref)
        self.assertEqual((1, 2, 3), marker.direction)
        self.assertEqual(self.ref, marker.reference_frame)
        self.assertTrue(marker.visible)
        self.assertEqual((1, 1, 1), marker.color)
        self.assertEqual("default", marker.icon)
        self.assertAlmostEqual(1, marker.size)
        marker.remove()
        self.assertRaises(self.destroyed, marker.remove)

    def test_direction(self):
        marker = self.add_marker()
        self.assertFalse(marker.visible)
        marker.direction = (0, 0, 1)
        marker.visible = True
        self.assertTrue(marker.visible)
        self.assertEqual((0, 0, 1), marker.direction)
        marker.remove()

    def test_color(self):
        marker = self.add_marker()
        marker.color = (1, 0, 0)
        self.assertEqual((1, 0, 0), marker.color)
        marker.remove()

    def test_size(self):
        marker = self.add_marker()
        marker.size = 2.5
        self.assertAlmostEqual(2.5, marker.size)
        marker.remove()

    def test_icon(self):
        marker = self.add_marker()
        icons = marker.available_icons()
        self.assertIn("default", icons)
        self.assertIn("marker", icons)
        for icon in icons:
            marker.icon = icon
            self.assertEqual(icon, marker.icon)
        marker.remove()

    def test_unknown_icon(self):
        marker = self.add_marker()
        with self.assertRaises(ValueError):
            marker.icon = "not-an-icon"
        self.assertEqual("default", marker.icon)
        marker.remove()

    def test_clear(self):
        marker = self.add_marker()
        self.drawing.clear()
        self.assertRaises(self.destroyed, marker.remove)


if __name__ == "__main__":
    unittest.main()
