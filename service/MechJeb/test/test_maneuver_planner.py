import math
import unittest

from krpctools.game.geometry import norm
from service.MechJeb.test.mechjeb_test_case import (
    CRAFT,
    MechJebInOrbitTestCase,
    MechJebTestCase,
)


class TestManeuverPlanner(MechJebTestCase):
    def test_settings(self):
        planner = self.mj.maneuver_planner
        self.assert_settings(
            [
                (planner.operation_apoapsis, "new_apoapsis", 123000),
                (
                    planner.operation_course_correction,
                    "final_periapsis_altitude",
                    123000,
                ),
                (planner.operation_course_correction, "intercept_distance", 123),
                (planner.operation_eccentricity, "new_eccentricity", 0.5),
                (planner.operation_ellipticize, "new_apoapsis", 123000),
                (planner.operation_ellipticize, "new_periapsis", 98000),
                (planner.operation_inclination, "new_inclination", 12),
                (
                    planner.operation_interplanetary_transfer,
                    "wait_for_phase_angle",
                    False,
                ),
                (planner.operation_lambert, "intercept_interval", 1234),
                (planner.operation_lan, "new_lan", 12),
                (planner.operation_longitude, "new_surface_longitude", 12),
                (planner.operation_moon_return, "moon_return_altitude", 123000),
                (planner.operation_periapsis, "new_periapsis", 98000),
                (planner.operation_resonant_orbit, "resonance_numerator", 3),
                (planner.operation_resonant_orbit, "resonance_denominator", 4),
                (planner.operation_semi_major, "new_semi_major_axis", 1234000),
                (planner.operation_transfer, "capture", False),
                (planner.operation_transfer, "plan_capture", False),
                (planner.operation_transfer, "rendezvous", False),
                (planner.operation_transfer, "lag_time", 12),
                (planner.operation_transfer, "coplanar", True),
                (planner, "autowarp", False),
                (planner, "lead_time", 12),
                (planner.operation_apoapsis.time_selector, "lead_time", 123),
                (
                    planner.operation_apoapsis.time_selector,
                    "circularize_altitude",
                    123000,
                ),
            ]
        )

    def test_read_only(self):
        self.assertFalse(self.mj.maneuver_planner.executing_node)

    def test_default_time_reference(self):
        self.assertEqual(
            self.mj.TimeReference.computed,
            self.mj.maneuver_planner.operation_transfer.time_selector.time_reference,
        )

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

    def test_invalid_time_reference(self):
        time_selector = self.mj.maneuver_planner.operation_apoapsis.time_selector
        with self.assertRaises(self.mj.OperationException):
            time_selector.time_reference = self.mj.TimeReference.closest_approach


class TestManeuverPlannerInOrbit(MechJebInOrbitTestCase):
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

    def test_eccentricity(self):
        operation = self.planner.operation_eccentricity
        operation.new_eccentricity = 0.1
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        nodes = operation.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertAlmostEqual(0.1, nodes[0].orbit.eccentricity, delta=0.001)

    def test_transfer_without_capture(self):
        operation = self.planner.operation_transfer
        mun = self.sc.bodies["Mun"]
        self.sc.target_body = mun
        try:
            self.wait_until(lambda: self.mj.target_controller.normal_target_exists)
            operation.time_selector.time_reference = self.mj.TimeReference.computed
            operation.capture = False
            nodes = operation.make_nodes()
            self.assertEqual("", operation.error_message)
            self.assertEqual(1, len(nodes))
            self.assertEqual(mun, nodes[0].orbit.next_orbit.body)
        finally:
            operation.capture = True
            self.sc.clear_target()

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
        operation = self.planner.operation_kill_rel_vel
        with self.assertRaises(self.mj.OperationException) as context:
            operation.make_nodes()
        self.assertNotEqual("", operation.error_message)
        self.assertIn(operation.error_message, str(context.exception))
        self.assertEqual(0, len(self.vessel.control.nodes))

    def test_error_message_cleared(self):
        operation = self.planner.operation_semi_major
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        # Larger than half of Kerbin's sphere of influence
        operation.new_semi_major_axis = 45000000
        operation.make_nodes()
        self.assertNotEqual("", operation.error_message)
        self.vessel.control.remove_nodes()
        operation.new_semi_major_axis = 1000000
        operation.make_nodes()
        self.assertEqual("", operation.error_message)

    def test_after_last_node(self):
        apoapsis = self.planner.operation_apoapsis
        apoapsis.new_apoapsis = 200000
        apoapsis.time_selector.time_reference = self.mj.TimeReference.x_from_now
        apoapsis.time_selector.lead_time = 60
        first = apoapsis.make_nodes()[0]
        circularize = self.planner.operation_circularize
        circularize.time_selector.time_reference = self.mj.TimeReference.apoapsis
        nodes = circularize.make_nodes()
        self.assertEqual(1, len(nodes))
        self.assertGreater(nodes[0].ut, first.ut + 60)
        orbit = nodes[0].orbit
        self.assertLess(orbit.eccentricity, 0.001)
        self.assertAlmostEqual(200000, orbit.periapsis_altitude, delta=1000)


