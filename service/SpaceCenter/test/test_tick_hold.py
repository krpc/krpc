import unittest

import krpctest


class TestTickHold(krpctest.TestCase):
    """Holding a physics tick, which is what lets a control loop read the game state,
    compute with it and write the result back without the game moving on in between.

    Universal time is advanced by the game's own update, so it is the thing to watch: it
    does not change at all while a tick is held, and changes again once it is released.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.krpc = cls.connect().krpc
        cls.space_center = cls.connect().space_center

    def tearDown(self):
        # Whatever a test did, the game must not be left held.
        self.krpc.release_tick()

    def test_the_game_does_not_advance_while_a_tick_is_held(self):
        self.krpc.hold_tick()
        try:
            ut = self.space_center.ut
            # Several ticks' worth of time, spent entirely inside the hold.
            self.wait(0.3)
            self.assertEqual(ut, self.space_center.ut)
        finally:
            self.krpc.release_tick()
        self.wait(0.3)
        self.assertGreater(self.space_center.ut, ut)

    def test_a_hold_that_is_not_released_runs_out_of_time(self):
        # The server's default tick hold timeout, which a test install has not changed.
        timeout = 1.0
        self.krpc.hold_tick()
        ut = self.space_center.ut
        self.wait(timeout + 0.3)
        self.assertGreater(self.space_center.ut, ut)

    def test_the_tick_cannot_be_held_twice(self):
        self.krpc.hold_tick()
        try:
            with self.assertRaises(RuntimeError):
                self.krpc.hold_tick()
        finally:
            self.krpc.release_tick()

    def test_releasing_a_tick_that_is_not_held_does_nothing(self):
        self.krpc.release_tick()
        self.krpc.release_tick()


if __name__ == "__main__":
    unittest.main()
