import unittest
import krpc
import krpctest


class TestLine(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.drawing = cls.connect().drawing
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ref = cls.vessel.reference_frame

    def add_line(self):
        return self.drawing.add_line((0, 0, 0), (0, 10, 0), self.ref, False)

    def test_line(self):
        line = self.drawing.add_line((10, 1, 2), (3, 10, 4), self.ref)
        self.assertEqual((10, 1, 2), line.start)
        self.assertEqual((3, 10, 4), line.end)
        self.assertEqual(self.ref, line.reference_frame)
        self.assertTrue(line.visible)
        self.assertEqual((1, 1, 1), line.color)
        self.assertEqual("Particles/Additive", line.material)
        self.assertAlmostEqual(0.1, line.thickness)
        self.wait()
        line.remove()
        self.assertRaises(krpc.client.RPCError, line.remove)

    def test_color(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.color = (1, 0, 0)
        line.visible = True
        self.assertTrue(line.visible)
        self.assertEqual((1, 0, 0), line.color)
        self.wait()
        line.remove()

    def test_thickness(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.thickness = 1.234
        line.visible = True
        self.assertTrue(line.visible)
        self.assertAlmostEqual(1.234, line.thickness)
        self.wait()
        line.remove()


if __name__ == '__main__':
    unittest.main()
