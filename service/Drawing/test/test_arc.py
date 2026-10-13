import math
import unittest
import krpctest


class TestArc(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_arc(self):
        arc = self.drawing.add_arc(
            (1, 2, 3), (0, 0, 1), (1, 0, 0), 1.5 * math.pi, 10, self.ref
        )
        self.assertEqual((1, 2, 3), arc.center)
        self.assertEqual((0, 0, 1), arc.normal)
        self.assertEqual((1, 0, 0), arc.start_direction)
        self.assertAlmostEqual(1.5 * math.pi, arc.angle)
        self.assertAlmostEqual(10, arc.radius)
        self.assertEqual(self.ref, arc.reference_frame)
        self.assertTrue(arc.visible)
        self.assertEqual((1, 1, 1), arc.color)
        self.assertAlmostEqual(0.1, arc.thickness)
        arc.remove()
        self.assertRaises(self.destroyed, arc.remove)

    def test_properties(self):
        arc = self.drawing.add_arc((0, 0, 0), (0, 0, 1), (1, 0, 0), 1, 10, self.ref)
        arc.end_arrow = True
        arc.center = (1, 1, 1)
        arc.normal = (0, 1, 0)
        arc.start_direction = (0, 0, 1)
        arc.angle = -0.5
        arc.radius = 2
        self.wait()
        self.assertEqual((1, 1, 1), arc.center)
        self.assertEqual((0, 1, 0), arc.normal)
        self.assertEqual((0, 0, 1), arc.start_direction)
        self.assertAlmostEqual(-0.5, arc.angle)
        self.assertAlmostEqual(2, arc.radius)
        arc.remove()

    def test_large_angle(self):
        # An arc of more than a full turn is drawn as a circle
        arc = self.drawing.add_arc((0, 0, 0), (0, 0, 1), (1, 0, 0), 1e12, 10, self.ref)
        self.wait()
        self.assertAlmostEqual(1e12, arc.angle)
        arc.angle = -1e12
        self.wait()
        arc.remove()

    def test_invalid_plane(self):
        self.assertRaises(
            ValueError,
            self.drawing.add_arc,
            (0, 0, 0),
            (0, 0, 0),
            (1, 0, 0),
            1,
            10,
            self.ref,
        )
        self.assertRaises(
            ValueError,
            self.drawing.add_arc,
            (0, 0, 0),
            (0, 0, 1),
            (0, 0, 2),
            1,
            10,
            self.ref,
        )
        arc = self.drawing.add_arc((0, 0, 0), (0, 0, 1), (1, 0, 0), 1, 10, self.ref)
        with self.assertRaises(ValueError):
            arc.start_direction = (0, 0, 1)
        self.assertEqual((1, 0, 0), arc.start_direction)
        arc.remove()


class TestCircle(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_circle(self):
        circle = self.drawing.add_circle((1, 2, 3), (0, 1, 0), 10, self.ref)
        self.assertEqual((1, 2, 3), circle.center)
        self.assertEqual((0, 1, 0), circle.normal)
        self.assertAlmostEqual(10, circle.radius)
        self.assertEqual(self.ref, circle.reference_frame)
        self.assertTrue(circle.visible)
        circle.end_arrow = True
        circle.normal = (1, 0, 0)
        self.wait()
        self.assertEqual((1, 0, 0), circle.normal)
        circle.remove()
        self.assertRaises(self.destroyed, circle.remove)

    def test_zero_normal(self):
        self.assertRaises(
            ValueError, self.drawing.add_circle, (0, 0, 0), (0, 0, 0), 10, self.ref
        )


if __name__ == "__main__":
    unittest.main()
