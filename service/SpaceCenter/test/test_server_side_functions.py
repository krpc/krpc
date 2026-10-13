import unittest

import krpc.error
import krpctest
from krpctest.geometry import norm


def send_on_eva(test, space_center):
    """Send the active vessel's first crew member outside. A kerbal climbing out takes
    hold of the ladder, which it is made to let go of."""
    kerbal = space_center.active_vessel.crew[0].eva()
    test.wait_until(
        lambda: space_center.active_vessel == kerbal, message="the kerbal to climb out"
    )
    control = kerbal.control
    test.wait(2)
    if control.ladder is not None:
        control.release_ladder()
        test.wait_until(lambda: control.ladder is None, message="the kerbal to let go")
    return kerbal


class TestServerSideFunctions(krpctest.TestCase):
    """Server side functions against a live game, where the calls a function embeds
    reach the real SpaceCenter service. The unit tests in core/test cover the expression
    algebra against mock procedures. These tests cover a function reading the vessel and
    its parts, changing them, and starting a procedure that pauses execution. They also
    cover state kept across stream updates, a handler catching a service exception, and
    the unnamed error of a procedure unavailable in the game scene.
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
        altitude = self.conn.run(lambda: vessel.flight().mean_altitude)
        self.assertAlmostEqual(vessel.flight().mean_altitude, altitude, delta=1)

    def test_every_call_reads_the_same_tick(self):
        sc = self.sc
        times = self.conn.run(lambda: [sc.ut, sc.ut, sc.ut])
        self.assertEqual([times[0]] * 3, times)

    def test_a_function_computes_over_the_vessels_parts(self):
        vessel = self.vessel
        names = [part.name for part in vessel.parts.all]
        panels = self.conn.run(
            lambda: len(
                [part for part in vessel.parts.all if part.name == "solarPanels1"]
            )
        )
        self.assertEqual(names.count("solarPanels1"), panels)
        mass = self.conn.run(lambda: sum(part.mass for part in vessel.parts.all))
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

        self.assertCountEqual(vessel.resources.names, self.conn.run(resource_names))

    def test_a_function_changes_the_vessel_controls(self):
        vessel = self.vessel

        def configure():
            control = vessel.control
            control.throttle = 1.0
            control.rcs = True
            control.lights = True

        self.assertIsNone(self.conn.run(configure))
        # The control inputs are the ones applied to the vessel, which refresh a
        # physics tick after they are set
        self.wait()
        control = vessel.control
        self.assertAlmostEqual(1, control.throttle)
        self.assertTrue(control.rcs)
        self.assertTrue(control.lights)

    def test_a_function_compiled_in_advance(self):
        vessel = self.vessel

        def lights_on():
            if not vessel.control.lights:
                vessel.control.lights = True

        function = self.conn.compile(lights_on)
        self.assertFalse(vessel.control.lights)
        self.conn.run(function)
        self.wait()
        self.assertTrue(vessel.control.lights)

    def test_a_compiled_function_takes_arguments(self):
        Vessel = self.sc.Vessel

        @self.conn.compile
        def set_throttle(target: Vessel, throttle: float = 1.0) -> None:
            target.control.throttle = throttle

        self.conn.run(set_throttle, self.vessel, 0.5)
        self.wait()
        self.assertAlmostEqual(0.5, self.vessel.control.throttle, places=3)
        self.conn.run(set_throttle, self.vessel)
        self.wait()
        self.assertAlmostEqual(1, self.vessel.control.throttle)

    def test_stdlib_operates_on_the_games_vectors(self):
        stdlib = self.conn.std_lib
        vessel = self.vessel
        frame = self.sc.bodies["Kerbin"].reference_frame
        distance = self.conn.run(
            lambda: stdlib.vector_magnitude(vessel.position(frame))
        )
        self.assertAlmostEqual(norm(vessel.position(frame)), distance, delta=10)

    def test_a_function_stream_follows_the_game(self):
        sc = self.sc
        start = sc.ut
        with self.conn.stream(lambda: sc.ut - start) as stream:
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
                # A wait that times out returns without the event firing
                self.assertTrue(event.stream())
            self.assertGreater(sc.ut, target)
        finally:
            event.remove()

    def test_state_is_kept_across_stream_updates(self):
        sc = self.sc
        updates = 0

        def count():
            nonlocal updates
            if sc.ut > 0:
                updates += 1
            return updates

        with self.conn.stream(count) as stream:
            first = stream()
            self.wait_until(
                lambda: stream() > first + 2, timeout=10, message="the count to rise"
            )
        # The state is kept on the server
        self.assertEqual(0, updates)

    def test_a_function_handles_a_service_exception(self):
        vessel = self.vessel

        def switch_modes() -> str:
            try:
                for engine in vessel.parts.engines:
                    engine.toggle_mode()
                return "switched"
            except Exception as error:
                return "could not switch: " + error

        self.assertEqual(
            "could not switch: The engine only has a single mode",
            self.conn.run(switch_modes),
        )

    def test_a_procedure_unavailable_in_the_scene_is_an_unnamed_error(self):
        sc = self.sc
        with self.assertRaises(krpc.error.RPCError) as caught:
            self.conn.run(lambda: sc.editor.delta_v_altitude)
        self.assertIs(krpc.error.RPCError, type(caught.exception))

        def caught_by_a_catch_all() -> float:
            try:
                return sc.editor.delta_v_altitude
            except Exception:
                return -1.0

        self.assertEqual(-1.0, self.conn.run(caught_by_a_catch_all))

    def test_a_procedure_that_pauses_is_an_error(self):
        sc = self.sc
        target = sc.ut + 60
        with self.assertRaises(RuntimeError) as caught:
            self.conn.run(lambda: sc.warp_to(target))
        self.assertIn("paused execution", str(caught.exception))
        # warp_to starts the warp before it pauses, and the call is never resumed
        self.assertNotEqual(sc.WarpMode.none, sc.warp_mode)
        sc.rails_warp_factor = 0
        self.wait_until(lambda: sc.warp_rate == 1, timeout=30, message="warp to end")


if __name__ == "__main__":
    unittest.main()


class TestServerSideFunctionsOverUnloadedObjects(krpctest.TestCase):
    """Function streams over a part of a vessel the game unloads. A function stream is
    never held, so one that meets an object that is not loaded ends, unless the function
    catches it."""

    def setUp(self):
        self.new_save(always_load=True)
        self.remove_other_vessels()
        self.conn = self.connect()
        self.sc = self.conn.space_center

    def unload(self, pod):
        send_on_eva(self, self.sc)
        # Far enough from the pod for the game to unload it
        self.set_landed("Kerbin", -0.05, -70)
        self.wait_until(lambda: not pod.loaded, message="the pod to unload")

    def test_a_function_that_catches_not_loaded_carries_on(self):
        pod = self.sc.active_vessel
        part = pod.parts.root
        not_loaded = self.conn.krpc.ObjectNotLoadedException

        def temperature() -> float:
            try:
                return part.temperature
            except not_loaded:
                return -1.0

        ended = []
        with self.conn.stream(temperature) as stream:
            stream.add_end_callback(ended.append)
            self.assertGreater(stream(), 0)
            self.unload(pod)
            self.wait_until(lambda: stream() == -1, message="the function to catch it")
            self.sc.active_vessel = pod
            self.wait_until(lambda: stream() > 0, message="the pod to load")
            self.assertEqual([], ended)

    def test_a_function_that_does_not_catch_not_loaded_ends(self):
        pod = self.sc.active_vessel
        part = pod.parts.root
        ended = []
        with self.conn.stream(lambda: part.temperature) as stream:
            stream.add_end_callback(ended.append)
            self.assertGreater(stream(), 0)
            self.unload(pod)
            self.wait_until(lambda: ended, message="the stream to end")
            self.assertIsInstance(ended[0], self.conn.krpc.ObjectNotLoadedException)