class TestNodeExecution(MechJebInOrbitTestCase):
    def make_node(self, lead_time):
        operation = self.planner.operation_apoapsis
        operation.new_apoapsis = 120000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        operation.time_selector.lead_time = lead_time
        operation.make_nodes()

    def test_execute_next_node(self):
        self.make_node(30)
        planner = self.planner
        planner.execute_next_node()
        self.assertTrue(planner.executing_node)
        self.wait_while(lambda: planner.executing_node, timeout=300)
        self.assertEqual(0, len(self.vessel.control.nodes))
        self.assertAlmostEqual(120000, self.vessel.orbit.apoapsis_altitude, delta=2000)
        self.set_circular_orbit("Kerbin", 100000)

    def test_abort(self):
        self.make_node(600)
        planner = self.planner
        planner.autowarp = False
        try:
            planner.execute_next_node()
            self.assertTrue(planner.executing_node)
            planner.abort_node_execution()
            self.assertFalse(planner.executing_node)
        finally:
            planner.autowarp = True

    def test_without_node(self):
        planner = self.planner
        for execute in (planner.execute_next_node, planner.execute_all_nodes):
            with self.subTest(method=execute.__name__):
                with self.assertRaisesRegex(RuntimeError, "no maneuver node"):
                    execute()
                self.assertFalse(planner.executing_node)


class TestManeuverPlannerTransferToVessel(MechJebTestCase):
    """A vessel with MechJeb in a 100 km circular orbit of Kerbin, targeting a vessel in a
    300 km circular orbit."""

    @classmethod
    def setUpClass(cls):
        cls.launch("Basic", directory=CRAFT)
        cls.set_circular_orbit("Kerbin", 300000)
        cls.vessel = cls.launch(
            "Basic", directory=CRAFT, save=None, keep_other_vessels=True
        )
        cls.set_circular_orbit("Kerbin", 100000)
        cls.conn = cls.connect()
        cls.mj = cls.conn.mech_jeb
        cls.sc = cls.conn.space_center
        cls.wait_until(lambda: cls.mj.ready)
        cls.vessel.control.activate_next_stage()
        cls.planner = cls.mj.maneuver_planner
        cls.target = next(v for v in cls.sc.vessels if v != cls.vessel)
        cls.sc.target_vessel = cls.target
        cls.wait_until(lambda: cls.mj.target_controller.normal_target_exists)

    def tearDown(self):
        self.vessel.control.remove_nodes()

    def test_transfer_with_capture(self):
        operation = self.planner.operation_transfer
        operation.time_selector.time_reference = self.mj.TimeReference.computed
        self.assertTrue(operation.capture)
        self.assertTrue(operation.rendezvous)
        operation.plan_capture = True
        nodes = operation.make_nodes()
        self.assertEqual("", operation.error_message)
        self.assertEqual(2, len(nodes))
        self.assertGreater(nodes[1].ut, nodes[0].ut)
        # The vessel meets the target at the capture node
        frame = self.sc.bodies["Kerbin"].non_rotating_reference_frame
        ut = nodes[1].ut
        position = nodes[0].orbit.position_at(ut, frame)
        target_position = self.target.orbit.position_at(ut, frame)
        self.assertLess(norm([a - b for a, b in zip(position, target_position)]), 1000)
        orbit = nodes[1].orbit
        self.assertAlmostEqual(300000, orbit.periapsis_altitude, delta=5000)
        self.assertAlmostEqual(300000, orbit.apoapsis_altitude, delta=5000)


class TestManeuverPlannerEllipticalOrbit(MechJebTestCase):
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


if __name__ == "__main__":
    unittest.main()
