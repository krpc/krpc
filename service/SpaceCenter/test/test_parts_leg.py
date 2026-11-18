import unittest

from krpctools.game.ksp import TestCase


class TestPartsLeg(TestCase):
    """The craft stands on four LT-2 legs. The LT-1 and LT-05 are mounted upside
    down on top, and never touch the ground."""

    @classmethod
    def setUpClass(cls):
        vessel = cls.launch("PartsWheelsLegs", editor="SPH")
        cls.State = cls.connect().space_center.DeployableState
        cls.legs = vessel.parts.legs
        cls.leg = vessel.parts.with_name("miniLandingLeg")[0].leg
        cls.control = vessel.control

    def all_deployed(self):
        return all(leg.deployed for leg in self.legs)

    def test_deploy_and_retract(self):
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.assertTrue(self.leg.deployable)
        self.assertFalse(self.control.legs)
        self.leg.deployed = True
        self.wait()
        self.assertEqual(self.State.deploying, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.deploying:
            self.wait()
        self.assertEqual(self.State.deployed, self.leg.state)
        self.assertTrue(self.leg.deployed)
        self.assertEqual(self.all_deployed(), self.control.legs)
        self.leg.deployed = False
        self.wait()
        self.assertEqual(self.State.retracting, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.retracting:
            self.wait()
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.assertFalse(self.leg.is_grounded)
        self.assertFalse(self.control.legs)

    def test_control_deploy(self):
        initial = [leg.deployed for leg in self.legs]
        self.assertEqual(all(initial), self.control.legs)
        self.control.legs = True
        while not self.control.legs:
            self.wait()
        for leg in self.legs:
            self.assertTrue(leg.deployed)
        # Retracting the legs sets the craft down on its wheels
        self.control.legs = False
        while self.control.legs:
            self.wait()
        for leg in self.legs:
            while leg.state == self.State.retracting:
                self.wait()
            self.assertFalse(leg.deployed)
        # Back to the legs' initial states, the craft standing on its LT-2 legs
        for leg, deployed in zip(self.legs, initial):
            leg.deployed = deployed
        for leg, deployed in zip(self.legs, initial):
            while leg.deployed != deployed:
                self.wait()
        self.wait(2)

    def test_config(self):
        # LT-05 Micro Landing Strut
        self.assertAlmostEqual(40, self.leg.impact_tolerance)
        self.assertAlmostEqual(1, self.leg.explode_multiplier)
        self.assertGreater(self.leg.suspension_spring_strength, 0)
        self.assertGreater(self.leg.suspension_damper_strength, 0)
        self.assertAlmostEqual(0.8, self.leg.suspension_travel, places=3)
        # LT-2 Landing Strut, its configured travel scaled by the part
        for part in self.legs[0].part.vessel.parts.with_name("landingLeg1-2"):
            self.assertAlmostEqual(600, part.leg.impact_tolerance)
            self.assertAlmostEqual(1.1875, part.leg.suspension_travel, places=3)


class TestPartsLegLanded(TestCase):
    """The craft stands on its four LT-2 legs on the runway."""

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("PartsWheelsLegs", editor="SPH")
        cls.body = cls.vessel.orbit.body
        cls.legs = cls.vessel.parts.legs
        cls.standing = [x.leg for x in cls.vessel.parts.with_name("landingLeg1-2")]
        cls.wait_game_time(5)

    def total_load(self):
        """The legs' summed load, averaged over a second"""
        samples = []
        for _ in range(20):
            samples.append(sum(leg.load for leg in self.standing))
            self.wait(0.05)
        return sum(samples) / len(samples)

    def test_grounded(self):
        self.assertEqual(4, len(self.standing))
        for leg in self.legs:
            self.assertEqual(leg in self.standing, leg.is_grounded)
        for wheel in self.vessel.parts.wheels:
            self.assertFalse(wheel.grounded)

    def test_load(self):
        # The game raises a grounded leg's load to at least 10 kN, which is more
        # than this craft puts on each leg. A downward force at the center of
        # mass lifts every leg's load above that, and the loads then carry the
        # weight and the force, in kN.
        for leg in self.legs:
            if leg in self.standing:
                self.assertGreaterEqual(leg.load, 10)
            else:
                self.assertAlmostEqual(0, leg.load)
        weight = self.vessel.mass * self.body.surface_gravity / 1000
        ref = self.vessel.surface_reference_frame
        root = self.vessel.parts.root
        for push in (45, 90):
            force = root.add_force((-push * 1000, 0, 0), (0, 0, 0), ref)
            try:
                self.wait_game_time(3)
                for leg in self.standing:
                    self.assertGreater(leg.load, 10)
                self.assertAlmostEqual(
                    weight + push, self.total_load(), delta=(weight + push) * 0.03
                )
            finally:
                force.remove()
                self.wait_game_time(3)

    def test_suspension(self):
        # The game only sets the spring and damper while the leg is grounded
        for leg in self.legs:
            if leg in self.standing:
                suspension_spring = leg.suspension_spring
                assert suspension_spring is not None
                self.assertGreater(suspension_spring, 0)
                suspension_damper = leg.suspension_damper
                assert suspension_damper is not None
                self.assertGreater(suspension_damper, 0)
            else:
                self.assertIsNone(leg.suspension_spring)
                self.assertIsNone(leg.suspension_damper)

    def test_contact_position(self):
        # The standing legs' feet are the lowest points of the craft, on the flat
        # runway. The other legs' feet are above them. The positions are read in
        # one held tick, as the craft moves on its suspension.
        ref = self.body.reference_frame

        def altitude(leg):
            return self.body.altitude_at_position(leg.contact_position(ref), ref)

        surface = self.vessel.surface_reference_frame
        krpc = self.connect().krpc
        krpc.hold_tick()
        try:
            bottom = (
                self.vessel.flight().mean_altitude
                + self.vessel.bounding_box(surface)[0][0]
            )
            runway = altitude(self.standing[0])
            altitudes = {leg: altitude(leg) for leg in self.legs}
        finally:
            krpc.release_tick()
        self.assertAlmostEqual(bottom, runway, delta=0.15)
        for leg in self.legs:
            if leg in self.standing:
                self.assertAlmostEqual(runway, altitudes[leg], delta=0.02)
            else:
                self.assertGreater(altitudes[leg], runway + 0.5)


if __name__ == "__main__":
    unittest.main()
