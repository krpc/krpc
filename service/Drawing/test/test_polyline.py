import unittest

import krpctest


class TestPolyline(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    vertices = [(0, 0, 0), (0, 10, 0), (10, 10, 0)]

    def test_polyline(self):
        polyline = self.drawing.add_polyline(self.vertices, self.ref)
        self.assertEqual(self.vertices, polyline.vertices)
        self.assertEqual(self.ref, polyline.reference_frame)
        self.assertTrue(polyline.visible)
        self.assertEqual((1, 1, 1), polyline.color)
        self.assertAlmostEqual(0.1, polyline.thickness)
        self.assertFalse(polyline.start_arrow)
        self.assertFalse(polyline.end_arrow)
        polyline.remove()
        self.assertRaises(self.destroyed, polyline.remove)

    def test_vertices(self):
        polyline = self.drawing.add_polyline(self.vertices, self.ref)
        polyline.end_arrow = True
        self.wait()
        # A polyline can change its number of vertices
        vertices = [(0, 0, 0), (5, 0, 0), (5, 5, 0), (0, 5, 0), (0, 0, 5)]
        polyline.vertices = vertices
        self.wait()
        self.assertEqual(vertices, polyline.vertices)
        polyline.vertices = [(0, 0, 0)]
        self.wait()
        polyline.vertices = []
        self.wait()
        self.assertEqual([], polyline.vertices)
        polyline.remove()


if __name__ == "__main__":
    unittest.main()
