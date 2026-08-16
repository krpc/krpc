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
        line.remove()
        self.assertRaises(self.destroyed, line.remove)

    def test_reading_a_removed_line_raises(self):
        line = self.add_line()
        line.remove()
        # What raises is what reaches into the game. The start, the end and the color are
        # the line's own configuration, and answer as they always did.
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

    def test_thickness(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.thickness = 1.234
        line.visible = True
        self.assertTrue(line.visible)
        self.assertAlmostEqual(1.234, line.thickness)
        line.remove()


if __name__ == "__main__":
    unittest.main()
