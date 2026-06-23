import unittest
import krpctest


class TestPartsFuelLines(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "PartsFuelLines":
            cls.launch_vessel_from_vab("PartsFuelLines")
            cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts

    def test_fuel_line_part(self):
        part = self.parts.with_name("fuelLine")[0]
        self.assertTrue(part.is_fuel_line)

    def test_single_fuel_line(self):
        part_from = self.parts.with_name("fuelTankSmall")[0]  # FL-T200
        part_to = self.parts.with_name("fuelTank")[0]  # FL-T400
        self.assertEqual([], part_from.fuel_lines_from)
        self.assertEqual([part_to], part_from.fuel_lines_to)
        self.assertEqual([part_from], part_to.fuel_lines_from)
        self.assertEqual([], part_to.fuel_lines_to)

    def test_dual_fuel_lines(self):
        part_from = self.parts.with_name("fuelTankSmallFlat")[0]  # FL-T100
        part_to = self.parts.with_name("fuelTank.long")[0]  # FL-T800
        self.assertEqual([], part_from.fuel_lines_from)
        self.assertEqual([part_to, part_to], part_from.fuel_lines_to)
        self.assertEqual([part_from, part_from], part_to.fuel_lines_from)
        self.assertEqual([], part_to.fuel_lines_to)

    def test_asparagus(self):
        # outer_tanks -> middle tanks -> central tank
        central_tank = self.parts.with_name("Rockomax32.BW")[0]  # X200-32
        middle_tanks = self.parts.with_name("Rockomax16.BW")  # X200-16
        outer_tanks = self.parts.with_name("Rockomax8BW")  # X200-8
        self.assertCountEqual(middle_tanks, central_tank.fuel_lines_from)
        self.assertCountEqual([], central_tank.fuel_lines_to)
        for tank in middle_tanks:
            self.assertEqual(1, len(tank.fuel_lines_from))
            self.assertTrue(tank.fuel_lines_from[0] in outer_tanks)
            self.assertCountEqual([central_tank], tank.fuel_lines_to)
        for tank in outer_tanks:
            self.assertCountEqual([], tank.fuel_lines_from)
            self.assertEqual(1, len(tank.fuel_lines_to))
            self.assertTrue(tank.fuel_lines_to[0] in middle_tanks)

    def test_error_on_fuel_line_part(self):
        part = self.parts.with_name("fuelLine")[0]
        with self.assertRaises(RuntimeError) as cm:
            getattr(part, "fuel_lines_to")
        self.assertTrue("Part is a fuel line" in str(cm.exception))
        with self.assertRaises(RuntimeError) as cm:
            getattr(part, "fuel_lines_from")
        self.assertTrue("Part is a fuel line" in str(cm.exception))


if __name__ == "__main__":
    unittest.main()
