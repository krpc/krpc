import unittest

import krpctest


class TestLaunchCrew(krpctest.TestCase):
    """Test the crew parameter of SpaceCenter.LaunchVessel: default assignments
    when null, no crew when empty, and exactly the named Kerbals, in order, when
    populated."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.sc = cls.connect().space_center
        # Stage the craft launched directly through the RPC in these tests.
        cls._stage_craft("Basic", "VAB", None)
        cls._stage_craft("Multi", "VAB", None)
        # Deterministic, available crew to launch with. Distinct names per test keep
        # the tests independent of one another's launched (and so assigned) Kerbals.
        for name in (
            "Alpha Kerman",
            "Busy Kerman",
            "Crowd One Kerman",
            "Crowd Two Kerman",
            "Order One Kerman",
            "Order Two Kerman",
            "Order Three Kerman",
            "Order Four Kerman",
            "Excess One Kerman",
            "Excess Two Kerman",
            "Excess Three Kerman",
        ):
            if cls.sc.get_kerbal(name) is None:
                cls.sc.create_kerbal(name, "Pilot", True)

    def launch(self, name, crew=None):
        # crew is left unset (the default, None) to request the default crew, or set
        # to a list to request specific crew. An empty list requests no crew.
        if crew is None:
            self.sc.launch_vessel("VAB", name, "LaunchPad")
        else:
            self.sc.launch_vessel("VAB", name, "LaunchPad", crew)
        return self.sc.active_vessel

    def crew_names(self, vessel):
        return sorted(member.name for member in vessel.crew)

    def test_default_crew(self):
        vessel = self.launch("Basic")
        self.assertEqual(1, vessel.crew_count)
        self.assertTrue(vessel.crew[0].name.endswith(" Kerman"))

    def test_no_crew(self):
        # Multi has crew seats but also probe cores, so it stays controllable with no
        # crew and launches without a control-warning dialog. An empty crew list
        # leaves every seat empty.
        vessel = self.launch("Multi", [])
        self.assertEqual(2, vessel.crew_capacity)
        self.assertEqual(0, vessel.crew_count)
        self.assertEqual([], vessel.crew)

    def test_named_crew(self):
        vessel = self.launch("Basic", ["Alpha Kerman"])
        self.assertEqual(["Alpha Kerman"], self.crew_names(vessel))

    def test_unknown_crew_raises(self):
        before = self.sc.active_vessel
        self.assertRaises(
            ValueError,
            self.sc.launch_vessel,
            "VAB",
            "Basic",
            "LaunchPad",
            ["Ghost Kerman"],
        )
        # The launch is aborted before any vessel is created.
        self.assertEqual(before, self.sc.active_vessel)

    def test_unavailable_crew_raises(self):
        self.launch("Basic", ["Busy Kerman"])
        # Busy Kerman is now assigned to a vessel, so is unavailable for another launch.
        self.assertTrue(self.sc.get_kerbal("Busy Kerman").on_mission)
        self.assertRaises(
            ValueError,
            self.sc.launch_vessel,
            "VAB",
            "Basic",
            "LaunchPad",
            ["Busy Kerman"],
        )

    def test_too_many_crew_raises(self):
        # Basic has a single seat, so two crew do not fit.
        self.assertRaises(
            ValueError,
            self.sc.launch_vessel,
            "VAB",
            "Basic",
            "LaunchPad",
            ["Crowd One Kerman", "Crowd Two Kerman"],
        )

    def test_too_many_crew_for_seats_raises(self):
        # Multi has two seats, so three crew do not fit.
        self.assertRaises(
            ValueError,
            self.sc.launch_vessel,
            "VAB",
            "Multi",
            "LaunchPad",
            ["Excess One Kerman", "Excess Two Kerman", "Excess Three Kerman"],
        )

    def test_crew_order(self):
        # Multi has two single-seat pods. Seating follows the order of the crew list,
        # so the Kerbal listed first always takes the same seat and the one listed
        # second the other seat. Two launches with different crew show this without
        # either needing to be freed from the first vessel.
        first = self.seat_map(
            self.launch("Multi", ["Order One Kerman", "Order Two Kerman"])
        )
        second = self.seat_map(
            self.launch("Multi", ["Order Three Kerman", "Order Four Kerman"])
        )

        # The craft has two distinct seats, and both launches fill the same two.
        self.assertEqual(2, len(first))
        self.assertEqual(set(first), set(second))

        first_seat = {name: seat for seat, name in first.items()}
        second_seat = {name: seat for seat, name in second.items()}
        # The first-listed Kerbal takes the same seat in both launches, as does the
        # second-listed one, and the two seats differ.
        self.assertEqual(
            first_seat["Order One Kerman"], second_seat["Order Three Kerman"]
        )
        self.assertEqual(
            first_seat["Order Two Kerman"], second_seat["Order Four Kerman"]
        )
        self.assertNotEqual(
            first_seat["Order One Kerman"], first_seat["Order Two Kerman"]
        )

    def seat_map(self, vessel):
        # Map each crewed seat, keyed by the crewed part's index in the vessel's part
        # list (stable across launches of the same craft), to the Kerbal in it.
        seats = {}
        index = 0
        for part in vessel.parts.all:
            if part.crew_capacity > 0:
                if part.crew:
                    seats[index] = part.crew[0].name
                index += 1
        return seats


if __name__ == "__main__":
    unittest.main()
