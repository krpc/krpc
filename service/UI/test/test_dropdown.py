import unittest
import krpctest


class TestDropdown(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.canvas = cls.conn.ui.stock_canvas

    def test_dropdown(self):
        dropdown = self.canvas.add_dropdown()
        self.assertIsNotNone(dropdown.rect_transform)
        self.assertTrue(dropdown.visible)
        self.assertEqual([], dropdown.options)
        self.assertEqual(0, dropdown.selected_index)
        self.assertFalse(dropdown.changed)
        dropdown.remove()
        self.assertRaises(self.destroyed, dropdown.remove)

    def test_options(self):
        dropdown = self.canvas.add_dropdown()
        dropdown.options = ["Kerbin", "Mun", "Minmus"]
        self.assertEqual(["Kerbin", "Mun", "Minmus"], dropdown.options)
        self.assertEqual(0, dropdown.selected_index)
        dropdown.selected_index = 2
        self.assertEqual(2, dropdown.selected_index)
        dropdown.remove()

    def test_setting_options_resets_selection(self):
        dropdown = self.canvas.add_dropdown()
        dropdown.options = ["One", "Two", "Three"]
        dropdown.selected_index = 2
        dropdown.options = ["Four", "Five"]
        self.assertEqual(["Four", "Five"], dropdown.options)
        self.assertEqual(0, dropdown.selected_index)
        dropdown.selected_index = 1
        dropdown.options = []
        self.assertEqual([], dropdown.options)
        self.assertEqual(0, dropdown.selected_index)
        dropdown.remove()

    def test_an_option_the_dropdown_does_not_have(self):
        dropdown = self.canvas.add_dropdown()
        dropdown.options = ["One", "Two"]
        dropdown.selected_index = 1
        self.assertRaises(ValueError, setattr, dropdown, "selected_index", 2)
        self.assertRaises(ValueError, setattr, dropdown, "selected_index", -1)
        self.assertEqual(1, dropdown.selected_index)
        # There is nothing to choose while the dropdown has no options.
        dropdown.options = []
        self.assertRaises(ValueError, setattr, dropdown, "selected_index", 0)
        dropdown.remove()

    def test_choosing_an_option_is_not_a_change_by_the_user(self):
        dropdown = self.canvas.add_dropdown()
        dropdown.options = ["Kerbin", "Mun", "Minmus"]
        self.assertFalse(dropdown.changed)
        dropdown.selected_index = 2
        self.assertFalse(dropdown.changed)
        dropdown.remove()

    def test_interactable(self):
        dropdown = self.canvas.add_dropdown()
        self.assertTrue(dropdown.interactable)
        dropdown.interactable = False
        self.assertFalse(dropdown.interactable)
        dropdown.remove()


if __name__ == "__main__":
    unittest.main()
