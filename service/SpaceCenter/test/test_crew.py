import unittest

from krpctools.game.ksp import TestCase


class TestCrew(TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.sc = cls.connect().space_center
        for name in ("Roster Kerman", "Veteran Kerman"):
            if cls.sc.get_kerbal(name) is None:
                cls.sc.create_kerbal(name, "Pilot", True)

    def test_kerbals(self):
        kerbals = self.sc.kerbals
        names = [kerbal.name for kerbal in kerbals]
        self.assertIn("Roster Kerman", names)
        self.assertIn("Veteran Kerman", names)
        self.assertEqual(len(names), len(set(names)))
        for kerbal in kerbals:
            self.assertEqual(kerbal, self.sc.get_kerbal(kerbal.name))

    def test_get_kerbal_missing(self):
        self.assertIsNone(self.sc.get_kerbal("Ghost Kerman"))

    def test_available_kerbal(self):
        kerbal = self.sc.get_kerbal("Roster Kerman")
        self.assertEqual(self.sc.RosterStatus.available, kerbal.roster_status)
        self.assertFalse(kerbal.on_mission)
        self.assertFalse(kerbal.inactive)
        self.assertIsNone(kerbal.inactive_until)

    def test_experience_level(self):
        kerbal = self.sc.get_kerbal("Veteran Kerman")
        for experience, level in ((0, 0), (2, 1), (8, 2), (16, 3), (32, 4), (64, 5)):
            kerbal.experience = experience
            self.assertEqual(experience, kerbal.experience)
            self.assertEqual(level, kerbal.experience_level)


# The experience points each level needs
EXPERIENCE_LEVELS = (0, 2, 8, 16, 32, 64)


class TestCrewCareer(TestCase):
    """A career game has experience enabled, and Jebediah starts in a vessel."""

    @classmethod
    def setUpClass(cls):
        cls.new_save("krpctest_career", always_load=True)
        cls.sc = cls.connect().space_center

    def test_roster(self):
        kerbals = self.sc.kerbals
        self.assertEqual(4, len(kerbals))
        for kerbal in kerbals:
            self.assertEqual(self.sc.CrewMemberType.crew, kerbal.type)
            self.assertEqual(kerbal, self.sc.get_kerbal(kerbal.name))

    def test_on_mission(self):
        status = self.sc.RosterStatus
        kerbals = self.sc.kerbals
        assigned = [k for k in kerbals if k.roster_status == status.assigned]
        available = [k for k in kerbals if k.roster_status == status.available]
        self.assertEqual(1, len(assigned))
        self.assertEqual(3, len(available))
        self.assertTrue(assigned[0].on_mission)
        for kerbal in available:
            self.assertFalse(kerbal.on_mission)
            self.assertFalse(kerbal.inactive)
            self.assertIsNone(kerbal.inactive_until)

    def test_experience_level(self):
        for kerbal in self.sc.kerbals:
            expected = sum(
                1 for points in EXPERIENCE_LEVELS[1:] if kerbal.experience >= points
            )
            self.assertEqual(expected, kerbal.experience_level)


if __name__ == "__main__":
    unittest.main()
