import unittest

from krpctools.game.geometry import distance, dot, norm, normalize
from service.Trajectories.test.trajectories_testcase import (
    KERBIN_RADIUS,
    MUN_RADIUS,
    KerbinDescentCase,
    KerbinOrbitCase,
    MunImpactCase,
)


class TestImpactInOrbit(KerbinOrbitCase):
    def test_no_impact(self):
        self.assertIsNone(self.traj.impact_patch())
        frame = self.kerbin.reference_frame
        self.assertIsNone(self.traj.impact_position(frame))
        self.assertIsNone(self.traj.impact_velocity(frame))


class TestImpactInDescent(KerbinDescentCase):
    def test_impact_position_of_the_trajectory(self):
        impact = self.some(self.traj.impact_patch())
        for frame in (
            self.kerbin.reference_frame,
            self.kerbin.non_rotating_reference_frame,
        ):
            self.assertAlmostEqual(
                self.some(impact.position(frame)),
                self.some(self.traj.impact_position(frame)),
                delta=1,
            )

    def test_impact_velocity_of_the_trajectory(self):
        impact = self.some(self.traj.impact_patch())
        for frame in (
            self.kerbin.reference_frame,
            self.kerbin.non_rotating_reference_frame,
        ):
            self.assertAlmostEqual(
                self.some(impact.velocity(frame)),
                self.some(self.traj.impact_velocity(frame)),
                delta=0.1,
            )

    def test_impact_position(self):
        impact = self.some(self.traj.impact_patch())
        frame = self.kerbin.reference_frame
        position = self.some(impact.position(frame))
        latitude = self.kerbin.latitude_at_position(position, frame)
        longitude = self.kerbin.longitude_at_position(position, frame)
        ground = max(0, self.kerbin.surface_height(latitude, longitude))
        self.assertAlmostEqual(ground, norm(position) - KERBIN_RADIUS, delta=200)
        # An equatorial orbit impacts on the equator
        self.assertAlmostEqual(0, latitude, delta=0.5)

    def test_impact_position_rotates_with_the_body(self):
        impact = self.some(self.traj.impact_patch())
        rotating = self.kerbin.reference_frame
        inertial = self.kerbin.non_rotating_reference_frame
        ut = self.sc.ut
        rotating_position = self.some(impact.position(rotating))
        inertial_position = self.some(impact.position(inertial))
        self.assertAlmostEqual(
            norm(inertial_position), norm(rotating_position), delta=1
        )
        self.assertDegreesAlmostEqual(
            self.expected_longitude(inertial_position, impact.end_ut - ut),
            self.kerbin.longitude_at_position(rotating_position, rotating),
            delta=0.1,
        )
        self.assertAlmostEqual(
            self.kerbin.latitude_at_position(inertial_position, inertial),
            self.kerbin.latitude_at_position(rotating_position, rotating),
            delta=0.01,
        )

    def test_impact_position_in_a_frame_fixed_to_the_body(self):
        impact = self.some(self.traj.impact_patch())
        body_frame = self.kerbin.reference_frame
        frame = self.sc.ReferenceFrame.create_relative(
            body_frame, position=(1000, 0, 0)
        )
        expected = self.sc.transform_position(
            self.some(impact.position(body_frame)), body_frame, frame
        )
        self.assertAlmostEqual(expected, self.some(impact.position(frame)), delta=10)

    def test_impact_position_in_another_frame(self):
        impact = self.some(self.traj.impact_patch())
        inertial = self.kerbin.non_rotating_reference_frame
        frame = self.vessel.orbital_reference_frame
        with self.held_tick():
            expected = self.sc.transform_position(
                self.some(impact.position(inertial)), inertial, frame
            )
            position = self.some(impact.position(frame))
        self.assertAlmostEqual(expected, position, delta=10)

    def test_impact_velocity(self):
        impact = self.some(self.traj.impact_patch())
        inertial = self.kerbin.non_rotating_reference_frame
        position = self.some(impact.position(inertial))
        velocity = self.some(impact.velocity(inertial))
        speed = norm(velocity)
        self.assertGreater(speed, 0)
        # The capsule falls into the ground
        self.assertLess(dot(velocity, normalize(position)), 0)
        # The atmosphere slows it below the speed it enters with
        space = self.patches[0]
        self.assertLess(
            speed, self.some(space.orbital_trajectory).speed_at(space.end_ut)
        )

    def test_impact_velocity_in_rotating_frame(self):
        impact = self.some(self.traj.impact_patch())
        rotating = self.kerbin.reference_frame
        inertial = self.kerbin.non_rotating_reference_frame
        self.check_surface_velocity(
            self.kerbin,
            self.some(impact.position(inertial)),
            self.some(impact.velocity(inertial)),
            self.some(impact.velocity(rotating)),
        )

    def test_descent_profile_changes_impact(self):
        frame = self.kerbin.reference_frame
        retrograde = self.some(self.some(self.traj.impact_patch()).position(frame))
        self.traj.DescentProfile.reset(0)
        self.wait_for_trajectory(self.sc.ut)
        self.wait_until(
            lambda: self.traj.impact_patch() is not None, message="an impact patch"
        )
        # A different attitude changes the drag, and so where the capsule lands
        self.assertGreater(
            distance(
                retrograde,
                self.some(self.some(self.traj.impact_patch()).position(frame)),
            ),
            100,
        )


class TestImpactAtMun(MunImpactCase):
    def test_impact_position(self):
        impact = self.some(self.traj.impact_patch())
        frame = self.mun.non_rotating_reference_frame
        radius = self.vessel.orbit.radius_at(impact.end_ut)
        self.assertAlmostEqual(
            radius, norm(self.some(impact.position(frame))), delta=10
        )
        # The mod finds the impact in 100 steps along the orbit, so it is near the
        # ground, a step before it
        altitude = norm(self.some(impact.position(frame))) - MUN_RADIUS
        self.assertGreater(altitude, -1000)
        self.assertLess(altitude, 15000)

    def test_impact_position_rotates_with_the_body(self):
        impact = self.some(self.traj.impact_patch())
        rotating = self.mun.reference_frame
        inertial = self.mun.non_rotating_reference_frame
        ut = self.sc.ut
        rotating_position = self.some(impact.position(rotating))
        inertial_position = self.some(impact.position(inertial))
        self.assertAlmostEqual(
            norm(inertial_position), norm(rotating_position), delta=1
        )
        longitude = self.mun.longitude_at_position(inertial_position, inertial)
        expected = longitude - 360 * (impact.end_ut - ut) / self.mun.rotational_period
        self.assertDegreesAlmostEqual(
            expected,
            self.mun.longitude_at_position(rotating_position, rotating),
            delta=0.05,
        )

    def test_impact_velocity(self):
        impact = self.some(self.traj.impact_patch())
        frame = self.mun.non_rotating_reference_frame
        expected = self.vessel.orbit.speed_at(impact.end_ut)
        velocity = self.some(impact.velocity(frame))
        self.assertAlmostEqual(expected, norm(velocity), delta=1)
        self.assertLess(dot(velocity, self.some(impact.position(frame))), 0)

    def test_impact_velocity_in_rotating_frame(self):
        impact = self.some(self.traj.impact_patch())
        inertial = self.mun.non_rotating_reference_frame
        self.check_surface_velocity(
            self.mun,
            self.some(impact.position(inertial)),
            self.some(impact.velocity(inertial)),
            self.some(impact.velocity(self.mun.reference_frame)),
        )


if __name__ == "__main__":
    unittest.main()
