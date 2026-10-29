import ksatest


class TestFuelLink(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        tanks = cls.vehicle.tanks
        cls.part_a = tanks[0].part
        cls.part_b = tanks[-1].part
        cls.conn.testing_tools.add_fuel_link(cls.part_a, cls.part_b)

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def link(self):
        (link,) = self.vehicle.fuel_links
        return link

    def test_link(self):
        link = self.link()
        self.assertEqual(self.part_a, link.part_a)
        self.assertEqual(self.part_b, link.part_b)
        self.assertEqual(self.ksa.FuelLinkFlow.both, link.flow)
        self.assertTrue(link.enabled)

    def test_flow(self):
        link = self.link()
        try:
            for flow in (self.ksa.FuelLinkFlow.a_to_b, self.ksa.FuelLinkFlow.b_to_a):
                link.flow = flow
                self.assertEqual(flow, link.flow)
        finally:
            link.flow = self.ksa.FuelLinkFlow.both
        self.assertEqual(self.ksa.FuelLinkFlow.both, link.flow)

    def test_enabled(self):
        link = self.link()
        try:
            link.enabled = False
            self.assertFalse(link.enabled)
        finally:
            link.enabled = True
        self.assertTrue(link.enabled)

    def test_removed_by_load(self):
        link = self.link()
        self.load_save()
        self.assertEqual([], self.vehicle_after_load().fuel_links)
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = link.enabled
        type(self).vehicle = self.vehicle_after_load()
        tanks = self.vehicle.tanks
        type(self).part_a = tanks[0].part
        type(self).part_b = tanks[-1].part
        self.conn.testing_tools.add_fuel_link(self.part_a, self.part_b)

    def vehicle_after_load(self):
        return self.ksa.active_vehicle
