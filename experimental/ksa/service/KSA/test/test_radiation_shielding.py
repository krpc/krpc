import ksatest

CAPSULE = "CoreCommandA_Prefab_MediumCapsuleVariantA"


class TestRadiationShielding(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.capsule = cls.vehicle.parts_with_name(CAPSULE)[0]

    def test_capsule_shield(self):
        # A 5 g/cm^2 hull, and a 500 kg polyethylene liner over 10 m^2
        self.assertAlmostEqual(
            50 + 500 / 10, self.capsule.shield_areal_density, places=3
        )
        self.assertAlmostEqual(500, self.capsule.shield_mass, places=3)

    def test_unshielded_parts(self):
        unshielded = [
            part for part in self.vehicle.parts if part.shield_areal_density == 0
        ]
        self.assertGreater(len(unshielded), 0)
        for part in unshielded:
            self.assertEqual(0, part.shield_mass)

    def test_tank_cover(self):
        for part in self.vehicle.parts:
            self.assertGreaterEqual(part.tank_cover, 0)
            self.assertLessEqual(part.tank_cover, 1)

    def test_dose_rate(self):
        unshielded = self.vehicle.flight.dose_rate
        self.assertGreater(unshielded, 0)
        # The capsule's shielding cuts the dose rate
        shielded = self.capsule.dose_rate
        self.assertGreater(shielded, 0)
        self.assertLess(shielded, unshielded * 0.9)
        # A part with no shielding and no tank cover gets the full dose rate
        for part in self.vehicle.parts:
            if part.shield_areal_density == 0 and part.tank_cover == 0:
                self.assertAlmostEqual(
                    unshielded, part.dose_rate, delta=unshielded * 1e-3
                )

    def test_crew_dose_rate(self):
        # A kitten in a seat receives the dose rate of the seat's part
        crew = self.vehicle.crew
        self.assertGreater(len(crew), 0)
        for kitten in crew:
            part = kitten.seat.part
            self.assertAlmostEqual(
                part.dose_rate, kitten.dose_rate, delta=part.dose_rate * 0.02
            )
