import unittest
import krpctest


def draw_all(drawing, ref):
    line = drawing.add_line((0, 0, 0), (0, 10, 0), ref)
    polygon = drawing.add_polygon([(0, 0, 0), (0, 10, 0), (10, 0, 0)], ref)
    text = drawing.add_text("Jebediah Kerman", ref, (0, 0, 0), (0, 0, 0, 1))
    return [line, polygon, text]


class TestMapView(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.drawing = cls.conn.drawing
        cls.camera = cls.conn.space_center.camera
        cls.mode = cls.conn.space_center.CameraMode
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.vessel = cls.conn.space_center.active_vessel
        cls.camera.mode = cls.mode.map

    @classmethod
    def tearDownClass(cls):
        cls.camera.mode = cls.mode.automatic

    def test_draw(self):
        for ref in (
            self.vessel.reference_frame,
            self.vessel.orbit.body.reference_frame,
        ):
            objects = draw_all(self.drawing, ref)
            self.wait()
            for obj in objects:
                self.assertEqual(ref, obj.reference_frame)
                self.assertTrue(obj.visible)
                obj.remove()
                self.assertRaises(self.destroyed, obj.remove)

    def test_switch_view(self):
        line = self.drawing.add_line((0, 0, 0), (0, 10, 0), self.vessel.reference_frame)
        self.wait()
        self.camera.mode = self.mode.automatic
        self.wait()
        self.assertEqual((0, 10, 0), line.end)
        self.camera.mode = self.mode.map
        self.wait()
        self.assertEqual((0, 10, 0), line.end)
        line.remove()

    def test_navball_marker(self):
        marker = self.drawing.add_navball_marker((0, 1, 0), self.vessel.reference_frame)
        self.wait()
        self.assertTrue(marker.visible)
        marker.remove()


class TestEditor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Kerbal X")
        cls.conn = cls.connect()
        cls.drawing = cls.conn.drawing
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.ref = cls.conn.space_center.editor.vessel.parts.root.reference_frame

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_draw(self):
        objects = draw_all(self.drawing, self.ref)
        self.wait()
        for obj in objects:
            self.assertEqual(self.ref, obj.reference_frame)
            self.assertTrue(obj.visible)
            obj.remove()
            self.assertRaises(self.destroyed, obj.remove)

    def test_flight_only(self):
        self.assertRaises(
            RuntimeError, self.drawing.add_navball_marker, (0, 1, 0), self.ref
        )
        self.assertRaises(
            RuntimeError, self.drawing.add_direction_from_com, (0, 1, 0), self.ref
        )

    def test_leaving_the_editor_destroys_drawings(self):
        line = self.drawing.add_line((0, 0, 0), (0, 10, 0), self.ref)
        self.leave_editor()
        self.enter_editor("VAB", craft="Kerbal X")
        self.assertRaises(self.destroyed, line.remove)


class TestTrackingStation(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls._set_game_scene(cls.conn.krpc.GameScene.tracking_station)
        cls.drawing = cls.conn.drawing
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.ref = cls.conn.space_center.bodies["Kerbin"].reference_frame

    @classmethod
    def tearDownClass(cls):
        cls._set_game_scene(cls.conn.krpc.GameScene.space_center)

    def test_draw(self):
        objects = draw_all(self.drawing, self.ref)
        self.wait()
        for obj in objects:
            self.assertEqual(self.ref, obj.reference_frame)
            self.assertTrue(obj.visible)
            obj.remove()
            self.assertRaises(self.destroyed, obj.remove)


if __name__ == "__main__":
    unittest.main()
