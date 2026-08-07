import unittest
import krpctest


class TestScrollView(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        ui = cls.conn.ui
        cls.canvas = ui.stock_canvas
        cls.constraint = ui.GridConstraint
        cls.fit = ui.ContentSizeFit

    def test_scroll_view(self):
        view = self.canvas.add_scroll_view()
        self.assertIsNotNone(view.rect_transform)
        self.assertTrue(view.visible)
        self.assertIsNotNone(view.content)
        self.assertTrue(view.horizontal)
        self.assertTrue(view.vertical)
        view.remove()
        self.assertRaises(self.destroyed, view.remove)

    def test_directions(self):
        view = self.canvas.add_scroll_view()
        view.horizontal = False
        self.assertFalse(view.horizontal)
        self.assertTrue(view.vertical)
        view.vertical = False
        self.assertFalse(view.vertical)
        view.remove()

    def test_content_cannot_be_removed_on_its_own(self):
        # The content is part of the scroll view, so it goes when the view goes.
        view = self.canvas.add_scroll_view()
        self.assertRaises(RuntimeError, view.content.remove)
        view.remove()

    def test_content_holds_elements(self):
        view = self.canvas.add_scroll_view()
        text = view.content.add_text("Foo")
        self.assertEqual("Foo", text.content)
        self.assertTrue(text.visible)
        view.remove()

    def test_table_of_values(self):
        # A scrolling table of numbers, which is what a scroll view is mainly here for.
        view = self.canvas.add_scroll_view()
        content = view.content
        layout = content.add_grid_layout()
        layout.cell_size = (60, 16)
        layout.constraint = self.constraint.fixed_column_count
        layout.constraint_count = 6
        content.size_fitter.vertical_fit = self.fit.preferred_size
        cells = [content.add_text(str(i)) for i in range(30)]
        self.assertEqual(30, len(cells))
        self.assertEqual(self.fit.preferred_size, content.size_fitter.vertical_fit)
        view.remove()


if __name__ == "__main__":
    unittest.main()
