import unittest
import krpctest


class TestPoint(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_point(self):
        point = self.drawing.add_point((1, 2, 3), self.ref)
        self.assertEqual((1, 2, 3), point.position)
        self.assertEqual(self.ref, point.reference_frame)
        self.assertTrue(point.visible)
        self.assertEqual((1, 1, 1), point.color)
        self.assertAlmostEqual(8, point.size)
        self.assertEqual("Legacy Shaders/Particles/Additive", point.material)
        point.remove()
        self.assertRaises(self.destroyed, point.remove)

    def test_properties(self):
        point = self.drawing.add_point((0, 0, 0), self.ref, False)
        point.position = (0, 10, 0)
        point.color = (1, 0, 0)
        point.size = 20
        point.visible = True
        self.wait()
        self.assertEqual((0, 10, 0), point.position)
        self.assertEqual((1, 0, 0), point.color)
        self.assertAlmostEqual(20, point.size)
        point.remove()
        self.assertRaises(self.destroyed, setattr, point, "color", (0, 1, 0))


if __name__ == "__main__":
    unittest.main()
