import unittest

import krpctest


class TestBox(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_box(self):
        box = self.drawing.add_box((1, 2, 3), (4, 5, 6), self.ref)
        self.assertEqual((1, 2, 3), box.center)
        self.assertEqual((4, 5, 6), box.size)
        self.assertEqual((0, 0, 0, 1), box.rotation)
        self.assertEqual(self.ref, box.reference_frame)
        self.assertTrue(box.visible)
        self.assertEqual((1, 1, 1), box.color)
        self.assertAlmostEqual(0.1, box.thickness)
        self.assertEqual("Legacy Shaders/Particles/Additive", box.material)
        box.remove()
        self.assertRaises(self.destroyed, box.remove)

    def test_properties(self):
        box = self.drawing.add_box((0, 0, 0), (1, 1, 1), self.ref, visible=False)
        box.center = (0, 5, 0)
        box.size = (3, 10, 3)
        box.rotation = (0, 0.7071068, 0, 0.7071068)
        box.color = (0, 1, 0)
        box.thickness = 0.5
        box.visible = True
        self.wait_for_frame()
        self.assertEqual((0, 5, 0), box.center)
        self.assertEqual((3, 10, 3), box.size)
        self.assertAlmostEqual((0, 0.7071068, 0, 0.7071068), box.rotation)
        self.assertEqual((0, 1, 0), box.color)
        self.assertAlmostEqual(0.5, box.thickness)
        box.remove()
        self.assertRaises(self.destroyed, setattr, box, "color", (1, 0, 0))


if __name__ == "__main__":
    unittest.main()
