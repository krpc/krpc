import unittest

from krpctools.game.geometry import distance, norm
from service.Trajectories.test.trajectories_testcase import (
    KERBIN_RADIUS,
    MUN_IMPACT_ECC,
    MUN_IMPACT_SMA,
    KerbinDescentCase,
    KerbinOrbitCase,
    LandedCase,
    MunImpactCase,
)


class TestPatchesInOrbit(KerbinOrbitCase):
    def setUp(self):
        self.hold_patches()

    def circularize(self, altitude):
        self.set_circular_orbit("Kerbin", altitude)
        self.wait_for_trajectory(self.sc.ut)

    def test_patches(self):
        patches = self.traj.patches()
        self.assertGreater(len(patches), 0)
        ut = self.sc.ut
        self.assertLessEqual(patches[0].start_ut, ut)
        for patch in patches:
            self.assertEqual(self.kerbin, patch.body)
            self.assertFalse(patch.atmospheric)
            self.assertGreater(patch.end_ut, patch.start_ut)
            frame = self.kerbin.non_rotating_reference_frame
            self.assertIsNone(patch.position(frame))
            self.assertIsNone(patch.velocity(frame))
            self.assertIsNone(patch.atmospheric_trajectory(frame))

    def test_orbital_trajectory(self):
        patch = self.traj.patches()[0]
        orbit = self.some(patch.orbital_trajectory)
        self.assertEqual(self.kerbin, orbit.body)
        self.assertAlmostEqual(KERBIN_RADIUS + 200000, orbit.semi_major_axis, delta=100)
        self.assertAlmostEqual(0, orbit.eccentricity, delta=0.001)
        self.assertAlmostEqual(
            self.vessel.orbit.semi_major_axis, orbit.semi_major_axis, delta=10
        )
        # A circular orbit has no impact, so the patch lasts one period
        self.assertAlmostEqual(patch.start_ut + orbit.period, patch.end_ut, delta=1)

    def test_orbital_trajectory_reads_the_latest_trajectory(self):
        orbit = self.some(self.traj.patches()[0].orbital_trajectory)
        self.addCleanup(self.circularize, 200000)
        self.circularize(250000)
        self.assertEqual(orbit, self.traj.patches()[0].orbital_trajectory)
        self.assertAlmostEqual(KERBIN_RADIUS + 250000, orbit.semi_major_axis, delta=100)

    def test_patch_reads_the_latest_trajectory(self):
        patch = self.traj.patches()[0]
        start_ut = patch.start_ut
        self.wait_for_trajectory(start_ut)
        self.assertGreater(patch.start_ut, start_ut)


