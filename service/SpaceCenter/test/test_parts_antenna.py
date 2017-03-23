import unittest
import krpctest


class TestPartsAntenna(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('PartsAntenna')
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.antennas = parts.antennas
        cls.fixed_antenna = parts.with_title(
            'RA-2 Relay Antenna')[0].antenna
        cls.deployable_antenna = parts.with_title(
            'HG-5 High Gain Antenna')[0].antenna
        cls.state = cls.connect().space_center.AntennaState

    def test_fixed_antenna(self):
        antenna = self.fixed_antenna
        self.assertEqual(self.state.deployed, antenna.state)
        self.assertFalse(antenna.deployable)
        self.assertTrue(antenna.deployed)
        self.assertTrue(antenna.can_transmit)
        self.assertRaises(RuntimeError, setattr,
                          antenna, 'deployed', True)
        self.assertEqual(2e9, antenna.power)
        self.assertTrue(antenna.combinable)
        self.assertEqual(0.75, antenna.combinable_exponent)
        self.assertAlmostEqual(0.35, antenna.packet_interval, places=5)
        self.assertEqual(1, antenna.packet_size)
        self.assertEqual(24, antenna.packet_resource_cost)

    def test_deployable_antenna(self):
        antenna = self.deployable_antenna
        self.assertEqual(self.state.retracted, antenna.state)
        self.assertTrue(antenna.deployable)
        self.assertFalse(antenna.deployed)
        self.assertTrue(antenna.can_transmit)
        self.assertEqual(5e6, antenna.power)
        self.assertTrue(antenna.combinable)
        self.assertEqual(0.75, antenna.combinable_exponent)
        self.assertAlmostEqual(0.35, antenna.packet_interval, places=5)
        self.assertEqual(2, antenna.packet_size)
        self.assertEqual(18, antenna.packet_resource_cost)

    def test_deploy(self):
        self.assertTrue(self.deployable_antenna.deployable)

        self.assertEqual(self.state.retracted, self.deployable_antenna.state)
        self.assertFalse(self.deployable_antenna.deployed)
        self.assertTrue(self.deployable_antenna.can_transmit)

        self.deployable_antenna.deployed = True

        self.wait()
        self.assertEqual(self.state.deploying, self.deployable_antenna.state)
        while self.deployable_antenna.state == self.state.deploying:
            self.wait()

        self.assertEqual(self.state.deployed, self.deployable_antenna.state)
        self.assertTrue(self.deployable_antenna.deployed)
        self.assertTrue(self.deployable_antenna.can_transmit)

        self.deployable_antenna.deployed = False

        self.wait()
        self.assertEqual(self.state.retracting, self.deployable_antenna.state)
        while self.deployable_antenna.state == self.state.retracting:
            self.wait()

        self.assertEqual(self.state.retracted, self.deployable_antenna.state)
        self.assertFalse(self.deployable_antenna.deployed)
        self.assertTrue(self.deployable_antenna.can_transmit)

    def test_control_deploy(self):
        self.assertFalse(self.control.antennas)
        self.control.antennas = True
        for antenna in self.antennas:
            if antenna.deployable:
                while antenna.state == self.state.deploying:
                    self.wait()
                self.assertTrue(antenna.deployed)
        self.assertTrue(self.control.antennas)
        self.control.antennas = False
        for antenna in self.antennas:
            if antenna.deployable:
                while antenna.state == self.state.retracting:
                    self.wait()
                self.assertFalse(antenna.deployed)
        self.assertFalse(self.control.antennas)

    def test_transmit(self):
        self.fixed_antenna.transmit()

    def test_cancel(self):
        self.fixed_antenna.cancel()

    def test_allow_partial(self):
        self.assertFalse(self.fixed_antenna.allow_partial)
        self.fixed_antenna.allow_partial = True
        self.wait()
        self.assertTrue(self.fixed_antenna.allow_partial)
        self.fixed_antenna.allow_partial = False
        self.wait()
        self.assertFalse(self.fixed_antenna.allow_partial)


class TestPartsAntennaBreak(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('PartsAntenna')
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.AntennaState
        cls.antenna = parts.with_title('Communotron 16')[0].antenna

    def test_break(self):
        self.antenna.deployed = True
        while self.antenna.state != self.state.deployed:
            self.wait()
        self.assertTrue(self.antenna.can_transmit)

        self.control.activate_next_stage()
        self.wait(3)

        self.assertEqual(self.state.broken, self.antenna.state)
        self.assertFalse(self.antenna.deployed)
        self.assertFalse(self.antenna.can_transmit)


if __name__ == '__main__':
    unittest.main()
