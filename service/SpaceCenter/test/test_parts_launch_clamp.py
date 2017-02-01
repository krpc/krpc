import unittest
import krpctest


class TestPartsLaunchClamp(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsSolarPanel')
        cls.remove_other_vessels()
        cls.clamp = cls.connect().space_center.active_vessel \
                                              .parts.launch_clamps[0]

    def test_launch_clamp(self):
        # TODO: improve this test
        self.clamp.release()


if __name__ == '__main__':
    unittest.main()
