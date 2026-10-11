import krpctest


class TestGameState(krpctest.TestCase):
    """The game states that the server counts as loaded."""

    @classmethod
    def setUpClass(cls):
        cls.conn = cls.connect()

    def setUp(self):
        self.new_save(always_load=True)
        self.remove_other_vessels()
        self.space_center = self.conn.space_center

    def test_a_quickload_is_one_load(self):
        self.space_center.quicksave()
        self.wait(1)
        loads = self.conn.krpc.game_state_loads
        self.space_center.quickload()
        self.wait_until(
            lambda: self.conn.krpc.game_state_loads > loads,
            message="the game to finish loading",
        )
        self.wait(5)
        self.assertEqual(loads + 1, self.conn.krpc.game_state_loads)

    def test_a_vessel_across_a_visit_to_the_editor(self):
        vessel = self.space_center.active_vessel
        self.enter_editor()
        self.wait(3)
        self.leave_editor()
        self._set_game_scene(self.conn.krpc.GameScene.flight)
        self.assertEqual("Basic", vessel.name)


class TestEmptyGame(krpctest.TestCase):
    """A game with no vessels in it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save(always_load=True)
        cls.remove_other_vessels()
        cls.conn = cls.connect()

    def test_a_recovered_vessel_is_destroyed(self):
        vessel = self.conn.space_center.active_vessel
        vessel.recover()
        destroyed = self.conn.krpc.ObjectDestroyedException

        def is_destroyed():
            try:
                vessel.name
            except destroyed:
                return True
            except self.conn.krpc.ObjectNotLoadedException:
                pass
            return False

        # The game has no vessels, which settles once the count has held for long enough
        self.wait_until(is_destroyed, message="the vessel to be destroyed")
