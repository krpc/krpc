import unittest

import krpctest


class TestAxes(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_axes(self):
        x, y, z = self.drawing.add_axes(self.ref, 5)
        self.assertEqual((5, 0, 0), x.end)
        self.assertEqual((0, 5, 0), y.end)
        self.assertEqual((0, 0, 5), z.end)
        self.assertEqual((1, 0, 0), x.color)
        self.assertEqual((0, 1, 0), y.color)
        self.assertEqual((0, 0, 1), z.color)
        for line in (x, y, z):
            self.assertEqual((0, 0, 0), line.start)
            self.assertEqual(self.ref, line.reference_frame)
            self.assertTrue(line.end_arrow)
            self.assertFalse(line.start_arrow)
        self.wait()
        # Each axis is a line of its own
        x.remove()
        self.assertRaises(self.destroyed, x.remove)
        self.assertEqual((0, 5, 0), y.end)
        y.remove()
        z.remove()


if __name__ == "__main__":
    unittest.main()
