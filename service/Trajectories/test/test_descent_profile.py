import unittest

from service.Trajectories.test.trajectories_testcase import (
    KerbinOrbitCase,
)


class TestDescentProfile(KerbinOrbitCase):
    def nodes(self):
        profile = self.traj.descent_profile
        return [
            profile.entry,
            profile.high_altitude,
            profile.low_altitude,
            profile.final_approach,
        ]

    def test_descent_profile(self):
        profile = self.traj.descent_profile
        self.assertEqual(profile, self.traj.descent_profile)
        self.assertEqual(profile.entry, self.traj.descent_profile.entry)
        nodes = self.nodes()
        self.assertEqual(4, len(set(nodes)))

    def test_default_descent_profile(self):
        for node in self.nodes():
            self.assertAlmostEqual(180, node.angle)
            self.assertEqual(self.traj.DescentProfileMode.angle_of_attack, node.mode)

    def test_node_angle(self):
        for node in self.nodes():
            for angle in (0, 30, -30, 90, -90, 120, -120, 180):
                node.angle = angle
                self.assertAlmostEqual(angle, node.angle, places=5)

    def test_node_angle_wraps(self):
        node = self.traj.descent_profile.high_altitude
        node.angle = 270
        self.assertAlmostEqual(-90, node.angle, places=5)
        node.angle = -180
        self.assertAlmostEqual(180, node.angle, places=5)
        node.angle = 400
        self.assertAlmostEqual(40, node.angle, places=5)

    def test_node_angle_is_per_node(self):
        nodes = self.nodes()
        for i, node in enumerate(nodes):
            node.angle = 10 * (i + 1)
        for i, node in enumerate(nodes):
            self.assertAlmostEqual(10 * (i + 1), node.angle, places=5)

    def test_node_mode(self):
        mode = self.traj.DescentProfileMode
        for node in self.nodes():
            node.mode = mode.horizon
            self.assertEqual(mode.horizon, node.mode)
            node.mode = mode.angle_of_attack
            self.assertEqual(mode.angle_of_attack, node.mode)

    def test_node_mode_keeps_angle(self):
        node = self.traj.descent_profile.low_altitude
        node.angle = 25
        node.mode = self.traj.DescentProfileMode.horizon
        self.assertAlmostEqual(25, node.angle, places=5)

    def test_reset(self):
        mode = self.traj.DescentProfileMode
        for node in self.nodes():
            node.angle = 45
            node.mode = mode.horizon
        self.traj.DescentProfile.reset(0)
        for node in self.nodes():
            self.assertAlmostEqual(0, node.angle, places=5)
            self.assertEqual(mode.angle_of_attack, node.mode)
        self.traj.DescentProfile.reset(30)
        for node in self.nodes():
            self.assertAlmostEqual(30, node.angle, places=5)
        self.traj.DescentProfile.reset(-150)
        for node in self.nodes():
            self.assertAlmostEqual(-150, node.angle, places=5)
        self.traj.DescentProfile.reset()
        for node in self.nodes():
            self.assertAlmostEqual(180, node.angle, places=5)

    def test_save_survives_quickload(self):
        node = self.traj.descent_profile.entry
        node.angle = 30
        node.mode = self.traj.DescentProfileMode.horizon
        self.traj.DescentProfile.save()
        self.sc.quicksave()
        node.angle = 60
        node.mode = self.traj.DescentProfileMode.angle_of_attack
        self.quickload()
        self.wait_until(
            lambda: abs(node.angle - 30) < 1e-5, message="the saved angle to load"
        )
        self.assertEqual(self.traj.DescentProfileMode.horizon, node.mode)

    def test_unsaved_profile_does_not_survive_quickload(self):
        # Reset saves the profile to the vessel
        self.traj.DescentProfile.reset()
        node = self.traj.descent_profile.entry
        node.angle = 30
        self.sc.quicksave()
        self.quickload()
        self.wait_until(
            lambda: abs(node.angle - 180) < 1e-5, message="the saved angle to load"
        )


if __name__ == "__main__":
    unittest.main()
