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


if __name__ == "__main__":
    unittest.main()
