import unittest
import time
import krpctest

class TestPartsParachute(krpctest.TestCase):

    def setUp(self):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsParachute')
        krpctest.remove_other_vessels()
        self.conn = krpctest.connect(self)
        self.vessel = self.conn.space_center.active_vessel
        self.state = self.conn.space_center.ParachuteState
        self.parachutes = self.vessel.parts.parachutes

    def tearDown(self):
        self.conn.close()

    def test_parachute_on_ground(self):
        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)
            self.assertClose(parachute.deploy_altitude, 500)
            self.assertClose(parachute.deploy_min_pressure, 0.01)

        for alt in (50, 200, 500, 750, 643):
            for parachute in self.parachutes:
                parachute.deploy_altitude = alt
                time.sleep(0.1)
                self.assertClose(parachute.deploy_altitude, alt)
                time.sleep(0.1)

        for parachute in self.parachutes:
            parachute.deploy()
        time.sleep(0.1)
        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(parachute.state, self.state.active)

    def test_parachute_on_descent(self):
        deploy_altitude = 80
        for parachute in self.parachutes:
            parachute.deploy_altitude = deploy_altitude

        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(self.state.stowed, parachute.state)
            self.assertClose(deploy_altitude, parachute.deploy_altitude)
            self.assertClose(0.01, parachute.deploy_min_pressure)

        flight = self.vessel.flight(self.vessel.orbit.body.reference_frame)
        self.vessel.control.activate_next_stage()
        while flight.vertical_speed > 0:
            pass

        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(self.state.stowed, parachute.state)

        for parachute in self.parachutes:
            parachute.deploy()
        time.sleep(0.5)

        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(self.state.semi_deployed, parachute.state)

        while flight.surface_altitude > 0.9*deploy_altitude:
            pass

        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(self.state.deployed, parachute.state)

        while abs(flight.vertical_speed) > 0.1:
            pass
        time.sleep(0.5)

        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(self.state.cut, parachute.state)

if __name__ == '__main__':
    unittest.main()
