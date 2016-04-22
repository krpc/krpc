import unittest
import testingtools
import krpc
import time

class TestPartsIntake(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsIntake')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_properties(self):
        intake = next(iter(filter(lambda e: e.part.title == 'XM-G50 Radial Air Intake', self.parts.intakes)))
        self.assertEqual(15, intake.speed)
        self.assertClose(4.14, intake.flow, error=0.05)
        self.assertClose(0.0031, intake.area)

    def test_open_and_close(self):
        intake = next(iter(filter(lambda e: e.part.title == 'XM-G50 Radial Air Intake', self.parts.intakes)))
        self.assertTrue(intake.open)
        intake.open = False
        time.sleep(0.1)
        self.assertFalse(intake.open)
        intake.open = True
        time.sleep(0.1)
        self.assertTrue(intake.open)

if __name__ == "__main__":
    unittest.main()
