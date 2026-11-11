import math
import unittest

from service.Trajectories.test.trajectories_testcase import (
    KerbinOrbitCase,
)


class TestComputation(KerbinOrbitCase):
    def test_available(self):
        self.assertTrue(self.traj.available)

    def test_always_update(self):
        self.traj.always_update = False
        self.assertFalse(self.traj.always_update)
        self.traj.always_update = True
        self.assertTrue(self.traj.always_update)

    def test_trajectory_is_stale_without_always_update(self):
        self.traj.always_update = False
        self.wait(0.5)
        start_ut = self.start_ut()
        self.assertGreater(start_ut, -math.inf)
        self.wait(1)
        self.assertEqual(start_ut, self.start_ut())

    def test_update_trajectory(self):
        self.traj.always_update = False
        self.wait(0.5)
        start_ut = self.start_ut()

        # The mod spreads one computation across several frames
        def updated():
            self.traj.update_trajectory()
            return self.start_ut() > start_ut

        self.wait_until(updated, message="update_trajectory to compute a trajectory")

    def test_always_update_keeps_trajectory_current(self):
        start_ut = self.start_ut()
        self.wait_until(
            lambda: self.start_ut() > start_ut,
            message="a newer trajectory",
        )


if __name__ == "__main__":
    unittest.main()
