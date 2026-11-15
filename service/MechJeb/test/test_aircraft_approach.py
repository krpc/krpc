import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAircraftApproach(MechJebTestCase):
    def test_settings(self):
        approach = self.mj.aircraft_approach
        self.assert_settings(
            [
                (approach, "glideslope", 3.5),
                (approach, "approach_speed", 90),
                (approach, "touchdown_speed", 50),
                (approach, "engage_reverse_if_available", False),
                (approach, "brake_as_soon_as_landed", True),
            ]
        )

    def test_runways(self):
        approach = self.mj.aircraft_approach
        runways = approach.runways
        self.assertGreater(len(runways), 1)
        self.assertIn(approach.runway, runways)
        old = approach.runway
        try:
            for runway in runways:
                approach.runway = runway
                self.assertEqual(runway, approach.runway)
                self.assertGreater(approach.distance_to_runway, 0)
        finally:
            approach.runway = old
        with self.assertRaises(ValueError):
            approach.runway = "No such runway"

    def test_autoland(self):
        approach = self.mj.aircraft_approach
        self.assertFalse(approach.enabled)
        approach.autoland()
        try:
            self.assertTrue(approach.enabled)
            self.assertIsInstance(approach.state, str)
        finally:
            approach.abort()
        self.assertFalse(approach.enabled)


if __name__ == "__main__":
    unittest.main()
