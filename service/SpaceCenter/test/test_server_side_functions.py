import unittest

import krpctest
from krpc import defer
from krpctest.geometry import norm


class TestServerSideFunctions(krpctest.TestCase):
    """Server side functions against a live game, where the calls a function embeds
    reach the real SpaceCenter service.

    The unit tests in core/test cover the expression algebra against mock procedures.
    What only a game can show is a function reading the vessel and its parts, changing
    them, and starting a procedure that pauses execution.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.conn = cls.connect()
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel

    def tearDown(self):
        control = self.vessel.control
        control.throttle = 0
        control.rcs = False
        control.lights = False

    def test_a_function_reads_the_game_state(self):
        vessel = self.vessel
        altitude = self.conn.run_function(lambda: vessel.flight().mean_altitude)
        self.assertAlmostEqual(vessel.flight().mean_altitude, altitude, delta=1)

    def test_every_call_reads_the_same_tick(self):
        sc = self.sc
        times = self.conn.run_function(lambda: [sc.ut, sc.ut, sc.ut])
        self.assertEqual([times[0]] * 3, times)

    def test_a_function_computes_over_the_vessels_parts(self):
        vessel = self.vessel
        names = [part.name for part in vessel.parts.all]
        panels = self.conn.run_function(
            lambda: len(
                [part for part in vessel.parts.all if part.name == "solarPanels1"]
            )
        )
        self.assertEqual(names.count("solarPanels1"), panels)
        mass = self.conn.run_function(
            lambda: sum(part.mass for part in vessel.parts.all)
        )
        self.assertAlmostEqual(vessel.mass, mass, delta=10)

    def test_a_loop_builds_a_collection_from_the_parts(self):
        vessel = self.vessel

        def resource_names():
            names: list[str] = []
            for part in vessel.parts.all:
                for resource in part.resources.all:
                    if resource.name not in names:
                        names.append(resource.name)
            return names

        self.assertCountEqual(
            vessel.resources.names, self.conn.run_function(resource_names)
        )

    def test_side_effects_apply_within_one_tick(self):
        vessel = self.vessel

        def configure():
            control = vessel.control
            control.throttle = 1.0
            control.rcs = True
            control.lights = True

        self.assertIsNone(self.conn.run_function(configure))
        # The control inputs are the ones applied to the vessel, which refresh a
        # physics tick after they are set
        self.wait()
        control = vessel.control
        self.assertAlmostEqual(1, control.throttle)
        self.assertTrue(control.rcs)
        self.assertTrue(control.lights)

    def test_stdlib_operates_on_the_games_vectors(self):
        stdlib = self.conn.std_lib
        vessel = self.vessel
        frame = self.sc.bodies["Kerbin"].reference_frame
        distance = self.conn.run_function(
            lambda: stdlib.vector_magnitude(vessel.position(frame))
        )
        self.assertAlmostEqual(norm(vessel.position(frame)), distance, delta=10)

    def test_a_function_stream_follows_the_game(self):
        sc = self.sc
        start = sc.ut
        with self.conn.function_stream(lambda: sc.ut - start) as stream:
            first = stream()
            self.wait_until(
                lambda: stream() > first, timeout=10, message="the stream to update"
            )
            self.assertAlmostEqual(sc.ut - start, stream(), delta=1)

    def test_an_event_fires_when_the_game_reaches_a_time(self):
        sc = self.sc
        target = sc.ut + 2
        event = self.conn.add_event(lambda: sc.ut > target)
        try:
            with event.condition:
                event.wait(timeout=30)
            self.assertGreater(sc.ut, target)
        finally:
            event.remove()

    def test_a_deferred_call_does_not_wait_for_the_warp(self):
        sc = self.sc
        vessel = self.vessel
        target = sc.ut + 60

        def warp_and_arm():
            defer(sc.warp_to(target))
            vessel.control.rcs = True
            return sc.ut

        ut = self.conn.run_function(warp_and_arm)
        # The function ran to its end in the tick that started the warp
        self.assertLess(ut, target)
        self.assertTrue(vessel.control.rcs)
        self.wait_until(
            lambda: sc.ut >= target, timeout=60, message="the deferred warp to finish"
        )
        self.wait_until(lambda: sc.warp_rate == 1, timeout=30, message="warp to end")

    def test_a_procedure_that_pauses_must_be_deferred(self):
        sc = self.sc
        target = sc.ut + 60
        with self.assertRaises(RuntimeError) as caught:
            self.conn.run_function(lambda: sc.warp_to(target))
        self.assertIn("paused execution", str(caught.exception))
        # warp_to starts the warp before it pauses, and nothing resumes the call
        sc.rails_warp_factor = 0
        self.wait_until(lambda: sc.warp_rate == 1, timeout=30, message="warp to end")


if __name__ == "__main__":
    unittest.main()
