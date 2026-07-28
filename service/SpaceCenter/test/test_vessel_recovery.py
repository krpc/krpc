import unittest

import krpctest

# A stock craft small enough to launch from an unupgraded pad or runway, and built only
# from parts a career game starts with, so the same craft serves both test cases. It has a
# command pod, so every vessel launched here is crewed.
CRAFT = "Jumping Flea"


class RecoveryTestCase(krpctest.TestCase):
    """Launches a pair of vessels: one onto the pad and one onto the runway. The runway one
    is flown, which leaves the pad one as a recoverable vessel that is not the active one.
    Recovery destroys a vessel, so the pair is launched again for every test."""

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
        self.other.name = "Recovered"
        self.sc.launch_vessel("VAB", CRAFT, "Runway")
        self.vessel = self.sc.active_vessel
        self.vessel.name = "Flown"
        self.assertTrue(self.other.recoverable)
        self.crew = [member.name for member in self.other.crew]
        self.assertGreater(len(self.crew), 0)

    def recover(self, vessel):
        """Recover the given vessel and wait for it to leave the game."""
        vessel.recover()
        self.wait_until(
            lambda: vessel not in self.sc.vessels,
            message="the vessel was not removed from the game",
        )


class TestVesselRecovery(RecoveryTestCase):
    """Recovering a vessel that is not the one being flown. That happens where the vessel
    stands, so the flight carries on: the scene, the active vessel and its control state are
    left alone, while the recovered vessel and its parts leave the game and its crew are
    returned to the astronaut complex."""

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

    def assert_no_dialog(self):
        """Check that nothing was left on screen for someone to dismiss. The mission summary
        the game shows on recovery would sit over the flight until it was clicked away.
        """
        self.assertEqual(0, self.tools.recovery_dialog_count)

    def assert_dropped_from_flight_state(self, listed_before):
        """Check that the recovered vessel is no longer listed in the game's flight state.
        A vessel left there is still recorded as standing where it was, and is recovered a
        second time by the next thing that reads the list."""
        self.assertEqual(listed_before - 1, self.tools.flight_state_vessel_count)

    def assert_crew_recovered(self, crew):
        # Recovered, not killed off with the vessel.
        for name in crew:
            self.assertEqual(
                self.sc.RosterStatus.available, self.sc.get_kerbal(name).roster_status
            )

    def test_recover_loaded_vessel(self):
        # The pad and the runway are about 2km apart, inside the stock loading range, so
        # the vessel being recovered is in the scene with all of its parts in it.
        self.assertTrue(self.other.loaded)
        parts_before = self.tools.loaded_part_count
        other_parts = len(self.other.parts.all)
        self.assertGreater(other_parts, 0)
        situation = self.vessel.situation
        parts = len(self.vessel.parts.all)
        listed_before = self.tools.flight_state_vessel_count
        self.vessel.control.sas = True

        self.recover(self.other)

        # The parts go with the vessel. Dropping a vessel from the game without taking its
        # parts out of the scene leaves them standing where they were.
        self.assertEqual(parts_before - other_parts, self.tools.loaded_part_count)
        self.assert_dropped_from_flight_state(listed_before)
        self.assert_crew_recovered(self.crew)
        self.assert_no_dialog()
        self.assert_flight_continued(situation, parts)

    def test_recover_unloaded_vessel(self):
        # Put the vessel being flown in orbit, which takes the one on the pad out of
        # loading range. It is then recovered from its protovessel alone.
        self.set_circular_orbit("Kerbin", 100000)
        self.wait_until(
            lambda: not self.other.loaded, message="the vessel stayed loaded"
        )
        parts_before = self.tools.loaded_part_count
        parts = len(self.vessel.parts.all)
        listed_before = self.tools.flight_state_vessel_count
        self.vessel.control.sas = True

        self.recover(self.other)

        # An unloaded vessel has no parts in the scene, so those of the vessel being flown
        # are all there were and all of them are still there.
        self.assertEqual(parts_before, self.tools.loaded_part_count)
        self.assert_dropped_from_flight_state(listed_before)
        self.assert_crew_recovered(self.crew)
        self.assert_no_dialog()
        self.assert_flight_continued(self.sc.VesselSituation.orbiting, parts)

    def test_recover_active_vessel_returns_to_space_center(self):
        # The vessel being flown cannot be taken out of the scene it is being flown in, so
        # recovering it ends the flight, as pressing recover in game does. The game saves,
        # loads the space center and recovers the vessel there, so the scene changes before
        # the vessel goes.
        crew = [member.name for member in self.vessel.crew]
        vessel = self.vessel
        vessel.recover()
        self.wait_until(
            lambda: self.krpc.game_scene == self.krpc.GameScene.space_center,
            message="the game did not return to the space center",
        )
        self.wait_until(
            lambda: vessel not in self.sc.vessels,
            message="the vessel was not removed from the game",
        )
        # The vessel that was not being flown is left where it was.
        self.assertEqual([self.other], self.sc.vessels)
        self.assert_crew_recovered(crew)


class TestVesselRecoveryCareer(RecoveryTestCase):
    """Recovering a vessel that is not the one being flown pays out, as recovering it from
    the tracking station does."""

    @classmethod
    def setUpClass(cls):
        cls.new_save("krpctest_career", always_load=True)
        cls.stage_craft()

    def test_recovery_awards_funds(self):
        funds_before = self.sc.funds
        self.recover(self.other)
        self.assertGreater(self.sc.funds, funds_before)


if __name__ == "__main__":
    unittest.main()
