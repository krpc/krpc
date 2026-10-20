import math
import unittest

import krpctest
from krpctest.geometry import norm


class TestEditor(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Staging")
        cls.space_center = cls.connect().space_center
        cls.facilities = cls.connect().space_center.EditorFacility

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_vessel(self):
        vessel = self.space_center.editor.vessel
        self.assertIsNotNone(vessel)
        self.assertEqual("Staging", vessel.name)
        self.assertEqual(self.facilities.vab, vessel.facility)
        self.assertGreater(vessel.mass, 0)
        self.assertGreater(vessel.mass, vessel.dry_mass)
        self.assertGreater(vessel.cost, 0)
        self.assertEqual(3, vessel.crew_capacity)
        box = vessel.bounding_box(vessel.reference_frame)
        self.assertGreater(min(b - a for a, b in zip(*box)), 0)

    def test_mass_properties(self):
        vessel = self.space_center.editor.vessel
        moi = vessel.moment_of_inertia
        self.assertEqual(3, len(moi))
        self.assertGreater(min(moi), 0)
        tensor = vessel.inertia_tensor
        self.assertEqual(9, len(tensor))
        self.assertAlmostEqual(moi[0], tensor[0], delta=1)
        self.assertAlmostEqual(moi[1], tensor[4], delta=1)
        self.assertAlmostEqual(moi[2], tensor[8], delta=1)

    def test_reference_frame(self):
        vessel = self.space_center.editor.vessel
        frame = vessel.reference_frame
        self.assertAlmostEqual((0, 0, 0), vessel.position(frame), places=3)
        self.assertAlmostEqual((0, 1, 0), vessel.direction(frame), places=3)
        rotation = vessel.rotation(frame)
        self.assertAlmostEqual(1, abs(rotation[3]), places=3)
        # The bounding box surrounds the center of mass
        lower, upper = vessel.bounding_box(frame)
        for i in range(3):
            self.assertLess(lower[i], 0)
            self.assertGreater(upper[i], 0)

    def test_reference_frame_is_root_part(self):
        # The vessel frame has the root part's axes, offset to the center of mass
        vessel = self.space_center.editor.vessel
        root = vessel.parts.root
        frame = vessel.reference_frame
        root_frame = root.reference_frame
        self.assertGreater(norm(vessel.position(root_frame)), 0.01)
        self.assertAlmostEqual(root.direction(frame), vessel.direction(frame), places=3)
        self.assertAlmostEqual(
            vessel.position(root_frame),
            self.space_center.transform_position((0, 0, 0), frame, root_frame),
            places=3,
        )

    def test_surface_reference_frame(self):
        # Staging is a vertical stack, so it points up the editor
        vessel = self.space_center.editor.vessel
        frame = vessel.surface_reference_frame
        self.assertAlmostEqual((0, 0, 0), vessel.position(frame), places=3)
        self.assertAlmostEqual((1, 0, 0), vessel.direction(frame), places=3)

    def test_simulated_flight(self):
        vessel = self.space_center.editor.vessel
        flight = vessel.flight()
        self.assertAlmostEqual((0, 0, 0), flight.center_of_mass, places=3)
        self.assertAlmostEqual((1, 0, 0), flight.direction, places=3)
        self.assertAlmostEqual(90, flight.pitch, places=1)
        self.assertEqual(vessel.surface_reference_frame, flight.surface_reference_frame)
        frame_flight = vessel.flight(vessel.reference_frame)
        self.assertAlmostEqual((0, 1, 0), frame_flight.direction, places=3)
        self.assertNotEqual(flight, vessel.flight())

    def test_simulated_flight_attitude(self):
        vessel = self.space_center.editor.vessel
        flight = vessel.flight()
        other = vessel.flight()
        # Lay the stack on its side: the simulated up is the vessel's -z axis
        side = math.sqrt(0.5)
        surface = self.space_center.ReferenceFrame.create_relative(
            vessel.reference_frame, rotation=(0, side, 0, side)
        )
        try:
            self.assertAlmostEqual(
                (0, 0, -1),
                self.space_center.transform_direction(
                    (1, 0, 0), surface, vessel.reference_frame
                ),
                places=3,
            )
            fixed = vessel.flight(vessel.reference_frame)
            flight.surface_reference_frame = surface
            fixed.surface_reference_frame = surface
            self.assertEqual(surface, flight.surface_reference_frame)
            # The default output frame follows the simulated surface, so the vessel
            # reads as flying level
            self.assertAlmostEqual(0, flight.pitch, delta=0.1)
            self.assertAlmostEqual(0, flight.direction[0], places=3)
            self.assertAlmostEqual((0, 0, 0), flight.center_of_mass, places=3)
            # A given output frame does not move with the simulated surface
            self.assertAlmostEqual((0, 1, 0), fixed.direction, places=3)
            # Each object holds its own attitude
            self.assertAlmostEqual(90, other.pitch, places=1)
            self.assertAlmostEqual((1, 0, 0), other.direction, places=3)
        finally:
            surface.remove()
        self.assertRaises(
            self.connect().krpc.ObjectDestroyedException, getattr, flight, "pitch"
        )

    def test_reference_frame_survives_load_vessel(self):
        editor = self.space_center.editor
        frame = editor.vessel.reference_frame
        self._stage_craft("Parts", "VAB", None)
        try:
            editor.load_vessel("VAB", "Parts")
            self.assertAlmostEqual((0, 0, 0), editor.vessel.position(frame), places=3)
        finally:
            editor.load_vessel("VAB", "Staging")

    def test_name_and_description(self):
        vessel = self.space_center.editor.vessel
        name = vessel.name
        description = vessel.description
        try:
            vessel.name = "Renamed"
            vessel.description = "A vessel"
            self.assertEqual("Renamed", vessel.name)
            self.assertEqual("A vessel", vessel.description)
        finally:
            vessel.name = name
            vessel.description = description

    def test_facility(self):
        self.assertEqual(self.facilities.vab, self.space_center.editor.facility)

    def test_load_vessel_not_found(self):
        self.assertRaises(
            ValueError, self.space_center.editor.load_vessel, "VAB", "DoesNotExist"
        )

    def test_load_vessel(self):
        editor = self.space_center.editor
        self._stage_craft("Parts", "VAB", None)
        try:
            editor.load_vessel("VAB", "Parts")
            self.assertEqual("Parts", editor.vessel.name)
        finally:
            editor.load_vessel("VAB", "Staging")
        self.assertEqual("Staging", editor.vessel.name)


class TestEditorSPH(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("SPH", craft="Aero")
        cls.space_center = cls.connect().space_center
        cls.facilities = cls.connect().space_center.EditorFacility

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_vessel(self):
        vessel = self.space_center.editor.vessel
        self.assertEqual("Aero", vessel.name)
        self.assertEqual(self.facilities.sph, vessel.facility)
        self.assertEqual(self.facilities.sph, self.space_center.editor.facility)

    def test_facility_follows_the_editor(self):
        # The editor keeps the vessel when moving between the VAB and the SPH, and
        # the vessel then reports the editor it is in, not the one it was designed in.
        self.enter_editor("VAB")
        try:
            self.assertEqual(self.facilities.vab, self.space_center.editor.facility)
            self.assertEqual(
                self.facilities.vab, self.space_center.editor.vessel.facility
            )
        finally:
            self.enter_editor("SPH", craft="Aero")


class TestEditorLaunchVessel(krpctest.TestCase):
    """Test launching the vessel being constructed, which leaves the editor, so each
    test opens the editor for itself."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center

    def test_launch_vessel(self):
        # The whole editor workflow: open the editor, load a vessel, read its design,
        # then launch the vessel that is being constructed.
        self.enter_editor("VAB", craft="Staging")
        editor = self.space_center.editor
        self.assertGreater(len(editor.vessel.parts.all), 0)
        self.assertGreater(editor.vessel.mass, 0)
        editor.launch_vessel("LaunchPad")
        vessel = self.space_center.active_vessel
        # The launched vessel is the one that was in the editor, not the auto-saved
        # craft file it was written to on the way out.
        self.assertEqual("Staging", vessel.name)
        self.assertGreater(len(vessel.parts.all), 0)

    def test_launch_edited_vessel(self):
        # A vessel renamed through the API launches under the name the API set.
        self.enter_editor("VAB", craft="Staging")
        editor = self.space_center.editor
        editor.vessel.name = "Renamed In Editor"
        editor.launch_vessel("LaunchPad")
        self.assertEqual("Renamed In Editor", self.space_center.active_vessel.name)

    def test_mass_properties_match_flight_staging(self):
        # Staging.craft has a mix of part kinds; a point-mass model can look
        # plausible until it is compared with the same vessel in flight.
        self._assert_mass_properties_match_flight("Staging")

    def test_mass_properties_match_flight_rover(self):
        # A rover is small enough that a part's own inertia is most of the
        # vessel figure rather than a correction to the parallel-axis term.
        self._assert_mass_properties_match_flight("Rover")

    def test_part_masses_match_flight(self):
        # Parts.craft carries the two cases a vessel-wide figure hides: physicsless
        # parts, whose mass physics puts on the part they hang off, and a crewed pod,
        # where a Kerbal weighs what they carry as well as themselves.
        self.enter_editor("VAB", craft="Parts")
        editor_parts = self.space_center.editor.vessel.parts.all
        editor_mass = sum(part.mass for part in editor_parts)
        self.space_center.editor.launch_vessel("LaunchPad")
        flight_parts = self.space_center.active_vessel.parts.all
        self.assertAlmostEqual(
            editor_mass, sum(part.mass for part in flight_parts), delta=1
        )

    def _assert_mass_properties_match_flight(self, craft):
        self.enter_editor("VAB", craft=craft)
        editor_vessel = self.space_center.editor.vessel
        mass = editor_vessel.mass
        moi = editor_vessel.moment_of_inertia
        root = editor_vessel.parts.root
        com = editor_vessel.position(root.reference_frame)
        part_com = root.center_of_mass(root.reference_frame)
        direction = editor_vessel.direction(editor_vessel.surface_reference_frame)
        box = editor_vessel.bounding_box(editor_vessel.reference_frame)
        self.space_center.editor.launch_vessel("LaunchPad")
        flight = self.space_center.active_vessel
        self.assertEqual(craft, flight.name)
        self.assertAlmostEqual(mass, flight.mass, delta=1)
        flight_moi = flight.moment_of_inertia
        for i in range(3):
            self.assertAlmostEqual(
                moi[i],
                flight_moi[i],
                delta=max(abs(moi[i]) * 0.05, 1),
            )
        root = flight.parts.root
        flight_com = flight.position(root.reference_frame)
        flight_part_com = root.center_of_mass(root.reference_frame)
        for i in range(3):
            self.assertAlmostEqual(com[i], flight_com[i], delta=0.5)
            self.assertAlmostEqual(part_com[i], flight_part_com[i], delta=0.2)
        # The vessel stands on the pad as it stood in the editor
        flight_direction = flight.direction(flight.surface_reference_frame)
        self.assertAlmostEqual(direction[0], flight_direction[0], delta=0.01)
        # The control point is the root part, so the vessel frames match. A rover's
        # box changes by a few percent on the pad
        flight_box = flight.bounding_box(flight.reference_frame)
        for i in range(3):
            size = box[1][i] - box[0][i]
            self.assertAlmostEqual(
                size,
                flight_box[1][i] - flight_box[0][i],
                delta=max(size * 0.1, 0.1),
            )


if __name__ == "__main__":
    unittest.main()
