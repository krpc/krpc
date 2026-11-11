import unittest

from service.Trajectories.test.trajectories_testcase import (
    KerbinOrbitCase,
    MunImpactCase,
)


class TestTarget(KerbinOrbitCase):
    def test_no_target(self):
        self.assertIsNone(self.traj.target_body)
        self.assertIsNone(self.traj.target_position(self.kerbin.reference_frame))

    def test_set_target(self):
        frame = self.kerbin.reference_frame
        position = self.kerbin.surface_position(-0.0972, -74.5577, frame)
        self.traj.set_target(frame, position)
        self.assertEqual(self.kerbin, self.traj.target_body)
        self.assertAlmostEqual(
            position, self.some(self.traj.target_position(frame)), delta=0.01
        )

    def test_set_target_in_another_frame(self):
        frame = self.vessel.orbital_reference_frame
        body_frame = self.kerbin.reference_frame
        position = (1000, 2000, 3000)
        with self.held_tick():
            self.traj.set_target(frame, position)
            actual = self.some(self.traj.target_position(frame))
            expected = self.sc.transform_position(position, frame, body_frame)
            actual_in_body_frame = self.some(self.traj.target_position(body_frame))
        self.assertEqual(self.kerbin, self.traj.target_body)
        self.assertAlmostEqual(position, actual, delta=0.01)
        self.assertAlmostEqual(expected, actual_in_body_frame, delta=0.01)

    def test_set_target_on_another_body(self):
        frame = self.mun.reference_frame
        position = self.mun.surface_position(5, 10, frame)
        self.traj.set_target(frame, position, self.mun)
        self.assertEqual(self.mun, self.traj.target_body)
        self.assertAlmostEqual(
            position, self.some(self.traj.target_position(frame)), delta=0.01
        )
        expected = self.sc.transform_position(
            position, frame, self.kerbin.non_rotating_reference_frame
        )
        self.assertAlmostEqual(
            expected,
            self.some(
                self.traj.target_position(self.kerbin.non_rotating_reference_frame)
            ),
            delta=0.1,
        )

    def test_set_target_replaces_target(self):
        frame = self.kerbin.reference_frame
        self.traj.set_target(frame, self.mun.surface_position(0, 0, frame), self.mun)
        position = self.kerbin.surface_position(1, 2, frame)
        self.traj.set_target(frame, position)
        self.assertEqual(self.kerbin, self.traj.target_body)
        self.assertAlmostEqual(
            position, self.some(self.traj.target_position(frame)), delta=0.01
        )

    def test_clear_target(self):
        frame = self.kerbin.reference_frame
        self.traj.set_target(frame, self.kerbin.surface_position(0, 0, frame))
        self.traj.clear_target()
        self.assertIsNone(self.traj.target_body)
        self.assertIsNone(self.traj.target_position(frame))

    def test_target_survives_quickload(self):
        frame = self.kerbin.reference_frame
        position = self.kerbin.surface_position(-0.0972, -74.5577, frame)
        self.traj.set_target(frame, position)
        self.sc.quicksave()
        self.traj.clear_target()
        self.quickload()
        # The mod reads the vessel's saved target on the frame it attaches to it
        self.wait_until(
            lambda: self.traj.target_body == self.kerbin, message="the target to load"
        )
        self.assertAlmostEqual(
            position, self.some(self.traj.target_position(frame)), delta=1
        )


class TestTargetAtMun(MunImpactCase):
    def test_set_target_defaults_to_the_body_orbited(self):
        frame = self.mun.reference_frame
        position = self.mun.surface_position(0, 0, frame)
        self.traj.set_target(frame, position)
        self.assertEqual(self.mun, self.traj.target_body)
        self.assertAlmostEqual(
            position, self.some(self.traj.target_position(frame)), delta=0.01
        )


if __name__ == "__main__":
    unittest.main()
