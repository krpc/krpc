import unittest
import krpctest


class TestPartsFairing(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsFairing')
        cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.fairing = parts.with_title(
            'AE-FF1 Airstream Protective Shell (1.25m)')[0].fairing

    def test_jettison(self):
        self.assertFalse(self.fairing.jettisoned)
        self.fairing.jettison()
        self.assertTrue(self.fairing.jettisoned)


if __name__ == '__main__':
    unittest.main()
