import unittest

import krpctest


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
        self.assertGreater(min(vessel.size), 0)

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


if __name__ == "__main__":
    unittest.main()
