import math
import os
import unittest

from krpctools.game.geometry import dot, norm
from krpctools.game.ksp import TestCase

CRAFT = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..",
    "..",
    "SpaceCenter",
    "test",
    "craft",
)


class MechJebTestCase(TestCase):
    mods = ["MechJeb"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic", directory=CRAFT)
        cls.conn = cls.connect()
        cls.mj = cls.conn.mech_jeb
        cls.sc = cls.conn.space_center
        cls.wait_until(lambda: cls.mj.ready)


class TestMechJeb(MechJebTestCase):
    def test_ready(self):
        self.assertTrue(self.mj.ready)

    def settings(self):
        """Each settable member, as (object, attribute, value to set)."""
        mj = self.mj
        ascent = mj.ascent_autopilot
        planner = mj.maneuver_planner
        smart_ass = mj.smart_ass
        return [
            (mj.airplane_autopilot, "heading_hold_enabled", True),
            (mj.airplane_autopilot, "altitude_hold_enabled", True),
            (mj.airplane_autopilot, "vert_speed_hold_enabled", True),
            (mj.airplane_autopilot, "roll_hold_enabled", True),
            (mj.airplane_autopilot, "speed_hold_enabled", True),
            (mj.airplane_autopilot, "altitude_target", 1234),
            (mj.airplane_autopilot, "heading_target", 123),
            (mj.airplane_autopilot, "roll_target", 12),
            (mj.airplane_autopilot, "speed_target", 123),
            (mj.airplane_autopilot, "vert_speed_target", 12),
            (mj.airplane_autopilot, "bank_angle", 12),
            (mj.airplane_autopilot, "acc_kp", 0.12),
            (mj.airplane_autopilot, "acc_ki", 0.12),
            (mj.airplane_autopilot, "acc_kd", 0.12),
            (mj.airplane_autopilot, "pit_kp", 0.12),
            (mj.airplane_autopilot, "pit_ki", 0.12),
            (mj.airplane_autopilot, "pit_kd", 0.12),
            (mj.airplane_autopilot, "rol_kp", 0.12),
            (mj.airplane_autopilot, "rol_ki", 0.12),
            (mj.airplane_autopilot, "rol_kd", 0.12),
            (mj.airplane_autopilot, "yaw_kp", 0.12),
            (mj.airplane_autopilot, "yaw_ki", 0.12),
            (mj.airplane_autopilot, "yaw_kd", 0.12),
            (mj.airplane_autopilot, "yaw_limit", 12),
            (mj.airplane_autopilot, "roll_limit", 12),
            (mj.airplane_autopilot, "pitch_up_limit", 12),
            (mj.airplane_autopilot, "pitch_down_limit", 12),
            (ascent, "desired_orbit_altitude", 123000),
            (ascent, "desired_inclination", 12),
            (ascent, "corrective_steering", True),
            (ascent, "corrective_steering_gain", 1.5),
            (ascent, "force_roll", False),
            (ascent, "vertical_roll", 12),
            (ascent, "turn_roll", 12),
            (ascent, "autodeploy_solar_panels", False),
            (ascent, "auto_deploy_antennas", False),
            (ascent, "skip_circularization", True),
            (ascent, "autostage", False),
            (ascent, "limit_ao_a", False),
            (ascent, "max_ao_a", 12),
            (ascent, "ao_a_limit_fadeout_pressure", 1234),
            (ascent, "launch_phase_angle", 12),
            (ascent, "launch_lan_difference", 12),
            (ascent, "warp_count_down", 12),
            (ascent.ascent_path_classic, "turn_start_altitude", 1234),
            (ascent.ascent_path_classic, "turn_start_velocity", 123),
            (ascent.ascent_path_classic, "turn_end_altitude", 12345),
            (ascent.ascent_path_classic, "turn_end_angle", 12),
            (ascent.ascent_path_classic, "turn_shape_exponent", 0.5),
            (ascent.ascent_path_classic, "auto_path", False),
            (ascent.ascent_path_classic, "auto_turn_percent", 0.5),
            (ascent.ascent_path_classic, "auto_turn_speed_factor", 12),
            (ascent.ascent_path_pvg, "pitch_start_velocity", 12),
            (ascent.ascent_path_pvg, "pitch_rate", 0.75),
            (ascent.ascent_path_pvg, "desired_apoapsis", 123000),
            (ascent.ascent_path_pvg, "attach_alt_flag", True),
            (ascent.ascent_path_pvg, "desired_attach_alt", 123000),
            (ascent.ascent_path_pvg, "dynamic_pressure_trigger", 1234),
            (ascent.ascent_path_pvg, "staging_trigger", 2),
            (ascent.ascent_path_pvg, "staging_trigger_flag", True),
            (ascent.ascent_path_pvg, "fixed_coast", False),
            (ascent.ascent_path_pvg, "fixed_coast_length", 12),
            (mj.docking_autopilot, "speed_limit", 1.5),
            (mj.docking_autopilot, "force_roll", True),
            (mj.docking_autopilot, "roll", 12),
            (mj.docking_autopilot, "override_safe_distance", True),
            (mj.docking_autopilot, "overriden_safe_distance", 12),
            (mj.docking_autopilot, "override_target_size", True),
            (mj.docking_autopilot, "overriden_target_size", 12),
            (mj.landing_autopilot, "touchdown_speed", 1.5),
            (mj.landing_autopilot, "deploy_gears", False),
            (mj.landing_autopilot, "limit_gears_stage", 2),
            (mj.landing_autopilot, "deploy_chutes", False),
            (mj.landing_autopilot, "limit_chutes_stage", 2),
            (mj.landing_autopilot, "rcs_adjustment", False),
            (mj.rendezvous_autopilot, "desired_distance", 123),
            (mj.rendezvous_autopilot, "max_phasing_orbits", 7),
            (smart_ass, "interface_mode", self.mj.SmartASSInterfaceMode.target),
            (smart_ass, "force_yaw", False),
            (smart_ass, "force_pitch", False),
            (smart_ass, "force_roll", True),
            (smart_ass, "surface_heading", 12),
            (smart_ass, "surface_pitch", 12),
            (smart_ass, "surface_roll", 12),
            (smart_ass, "surface_vel_yaw", 12),
            (smart_ass, "surface_vel_pitch", 12),
            (smart_ass, "surface_vel_roll", 12),
            (
                smart_ass,
                "advanced_reference",
                self.mj.AttitudeReference.surface_horizontal,
            ),
            (smart_ass, "advanced_direction", self.mj.Direction.left),
            (mj.smart_rcs, "auto_disable_smart_rcs", False),
            (mj.translatron, "kill_horizontal_speed", True),
            (mj.antenna_controller, "auto_deploy", True),
            (mj.solar_panel_controller, "auto_deploy", True),
            (mj.node_executor, "autowarp", False),
            (mj.node_executor, "lead_time", 12),
            (mj.rcs_controller, "rcs_throttle", False),
            (mj.rcs_controller, "rcs_for_rotation", False),
            (mj.staging_controller, "autostage_pre_delay", 1.5),
            (mj.staging_controller, "autostage_post_delay", 1.5),
            (mj.staging_controller, "autostage_limit", 2),
            (mj.staging_controller, "fairing_max_dynamic_pressure", 1234),
            (mj.staging_controller, "fairing_min_altitude", 12345),
            (mj.staging_controller, "clamp_auto_stage_thrust_pct", 0.5),
            (mj.staging_controller, "fairing_max_aerothermal_flux", 1234),
            (mj.staging_controller, "hot_staging", True),
            (mj.staging_controller, "hot_staging_lead_time", 1.5),
            (mj.staging_controller, "autostaging_once", True),
            (mj.thrust_controller, "limit_dynamic_pressure", True),
            (mj.thrust_controller, "max_dynamic_pressure", 12345),
            (mj.thrust_controller, "limit_to_prevent_overheats", True),
            (mj.thrust_controller, "limit_acceleration", True),
            (mj.thrust_controller, "max_acceleration", 12),
            (mj.thrust_controller, "limit_throttle", True),
            (mj.thrust_controller, "max_throttle", 0.5),
            (mj.thrust_controller, "limiter_min_throttle", False),
            (mj.thrust_controller, "min_throttle", 0.25),
            (mj.thrust_controller, "limit_to_prevent_flameout", True),
            (mj.thrust_controller, "flameout_safety_pct", 12),
            (mj.thrust_controller, "manage_intakes", True),
            (mj.thrust_controller, "differential_throttle", True),
            (mj.thrust_controller, "electric_throttle", True),
            (mj.thrust_controller, "electric_throttle_lo", 0.25),
            (mj.thrust_controller, "electric_throttle_hi", 0.5),
            (planner.operation_apoapsis, "new_apoapsis", 123000),
            (planner.operation_course_correction, "course_correct_final_pe_a", 123000),
            (planner.operation_course_correction, "intercept_distance", 123),
            (planner.operation_ellipticize, "new_apoapsis", 123000),
            (planner.operation_ellipticize, "new_periapsis", 98000),
            (planner.operation_inclination, "new_inclination", 12),
            (planner.operation_interplanetary_transfer, "wait_for_phase_angle", False),
            (planner.operation_lambert, "intercept_interval", 1234),
            (planner.operation_lan, "new_lan", 12),
            (planner.operation_longitude, "new_surface_longitude", 12),
            (planner.operation_moon_return, "moon_return_altitude", 123000),
            (planner.operation_periapsis, "new_periapsis", 98000),
            (planner.operation_resonant_orbit, "resonance_numerator", 3),
            (planner.operation_resonant_orbit, "resonance_denominator", 4),
            (planner.operation_semi_major, "new_semi_major_axis", 1234000),
            (planner.operation_transfer, "intercept_only", True),
            (planner.operation_transfer, "simple_transfer", True),
            (planner.operation_apoapsis.time_selector, "lead_time", 123),
            (planner.operation_apoapsis.time_selector, "circularize_altitude", 123000),
        ]

    def test_settings(self):
        for obj, name, value in self.settings():
            with self.subTest(cls=type(obj).__name__, member=name):
                old = getattr(obj, name)
                self.assertNotEqual(value, old)
                setattr(obj, name, value)
                if isinstance(value, float):
                    self.assertAlmostEqual(value, getattr(obj, name), places=4)
                else:
                    self.assertEqual(value, getattr(obj, name))
                setattr(obj, name, old)
                self.assertEqual(old, getattr(obj, name))

    def test_read_only(self):
        mj = self.mj
        ascent = mj.ascent_autopilot
        classic = ascent.ascent_path_classic
        self.assertEqual("Off", ascent.status)
        self.assertFalse(ascent.enabled)
        self.assertEqual(mj.AscentLaunchMode.normal, ascent.launch_mode)
        self.assertGreater(classic.auto_turn_start_altitude, 0)
        self.assertGreater(classic.auto_turn_start_velocity, 0)
        self.assertGreater(classic.auto_turn_end_altitude, 0)
        self.assertEqual("Off", mj.landing_autopilot.status)
        self.assertIsInstance(mj.docking_autopilot.status, str)
        self.assertIsInstance(mj.rendezvous_autopilot.status, str)
        self.assertGreater(mj.docking_autopilot.safe_distance, 0)
        self.assertGreater(mj.docking_autopilot.target_size, 0)
        self.assertFalse(mj.node_executor.enabled)
        self.assertEqual(mj.TranslatronMode.off, mj.translatron.mode)
        self.assertEqual(mj.SmartRCSMode.off, mj.smart_rcs.mode)
        self.assertEqual(mj.SmartASSAutopilotMode.off, mj.smart_ass.autopilot_mode)
        self.assertIsInstance(
            mj.thrust_controller.differential_throttle_status,
            mj.DifferentialThrottleStatus,
        )
        self.assertEqual(
            mj.TimeReference.computed,
            mj.maneuver_planner.operation_transfer.time_selector.time_reference,
        )

    def test_same_object(self):
        mj = self.mj
        self.assertEqual(mj.thrust_controller, mj.ascent_autopilot.thrust_controller)
        self.assertEqual(mj.staging_controller, mj.ascent_autopilot.staging_controller)
        self.assertEqual(mj.rcs_controller, mj.smart_rcs.rcs_controller)

    def test_ascent_path_index(self):
        ascent = self.mj.ascent_autopilot
        self.assertEqual(0, ascent.ascent_path_index)
        ascent.ascent_path_index = 2
        self.assertEqual(2, ascent.ascent_path_index)
        ascent.ascent_path_index = 0
        self.assertEqual(0, ascent.ascent_path_index)
        for index in (1, 3, -1):
            with self.assertRaises(ValueError):
                ascent.ascent_path_index = index
        self.assertEqual(0, ascent.ascent_path_index)

    def test_ascent_enable(self):
        ascent = self.mj.ascent_autopilot
        ascent.enabled = True
        try:
            self.assertTrue(ascent.enabled)
            self.wait_until(lambda: ascent.status != "Off")
        finally:
            ascent.enabled = False
        self.assertFalse(ascent.enabled)

    def test_ascent_disable_after_path_change(self):
        ascent = self.mj.ascent_autopilot
        ascent.enabled = True
        try:
            ascent.ascent_path_index = 2
            ascent.enabled = False
            ascent.ascent_path_index = 0
            self.assertFalse(ascent.enabled)
        finally:
            ascent.enabled = False
            ascent.ascent_path_index = 0

    def test_timed_launch(self):
        ascent = self.mj.ascent_autopilot
        modes = self.mj.AscentLaunchMode
        self.sc.target_body = self.sc.bodies["Mun"]
        try:
            target = self.mj.target_controller
            self.wait_until(lambda: target.normal_target_exists)
            self.assertAlmostEqual(
                self.sc.bodies["Mun"].orbit.semi_major_axis,
                target.target_orbit.semi_major_axis,
                delta=1,
            )

            ascent.launch_to_target_plane()
            self.assertEqual(modes.target_plane, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)

            ascent.launch_to_rendezvous()
            self.assertEqual(modes.rendezvous, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)

            ascent.ascent_path_index = 2
            with self.assertRaises(RuntimeError):
                ascent.launch_to_rendezvous()
        finally:
            ascent.ascent_path_index = 0
            self.sc.clear_target()

    def test_position_target(self):
        target = self.mj.target_controller
        target.set_position_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            self.assertNotEqual("", target.get_position_target_string())
            self.assertEqual(3, len(target.get_position_target_position()))
        finally:
            self.sc.clear_target()

    def test_invalid_modes(self):
        mj = self.mj
        with self.assertRaises(mj.MJServiceException):
            mj.smart_rcs.mode = mj.SmartRCSMode.zero_velocity
        with self.assertRaises(mj.MJServiceException):
            mj.translatron.mode = mj.TranslatronMode.direct
        with self.assertRaises(mj.MJServiceException):
            mj.smart_ass.autopilot_mode = mj.SmartASSAutopilotMode.automatic
        time_selector = mj.maneuver_planner.operation_apoapsis.time_selector
        with self.assertRaises(mj.OperationException):
            time_selector.time_reference = mj.TimeReference.closest_approach

    def test_time_reference(self):
        time_selector = self.mj.maneuver_planner.operation_apoapsis.time_selector
        old = time_selector.time_reference
        try:
            for reference in ("apoapsis", "x_from_now", "eq_ascending"):
                value = getattr(self.mj.TimeReference, reference)
                time_selector.time_reference = value
                self.assertEqual(value, time_selector.time_reference)
        finally:
            time_selector.time_reference = old

    def test_translatron(self):
        translatron = self.mj.translatron
        translatron.mode = self.mj.TranslatronMode.keep_vertical
        try:
            self.assertEqual(self.mj.TranslatronMode.keep_vertical, translatron.mode)
            translatron.translation_speed = 0
            self.assertEqual(0, translatron.translation_speed)
        finally:
            translatron.mode = self.mj.TranslatronMode.off
        self.assertEqual(self.mj.TranslatronMode.off, translatron.mode)

    def test_solar_panels(self):
        controller = self.mj.solar_panel_controller
        self.assertTrue(controller.all_retracted())
        controller.extend_all()
        self.wait_until(lambda: not controller.all_retracted())
        controller.retract_all()
        self.wait_until(controller.all_retracted, timeout=60)


class TestMechJebInOrbit(MechJebTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.wait_until(lambda: cls.mj.ready)
        # Activate the engine, for the node executor
        cls.vessel.control.activate_next_stage()
        cls.planner = cls.mj.maneuver_planner

    def tearDown(self):
        self.vessel.control.remove_nodes()

    def test_apoapsis(self):
        operation = self.planner.operation_apoapsis
        operation.new_apoapsis = 200000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        operation.time_selector.lead_time = 60
        ut = self.sc.ut
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertAlmostEqual(ut + 60, nodes[0].ut, delta=5)
        self.assertAlmostEqual(200000, nodes[0].orbit.apoapsis_altitude, delta=1000)
        self.assertEqual("", operation.error_message)

    def test_ellipticize(self):
        operation = self.planner.operation_ellipticize
        operation.new_apoapsis = 300000
        operation.new_periapsis = 90000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertAlmostEqual(300000, nodes[0].orbit.apoapsis_altitude, delta=1000)
        self.assertAlmostEqual(90000, nodes[0].orbit.periapsis_altitude, delta=1000)

    def test_inclination(self):
        operation = self.planner.operation_inclination
        operation.new_inclination = 10
        operation.time_selector.time_reference = self.mj.TimeReference.eq_nearest_ad
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertAlmostEqual(
            math.radians(10), nodes[0].orbit.inclination, delta=math.radians(0.1)
        )

    def test_operation_exception(self):
        with self.assertRaises(self.mj.OperationException):
            self.planner.operation_kill_rel_vel.make_nodes()
        self.assertEqual(0, len(self.vessel.control.nodes))

    def test_execute_node(self):
        operation = self.planner.operation_apoapsis
        operation.new_apoapsis = 120000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        operation.time_selector.lead_time = 30
        operation.make_nodes()
        executor = self.mj.node_executor
        executor.execute_one_node()
        self.assertTrue(executor.enabled)
        self.wait_while(lambda: executor.enabled, timeout=300)
        self.assertEqual(0, len(self.vessel.control.nodes))
        self.assertAlmostEqual(120000, self.vessel.orbit.apoapsis_altitude, delta=2000)
        self.set_circular_orbit("Kerbin", 100000)

    def test_execute_node_abort(self):
        operation = self.planner.operation_apoapsis
        operation.new_apoapsis = 120000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        operation.time_selector.lead_time = 600
        operation.make_nodes()
        executor = self.mj.node_executor
        executor.autowarp = False
        try:
            executor.execute_one_node()
            self.assertTrue(executor.enabled)
            executor.abort()
            self.assertFalse(executor.enabled)
        finally:
            executor.autowarp = True

    def test_smart_ass(self):
        smart_ass = self.mj.smart_ass
        smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.normal_plus
        smart_ass.update(False)
        try:
            frame = self.vessel.orbital_reference_frame
            self.wait_until(
                lambda: (
                    dot(self.vessel.direction(frame), (0, 0, 1))
                    > math.cos(math.radians(5))
                ),
                timeout=60,
            )
            self.assertAlmostEqual(1, norm(self.vessel.direction(frame)), places=3)
        finally:
            smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.off
            smart_ass.update(False)


class TestMechJebEllipticalOrbit(MechJebTestCase):
    """Maneuver cases from KRPC.MechJeb's own test suite."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        # A 100 km by 200 km orbit, inclined to the Mun's
        radius = cls.sc.bodies["Kerbin"].equatorial_radius
        semi_major_axis = radius + 150000
        cls.set_orbit(
            "Kerbin", semi_major_axis, 50000 / semi_major_axis, 5, 0, 0, 0, cls.sc.ut
        )
        cls.wait_until(lambda: cls.mj.ready)
        cls.planner = cls.mj.maneuver_planner

    def tearDown(self):
        self.vessel.control.remove_nodes()

    def circularize(self, reference):
        operation = self.planner.operation_circularize
        operation.time_selector.time_reference = getattr(
            self.mj.TimeReference, reference
        )
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertEqual("", operation.error_message)
        orbit = nodes[0].orbit
        self.assertLess(orbit.eccentricity, 0.001)
        self.assertAlmostEqual(
            self.vessel.orbit.inclination, orbit.inclination, delta=math.radians(0.01)
        )
        return nodes[0]

    def test_circularize_at_apoapsis(self):
        node = self.circularize("apoapsis")
        apoapsis = self.vessel.orbit.apoapsis_altitude
        self.assertAlmostEqual(apoapsis, node.orbit.periapsis_altitude, delta=1000)

    def test_circularize_at_periapsis(self):
        node = self.circularize("periapsis")
        periapsis = self.vessel.orbit.periapsis_altitude
        self.assertAlmostEqual(periapsis, node.orbit.apoapsis_altitude, delta=1000)

    def test_circularize_at_altitude(self):
        for altitude in (115000, 150000, 180000):
            with self.subTest(altitude=altitude):
                time_selector = self.planner.operation_circularize.time_selector
                time_selector.circularize_altitude = altitude
                node = self.circularize("altitude")
                self.assertAlmostEqual(
                    altitude, node.orbit.periapsis_altitude, delta=1000
                )
                self.assertAlmostEqual(
                    altitude, node.orbit.apoapsis_altitude, delta=1000
                )
                self.vessel.control.remove_nodes()

    def test_circularize_after_time(self):
        time_selector = self.planner.operation_circularize.time_selector
        time_selector.lead_time = 314
        ut = self.sc.ut
        node = self.circularize("x_from_now")
        self.assertAlmostEqual(ut + 314, node.ut, delta=5)

    def test_circularize_outside_orbit(self):
        operation = self.planner.operation_circularize
        operation.time_selector.time_reference = self.mj.TimeReference.altitude
        for altitude in (80000, 240000):
            with self.subTest(altitude=altitude):
                operation.time_selector.circularize_altitude = altitude
                with self.assertRaises(self.mj.OperationException):
                    operation.make_nodes()
        self.assertEqual(0, len(self.vessel.control.nodes))

    def test_periapsis(self):
        operation = self.planner.operation_periapsis
        operation.new_periapsis = 80000
        operation.time_selector.time_reference = self.mj.TimeReference.apoapsis
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertAlmostEqual(80000, nodes[0].orbit.periapsis_altitude, delta=1000)
        apoapsis = self.vessel.orbit.apoapsis_altitude
        self.assertAlmostEqual(apoapsis, nodes[0].orbit.apoapsis_altitude, delta=1000)

    def test_plane(self):
        mun = self.sc.bodies["Mun"]
        self.sc.target_body = mun
        try:
            self.wait_until(lambda: self.mj.target_controller.normal_target_exists)
            operation = self.planner.operation_plane
            for reference in (
                "rel_highest_ad",
                "rel_nearest_ad",
                "rel_ascending",
                "rel_descending",
            ):
                with self.subTest(reference=reference):
                    time_selector = operation.time_selector
                    time_selector.time_reference = getattr(
                        self.mj.TimeReference, reference
                    )
                    nodes = operation.make_nodes()
                    self.assertEqual(1, len(nodes))
                    self.assertAlmostEqual(
                        mun.orbit.inclination,
                        nodes[0].orbit.inclination,
                        delta=math.radians(0.1),
                    )
                    self.vessel.control.remove_nodes()
        finally:
            self.sc.clear_target()

    def test_plane_without_target(self):
        with self.assertRaises(self.mj.OperationException):
            self.planner.operation_plane.make_nodes()

    def test_plane_outside_sphere_of_influence(self):
        self.sc.target_body = self.sc.bodies["Duna"]
        try:
            self.wait_until(lambda: self.mj.target_controller.normal_target_exists)
            with self.assertRaises(self.mj.OperationException):
                self.planner.operation_plane.make_nodes()
        finally:
            self.sc.clear_target()


class TestMechJebNotInstalled(TestCase):
    def test_not_installed(self):
        conn = self.connect()
        with self.assertRaisesRegex(
            RuntimeError, "MechJeb is not available: not installed"
        ):
            _ = conn.mech_jeb.smart_ass


if __name__ == "__main__":
    unittest.main()
