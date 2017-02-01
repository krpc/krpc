import unittest
import krpctest


@unittest.skipIf(not krpctest.TestCase.connect().remote_tech.available,
                 "RemoteTech not installed")
class TestComms(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('RemoteTech')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        cls.other_vessel = next(iter(vessel.parts.decouplers)).decouple()
        cls.rt = cls.connect().remote_tech
        cls.comms = cls.rt.comms(vessel)

    def test_comms(self):
        self.assertEqual('RemoteTech', self.comms.vessel.name)
        self.assertTrue(self.comms.has_local_control)
        self.assertTrue(self.comms.has_flight_computer)
        self.assertTrue(self.comms.has_connection)
        self.assertTrue(self.comms.has_connection_to_ground_station)
        self.assertGreater(self.comms.signal_delay, 0)
        self.assertGreater(self.comms.signal_delay_to_ground_station, 0)
        self.assertGreater(
            self.comms.signal_delay_to_vessel(self.other_vessel), 0)
        self.assertItemsEqual(['Reflectron DP-10', 'Reflectron KR-7'],
                              [x.part.title for x in self.comms.antennas])


if __name__ == '__main__':
    unittest.main()
