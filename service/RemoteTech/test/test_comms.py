import unittest
import krpctest

class TestComms(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('RemoteTech')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.other_vessel = next(iter(cls.conn.space_center.active_vessel.parts.decouplers)).decouple()
        cls.rt = cls.conn.remote_tech
        cls.comms = cls.rt.comms(cls.conn.space_center.active_vessel)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_comms(self):
        self.assertEquals('RemoteTech', self.comms.vessel.name)
        self.assertTrue(self.comms.has_local_control)
        self.assertTrue(self.comms.has_flight_computer)
        self.assertTrue(self.comms.has_connection)
        self.assertTrue(self.comms.has_connection_to_ground_station)
        self.assertGreater(self.comms.signal_delay, 0)
        self.assertGreater(self.comms.signal_delay_to_ground_station, 0)
        self.assertGreater(self.comms.signal_delay_to_vessel(self.other_vessel), 0)
        self.assertEquals(['Reflectron DP-10', 'Reflectron KR-7'],
                          sorted(x.part.title for x in self.comms.antennas))

if __name__ == '__main__':
    unittest.main()
