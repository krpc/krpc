import unittest

import krpctest


class TestSphere(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_sphere(self):
        sphere = self.drawing.add_sphere((1, 2, 3), 4, self.ref)
        self.assertEqual((1, 2, 3), sphere.center)
        self.assertAlmostEqual(4, sphere.radius)
        self.assertEqual(self.ref, sphere.reference_frame)
        self.assertTrue(sphere.visible)
        self.assertEqual((1, 1, 1), sphere.color)
        self.assertEqual("Legacy Shaders/Particles/Additive", sphere.material)
        sphere.remove()
        self.assertRaises(self.destroyed, sphere.remove)

    def test_properties(self):
        sphere = self.drawing.add_sphere((0, 0, 0), 1, self.ref, False)
        sphere.center = (0, 10, 0)
        sphere.radius = 5
        sphere.color = (0.2, 0.2, 0.5)
        sphere.visible = True
        self.wait()
        self.assertEqual((0, 10, 0), sphere.center)
        self.assertAlmostEqual(5, sphere.radius)
        self.assertAlmostEqual((0.2, 0.2, 0.5), sphere.color)
        sphere.remove()
        self.assertRaises(self.destroyed, setattr, sphere, "color", (0, 1, 0))


if __name__ == "__main__":
    unittest.main()
