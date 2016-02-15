import unittest
import testingtools
import krpc
import time

class TestPartsParachute(testingtools.TestCase):

    def setUp(self):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsParachute')
        testingtools.remove_other_vessels()
        self.conn = testingtools.connect(name='TestPartsParachute')
        self.vessel = self.conn.space_center.active_vessel
        self.parts = self.vessel.parts
        self.state = self.conn.space_center.ParachuteState

    def tearDown(self):
        self.conn.close()

    def test_parachute_on_ground(self):
        parachutes = self.parts.parachutes
        for parachute in parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)
            self.assertClose(parachute.deploy_altitude, 500)
            self.assertClose(parachute.deploy_min_pressure, 0.01)

        for alt in [50,200,500,750,643]:
            for parachute in parachutes:
                parachute.deploy_altitude = alt
                time.sleep(0.1)
                self.assertClose(parachute.deploy_altitude, alt)
                time.sleep(0.1)

        for parachute in parachutes:
            parachute.deploy()
        time.sleep(0.1)
        for parachute in parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(parachute.state, self.state.active)

    def test_parachute_on_descent(self):
        parachutes = self.parts.parachutes
        alt = 30
        for parachute in parachutes:
            parachute.deploy_altitude = alt

        for parachute in parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)
            self.assertClose(parachute.deploy_altitude, alt)
            self.assertClose(parachute.deploy_min_pressure, 0.01)

        flight = self.vessel.flight(self.vessel.orbit.body.reference_frame)
        self.vessel.control.activate_next_stage()
        while flight.vertical_speed > 0:
            pass

        for parachute in parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)

        for parachute in parachutes:
            parachute.deploy()
        time.sleep(0.5)

        for parachute in parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(parachute.state, self.state.semi_deployed)

        while flight.surface_altitude > 0.9*alt:
            pass

        for parachute in parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(parachute.state, self.state.deployed)

        while abs(flight.vertical_speed) > 0.1:
            pass
        time.sleep(0.5)

        for parachute in parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(parachute.state, self.state.cut)

if __name__ == "__main__":
    unittest.main()
