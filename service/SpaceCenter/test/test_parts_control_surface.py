import unittest

import krpctest


class TestPartsControlSurface(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "PartsControlSurface":
            cls.new_save()
            cls.launch_vessel_from_vab("PartsControlSurface")
            cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.wait_for_rest(parts.root)
        cls.ctrlsrf = parts.with_name("airlinerCtrlSrf")[0].control_surface
        cls.winglets = [x.control_surface for x in parts.with_name("winglet3")]
        cls.winglet = cls.winglets[0]

        # KSP re-derives a control surface's pitch/yaw/roll authority from its
        # geometry at launch, overriding the ignorePitch/ignoreYaw/ignoreRoll
        # values persisted in the craft file (unlike deploy/inverted, which are
        # not geometry-derived and do survive). The tail airlinerCtrlSrf, saved
        # as yaw-only, comes back pitch-enabled. So establish the expected axis
        # baseline here via the API rather than trusting the craft to survive
        # the launch; the get/toggle tests then exercise the getter/setter
        # round-trip against a known starting state.
        cls.ctrlsrf.pitch_enabled = False
        cls.ctrlsrf.yaw_enabled = True
        cls.ctrlsrf.roll_enabled = False
        for winglet in cls.winglets:
            winglet.pitch_enabled = True
            winglet.yaw_enabled = False
            winglet.roll_enabled = True
        cls.wait()

    def test_get_pyr_enabled(self):
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        for winglet in self.winglets:
            self.assertTrue(winglet.pitch_enabled)
            self.assertFalse(winglet.yaw_enabled)
            self.assertTrue(winglet.roll_enabled)

    def test_toggle_pyr_enabled(self):
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.pitch_enabled = True
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.yaw_enabled = False
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.roll_enabled = True
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertTrue(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.pitch_enabled = False
        self.ctrlsrf.yaw_enabled = True
        self.ctrlsrf.roll_enabled = False
        self.wait()
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)

    def test_authority_limiter(self):
        self.assertEqual(100, self.ctrlsrf.authority_limiter)
        self.ctrlsrf.authority_limiter = 50
        self.assertEqual(50, self.ctrlsrf.authority_limiter)
        self.ctrlsrf.authority_limiter = 100
        self.assertEqual(100, self.ctrlsrf.authority_limiter)

    def test_inverted(self):
        self.assertFalse(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = True
        self.wait()
        self.assertTrue(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = False
        self.wait()
        self.assertFalse(self.ctrlsrf.inverted)

    def test_deployed(self):
        self.assertFalse(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = True
        self.wait()
        self.assertTrue(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = False
        self.wait()
        self.assertFalse(self.ctrlsrf.deployed)

    def test_deploy_angle(self):
        # The limits default to +/-1.5 * ctrlSurfaceRange, and the angle itself
        # to ctrlSurfaceRange, so they differ between the two part types
        self.assertAlmostEqual(-22.5, self.ctrlsrf.min_deploy_angle)
        self.assertAlmostEqual(22.5, self.ctrlsrf.max_deploy_angle)
        self.assertAlmostEqual(15, self.ctrlsrf.deploy_angle)
        self.assertAlmostEqual(-37.5, self.winglet.min_deploy_angle)
        self.assertAlmostEqual(37.5, self.winglet.max_deploy_angle)
        self.assertAlmostEqual(25, self.winglet.deploy_angle)
        self.ctrlsrf.deploy_angle = -10
        self.wait()
        self.assertAlmostEqual(-10, self.ctrlsrf.deploy_angle)
        # Out of range values clamp to the limits
        self.ctrlsrf.deploy_angle = 100
        self.assertAlmostEqual(22.5, self.ctrlsrf.deploy_angle)
        self.ctrlsrf.deploy_angle = -100
        self.assertAlmostEqual(-22.5, self.ctrlsrf.deploy_angle)
        # The angle is persistent, so restore it for the other tests
        self.ctrlsrf.deploy_angle = 15
        self.wait()
        self.assertAlmostEqual(15, self.ctrlsrf.deploy_angle)

    def test_deploy_angle_during_override(self):
        cs = self.ctrlsrf
        cs.deflection_override = True
        self.wait()
        # The override drives the angle, so a value set here is held for the release
        cs.deploy_angle = -5
        self.assertAlmostEqual(-5, cs.deploy_angle)
        cs.deflection_override = False
        self.wait()
        self.assertAlmostEqual(-5, cs.deploy_angle)
        cs.deploy_angle = 15
        self.wait()

    def test_deployed_during_override(self):
        cs = self.ctrlsrf
        self.assertFalse(cs.deployed)
        cs.deflection_override = True
        self.wait()
        # The override holds the surface deployed, so it cannot be set
        self.assertTrue(cs.deployed)
        self.assertRaises(RuntimeError, setattr, cs, "deployed", False)
        self.assertRaises(RuntimeError, setattr, cs, "deployed", True)
        cs.deflection_override = False
        self.wait()
        self.assertFalse(cs.deployed)

    def test_deflection_override(self):
        cs = self.ctrlsrf
        self.assertFalse(cs.deflection_override)
        self.assertAlmostEqual(0, cs.deflection)
        # Enabling the override removes cooked control and deploys the surface
        cs.deflection_override = True
        self.wait()
        self.assertTrue(cs.deflection_override)
        self.assertFalse(cs.pitch_enabled)
        self.assertFalse(cs.yaw_enabled)
        self.assertFalse(cs.roll_enabled)
        self.assertTrue(cs.deployed)
        # The deflection command round-trips and clamps to [-1, 1]
        for deflection in (1, -1, 0.5, 0):
            cs.deflection = deflection
            self.assertAlmostEqual(deflection, cs.deflection)
        cs.deflection = 5
        self.assertAlmostEqual(1, cs.deflection)
        cs.deflection = -5
        self.assertAlmostEqual(-1, cs.deflection)
        # Disabling restores the prior control-surface state (the axis baseline
        # and deploy state set up in setUpClass)
        cs.deflection_override = False
        self.wait()
        self.assertFalse(cs.deflection_override)
        self.assertAlmostEqual(0, cs.deflection)
        self.assertFalse(cs.pitch_enabled)
        self.assertTrue(cs.yaw_enabled)
        self.assertFalse(cs.roll_enabled)
        self.assertFalse(cs.deployed)
        self.assertAlmostEqual(15, cs.deploy_angle)

    def test_deflection_override_released_on_disconnect(self):
        cs = self.ctrlsrf
        self.assertFalse(cs.deflection_override)

        conn = self.connect(use_cached=False)
        parts = conn.space_center.active_vessel.parts
        other = parts.with_name("airlinerCtrlSrf")[0].control_surface
        other.deflection_override = True
        other.deflection = 1
        self.wait()
        self.assertTrue(cs.deflection_override)
        conn.close()

        # The disconnect restores the prior control-surface state (the axis
        # baseline and deploy state set up in setUpClass)
        self.wait()
        self.assertFalse(cs.deflection_override)
        self.assertAlmostEqual(0, cs.deflection)
        self.assertFalse(cs.pitch_enabled)
        self.assertTrue(cs.yaw_enabled)
        self.assertFalse(cs.roll_enabled)
        self.assertFalse(cs.deployed)
        self.assertAlmostEqual(15, cs.deploy_angle)

    def test_surface_area(self):
        self.assertAlmostEqual(1, self.ctrlsrf.surface_area)
        self.assertAlmostEqual(0.2, self.winglet.surface_area)

    def test_control_range(self):
        self.assertAlmostEqual(15, self.ctrlsrf.control_range)
        self.assertAlmostEqual(25, self.winglet.control_range)

    def test_actuator_speed(self):
        self.assertAlmostEqual(25, self.ctrlsrf.actuator_speed)
        self.assertAlmostEqual(25, self.winglet.actuator_speed)

    def test_deflection_angle(self):
        # A full pitch input moves the pitch-enabled winglet at its actuator speed,
        # and releasing it returns the winglet to neutral
        control = self.connect().space_center.active_vessel.control
        self.assertAlmostEqual(0, self.winglet.deflection_angle, delta=0.01)
        control.pitch = 1
        try:
            self.wait(0.4)
            angle = abs(self.winglet.deflection_angle)
            self.assertGreater(angle, 1)
            self.assertLess(angle, self.winglet.control_range)
            self.wait(1.5)
            self.assertAlmostEqual(
                self.winglet.control_range,
                abs(self.winglet.deflection_angle),
                delta=self.winglet.control_range * 0.5,
            )
        finally:
            control.pitch = 0
        self.wait(1.5)
        self.assertAlmostEqual(0, self.winglet.deflection_angle, delta=0.01)

    def test_available_torque(self):
        # Not verified here: that authority_limiter scales available_torque proportionally
        # (mirrors test_authority_limiter in test_parts_reaction_wheel.py). This
        # requires the vessel to be in motion, as GetPotentialTorque() returns zero
        # at rest with no airspeed, so the scaling cannot be exercised here.
        self.assertAlmostEqual((0, 0, 0), self.ctrlsrf.available_torque[0], places=3)
        self.assertAlmostEqual((0, 0, 0), self.winglet.available_torque[1], places=3)


class TestPartsControlSurfaceFlight(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        # The elevators sit well off the centerline, which a torque estimate scaled
        # by the lever arm along the pitch axis would inflate
        cls.launch_vessel_from_sph("Mallard")
        cls.conn = cls.connect()
        cls.vessel = cls.conn.space_center.active_vessel

    def fly(self, pitch):
        """The pitch torque the game applies to the vessel, and the fraction of its
        range an elevator has deflected, 0.1 s after a pitch input from level flight.
        Also the available pitch torque before the input."""
        vessel = self.vessel
        control = vessel.control
        control.sas = False
        control.pitch = 0
        self.set_flight(altitude=3000, speed=120)
        self.wait(1)
        flight = vessel.flight(vessel.reference_frame)
        elevator = next(x for x in vessel.parts.control_surfaces if x.pitch_enabled)
        space_center = self.conn.space_center
        available = vessel.available_control_surface_torque.positive[0]

        def sample():
            return (
                space_center.ut,
                flight.aerodynamic_torque,
                elevator.deflection_angle,
            )

        function = self.conn.compile(sample)
        with self.conn.stream(function) as stream:
            start = stream()[0]
            control.pitch = pitch
            while True:
                ut, torque, angle = stream()
                if ut - start >= 0.1:
                    break
                self.wait(0.005)
        control.pitch = 0
        return torque[0], angle / elevator.control_range, available

    def test_available_torque(self):
        # Early on, before the attitude moves, the change in the applied pitch
        # torque is the available torque scaled by the deflection
        torque, _, _ = self.fly(0)
        deflected_torque, deflection, available = self.fly(1)
        self.assertGreater(available, 0)
        self.assertGreater(abs(deflection), 0.05)
        self.assertAlmostEqual(
            available * abs(deflection),
            abs(deflected_torque - torque),
            delta=0.05 * available * abs(deflection),
        )


if __name__ == "__main__":
    unittest.main()
