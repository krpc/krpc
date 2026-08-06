import unittest

import krpctest

# A stock craft small enough to launch from an unupgraded pad or runway, and built only
# from parts a career game starts with, so the same craft serves both test cases. It has a
# command pod, so every vessel launched here is crewed.
CRAFT = "Jumping Flea"


class TerminationTestCase(krpctest.TestCase):
    """Launches a pair of vessels: one onto the pad and one onto the runway. The runway one
    is flown, which leaves the pad one as a vessel that is not the active one. Termination
    destroys a vessel, so the pair is launched again for every test."""

    @classmethod
    def stage_craft(cls):
        cls.sc = cls.connect().space_center
        cls.krpc = cls.connect().krpc
        cls.tools = cls.connect().testing_tools
        cls._stage_craft(CRAFT, "VAB", None)

    def setUp(self):
        self.sc.launch_vessel("VAB", CRAFT, "LaunchPad")
        # Clear out whatever the save started with, and whatever the last test left on the
        # runway, so the vessel just launched is the only other one in the game.
        self.remove_other_vessels()
        self.other = self.sc.active_vessel
        self.other.name = "Terminated"
        self.sc.launch_vessel("VAB", CRAFT, "Runway")
        self.vessel = self.sc.active_vessel
        self.vessel.name = "Flown"
        self.crew = [member.name for member in self.other.crew]
        self.assertGreater(len(self.crew), 0)

    def terminate(self, vessel):
        """Terminate the given vessel and wait for it to leave the game."""
        vessel.terminate()
        self.wait_until(
            lambda: vessel not in self.sc.vessels,
            message="the vessel was not removed from the game",
        )


class TestVesselTermination(TerminationTestCase):
    """Terminating a vessel that is not the one being flown. The vessel and its parts leave
    the game, its crew are reported missing, and the flight carries on: the scene, the
    active vessel and its control state are left alone."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.stage_craft()

    def assert_flight_continued(self, situation, parts):
        """Check that the vessel being flown carried on as if nothing had happened. The
        clock is read after a pause, so this shows the flight still running rather than
        only that it survived the call."""
        ut = self.sc.ut
        self.assertEqual(self.krpc.GameScene.flight, self.krpc.game_scene)
        self.assertEqual(self.vessel, self.sc.active_vessel)
        self.assertEqual([self.vessel], self.sc.vessels)
        self.assertEqual("Flown", self.vessel.name)
        self.assertEqual(situation, self.vessel.situation)
        self.assertEqual(parts, len(self.vessel.parts.all))
        self.assertTrue(self.vessel.control.sas)
        self.wait(1)
        self.assertGreater(self.sc.ut, ut)

    def assert_dropped_from_flight_state(self, listed_before):
        """Check that the terminated vessel is no longer listed in the game's flight state.
        A vessel left there is still recorded as standing where it was."""
        self.assertEqual(listed_before - 1, self.tools.flight_state_vessel_count)

    def assert_crew_missing(self, crew):
        # Reported missing rather than killed, which is how the game treats the crew of a
        # vessel terminated from the tracking station. They stay in the roster, so they
        # can come back when their respawn timer expires.
        for name in crew:
            self.assertEqual(
                self.sc.RosterStatus.missing, self.sc.get_kerbal(name).roster_status
            )

    def test_terminate_loaded_vessel(self):
        # The pad and the runway are about 2km apart, inside the stock loading range, so
        # the vessel being terminated is in the scene with all of its parts in it.
        self.assertTrue(self.other.loaded)
        parts_before = self.tools.loaded_part_count
        other_parts = len(self.other.parts.all)
        self.assertGreater(other_parts, 0)
        situation = self.vessel.situation
        parts = len(self.vessel.parts.all)
        listed_before = self.tools.flight_state_vessel_count
        self.vessel.control.sas = True

        self.terminate(self.other)

        # The parts go with the vessel. Dropping a vessel from the game without taking its
        # parts out of the scene leaves them standing where they were.
        self.assertEqual(parts_before - other_parts, self.tools.loaded_part_count)
        self.assert_dropped_from_flight_state(listed_before)
        self.assert_crew_missing(self.crew)
        self.assert_flight_continued(situation, parts)

    def test_terminate_unloaded_vessel(self):
        # Put the vessel being flown in orbit, which takes the one on the pad out of
        # loading range. It is then terminated from its protovessel alone.
        self.set_circular_orbit("Kerbin", 100000)
        self.wait_until(
            lambda: not self.other.loaded, message="the vessel stayed loaded"
        )
        parts_before = self.tools.loaded_part_count
        parts = len(self.vessel.parts.all)
        listed_before = self.tools.flight_state_vessel_count
        self.vessel.control.sas = True

        self.terminate(self.other)

        # An unloaded vessel has no parts in the scene, so those of the vessel being flown
        # are all there were and all of them are still there.
        self.assertEqual(parts_before, self.tools.loaded_part_count)
        self.assert_dropped_from_flight_state(listed_before)
        self.assert_crew_missing(self.crew)
        self.assert_flight_continued(self.sc.VesselSituation.orbiting, parts)

    def test_terminate_active_vessel_raises(self):
        # The vessel being flown cannot be taken out of the scene it is being flown in.
        with self.assertRaises(RuntimeError):
            self.vessel.terminate()
        self.wait(1)
        self.assertEqual(self.vessel, self.sc.active_vessel)
        self.assertEqual(
            {"Flown", "Terminated"}, {vessel.name for vessel in self.sc.vessels}
        )


class TestVesselTerminationCareer(TerminationTestCase):
    """Terminating a vessel that is not the one being flown recovers nothing, unlike
    recovering it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save("krpctest_career", always_load=True)
        cls.stage_craft()

    def test_termination_awards_nothing(self):
        funds_before = self.sc.funds
        science_before = self.sc.science
        self.terminate(self.other)
        self.assertEqual(funds_before, self.sc.funds)
        self.assertEqual(science_before, self.sc.science)


if __name__ == "__main__":
    unittest.main()
