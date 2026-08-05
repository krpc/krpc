import unittest
import krpctest


class TestLayout(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        ui = cls.connect().ui
        cls.canvas = ui.stock_canvas
        cls.anchor = ui.TextAnchor
        cls.constraint = ui.GridConstraint
        cls.fit = ui.ContentSizeFit

    def test_no_layout_by_default(self):
        panel = self.canvas.add_panel()
        self.assertIsNone(panel.layout)
        panel.remove()

    def test_horizontal_layout(self):
        panel = self.canvas.add_panel()
        layout = panel.add_horizontal_layout()
        self.assertIsNotNone(panel.layout)
        layout.spacing = 4
        layout.padding = (1, 2, 3, 4)
        layout.child_alignment = self.anchor.middle_center
        self.assertEqual(4, layout.spacing)
        self.assertEqual((1, 2, 3, 4), layout.padding)
        self.assertEqual(self.anchor.middle_center, layout.child_alignment)
        panel.remove()

    def test_vertical_layout(self):
        panel = self.canvas.add_panel()
        layout = panel.add_vertical_layout()
        layout.spacing = 2
        self.assertEqual(2, layout.spacing)
        panel.remove()

    def test_only_one_layout_per_panel(self):
        panel = self.canvas.add_panel()
        panel.add_vertical_layout()
        self.assertRaises(RuntimeError, panel.add_horizontal_layout)
        panel.remove()

    def test_grid_layout(self):
        panel = self.canvas.add_panel()
        layout = panel.add_grid_layout()
        layout.cell_size = (60, 20)
        layout.spacing = 3
        layout.constraint = self.constraint.fixed_column_count
        layout.constraint_count = 6
        self.assertEqual((60, 20), layout.cell_size)
        self.assertEqual(3, layout.spacing)
        self.assertEqual(self.constraint.fixed_column_count, layout.constraint)
        self.assertEqual(6, layout.constraint_count)
        panel.remove()

    def test_a_constraint_count_below_one_is_refused(self):
        # Unity would silently move the count up to one, leaving the client reading back
        # a count it did not set with no way to tell.
        panel = self.canvas.add_panel()
        layout = panel.add_grid_layout()
        layout.constraint_count = 3
        self.assertRaises(ValueError, setattr, layout, "constraint_count", 0)
        self.assertRaises(ValueError, setattr, layout, "constraint_count", -1)
        self.assertEqual(3, layout.constraint_count)
        panel.remove()

    def test_cell_size_is_grid_only(self):
        panel = self.canvas.add_panel()
        layout = panel.add_vertical_layout()
        self.assertRaises(RuntimeError, getattr, layout, "cell_size")
        self.assertRaises(RuntimeError, getattr, layout, "constraint")
        panel.remove()

    def test_spacing_of_a_grid_applies_in_both_directions(self):
        panel = self.canvas.add_panel()
        layout = panel.add_grid_layout()
        layout.spacing = 5
        self.assertEqual(5, layout.spacing)
        panel.remove()

    def test_layout_element(self):
        panel = self.canvas.add_panel()
        text = panel.add_text("Foo")
        element = text.layout_element
        element.min_size = (10, 20)
        element.preferred_size = (100, 30)
        element.flexible_size = (1, 0)
        self.assertEqual((10, 20), element.min_size)
        self.assertEqual((100, 30), element.preferred_size)
        self.assertEqual((1, 0), element.flexible_size)
        self.assertFalse(element.ignore_layout)
        element.ignore_layout = True
        self.assertTrue(text.layout_element.ignore_layout)
        panel.remove()

    def test_size_fitter(self):
        panel = self.canvas.add_panel()
        fitter = panel.size_fitter
        self.assertEqual(self.fit.unconstrained, fitter.horizontal_fit)
        self.assertEqual(self.fit.unconstrained, fitter.vertical_fit)
        fitter.horizontal_fit = self.fit.preferred_size
        fitter.vertical_fit = self.fit.min_size
        self.assertEqual(self.fit.preferred_size, panel.size_fitter.horizontal_fit)
        self.assertEqual(self.fit.min_size, panel.size_fitter.vertical_fit)
        panel.remove()

    def test_grid_of_text(self):
        # The table of numbers that a layout is mainly here to make possible.
        panel = self.canvas.add_panel()
        layout = panel.add_grid_layout()
        layout.cell_size = (60, 16)
        layout.constraint = self.constraint.fixed_column_count
        layout.constraint_count = 3
        cells = [panel.add_text(str(i)) for i in range(9)]
        self.assertEqual(9, len(cells))
        for i, cell in enumerate(cells):
            self.assertEqual(str(i), cell.content)
        panel.remove()


if __name__ == "__main__":
    unittest.main()
