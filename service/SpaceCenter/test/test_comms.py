import unittest
import math
import krpc
import krpc.error
import krpctest


class TestCommsSingleHop(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Probe')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 75e6)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.comms = cls.vessel.comms
        cls.CommLinkType = cls.space_center.CommLinkType
        cls.antennas = cls.vessel.parts.antennas
        cls.signal_strength = 0.885

    def test_properties(self):
        self.assertTrue(self.comms.can_communicate)
        self.assertTrue(self.comms.can_transmit_science)
        self.assertAlmostEqual(
            self.signal_strength, self.comms.signal_strength, places=2)
        self.assertAlmostEqual(0, self.comms.signal_delay, places=2)

    def test_power(self):
        powers = [x.power for x in self.antennas]
        exponents = [x.combinable_exponent for x in self.antennas]
        exponent = sum(power * exponent for power, exponent
                       in zip(powers, exponents)) / sum(powers)
        power = max(powers) * sum(power/max(powers)
                                  for power in powers) ** exponent
        self.assertAlmostEqual(power, self.comms.power, places=4)

    def test_control_path(self):
        path = self.comms.control_path
        self.assertEqual(1, len(path))
        link = path[0]
        self.assertEqual(self.CommLinkType.home, link.type)
        self.assertAlmostEqual(
            self.signal_strength, link.signal_strength, places=2)
        # Start is vessel
        start = link.start
        self.assertEqual('probeStackSmall', start.name)
        self.assertFalse(start.is_home)
        self.assertFalse(start.is_control_point)
        self.assertTrue(start.is_vessel)
        self.assertEqual('Probe', start.vessel.name)
        # End is kerbin base station
        end = link.end
        self.assertTrue(end.name.startswith('Kerbin:'))
        self.assertTrue(end.is_home)
        self.assertTrue(end.is_control_point)
        self.assertFalse(end.is_vessel)
        self.assertRaises(krpc.error.RPCError, getattr, end, 'vessel')


class TestCommsMultiHop(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('MediumRangeProbe')
        cls.remove_other_vessels()
        cls.set_orbit('Sun', 14e9 + 16e9, 0, 0, 0, 0, math.pi, 0)
        cls.launch_vessel_from_vab('ShortRangeProbe')
        cls.set_orbit('Sun', 14e9 + 16e9 + 100, 0, 0, 0, 0, math.pi, 0)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.comms = cls.vessel.comms
        cls.CommLinkType = cls.space_center.CommLinkType

    def test_properties(self):
        self.assertTrue(self.comms.can_communicate)
        self.assertTrue(self.comms.can_transmit_science)
        self.assertAlmostEqual(0.175, self.comms.signal_strength, places=2)
        self.assertAlmostEqual(0, self.comms.signal_delay, places=4)

    def test_control_path(self):
        path = self.comms.control_path
        self.assertEqual(2, len(path))
        link0 = path[0]
        link1 = path[1]
        self.assertEqual(self.CommLinkType.relay, link0.type)
        self.assertEqual(self.CommLinkType.home, link1.type)
        self.assertAlmostEqual(1, link0.signal_strength, places=2)
        self.assertAlmostEqual(0.175, link1.signal_strength, places=2)
        # Start is vessel
        start = link0.start
        self.assertEqual('probeCoreOcto2', start.name)
        self.assertFalse(start.is_home)
        self.assertFalse(start.is_control_point)
        self.assertTrue(start.is_vessel)
        self.assertEqual('ShortRangeProbe', start.vessel.name)
        # Mid is relay satellite
        mid = link0.end
        self.assertEqual(mid, link1.start)
        self.assertEqual('probeCoreOcto2 (MediumRangeProbe)', mid.name)
        self.assertFalse(mid.is_home)
        self.assertFalse(mid.is_control_point)
        self.assertTrue(mid.is_vessel)
        self.assertEqual('MediumRangeProbe', mid.vessel.name)
        # End is kerbin base station
        end = link1.end
        self.assertTrue(end.name.startswith('Kerbin:'))
        self.assertTrue(end.is_home)
        self.assertTrue(end.is_control_point)
        self.assertFalse(end.is_vessel)
        self.assertRaises(krpc.error.RPCError, getattr, end, 'vessel')


if __name__ == '__main__':
    unittest.main()
