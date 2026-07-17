import unittest
import krpctest


class TestPartsRealChute(krpctest.TestCase):
    """Exercise the SpaceCenter Parachute wrapper against a RealChute parachute.

    The test craft carries a single RealChute stack chute (RC_stack). RealChute
    parachutes deploy in a single step (there is no stock-style semi-deployed phase),
    are armed by staging rather than by a right-click event, and do not expose the
    stock deploy-altitude / deploy-min-pressure settings.
    """

    mods = ["RealChute"]

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab("PartsRealChute")
        self.remove_other_vessels()
        self.vessel = self.connect().space_center.active_vessel
        self.control = self.vessel.control
        self.state = self.connect().space_center.ParachuteState
        self.parachutes = self.vessel.parts.parachutes

    def test_state_on_ground(self):
        self.assertGreater(len(self.parachutes), 0)
        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertFalse(parachute.armed)
            self.assertEqual(parachute.state, self.state.stowed)
            # The deploy-altitude and deploy-min-pressure settings are stock only.
            self.assertRaises(RuntimeError, getattr, parachute, "deploy_altitude")
            self.assertRaises(RuntimeError, setattr, parachute, "deploy_altitude", 100)
            self.assertRaises(RuntimeError, getattr, parachute, "deploy_min_pressure")
            self.assertRaises(
                RuntimeError, setattr, parachute, "deploy_min_pressure", 0.1
            )

    def test_deploy_and_cut(self):
        self.set_flight(altitude=2500, speed=50)
        for parachute in self.parachutes:
            self.assertFalse(parachute.deployed)
            self.assertEqual(parachute.state, self.state.stowed)

        for parachute in self.parachutes:
            parachute.deploy()
        for parachute in self.parachutes:
            self.wait_until(lambda p=parachute: p.deployed, message="deployed")
            self.assertEqual(parachute.state, self.state.deployed)
            self.assertFalse(parachute.armed)

        for parachute in self.parachutes:
            parachute.cut()
        for parachute in self.parachutes:
            self.wait_until(
                lambda p=parachute: p.state == self.state.cut, message="cut"
            )
            self.assertFalse(parachute.deployed)
            self.assertFalse(parachute.armed)

        # Park the now chute-less pod on the ground so it does not fall and crash, which
        # would pop a flight-results dialog and block the next test's setup.
        self.set_landed("Kerbin", self.KSC_LATITUDE, self.KSC_LONGITUDE)

    def test_arm_by_staging(self):
        # Point up and climb so the chute cannot deploy yet, then stage it. A RealChute
        # that must go down before deploying arms and waits instead.
        self.set_flight(altitude=6000, speed=100, pitch=80)
        for parachute in self.parachutes:
            self.assertFalse(parachute.armed)
            self.assertEqual(parachute.state, self.state.stowed)

        self.control.activate_next_stage()
        for parachute in self.parachutes:
            self.wait_until(lambda p=parachute: p.armed, message="armed")
            self.assertEqual(parachute.state, self.state.armed)
            self.assertFalse(parachute.deployed)

        # Once descending, an armed chute deploys on its own.
        self.set_flight(altitude=2500, speed=50)
        for parachute in self.parachutes:
            self.wait_until(lambda p=parachute: p.deployed, message="deployed")
            self.assertEqual(parachute.state, self.state.deployed)
            self.assertFalse(parachute.armed)


if __name__ == "__main__":
    unittest.main()
