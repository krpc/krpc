import unittest

import krpctest


class TestLine(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.drawing = cls.conn.drawing
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ref = cls.vessel.reference_frame
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    def add_line(self):
        return self.drawing.add_line((0, 0, 0), (0, 10, 0), self.ref, False)

    def test_line(self):
        line = self.drawing.add_line((10, 1, 2), (3, 10, 4), self.ref)
        self.assertEqual((10, 1, 2), line.start)
        self.assertEqual((3, 10, 4), line.end)
        self.assertEqual(self.ref, line.reference_frame)
        self.assertTrue(line.visible)
        self.assertEqual((1, 1, 1), line.color)
        self.assertEqual("Legacy Shaders/Particles/Additive", line.material)
        self.assertAlmostEqual(0.1, line.thickness)
        self.assertFalse(line.start_arrow)
        self.assertFalse(line.end_arrow)
        self.assertAlmostEqual(1, line.arrow_size)
        line.remove()
        self.assertRaises(self.destroyed, line.remove)

    def test_reading_a_removed_line_raises(self):
        line = self.add_line()
        line.remove()
        # A member that reaches into the game raises. The start, the end and the color are
        # the line's own configuration, and still answer.
        self.assertEqual((0, 0, 0), line.start)
        self.assertRaises(self.destroyed, getattr, line, "material")
        self.assertRaises(self.destroyed, setattr, line, "thickness", 1)
        self.assertRaises(self.destroyed, setattr, line, "color", (1, 0, 0))

    def test_color(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.color = (1, 0, 0)
        line.visible = True
        self.assertTrue(line.visible)
        self.assertEqual((1, 0, 0), line.color)
        line.remove()

    def test_colors(self):
        line = self.add_line()
        self.assertEqual([(1, 1, 1)], line.colors)
        line.colors = [(1, 0, 0), (0, 1, 0), (0, 0, 1)]
        line.end_arrow = True
        line.visible = True
        self.wait_for_frame()
        self.assertEqual([(1, 0, 0), (0, 1, 0), (0, 0, 1)], line.colors)
        self.assertEqual((1, 0, 0), line.color)
        line.color = (0, 1, 1)
        self.assertEqual([(0, 1, 1)], line.colors)
        line.remove()

    def test_colors_out_of_range(self):
        line = self.add_line()
        self.assertRaises(ValueError, setattr, line, "colors", [])
        self.assertRaises(ValueError, setattr, line, "colors", [(1, 1, 1)] * 9)
        self.assertEqual([(1, 1, 1)], line.colors)
        line.remove()

    def test_dashed(self):
        line = self.add_line()
        self.assertFalse(line.dashed)
        line.dashed = True
        line.end_arrow = True
        line.visible = True
        self.wait_for_frame()
        self.assertTrue(line.dashed)
        line.remove()

    def test_label(self):
        line = self.add_line()
        self.assertEqual("", line.label)
        line.label = "Prograde"
        line.visible = True
        self.wait_for_frame()
        self.assertEqual("Prograde", line.label)
        line.remove()
        self.assertEqual("Prograde", line.label)
        self.assertRaises(self.destroyed, setattr, line, "label", "")

    def test_thickness(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.thickness = 1.234
        line.visible = True
        self.assertTrue(line.visible)
        self.assertAlmostEqual(1.234, line.thickness)
        line.remove()

    def test_arrows(self):
        line = self.add_line()
        line.start_arrow = True
        line.end_arrow = True
        line.arrow_size = 2.5
        line.visible = True
        self.wait_for_frame()
        self.assertTrue(line.start_arrow)
        self.assertTrue(line.end_arrow)
        self.assertAlmostEqual(2.5, line.arrow_size)
        line.remove()

    def test_arrows_on_a_short_line(self):
        line = self.drawing.add_line((0, 0, 0), (0, 0.01, 0), self.ref)
        line.end_arrow = True
        self.wait_for_frame()
        self.assertTrue(line.end_arrow)
        line.remove()


if __name__ == "__main__":
    unittest.main()
