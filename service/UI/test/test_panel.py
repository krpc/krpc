import unittest
import krpctest


class TestPanel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        ui = cls.conn.ui
        cls.canvas = ui.stock_canvas
        cls.panel_style = ui.PanelStyle

    def test_panel(self):
        panel = self.canvas.add_panel()
        self.assertIsNotNone(panel.rect_transform)
        self.assertTrue(panel.visible)
        self.assertEqual(self.panel_style.window, panel.style)
        self.assertFalse(panel.draggable)
        panel.remove()
        self.assertRaises(self.destroyed, panel.remove)

    def test_visible_is_the_elements_own_setting(self):
        # An element is only drawn when everything it is inside is visible as well, so
        # an interface can be built with its parts visible, inside a panel that is not,
        # and shown all at once.
        panel = self.canvas.add_panel()
        text = panel.add_text("Foo")
        self.assertTrue(text.visible)
        panel.visible = False
        self.assertFalse(panel.visible)
        self.assertTrue(text.visible)
        text.visible = False
        self.assertFalse(text.visible)
        text.visible = True
        self.assertTrue(text.visible)
        panel.visible = True
        self.assertTrue(panel.visible)
        self.assertTrue(text.visible)
        panel.remove()

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

    def test_bring_to_front(self):
        # The panel drawn on top cannot be read back, so this covers only that the call
        # is accepted, including on a panel that is already at the front.
        older = self.canvas.add_panel()
        newer = self.canvas.add_panel()
        older.bring_to_front()
        newer.bring_to_front()
        older.remove()
        newer.remove()

    def test_group_box(self):
        # A box with a caption, which is what the style is mainly here for.
        panel = self.canvas.add_panel()
        panel.style = self.panel_style.box
        caption = panel.add_text("Orbit Parameters")
        self.assertEqual("Orbit Parameters", caption.content)
        panel.remove()


if __name__ == "__main__":
    unittest.main()
