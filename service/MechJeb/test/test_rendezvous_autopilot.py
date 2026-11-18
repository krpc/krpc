import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestRendezvousAutopilot(MechJebTestCase):
    def test_settings(self):
        rendezvous = self.mj.rendezvous_autopilot
        self.assert_settings(
            [
                (rendezvous, "desired_distance", 123),
                (rendezvous, "max_phasing_orbits", 7),
                (rendezvous, "max_closing_speed", 12),
                (rendezvous, "autowarp", False),
            ]
        )

    def test_read_only(self):
        self.assertIsInstance(self.mj.rendezvous_autopilot.status, str)


if __name__ == "__main__":
    unittest.main()
