import krpctest


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


class TestStreamsAcrossLoads(krpctest.TestCase):
    """Streams over game objects while the game loads a state or unloads a vessel."""

    @classmethod
    def setUpClass(cls):
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    def setUp(self):
        self.new_save(always_load=True)
        self.remove_other_vessels()
        self.space_center = self.conn.space_center

    def quickload(self):
        loads = self.conn.krpc.game_state_loads
        self.space_center.quickload()
        self.wait_until(
            lambda: self.conn.krpc.game_state_loads > loads,
            message="the game to finish loading",
        )

    def test_stream_over_a_surviving_vessel(self):
        vessel = self.space_center.active_vessel
        with self.conn.stream(getattr, vessel, "name") as name:
            self.assertEqual("Basic", name())
            self.space_center.quicksave()
            self.wait(1)
            self.quickload()
            # The stream carries on with the vessel the load built
            with name.condition:
                vessel.name = "Renamed"
                self.assertTrue(name.wait(10))
            self.assertEqual("Renamed", name())

    def test_stream_over_a_vessel_while_in_the_editor(self):
        vessel = self.space_center.active_vessel
        ended = []
        with self.conn.stream(getattr, vessel, "name") as name:
            name.add_end_callback(ended.append)
            self.assertEqual("Basic", name())
            self.enter_editor()
            self.wait(3)
            self.assertEqual([], ended)
            self.leave_editor()
            self._set_game_scene(self.conn.krpc.GameScene.flight)
            self.assertEqual("Basic", vessel.name)
            self.assertEqual([], ended)
            # The stream carries on in flight
            vessel.name = "Renamed"
            self.wait_until(lambda: name() == "Renamed", message="the stream to update")

    def test_stream_over_a_flight_property_outside_flight(self):
        control = self.space_center.active_vessel.control
        ended = []
        with self.conn.stream(getattr, control, "throttle") as throttle:
            throttle.add_end_callback(ended.append)
            throttle()
            self.enter_editor()
            self.wait(3)
            self.assertEqual([], ended)
            self.leave_editor()
            self._set_game_scene(self.conn.krpc.GameScene.flight)
            # The stream carries on in flight
            control.throttle = 0.75
            self.wait_until(
                lambda: abs(throttle() - 0.75) < 1e-3, message="the stream to update"
            )
            self.assertEqual([], ended)

    def test_stream_over_a_vessel_removed_by_a_load(self):
        self.space_center.quicksave()
        self.wait(1)
        kerbal = send_on_eva(self, self.space_center)
        ended = []
        with self.conn.stream(getattr, kerbal, "name") as name:
            name.add_end_callback(ended.append)
            name()
            self.quickload()
            self.wait_until(lambda: ended, message="the stream to end")
            self.assertIsInstance(ended[0], self.destroyed)
            self.assertRaises(self.destroyed, name)

    def test_stream_over_a_part_of_an_unloaded_vessel(self):
        pod = self.space_center.active_vessel
        part = pod.parts.root
        with self.conn.stream(getattr, part, "temperature") as temperature:
            temperature()
            kerbal = send_on_eva(self, self.space_center)
            # Far enough from the pod for the game to unload it
            self.set_landed("Kerbin", -0.05, -70)
            self.wait_until(lambda: not pod.loaded, message="the pod to unload")
            ended = []
            temperature.add_end_callback(ended.append)
            self.wait(1)
            self.assertEqual([], ended)
            # The stream resumes once the game loads the pod again
            self.space_center.active_vessel = pod
            self.wait_until(lambda: pod.loaded, message="the pod to load")
            with temperature.condition:
                self.assertTrue(temperature.wait(10))
            self.assertEqual([], ended)
            kerbal.recover()


class TestStreamsInAnEmptyGame(krpctest.TestCase):
    """A stream over a vessel that leaves the game with nothing in it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save(always_load=True)
        cls.remove_other_vessels()
        cls.conn = cls.connect()

    def test_stream_over_a_recovered_vessel(self):
        vessel = self.conn.space_center.active_vessel
        ended = []
        name = self.conn.add_stream(getattr, vessel, "name")
        name.add_end_callback(ended.append)
        name()
        vessel.recover()
        # The game has no vessels, which settles once the count has held for long enough
        self.wait_until(lambda: ended, message="the stream to end")
        self.assertIsInstance(ended[0], self.conn.krpc.ObjectDestroyedException)


class TestStreamsAcrossCareerLoads(krpctest.TestCase):
    """Streams over the contracts of a career game while the game loads."""

    @classmethod
    def setUpClass(cls):
        cls.new_save("krpctest_career", always_load=True)
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center

    def test_stream_over_a_contract_across_a_load(self):
        contract = self.space_center.contract_manager.active_contracts[0]
        ended = []
        with self.conn.stream(getattr, contract, "title") as title:
            title.add_end_callback(ended.append)
            self.assertEqual("Orbit Kerbin!", title())
            self.space_center.save("contract_stream")
            loads = self.conn.krpc.game_state_loads
            self.space_center.load("contract_stream")
            self.wait_until(
                lambda: self.conn.krpc.game_state_loads > loads,
                message="the game to finish loading",
            )
            self.wait(3)
            # The contract survives the load, which parses contracts a frame late
            self.assertEqual([], ended)
            self.assertEqual("Orbit Kerbin!", contract.title)
