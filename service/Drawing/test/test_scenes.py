import unittest
import krpctest


def draw_all(drawing, ref):
    line = drawing.add_line((0, 0, 0), (0, 10, 0), ref)
    line.end_arrow = True
    polygon = drawing.add_polygon([(0, 0, 0), (0, 10, 0), (10, 0, 0)], ref)
    polyline = drawing.add_polyline([(0, 0, 0), (0, 10, 0), (10, 0, 0)], ref)
    circle = drawing.add_circle((0, 0, 0), (0, 1, 0), 10, ref)
    arc = drawing.add_arc((0, 0, 0), (0, 1, 0), (1, 0, 0), 4, 10, ref)
    arc.end_arrow = True
    point = drawing.add_point((0, 0, 0), ref)
    text = drawing.add_text("Jebediah Kerman", ref, (0, 0, 0), (0, 0, 0, 1))
    text.face_camera = True
    line.dashed = True
    line.label = "Up"
    arc.colors = [(1, 0, 0), (0, 0, 1)]
    box = drawing.add_box((0, 0, 0), (5, 10, 5), ref)
    sphere = drawing.add_sphere((0, 0, 0), 2, ref)
    sphere.color = (0.2, 0.2, 0.2)
    return [
        line,
        polygon,
        polyline,
        circle,
        arc,
        point,
        text,
        box,
        sphere,
    ] + drawing.add_axes(ref)


def add_map_markers(test, drawing, ref):
    icons = drawing.MapMarkerIcon
    marker = drawing.add_map_marker((0, 0, 0), ref, icons.station, "Station")
    test.wait()
    for icon in (icons.apoapsis, icons.probe, icons.encounter, icons.ksc):
        marker.icon = icon
        test.wait()
        test.assertEqual(icon, marker.icon)
    test.assertEqual("Station", marker.caption)
    marker.remove()


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

    def test_orbit(self):
        mun = self.conn.space_center.bodies["Mun"]
        for orbit in (self.vessel.orbit, mun.orbit):
            path = self.drawing.add_orbit(orbit)
            path.end_arrow = True
            self.wait()
            self.assertEqual(orbit, path.orbit)
            path.remove()

    def test_map_marker(self):
        add_map_markers(self, self.drawing, self.vessel.reference_frame)

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
        mun = self.conn.space_center.bodies["Mun"]
        self.assertRaises(RuntimeError, self.drawing.add_orbit, mun.orbit)
        self.assertRaises(
            RuntimeError, self.drawing.add_map_marker, (0, 0, 0), self.ref
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

    def test_orbit(self):
        mun = self.conn.space_center.bodies["Mun"]
        path = self.drawing.add_orbit(mun.orbit)
        path.end_arrow = True
        self.wait()
        self.assertEqual(mun.orbit, path.orbit)
        path.remove()

    def test_map_marker(self):
        add_map_markers(self, self.drawing, self.ref)


if __name__ == "__main__":
    unittest.main()
