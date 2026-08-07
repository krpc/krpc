import unittest
import krpctest


class TestToggle(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.canvas = cls.conn.ui.stock_canvas

    def test_toggle(self):
        toggle = self.canvas.add_toggle("Foo")
        self.assertIsNotNone(toggle.rect_transform)
        self.assertTrue(toggle.visible)
        self.assertIsNotNone(toggle.text)
        self.assertEqual("Foo", toggle.text.content)
        self.assertFalse(toggle.checked)
        self.assertFalse(toggle.changed)
        self.assertIsNone(toggle.group)
        toggle.remove()
        self.assertRaises(self.destroyed, toggle.remove)

    def test_checked(self):
        toggle = self.canvas.add_toggle("Foo")
        self.assertFalse(toggle.checked)
        toggle.checked = True
        self.assertTrue(toggle.checked)
        toggle.checked = False
        self.assertFalse(toggle.checked)
        toggle.remove()

    def test_checking_is_not_a_change_by_the_user(self):
        toggle = self.canvas.add_toggle("Foo")
        toggle.checked = True
        self.assertFalse(toggle.changed)
        toggle.checked = False
        self.assertFalse(toggle.changed)
        toggle.remove()

    def test_checking_a_group_member_is_not_a_change_by_the_user(self):
        # Checking one toggle unchecks the rest of its group, which is not a change by
        # the user for them either.
        group = self.canvas.add_toggle_group()
        toggles = [self.canvas.add_toggle(str(i)) for i in range(3)]
        for toggle in toggles:
            toggle.group = group
        toggles[0].checked = True
        toggles[1].checked = True
        self.assertEqual([False, True, False], [t.checked for t in toggles])
        self.assertEqual([False, False, False], [t.changed for t in toggles])
        for toggle in toggles:
            toggle.remove()
        group.remove()

    def test_color(self):
        toggle = self.canvas.add_toggle("Foo")
        self.assertEqual((1, 1, 1, 1), toggle.color)
        toggle.color = (1, 0, 0, 0.5)
        self.assertEqual((1, 0, 0, 0.5), toggle.color)
        toggle.remove()

    def test_interactable(self):
        toggle = self.canvas.add_toggle("Foo")
        self.assertTrue(toggle.interactable)
        toggle.interactable = False
        self.assertFalse(toggle.interactable)
        self.assertTrue(toggle.visible)
        toggle.interactable = True
        toggle.remove()

    def test_group_is_exclusive(self):
        group = self.canvas.add_toggle_group()
        toggles = [self.canvas.add_toggle(str(i)) for i in range(3)]
        for toggle in toggles:
            toggle.group = group
        self.assertIsNone(group.selected)

        toggles[0].checked = True
        self.assertEqual(toggles[0], group.selected)
        self.assertEqual([True, False, False], [t.checked for t in toggles])

        toggles[2].checked = True
        self.assertEqual(toggles[2], group.selected)
        self.assertEqual([False, False, True], [t.checked for t in toggles])

        for toggle in toggles:
            toggle.remove()
        group.remove()

    def test_groups_are_independent(self):
        # Toggles are grouped by the group they refer to, not by what contains them,
        # so two groups of toggles in the same panel do not affect each other.
        panel = self.canvas.add_panel()
        first = self.canvas.add_toggle_group()
        second = self.canvas.add_toggle_group()
        one = panel.add_toggle("One")
        two = panel.add_toggle("Two")
        one.group = first
        two.group = second

        one.checked = True
        two.checked = True
        self.assertTrue(one.checked)
        self.assertTrue(two.checked)
        self.assertEqual(one, first.selected)
        self.assertEqual(two, second.selected)
        panel.remove()
        first.remove()
        second.remove()

    def test_group_is_exclusive_while_it_is_hidden(self):
        # A group is kept exclusive whether or not it is on the screen, so that a
        # dialog can be built out of sight and shown once it is ready.
        panel = self.canvas.add_panel(False)
        group = self.canvas.add_toggle_group()
        toggles = [panel.add_toggle(str(i)) for i in range(3)]
        for toggle in toggles:
            toggle.group = group

        toggles[0].checked = True
        self.assertEqual([True, False, False], [t.checked for t in toggles])
        self.assertEqual(toggles[0], group.selected)
        toggles[2].checked = True
        self.assertEqual([False, False, True], [t.checked for t in toggles])
        self.assertEqual(toggles[2], group.selected)

        # Showing the group leaves it as it was, and reports no change: there is
        # nothing for the game to put right, so nothing tells the toggles' listeners.
        panel.visible = True
        self.wait()
        self.assertEqual([False, False, True], [t.checked for t in toggles])
        self.assertEqual([False, False, False], [t.changed for t in toggles])
        self.assertEqual(toggles[2], group.selected)
        panel.remove()
        group.remove()

    def test_joining_a_group_while_checked_unchecks_the_rest(self):
        group = self.canvas.add_toggle_group()
        one = self.canvas.add_toggle("One")
        two = self.canvas.add_toggle("Two")
        one.group = group
        one.checked = True
        two.checked = True
        two.group = group
        self.assertFalse(one.checked)
        self.assertTrue(two.checked)
        self.assertEqual(two, group.selected)
        # Grouping a toggle is not a change the user made, to it or to the rest.
        self.assertEqual([False, False], [one.changed, two.changed])
        one.remove()
        two.remove()
        group.remove()

    def test_leaving_a_group_keeps_the_toggle_as_it_was(self):
        group = self.canvas.add_toggle_group()
        toggle = self.canvas.add_toggle("Foo")
        toggle.group = group
        toggle.checked = True
        toggle.group = None
        self.assertIsNone(toggle.group)
        self.assertTrue(toggle.checked)
        self.assertFalse(toggle.changed)
        self.assertIsNone(group.selected)
        toggle.remove()
        group.remove()

    def test_a_client_can_uncheck_a_group_that_cannot_be_switched_off(self):
        # Not allowing switch off stops the user clearing the checked toggle. A client
        # saying what a toggle is set to is obeyed.
        group = self.canvas.add_toggle_group()
        group.allow_switch_off = False
        toggle = self.canvas.add_toggle("Foo")
        toggle.group = group
        toggle.checked = True
        toggle.checked = False
        self.assertFalse(toggle.checked)
        self.assertIsNone(group.selected)
        toggle.remove()
        group.remove()

    def test_removing_a_group_ungroups_its_toggles(self):
        group = self.canvas.add_toggle_group()
        toggle = self.canvas.add_toggle("Foo")
        toggle.group = group
        self.assertEqual(group, toggle.group)
        group.remove()
        self.assertIsNone(toggle.group)
        toggle.remove()

    def test_allow_switch_off(self):
        group = self.canvas.add_toggle_group()
        self.assertTrue(group.allow_switch_off)
        group.allow_switch_off = False
        self.assertFalse(group.allow_switch_off)
        group.remove()


if __name__ == "__main__":
    unittest.main()
