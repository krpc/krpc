import unittest
import krpctest
import krpc


class TestPartsFuelLines(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsFuelLines':
            cls.launch_vessel_from_vab('PartsFuelLines')
            cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts

    def test_fuel_line_part(self):
        part = self.parts.with_title('FTX-2 External Fuel Duct')[0]
        self.assertTrue(part.is_fuel_line)

    def test_single_fuel_line(self):
        part_from = self.parts.with_title('FL-T200 Fuel Tank')[0]
        part_to = self.parts.with_title('FL-T400 Fuel Tank')[0]
        self.assertEqual([], part_from.fuel_lines_from)
        self.assertEqual([part_to], part_from.fuel_lines_to)
        self.assertEqual([part_from], part_to.fuel_lines_from)
        self.assertEqual([], part_to.fuel_lines_to)

    def test_dual_fuel_lines(self):
        part_from = self.parts.with_title('FL-T100 Fuel Tank')[0]
        part_to = self.parts.with_title('FL-T800 Fuel Tank')[0]
        self.assertEqual([], part_from.fuel_lines_from)
        self.assertEqual([part_to, part_to], part_from.fuel_lines_to)
        self.assertEqual([part_from, part_from], part_to.fuel_lines_from)
        self.assertEqual([], part_to.fuel_lines_to)

    def test_asparagus(self):
        # outer_tanks -> middle tanks -> central tank
        central_tank = self.parts.with_title('Rockomax X200-32 Fuel Tank')[0]
        middle_tanks = self.parts.with_title('Rockomax X200-16 Fuel Tank')
        outer_tanks = self.parts.with_title('Rockomax X200-8 Fuel Tank')
        self.assertItemsEqual(middle_tanks, central_tank.fuel_lines_from)
        self.assertItemsEqual([], central_tank.fuel_lines_to)
        for tank in middle_tanks:
            self.assertEqual(1, len(tank.fuel_lines_from))
            self.assertTrue(tank.fuel_lines_from[0] in outer_tanks)
            self.assertItemsEqual([central_tank], tank.fuel_lines_to)
        for tank in outer_tanks:
            self.assertItemsEqual([], tank.fuel_lines_from)
            self.assertEqual(1, len(tank.fuel_lines_to))
            self.assertTrue(tank.fuel_lines_to[0] in middle_tanks)

    def test_error_on_fuel_line_part(self):
        part = self.parts.with_title('FTX-2 External Fuel Duct')[0]
        with self.assertRaises(krpc.error.RPCError) as cm:
            getattr(part, 'fuel_lines_to')
        self.assertTrue('Part is a fuel line' in str(cm.exception))
        with self.assertRaises(krpc.error.RPCError) as cm:
            getattr(part, 'fuel_lines_from')
        self.assertTrue('Part is a fuel line' in str(cm.exception))


if __name__ == '__main__':
    unittest.main()
