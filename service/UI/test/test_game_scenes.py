import time
import unittest
import krpctest


class TestGameScenes(krpctest.TestCase):
    """The user interface is available in every game scene, not only in flight."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        conn = cls.connect()
        cls.krpc = conn.krpc
        cls.ui = conn.ui
        cls.scenes = conn.krpc.GameScene

    @classmethod
    def tearDownClass(cls):
        # Leave the game in the scene the rest of the tests expect, and wait for it to
        # be there rather than handing a half-changed scene to whatever runs next.
        cls.krpc.game_scene = cls.scenes.space_center
        deadline = time.time() + 120
        while cls.krpc.game_scene != cls.scenes.space_center:
            if time.time() > deadline:
                raise RuntimeError("Timed out returning to the space center")
            cls.wait()

    def set_scene(self, scene, timeout=120):
        self.krpc.game_scene = scene
        self.wait_until(
            lambda: self.krpc.game_scene == scene,
            timeout=timeout,
            message=f"game scene to become {scene}",
        )

    def check_interface_can_be_built(self):
        # Build one of each kind of control, read it back and remove it. The skin the
        # game draws its own interface in is looked up per control, so a scene that
        # does not provide it would show up here rather than as an empty screen.
        canvas = self.ui.stock_canvas
        panel = canvas.add_panel()
        panel.add_vertical_layout()
        text = panel.add_text("Foo")
        button = panel.add_button("Bar")
        toggle = panel.add_toggle("Baz")
        toggle.checked = True
        self.assertEqual("Foo", text.content)
        self.assertEqual("Bar", button.text.content)
        self.assertTrue(toggle.checked)
        self.assertTrue(button.interactable)
        panel.remove()

    def test_space_center(self):
        self.set_scene(self.scenes.space_center)
        self.check_interface_can_be_built()

    def test_tracking_station(self):
        self.set_scene(self.scenes.space_center)
        self.set_scene(self.scenes.tracking_station)
        self.check_interface_can_be_built()

    def test_editors(self):
        self.set_scene(self.scenes.space_center)
        self.set_scene(self.scenes.editor_vab)
        self.check_interface_can_be_built()
        self.set_scene(self.scenes.space_center)
        self.set_scene(self.scenes.editor_sph)
        self.check_interface_can_be_built()

    def test_elements_do_not_survive_a_scene_change(self):
        # The game keeps the stock canvas across a change of scene, so an element added
        # to it is not taken away by the scene it was built in being unloaded. kRPC has
        # to take it away itself, which it does once the next scene is running.
        self.set_scene(self.scenes.space_center)
        panel = self.ui.stock_canvas.add_panel()
        text = panel.add_text("Foo")
        self.assertEqual("Foo", text.content)

        self.set_scene(self.scenes.tracking_station)

        # The game reports the new scene before the addons in it have swept up after the
        # previous one, so the elements go a moment after the change rather than with it.
        # Ask for the visibility rather than the content: the text of a label is a plain
        # field that keeps its value once the object behind it has gone, so reading it
        # back would say nothing about whether the element is still there.
        def gone():
            try:
                _ = panel.visible
                return False
            except RuntimeError:
                return True

        self.wait_until(gone, timeout=30, message="the elements to be taken away")
        self.assertRaises(RuntimeError, getattr, text, "visible")
        self.assertRaises(ValueError, panel.remove)

    def test_an_element_added_after_a_scene_change_is_kept(self):
        # Sweeping up after the previous scene must not take away what a client adds to
        # the new one before the sweep happens. The game reports the new scene before
        # that sweep runs, so the panel is added into the window between the two; the
        # test then waits long enough for the sweep to have happened before asking
        # whether the panel survived it. Whether the add lands in the window depends on
        # timing the test does not control, so a run in which it lands after the sweep
        # passes without exercising it.
        self.set_scene(self.scenes.space_center)
        self.ui.stock_canvas.add_panel().remove()
        self.set_scene(self.scenes.tracking_station)
        panel = self.ui.stock_canvas.add_panel()
        text = panel.add_text("Foo")
        self.wait(1)
        self.assertTrue(panel.visible)
        self.assertEqual("Foo", text.content)
        panel.remove()

    def test_added_canvas_outside_flight(self):
        self.set_scene(self.scenes.space_center)
        canvas = self.ui.add_canvas()
        text = canvas.add_text("Foo")
        self.assertEqual("Foo", text.content)
        canvas.remove()


if __name__ == "__main__":
    unittest.main()
