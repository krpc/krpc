import math
import unittest

from krpctools.game.geometry import angle_between, dot, norm, normalize
from service.Trajectories.test.trajectories_testcase import (
    KerbinDescentCase,
    KerbinOrbitCase,
    MunImpactCase,
)


class TestDirectionsInOrbit(KerbinOrbitCase):
    def test_directions_without_target(self):
        frame = self.vessel.reference_frame
        self.assertIsNone(self.traj.planned_direction(frame))
        self.assertIsNone(self.traj.corrected_direction(frame))


class TestDirectionsInDescent(KerbinDescentCase):
    def set_target_at_impact(self):
        frame = self.kerbin.reference_frame
        self.traj.set_target(
            frame, self.some(self.some(self.traj.impact_patch()).position(frame))
        )
        # The navball overlay computes the directions once a frame, and they read
        # zero until it first does
        self.wait_until(
            lambda: self.is_unit(self.traj.planned_direction(frame)),
            message="the planned direction",
        )

    def is_unit(self, direction):
        return direction is not None and abs(norm(direction) - 1) < 0.001

    def wait_for_planned_direction(self, predicate, message):
        frame = self.kerbin.reference_frame

        def check():
            direction = self.traj.planned_direction(frame)
            return direction is not None and predicate(direction)

        self.wait_until(check, message=message, timeout=10)

    def test_directions_without_target(self):
        frame = self.kerbin.reference_frame
        self.assertIsNone(self.traj.planned_direction(frame))
        self.assertIsNone(self.traj.corrected_direction(frame))

    def test_planned_direction_retrograde(self):
        self.set_target_at_impact()
        # The overlay keeps the direction from the last target until it next updates
        self.wait_for_planned_direction(
            lambda d: angle_between(d, self.surface_velocity(self.kerbin)) > 179,
            "the planned direction to be retrograde",
        )

    def test_planned_direction_prograde(self):
        self.traj.DescentProfile.reset(0)
        self.set_target_at_impact()
        self.wait_for_planned_direction(
            lambda d: angle_between(d, self.surface_velocity(self.kerbin)) < 1,
            "the planned direction to be prograde",
        )

    def test_planned_direction_angle_of_attack(self):
        # Above the atmosphere the entry node sets the direction
        self.traj.descent_profile.entry.angle = 30
        self.set_target_at_impact()
        self.wait_for_planned_direction(
            lambda d: (
                abs(angle_between(d, self.surface_velocity(self.kerbin)) - 30) < 1
            ),
            "the planned direction to be 30 degrees from prograde",
        )
        direction = self.some(self.traj.planned_direction(self.kerbin.reference_frame))
        up = self.up(self.kerbin)
        # A positive angle of attack pitches the nose up
        self.assertGreater(
            dot(direction, up), dot(normalize(self.surface_velocity(self.kerbin)), up)
        )

    def test_planned_direction_other_nodes_have_no_effect(self):
        # Above the atmosphere only the entry node applies
        profile = self.traj.descent_profile
        profile.entry.angle = 30
        profile.high_altitude.angle = 0
        profile.low_altitude.angle = 0
        profile.final_approach.angle = 0
        self.set_target_at_impact()
        self.wait_for_planned_direction(
            lambda d: (
                abs(angle_between(d, self.surface_velocity(self.kerbin)) - 30) < 1
            ),
            "the planned direction to follow the entry node",
        )

    def test_planned_direction_horizon(self):
        node = self.traj.descent_profile.entry
        node.mode = self.traj.DescentProfileMode.horizon
        node.angle = 10
        self.set_target_at_impact()
        self.wait_for_planned_direction(
            lambda d: (
                abs(dot(d, self.up(self.kerbin)) - math.sin(math.radians(10))) < 0.01
            ),
            "the planned direction to be 10 degrees above the horizon",
        )

    def test_planned_direction_horizon_level(self):
        node = self.traj.descent_profile.entry
        node.mode = self.traj.DescentProfileMode.horizon
        node.angle = 0
        self.set_target_at_impact()
        self.wait_for_planned_direction(
            lambda d: abs(dot(d, self.up(self.kerbin))) < 0.01,
            "the planned direction to be level with the horizon",
        )

    def test_planned_direction_in_another_frame(self):
        self.set_target_at_impact()
        body_frame = self.kerbin.reference_frame
        frame = self.vessel.reference_frame
        with self.held_tick():
            expected = self.sc.transform_direction(
                self.some(self.traj.planned_direction(body_frame)), body_frame, frame
            )
            direction = self.some(self.traj.planned_direction(frame))
        self.assertAlmostEqual(0, angle_between(expected, direction), delta=1)

    def test_corrected_direction_on_target(self):
        self.set_target_at_impact()
        frame = self.kerbin.reference_frame
        planned = self.some(self.traj.planned_direction(frame))
        corrected = self.some(self.traj.corrected_direction(frame))
        self.assertAlmostEqual(1, norm(corrected), delta=0.001)
        self.assertLess(angle_between(planned, corrected), 3)

    def test_corrected_direction_off_target(self):
        frame = self.kerbin.reference_frame
        position = self.some(self.some(self.traj.impact_patch()).position(frame))
        latitude = self.kerbin.latitude_at_position(position, frame)
        longitude = self.kerbin.longitude_at_position(position, frame)
        # About 50 km east of the impact
        self.traj.set_target(
            frame, self.kerbin.surface_position(latitude, longitude + 5, frame)
        )
        self.wait_until(
            lambda: self.is_unit(self.traj.corrected_direction(frame)),
            message="the corrected direction",
        )
        planned = self.some(self.traj.planned_direction(frame))
        corrected = self.some(self.traj.corrected_direction(frame))
        self.assertAlmostEqual(1, norm(corrected), delta=0.001)
        self.assertGreater(angle_between(planned, corrected), 20)


class TestDirectionsAtMun(MunImpactCase):
    def test_final_approach_sets_direction_without_atmosphere(self):
        frame = self.mun.reference_frame
        self.traj.descent_profile.entry.angle = 0
        self.traj.descent_profile.final_approach.angle = 180
        self.traj.set_target(
            frame,
            self.some(self.some(self.traj.impact_patch()).position(frame)),
            self.mun,
        )

        def retrograde():
            direction = self.traj.planned_direction(frame)
            velocity = self.vessel.flight(frame).velocity
            return (
                direction is not None
                and abs(angle_between(direction, velocity) - 180) < 1
            )

        self.wait_until(retrograde, message="the planned direction to be retrograde")


if __name__ == "__main__":
    unittest.main()