class TestPatchesInDescent(KerbinDescentCase):
    def test_patches(self):
        self.assertEqual(2, len(self.patches))
        space, atmosphere = self.patches
        self.assertEqual(self.kerbin, space.body)
        self.assertEqual(self.kerbin, atmosphere.body)
        self.assertFalse(space.atmospheric)
        self.assertTrue(atmosphere.atmospheric)
        self.assertLessEqual(space.start_ut, self.sc.ut)
        self.assertAlmostEqual(space.end_ut, atmosphere.start_ut, delta=0.1)
        self.assertGreater(atmosphere.end_ut, atmosphere.start_ut)

    def test_consecutive_patches(self):
        space, atmosphere = self.patches
        self.assertAlmostEqual(space.end_ut, atmosphere.start_ut, delta=0.1)

    def test_patch_equality(self):
        space, atmosphere = self.patches
        self.assertEqual(space, self.traj.patches()[0])
        self.assertNotEqual(space, atmosphere)

    def test_space_patch(self):
        space = self.patches[0]
        frame = self.kerbin.non_rotating_reference_frame
        self.assertIsNone(space.position(frame))
        self.assertIsNone(space.velocity(frame))
        self.assertIsNone(space.atmospheric_trajectory(frame))
        orbit = self.some(space.orbital_trajectory)
        self.assertEqual(self.kerbin, orbit.body)
        self.assertAlmostEqual(100000, orbit.apoapsis_altitude, delta=100)
        self.assertAlmostEqual(10000, orbit.periapsis_altitude, delta=100)
        # The space patch ends at the edge of the atmosphere
        self.assertAlmostEqual(
            KERBIN_RADIUS + self.kerbin.atmosphere_depth,
            orbit.radius_at(space.end_ut),
            delta=500,
        )

    def test_impact_patch(self):
        impact = self.some(self.traj.impact_patch())
        self.assertEqual(self.patches[-1], impact)
        self.assertTrue(impact.atmospheric)
        self.assertIsNone(impact.orbital_trajectory)
        self.assertGreater(impact.end_ut, self.sc.ut)

    def test_orbital_trajectory_once_the_patch_is_atmospheric(self):
        orbit = self.some(self.patches[0].orbital_trajectory)
        # From an apoapsis inside the atmosphere, the first patch is atmospheric
        sma = KERBIN_RADIUS + (50000 + 10000) / 2
        self.place_at_apoapsis("Kerbin", sma, (50000 - 10000) / (2 * sma))
        self.assertTrue(self.traj.patches()[0].atmospheric)
        with self.assertRaisesRegex(RuntimeError, "is atmospheric"):
            _ = orbit.semi_major_axis

    def test_patch_past_the_end(self):
        atmosphere = self.patches[1]
        self.set_circular_orbit("Kerbin", 200000)
        self.wait_for_trajectory(self.sc.ut)
        self.assertEqual(1, len(self.traj.patches()))
        frame = self.kerbin.reference_frame
        reads = {
            "start_ut": lambda: atmosphere.start_ut,
            "end_ut": lambda: atmosphere.end_ut,
            "body": lambda: atmosphere.body,
            "atmospheric": lambda: atmosphere.atmospheric,
            "position": lambda: atmosphere.position(frame),
            "velocity": lambda: atmosphere.velocity(frame),
            "atmospheric_trajectory": lambda: atmosphere.atmospheric_trajectory(frame),
            "orbital_trajectory": lambda: atmosphere.orbital_trajectory,
        }
        for name, read in reads.items():
            with self.subTest(name):
                with self.assertRaisesRegex(RuntimeError, "no patch at index 1"):
                    read()

    def test_atmospheric_trajectory(self):
        atmosphere = self.patches[1]
        frame = self.kerbin.non_rotating_reference_frame
        points = self.some(atmosphere.atmospheric_trajectory(frame))
        self.assertGreater(len(points), 1)
        times = [point[0] for point in points]
        self.assertEqual(sorted(times), times)
        for t in times:
            self.assertGreaterEqual(t, atmosphere.start_ut - 0.1)
            self.assertLessEqual(t, atmosphere.end_ut + 0.1)
        altitudes = [norm(point[1:]) - KERBIN_RADIUS for point in points]
        atmosphere_depth = self.kerbin.atmosphere_depth
        self.assertAlmostEqual(atmosphere_depth, altitudes[0], delta=2000)
        for altitude in altitudes:
            self.assertLessEqual(altitude, atmosphere_depth + 10)
            self.assertGreater(altitude, -500)

    def test_atmospheric_trajectory_ends_at_impact(self):
        atmosphere = self.patches[1]
        for frame in (
            self.kerbin.reference_frame,
            self.kerbin.non_rotating_reference_frame,
        ):
            points = self.some(atmosphere.atmospheric_trajectory(frame))
            # The mod stores a point every 10 seconds
            self.assertLess(
                distance(self.some(atmosphere.position(frame)), points[-1][1:]), 5000
            )

    def test_atmospheric_trajectory_rotates_with_the_body(self):
        atmosphere = self.patches[1]
        rotating = self.kerbin.reference_frame
        inertial = self.kerbin.non_rotating_reference_frame
        ut = self.sc.ut
        rotating_points = self.some(atmosphere.atmospheric_trajectory(rotating))
        inertial_points = self.some(atmosphere.atmospheric_trajectory(inertial))
        self.assertEqual(len(inertial_points), len(rotating_points))
        for inertial_point, rotating_point in zip(inertial_points, rotating_points):
            self.assertEqual(inertial_point[0], rotating_point[0])
            self.assertAlmostEqual(
                norm(inertial_point[1:]), norm(rotating_point[1:]), delta=1
            )
        inertial_point = inertial_points[0]
        rotating_point = rotating_points[0]
        self.assertDegreesAlmostEqual(
            self.expected_longitude(inertial_point[1:], inertial_point[0] - ut),
            self.kerbin.longitude_at_position(rotating_point[1:], rotating),
            delta=0.1,
        )

    def test_atmospheric_trajectory_in_another_frame(self):
        atmosphere = self.patches[1]
        inertial = self.kerbin.non_rotating_reference_frame
        frame = self.vessel.orbital_reference_frame
        with self.held_tick():
            inertial_points = self.some(atmosphere.atmospheric_trajectory(inertial))
            points = self.some(atmosphere.atmospheric_trajectory(frame))
            expected = [
                self.sc.transform_position(p[1:], inertial, frame)
                for p in inertial_points
            ]
        for expected_point, point in zip(expected, points):
            self.assertAlmostEqual(expected_point, point[1:], delta=10)


class TestPatchesAtMun(MunImpactCase):
    def test_patches(self):
        patches = self.traj.patches()
        self.assertEqual(1, len(patches))
        patch = patches[0]
        self.assertEqual(self.mun, patch.body)
        self.assertFalse(patch.atmospheric)
        self.assertIsNone(patch.atmospheric_trajectory(self.mun.reference_frame))
        self.assertLessEqual(patch.start_ut, self.sc.ut)
        self.assertGreater(patch.end_ut, self.sc.ut)

    def test_impact_patch(self):
        impact = self.some(self.traj.impact_patch())
        self.assertEqual(self.mun, impact.body)
        self.assertFalse(impact.atmospheric)
        self.assertEqual(self.traj.patches()[0], impact)

    def test_orbital_trajectory(self):
        orbit = self.some(self.some(self.traj.impact_patch()).orbital_trajectory)
        self.assertEqual(self.mun, orbit.body)
        self.assertAlmostEqual(MUN_IMPACT_SMA, orbit.semi_major_axis, delta=100)
        self.assertAlmostEqual(MUN_IMPACT_ECC, orbit.eccentricity, delta=0.001)


class TestPatchesLanded(LandedCase):
    def test_no_patches(self):
        self.wait_until(
            lambda: len(self.traj.patches()) == 0, message="the patches to clear"
        )
        self.assertIsNone(self.traj.impact_patch())
        frame = self.kerbin.reference_frame
        self.assertIsNone(self.traj.impact_position(frame))
        self.assertIsNone(self.traj.impact_velocity(frame))


if __name__ == "__main__":
    unittest.main()
