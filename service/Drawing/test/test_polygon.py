import unittest
import time
import krpc
import krpctest

class TestPolygon(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.drawing = cls.conn.drawing
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ref = cls.vessel.reference_frame

    @classmethod
    def tearDown(cls):
        cls.conn.close()

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
        self.assertEquals(self.ref, polygon.reference_frame)
        self.assertTrue(polygon.visible)
        self.assertEquals((1, 1, 1), polygon.color)
        self.assertEquals("Particles/Additive", polygon.material)
        self.assertClose(0.1, polygon.thickness)
        time.sleep(0.5)
        polygon.remove()
        self.assertRaises(krpc.client.RPCError, polygon.remove)

    def test_color(self):
        polygon = self.add_polygon()
        self.assertFalse(polygon.visible)
        polygon.color = (1, 0, 0)
        polygon.visible = True
        self.assertTrue(polygon.visible)
        self.assertEquals((1, 0, 0), polygon.color)
        time.sleep(0.5)
        polygon.remove()

    def test_thickness(self):
        polygon = self.add_polygon()
        self.assertFalse(polygon.visible)
        polygon.thickness = 1.234
        polygon.visible = True
        self.assertTrue(polygon.visible)
        self.assertClose(1.234, polygon.thickness)
        time.sleep(0.5)
        polygon.remove()

if __name__ == '__main__':
    unittest.main()
