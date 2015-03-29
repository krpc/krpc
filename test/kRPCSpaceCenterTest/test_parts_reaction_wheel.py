import unittest
import testingtools
import krpc
import time

class TestPartsReactionWheel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpc.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestPartsReactionWheel')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_reaction_wheel(self):
        wheel = filter(lambda e: e.part.title == 'Advanced Reaction Wheel Module, Large', self.parts.reaction_wheels)[0]
        self.assertFalse(wheel.broken)
        self.assertEquals(30000, wheel.pitch_torque)
        self.assertEquals(30000, wheel.yaw_torque)
        self.assertEquals(30000, wheel.roll_torque)
        self.assertTrue(wheel.active)
        time.sleep(0.1)
        wheel.active = False
        time.sleep(0.1)
        self.assertFalse(wheel.active)
        wheel.active = True
        time.sleep(0.1)
        self.assertTrue(wheel.active)
        wheel.active = True
        time.sleep(0.1)
        self.assertTrue(wheel.active)

if __name__ == "__main__":
    unittest.main()
