import unittest

import krpctest


class TestPartsLaunchClamp(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsSolarPanel")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.clamp = cls.vessel.parts.launch_clamps[0]

    def test_launch_clamp(self):
        self.assertEqual(self.vessel, self.clamp.part.vessel)
        self.clamp.release()
        self.wait_until(
            lambda: self.clamp.part.vessel != self.vessel,
            message="the clamp to detach from the vessel",
        )
        self.assertNotIn(self.clamp, self.vessel.parts.launch_clamps)


if __name__ == "__main__":
    unittest.main()
