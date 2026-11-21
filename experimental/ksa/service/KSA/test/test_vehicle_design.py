import ksatest
from test_crew import EVA_KITTENS
from test_parachute import SITE


class TestVehicleDesign(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.designs = cls.ksa.vehicle_designs

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_designs(self):
        self.assertIn("Rocket", self.designs)
        for name in EVA_KITTENS:
            self.assertNotIn(name, self.designs)
        for name, design in self.designs.items():
            self.assertEqual(name, design.name)

    def test_default_design(self):
        self.assertTrue(self.designs["Rocket"].is_default)

    def test_mass(self):
        for design in self.designs.values():
            with self.subTest(design=design.name):
                self.assertGreater(design.part_count, 0)
                self.assertGreater(design.dry_mass, 0)
                self.assertGreaterEqual(design.mass, design.dry_mass)
                self.assertGreaterEqual(design.delta_v, 0)

    def test_matches_launched_vehicle(self):
        design = self.designs["Rocket"]
        vehicle = self.ksa.launch_vehicle(design.name, SITE)
        self.assertEqual(len(vehicle.parts), design.part_count)
        self.assertAlmostEqual(vehicle.mass, design.mass, delta=0.001 * design.mass)
        self.assertGreater(design.mass, design.dry_mass)
        self.assertGreater(design.delta_v, 0)
