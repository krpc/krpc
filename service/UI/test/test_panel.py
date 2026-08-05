import unittest
import krpctest


class TestPanel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        ui = cls.connect().ui
        cls.canvas = ui.stock_canvas
        cls.panel_style = ui.PanelStyle

    def test_panel(self):
        panel = self.canvas.add_panel()
        self.assertIsNotNone(panel.rect_transform)
        self.assertTrue(panel.visible)
        self.assertEqual(self.panel_style.window, panel.style)
        self.assertFalse(panel.draggable)
        panel.remove()
        self.assertRaises(ValueError, panel.remove)

    def test_style(self):
        panel = self.canvas.add_panel()
        panel.style = self.panel_style.box
        self.assertEqual(self.panel_style.box, panel.style)
        panel.style = self.panel_style.none
        self.assertEqual(self.panel_style.none, panel.style)
        panel.style = self.panel_style.window
        self.assertEqual(self.panel_style.window, panel.style)
        panel.remove()

    def test_style_of_a_panel_that_starts_out_with_no_background(self):
        # The contents of a scroll view are a panel that is not drawn until it is given
        # a style, and it is then drawn untinted rather than left invisible.
        view = self.canvas.add_scroll_view()
        content = view.content
        self.assertEqual(self.panel_style.none, content.style)
        self.assertEqual((1, 1, 1, 1), content.color)
        content.style = self.panel_style.box
        self.assertEqual(self.panel_style.box, content.style)
        view.remove()

    def test_color(self):
        panel = self.canvas.add_panel()
        panel.color = (1, 0, 0, 1)
        self.assertEqual((1, 0, 0, 1), panel.color)
        # A panel can be seen through, which is what the alpha channel is mainly here for.
        panel.color = (1, 0, 0, 0.25)
        self.assertEqual((1, 0, 0, 0.25), panel.color)
        panel.remove()

    def test_draggable(self):
        panel = self.canvas.add_panel()
        self.assertFalse(panel.draggable)
        panel.draggable = True
        self.assertTrue(panel.draggable)
        panel.draggable = False
        self.assertFalse(panel.draggable)
        panel.remove()

    def test_group_box(self):
        # A box with a caption, which is what the style is mainly here for.
        panel = self.canvas.add_panel()
        panel.style = self.panel_style.box
        caption = panel.add_text("Orbit Parameters")
        self.assertEqual("Orbit Parameters", caption.content)
        panel.remove()


if __name__ == "__main__":
    unittest.main()
