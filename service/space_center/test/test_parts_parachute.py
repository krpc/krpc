import unittest
import krpctest


class TestPartsParachute(krpctest.TestCase):

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab('PartsParachute')
        self.remove_other_vessels()
        self.vessel = self.connect().space_center.active_vessel
        self.control = self.vessel.control
        self.state = self.connect().space_center.ParachuteState
        self.parachutes = self.vessel.parts.parachutes

    def test_parachute_on_ground(self):
        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)
            self.assertAlmostEqual(parachute.deploy_altitude, 500)
            self.assertAlmostEqual(parachute.deploy_min_pressure, 0.01)

        for alt in (50, 200, 500, 750, 643):
            for parachute in self.parachutes:
                parachute.deploy_altitude = alt
                self.wait()
                self.assertAlmostEqual(parachute.deploy_altitude, alt)
                self.wait()

        for parachute in self.parachutes:
            parachute.deploy()
        self.wait()
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
            self.assertAlmostEqual(deploy_altitude, parachute.deploy_altitude)
            self.assertAlmostEqual(0.01, parachute.deploy_min_pressure)

        flight = self.vessel.flight(self.vessel.orbit.body.reference_frame)
        self.vessel.control.activate_next_stage()
        while flight.vertical_speed > 0:
            pass

        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(self.state.stowed, parachute.state)

        for parachute in self.parachutes:
            parachute.deploy()
        self.wait(0.5)
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
        self.wait(0.5)

        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(self.state.cut, parachute.state)

    def test_control(self):
        deploy_altitude = 80
        for parachute in self.parachutes:
            parachute.deploy_altitude = deploy_altitude

        self.assertFalse(self.control.parachutes)
        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)

        flight = self.vessel.flight(self.vessel.orbit.body.reference_frame)
        self.vessel.control.activate_next_stage()
        while flight.vertical_speed > 0:
            pass

        self.control.parachutes = True
        self.wait(0.5)

        self.assertTrue(self.control.parachutes)
        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)

        while abs(flight.vertical_speed) > 0.1:
            pass
        self.wait(0.5)

        for parachute in self.parachutes:
            self.assertTrue(parachute.deployed)
            self.assertEqual(self.state.cut, parachute.state)


if __name__ == '__main__':
    unittest.main()
