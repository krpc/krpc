import unittest

import krpctest


class TestGameScene(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.krpc = cls.connect().krpc
        cls.scenes = cls.connect().krpc.GameScene

    def set_scene(self, scene, timeout=120):
        self.krpc.game_scene = scene
        self.wait_until(
            lambda: self.krpc.game_scene == scene,
            timeout=timeout,
            message="game scene to become %s" % scene,
        )

    def ensure_space_center(self):
        if self.krpc.game_scene != self.scenes.space_center:
            self.set_scene(self.scenes.space_center)

    def test_editors(self):
        self.ensure_space_center()
        self.set_scene(self.scenes.editor_vab)
        self.set_scene(self.scenes.space_center)
        self.set_scene(self.scenes.editor_sph)
        self.set_scene(self.scenes.space_center)

    def test_astronaut_complex(self):
        # The astronaut complex is the only facility available in a sandbox
        # game, which is what the test save uses.
        self.ensure_space_center()
        self.set_scene(self.scenes.astronaut_complex, timeout=30)
        # setting the space center scene closes the open facility
        self.set_scene(self.scenes.space_center, timeout=30)

    def test_facility_unavailable_in_sandbox(self):
        # Mission control, R&D and administration are disabled in a sandbox
        # game; opening them must raise rather than wedge the game.
        self.ensure_space_center()
        for scene in (
            self.scenes.mission_control,
            self.scenes.research_and_development,
            self.scenes.administration,
        ):
            self.assertRaises(RuntimeError, setattr, self.krpc, "game_scene", scene)
        # the failed attempts left us in the space center
        self.assertEqual(self.scenes.space_center, self.krpc.game_scene)

    def test_facility_from_wrong_scene(self):
        self.ensure_space_center()
        self.set_scene(self.scenes.tracking_station)
        self.assertRaises(
            RuntimeError,
            setattr,
            self.krpc,
            "game_scene",
            self.scenes.astronaut_complex,
        )
        self.set_scene(self.scenes.space_center)

    def test_flight_resume(self):
        self.ensure_space_center()
        self.set_scene(self.scenes.flight)
        self.assertIsNotNone(self.connect().space_center.active_vessel)

    def test_mission_builder_raises(self):
        self.assertRaises(
            RuntimeError,
            setattr,
            self.krpc,
            "game_scene",
            self.scenes.mission_builder,
        )

    def test_set_to_current_scene_is_noop(self):
        self.ensure_space_center()
        self.krpc.game_scene = self.scenes.space_center
        self.wait(1)
        self.assertEqual(self.scenes.space_center, self.krpc.game_scene)

    def test_tracking_station(self):
        self.ensure_space_center()
        self.set_scene(self.scenes.tracking_station)
        self.set_scene(self.scenes.space_center)


if __name__ == "__main__":
    unittest.main()
