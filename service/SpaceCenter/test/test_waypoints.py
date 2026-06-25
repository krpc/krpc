import unittest
import krpctest


class TestWaypoints(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        cls.wpm = cls.space_center.waypoint_manager
        cls.body = cls.space_center.bodies["Kerbin"]

    def test_manager(self):
        # On a fresh save KSP creates a waypoint for each launch site. The two
        # Making History launch sites are only present when that DLC is
        # installed, so they are optional; the base-game sites are always there.
        base_game_sites = {"Dessert Airfield", "Island Airfield", "KSC"}
        dlc_sites = {"Dessert Launch Site", "Woomerang Launch Site"}
        waypoint_names = {wp.name for wp in self.wpm.waypoints}
        # All base-game launch site waypoints are present
        self.assertEqual(set(), base_game_sites - waypoint_names)
        # The only other waypoints allowed are the optional DLC launch sites
        self.assertEqual(set(), waypoint_names - base_game_sites - dlc_sites)
        self.assertCountEqual(
            [
                "ksc",
                "launchsite",
                "runway",
                "balloon",
                "default",
                "dish",
                "eva",
                "gravity",
                "marker",
                "pressure",
                "report",
                "sample",
                "seismic",
                "thermometer",
                "vessel",
                "custom",
            ],
            self.wpm.icons,
        )
        colors = self.wpm.colors
        self.assertTrue("blue" in colors)
        self.assertEqual(1115, colors["blue"])

    def test_add_waypoint(self):
        wp = self.wpm.add_waypoint(10, 20, self.body, "my-waypoint")
        try:
            self.assertEqual("my-waypoint", wp.name)
            self.assertEqual("report", wp.icon)
            self.assertEqual(1115, wp.color)
            self.assertEqual("Kerbin", wp.body.name)
            self.assertEqual(10, wp.latitude)
            self.assertEqual(20, wp.longitude)
            self.assertAlmostEqual(726.8, wp.mean_altitude, places=1)
            self.assertAlmostEqual(0, wp.surface_altitude, places=1)
            self.assertAlmostEqual(0, wp.bedrock_altitude, places=1)
            self.assertTrue(wp.near_surface)
            self.assertFalse(wp.grounded)
            self.assertEqual(0, wp.index)
            self.assertFalse(wp.clustered)
            self.assertFalse(wp.has_contract)
            self.assertRaises(RuntimeError, getattr, wp, "contract")
        finally:
            wp.remove()

    def test_on_sea(self):
        wp = self.wpm.add_waypoint(10, 0, self.body, "waypoint on sea")
        try:
            self.assertAlmostEqual(0, wp.surface_altitude, places=1)
            self.assertAlmostEqual(0, wp.mean_altitude, places=1)
            self.assertAlmostEqual(1125.7, wp.bedrock_altitude, places=1)
        finally:
            wp.remove()

    def test_above_sea(self):
        wp = self.wpm.add_waypoint(10, 0, self.body, "waypoint above sea")
        wp.surface_altitude = 1234
        try:
            self.assertAlmostEqual(1234, wp.surface_altitude, places=1)
            self.assertAlmostEqual(1234, wp.mean_altitude, places=1)
            self.assertAlmostEqual(1234 + 1125.7, wp.bedrock_altitude, places=1)
        finally:
            wp.remove()

    def test_on_surface(self):
        wp = self.wpm.add_waypoint(-10, 0, self.body, "waypoint on surface")
        try:
            self.assertAlmostEqual(0, wp.surface_altitude, places=1)
            self.assertAlmostEqual(601.4, wp.mean_altitude, places=1)
            self.assertAlmostEqual(0, wp.bedrock_altitude, places=1)
        finally:
            wp.remove()

    def test_above_surface(self):
        wp = self.wpm.add_waypoint(-10, 0, self.body, "waypoint above surface")
        wp.surface_altitude = 1234
        try:
            self.assertAlmostEqual(1234, wp.surface_altitude, places=1)
            self.assertAlmostEqual(1234 + 601.4, wp.mean_altitude, places=1)
            self.assertAlmostEqual(1234, wp.bedrock_altitude, places=1)
        finally:
            wp.remove()


if __name__ == "__main__":
    unittest.main()
