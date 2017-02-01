import unittest
import krpc
import krpctest


class TestPolygon(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.drawing = cls.connect().drawing
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ref = cls.vessel.reference_frame

    vertices = [
        (0, 0, 0),
        (0, 10, 0),
        (10, 10, 0)
    ]

    def add_polygon(self):
        return self.drawing.add_polygon(self.vertices, self.ref, False)

    def test_polygon(self):
        polygon = self.add_polygon()
        polygon.visible = True
        self.assertEqual(self.vertices, polygon.vertices)
        self.assertEqual(self.ref, polygon.reference_frame)
        self.assertTrue(polygon.visible)
        self.assertEqual((1, 1, 1), polygon.color)
        self.assertEqual("Particles/Additive", polygon.material)
        self.assertAlmostEqual(0.1, polygon.thickness)
        self.wait()
        polygon.remove()
        self.assertRaises(krpc.client.RPCError, polygon.remove)

    def test_color(self):
        polygon = self.add_polygon()
        self.assertFalse(polygon.visible)
        polygon.color = (1, 0, 0)
        polygon.visible = True
        self.assertTrue(polygon.visible)
        self.assertEqual((1, 0, 0), polygon.color)
        self.wait()
        polygon.remove()

    def test_thickness(self):
        polygon = self.add_polygon()
        self.assertFalse(polygon.visible)
        polygon.thickness = 1.234
        polygon.visible = True
        self.assertTrue(polygon.visible)
        self.assertAlmostEqual(1.234, polygon.thickness)
        self.wait()
        polygon.remove()


if __name__ == '__main__':
    unittest.main()
