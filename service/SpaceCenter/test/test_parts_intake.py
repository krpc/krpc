import unittest
import krpctest

class TestPartsIntake(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.intake = parts.with_title('XM-G50 Radial Air Intake')[0].intake

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_properties(self):
        self.assertEqual(15, self.intake.speed)
        self.assertClose(4.14, self.intake.flow, error=0.05)
        self.assertClose(0.0031, self.intake.area)

    def test_open_and_close(self):
        self.assertTrue(self.intake.open)
        self.intake.open = False
        self.assertFalse(self.intake.open)
        self.intake.open = True
        self.assertTrue(self.intake.open)

if __name__ == '__main__':
    unittest.main()
