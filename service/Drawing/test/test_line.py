import unittest
import time
import krpc
import krpctest

class TestLine(krpctest.TestCase):

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

    def add_line(self):
        return self.drawing.add_line((0, 0, 0), (0, 10, 0), self.ref, False)

    def test_line(self):
        line = self.drawing.add_line((10, 1, 2), (3, 10, 4), self.ref)
        self.assertEquals((10, 1, 2), line.start)
        self.assertEquals((3, 10, 4), line.end)
        self.assertEquals(self.ref, line.reference_frame)
        self.assertTrue(line.visible)
        self.assertEquals((1, 1, 1), line.color)
        self.assertEquals("Particles/Additive", line.material)
        self.assertClose(0.1, line.thickness)
        time.sleep(0.5)
        line.remove()
        self.assertRaises(krpc.client.RPCError, line.remove)

    def test_color(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.color = (1, 0, 0)
        line.visible = True
        self.assertTrue(line.visible)
        self.assertEquals((1, 0, 0), line.color)
        time.sleep(0.5)
        line.remove()

    def test_thickness(self):
        line = self.add_line()
        self.assertFalse(line.visible)
        line.thickness = 1.234
        line.visible = True
        self.assertTrue(line.visible)
        self.assertClose(1.234, line.thickness)
        time.sleep(0.5)
        line.remove()

if __name__ == '__main__':
    unittest.main()
