import unittest

import krpctest


class TestMapMarker(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.icons = cls.drawing.MapMarkerIcon
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_map_marker(self):
        marker = self.drawing.add_map_marker((1, 2, 3), self.ref)
        self.assertEqual((1, 2, 3), marker.position)
        self.assertEqual(self.ref, marker.reference_frame)
        self.assertEqual(self.icons.generic, marker.icon)
        self.assertEqual("", marker.caption)
        self.assertEqual((1, 1, 1), marker.color)
        self.assertTrue(marker.visible)
        marker.remove()
        self.assertRaises(self.destroyed, marker.remove)

    def test_properties(self):
        marker = self.drawing.add_map_marker(
            (0, 0, 0), self.ref, self.icons.apoapsis, "Apoapsis", False
        )
        marker.position = (0, 10, 0)
        marker.caption = "Top"
        marker.color = (1, 0, 0)
        marker.visible = True
        self.wait_for_frame()
        self.assertEqual((0, 10, 0), marker.position)
        self.assertEqual(self.icons.apoapsis, marker.icon)
        self.assertEqual("Top", marker.caption)
        self.assertEqual((1, 0, 0), marker.color)
        marker.remove()
        self.assertRaises(self.destroyed, setattr, marker, "icon", self.icons.ship)

    def test_icons(self):
        marker = self.drawing.add_map_marker((0, 0, 0), self.ref)
        for icon in self.icons:
            marker.icon = icon
            self.wait_for_frame()
            self.assertEqual(icon, marker.icon)
        marker.remove()


if __name__ == "__main__":
    unittest.main()
