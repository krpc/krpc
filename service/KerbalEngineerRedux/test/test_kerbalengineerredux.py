import math
import os
import unittest

from krpctools.game.ksp import TestCase

CRAFT = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..",
    "..",
    "SpaceCenter",
    "test",
    "craft",
)


class KerbalEngineerReduxTestCase(TestCase):
    mods = ["KerbalEngineerRedux"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Staging", directory=CRAFT)
        cls.conn = cls.connect()
        cls.ker = cls.conn.kerbal_engineer_redux
        cls.sc = cls.conn.space_center


class TestKerbalEngineerRedux(KerbalEngineerReduxTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_circular_orbit("Kerbin", 100000)

    def test_stages(self):
        stages = self.ker.stages(self.vessel)
        self.assertEqual(list(range(len(stages))), [stage.number for stage in stages])
        current = stages[-1]
        self.assertAlmostEqual(self.vessel.mass, current.start_mass, delta=1)
        self.assertAlmostEqual(
            sum(stage.delta_v for stage in stages), current.total_delta_v, delta=1
        )
        for stage in stages:
            self.assertGreaterEqual(stage.delta_v, 0)
            self.assertGreaterEqual(stage.burn_time, 0)
        self.assertEqual(len(self.vessel.parts.all), current.start_part_count)
        self.assertEqual(
            current.start_part_count, sum(stage.dropped_part_count for stage in stages)
        )

    def test_stage_thrust(self):
        # The engines that are activated and still attached during the stage
        first = first_burning_stage(self.ker.stages(self.vessel))
        engines = [
            part.engine
            for part in self.vessel.parts.all
            if part.engine is not None
            and part.stage >= first.number
            and part.decouple_stage < first.number
        ]
        self.assertGreater(len(engines), 0)
        self.assertAlmostEqual(
            sum(engine.max_vacuum_thrust for engine in engines),
            first.available_thrust,
            delta=first.available_thrust * 0.01,
        )
        g = self.vessel.orbit.body.gravitational_parameter / self.vessel.orbit.radius**2
        self.assertAlmostEqual(
            first.available_thrust / (first.start_mass * g),
            first.available_twr,
            delta=first.available_twr * 0.01,
        )

    def test_burn_time(self):
        first = first_burning_stage(self.ker.stages(self.vessel))
        burn = self.ker.burn_time(self.vessel, first.delta_v)
        self.assertIsNotNone(burn)
        self.assertAlmostEqual(
            first.burn_time, burn.duration, delta=first.burn_time * 0.05
        )
        self.assertLess(burn.half_duration, burn.duration)
        self.assertGreater(burn.half_duration, burn.duration / 2)

    def test_burn_time_zero(self):
        burn = self.ker.burn_time(self.vessel, 0)
        self.assertEqual(0, burn.duration)
        self.assertEqual(0, burn.half_duration)

    def test_burn_time_too_much_delta_v(self):
        total = self.ker.stages(self.vessel)[-1].total_delta_v
        self.assertIsNone(self.ker.burn_time(self.vessel, total + 100))

    def test_no_impact(self):
        self.assertIsNone(self.ker.impact(self.vessel))
        self.assertIsNone(self.ker.suicide_burn(self.vessel))


class TestStaging(KerbalEngineerReduxTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.vessel.control.throttle = 0

    def check_stages(self, vessel):
        """The stages describe the vessel as it is now."""
        stages = self.ker.stages(vessel)
        self.assertEqual(len(vessel.parts.all), stages[-1].start_part_count)

    def test_staging_and_switching(self):
        self.check_stages(self.vessel)
        debris = []
        while not debris and self.vessel.control.current_stage > 0:
            debris = self.vessel.control.activate_next_stage()
            self.check_stages(self.vessel)
        self.assertGreater(len(debris), 0)
        self.sc.active_vessel = debris[0]
        self.check_stages(debris[0])
        self.sc.active_vessel = self.vessel
        self.check_stages(self.vessel)


class TestEditorStages(TestCase):
    mods = ["KerbalEngineerRedux"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Staging", directory=CRAFT)
        conn = cls.connect()
        cls.ker = conn.kerbal_engineer_redux
        cls.sc = conn.space_center
        cls.kerbin = cls.sc.bodies["Kerbin"]
        cls.mun = cls.sc.bodies["Mun"]

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_stages(self):
        stages = self.ker.editor_stages(self.kerbin)
        vessel = self.sc.editor.vessel
        self.assertEqual(list(range(len(stages))), [stage.number for stage in stages])
        first = stages[-1]
        # The editor vessel's mass counts its crew, at 94 kg a kerbal
        self.assertAlmostEqual(
            vessel.mass - vessel.crew_capacity * 94, first.start_mass, delta=1
        )
        # The mod prices parts by its own rules, a little apart from the game's
        self.assertAlmostEqual(vessel.cost, first.start_cost, delta=vessel.cost * 0.01)
        self.assertEqual(len(vessel.parts.all), first.start_part_count)
        self.assertEqual(
            first.start_part_count, sum(stage.dropped_part_count for stage in stages)
        )

    def test_atmosphere(self):
        sea_level = self.ker.editor_stages(self.kerbin)[-1]
        vacuum = self.ker.editor_stages(self.kerbin, altitude=80000)[-1]
        self.assertGreater(vacuum.total_delta_v, sea_level.total_delta_v)

    def test_gravity(self):
        # Neither body has an atmosphere at these altitudes, so only the gravity differs
        kerbin = first_burning_stage(
            self.ker.editor_stages(self.kerbin, altitude=80000)
        )
        mun = first_burning_stage(self.ker.editor_stages(self.mun))
        self.assertAlmostEqual(kerbin.available_thrust, mun.available_thrust, delta=1)
        self.assertAlmostEqual(
            kerbin.available_twr
            * self.kerbin.surface_gravity
            / self.mun.surface_gravity,
            mun.available_twr,
            delta=mun.available_twr * 0.01,
        )


class TestRendezvous(KerbalEngineerReduxTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_orbit("Kerbin", 800000, 0, 10, 0, 0, 0, 0)
        cls.mun = cls.sc.bodies["Mun"]

    def tearDown(self):
        self.sc.clear_target()

    def test_no_target(self):
        self.sc.clear_target()
        self.assertIsNone(self.ker.rendezvous(self.vessel))

    def test_target_parent_body(self):
        self.sc.target_body = self.sc.bodies["Kerbin"]
        self.assertIsNone(self.ker.rendezvous(self.vessel))

    def test_intercept_angle(self):
        self.sc.target_body = self.mun
        rendezvous = self.ker.rendezvous(self.vessel)
        self.assertIsNotNone(rendezvous)
        r1 = self.vessel.orbit.semi_major_axis
        r2 = self.mun.orbit.semi_major_axis
        expected = math.pi * (1 - ((r1 + r2) / (2 * r2)) ** 1.5)
        self.assertAlmostEqual(expected, rendezvous.intercept_angle, delta=0.01)

    def test_phase_angle(self):
        self.sc.target_body = self.mun
        mun_orbit = self.mun.orbit
        assert mun_orbit is not None
        rendezvous = self.ker.rendezvous(self.vessel)
        frame = self.sc.bodies["Kerbin"].non_rotating_reference_frame
        position = self.vessel.position(frame)
        velocity = self.vessel.velocity(frame)
        target = self.mun.position(frame)
        normal = cross(position, velocity)
        projected = sub(
            target, scale(normal, dot(target, normal) / dot(normal, normal))
        )
        angle = math.atan2(
            dot(cross(position, projected), normal) / norm(normal),
            dot(position, projected),
        )
        self.assertAlmostEqual(
            angle % (2 * math.pi), rendezvous.phase_angle, delta=0.01
        )
        # The phase drifts at the difference of the mean motions
        rate = 2 * math.pi / self.vessel.orbit.period - 2 * math.pi / mun_orbit.period
        lead = (rendezvous.phase_angle - rendezvous.intercept_angle) % (2 * math.pi)
        if lead > math.radians(340):
            lead -= 2 * math.pi
        self.assertAlmostEqual(
            lead / rate,
            rendezvous.time_to_intercept_angle,
            delta=abs(lead / rate) * 0.01 + 1,
        )

    def test_nodes(self):
        self.sc.target_body = self.mun
        rendezvous = self.ker.rendezvous(self.vessel)
        orbit = self.vessel.orbit
        mun_orbit = self.mun.orbit
        assert mun_orbit is not None
        ut = self.sc.ut
        period = orbit.period
        for anomaly, time in (
            (orbit.true_anomaly_at_an(mun_orbit), rendezvous.time_to_ascending_node),
            (orbit.true_anomaly_at_dn(mun_orbit), rendezvous.time_to_descending_node),
        ):
            expected = (orbit.ut_at_true_anomaly(anomaly) - ut) % period
            self.assertAlmostEqual(expected, time, delta=period * 0.005)
        self.assertAlmostEqual(
            math.pi,
            (rendezvous.angle_to_descending_node - rendezvous.angle_to_ascending_node)
            % (2 * math.pi),
            delta=0.01,
        )


class TestImpact(KerbalEngineerReduxTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        # An orbit of the Mun with its apoapsis 50 km up and its periapsis below the
        # surface, starting at apoapsis
        cls.body = cls.sc.bodies["Mun"]
        radius = cls.body.equatorial_radius
        apoapsis = radius + 50000
        periapsis = radius / 2
        cls.set_orbit(
            "Mun",
            (apoapsis + periapsis) / 2,
            (apoapsis - periapsis) / (apoapsis + periapsis),
            0,
            0,
            0,
            math.pi,
            cls.sc.ut,
        )

    def test_impact(self):
        impact = self.ker.impact(self.vessel)
        self.assertIsNotNone(impact)
        self.assertGreater(impact.time_to, 0)
        self.assertLess(impact.time_to, self.vessel.orbit.time_to_periapsis)
        self.assertAlmostEqual(self.sc.ut + impact.time_to, impact.ut, delta=1)
        self.assertAlmostEqual(0, impact.latitude, delta=1)
        self.assertAlmostEqual(
            self.body.surface_height(impact.latitude, impact.longitude),
            impact.elevation,
            delta=10,
        )
        self.assertNotEqual("", impact.biome)

    def test_suicide_burn(self):
        impact = self.ker.impact(self.vessel)
        burn = self.ker.suicide_burn(self.vessel)
        self.assertIsNotNone(burn)
        self.assertGreater(burn.time_to, 0)
        self.assertLess(burn.time_to, impact.time_to)
        self.assertAlmostEqual(self.sc.ut + burn.time_to, burn.ut, delta=1)
        self.assertGreater(burn.altitude, impact.elevation)
        self.assertGreater(burn.delta_v, 0)
        self.assertGreater(burn.duration, 0)
        self.assertGreater(burn.distance, 0)


def first_burning_stage(stages):
    """The next stage to have delta-v, the first one a burn uses."""
    return next(stage for stage in reversed(stages) if stage.delta_v > 0)


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def sub(a, b):
    return tuple(x - y for x, y in zip(a, b))


def scale(a, s):
    return tuple(x * s for x in a)


def norm(a):
    return math.sqrt(dot(a, a))


class TestKerbalEngineerReduxNotInstalled(TestCase):
    def test_not_installed(self):
        conn = self.connect()
        with self.assertRaisesRegex(
            RuntimeError, "KerbalEngineerRedux is not available: not installed"
        ):
            conn.kerbal_engineer_redux.editor_stages(conn.space_center.bodies["Kerbin"])


if __name__ == "__main__":
    unittest.main()
