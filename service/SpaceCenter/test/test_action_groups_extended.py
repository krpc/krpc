import unittest
import krpctest


class TestActionGroupsExtended(krpctest.TestCase):

    # Exercises kRPC's support for the Action Groups Extended mod, which raises the number of
    # action groups from the stock 0-9 to 0-250. When the mod is installed the SpaceCenter Control
    # class routes get/set/toggle and action queries for these groups through the mod instead of
    # the stock action-group system. The craft is built with the mod installed and has actions
    # assigned to extended groups (see the craft comment below).

    mods = ["AGExt"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("ActionGroupsExtended")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.control = cls.space_center.active_vessel.control

    def test_extended_group_state_round_trip(self):
        # Groups beyond the stock range can be set, read back and toggled.
        for group in (10, 50, 250):
            self.control.set_action_group(group, False)
            self.assertFalse(self.control.get_action_group(group))
            self.control.set_action_group(group, True)
            self.assertTrue(self.control.get_action_group(group))
            self.control.toggle_action_group(group)
            self.assertFalse(self.control.get_action_group(group))

    def test_extended_group_range(self):
        # The mod extends the valid range to 0-250 inclusive; 251 is still out of range.
        self.control.set_action_group(250, False)
        self.assertRaises(ValueError, self.control.set_action_group, 251, False)
        self.assertRaises(ValueError, self.control.get_action_group, 251)
        self.assertRaises(ValueError, self.control.toggle_action_group, 251)
        self.assertRaises(ValueError, self.control.get_action_group_actions, 251)

    def test_group_with_multiple_actions(self):
        # Group 11 has the Extend action of all three solar panels assigned to it.
        actions = self.control.get_action_group_actions(11)
        self.assertEqual(3, len(actions))
        for action in actions:
            self.assertEqual("ModuleDeployableSolarPanel", action.module.name)
            self.assertEqual("ExtendAction", action.id)
            self.assertNotEqual("", action.name)
            self.assertEqual(action.part, action.module.part)
            self.assertNotEqual("", action.part.title)

    def test_group_with_single_action(self):
        # Group 12 has the light Toggle action assigned to it.
        actions = self.control.get_action_group_actions(12)
        self.assertEqual(1, len(actions))
        action = actions[0]
        self.assertEqual("ModuleColorChanger", action.module.name)
        self.assertEqual("ToggleAction", action.id)
        self.assertNotEqual("", action.name)

    def test_empty_group(self):
        # Group 13 has no actions assigned to it.
        self.assertEqual([], self.control.get_action_group_actions(13))


if __name__ == "__main__":
    unittest.main()
