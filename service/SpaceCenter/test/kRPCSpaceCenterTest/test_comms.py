import unittest
import testingtools
from testingtools import load_save
import krpc
import time

class TestComms(testingtools.TestCase):

    def setUp(self):
        self.conn = krpc.connect(name='TestComms')
        if not self.conn.space_center.remote_tech_available:
            self.skipTest('RemoteTech not installed')
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Comms')
        testingtools.remove_other_vessels()
        self.vessel = self.conn.space_center.active_vessel

    def tearDown(self):
        self.conn.close()

    def test_basic(self):
        base = self.vessel.control.activate_next_stage()[0]
        self.vessel = self.conn.space_center.active_vessel

        comms = self.vessel.comms

        self.assertFalse(comms.has_local_control)
        self.assertTrue(comms.has_flight_computer)
        self.assertTrue(comms.has_connection)
        self.assertTrue(comms.has_connection_to_ground_station)

        self.assertGreater(comms.signal_delay, 0)
        self.assertGreater(comms.signal_delay_to_ground_station, 0)

        self.assertClose(comms.signal_delay_to_vessel(self.vessel), 0)
        self.assertGreater(comms.signal_delay_to_vessel(base), 0)

        comms = base.comms

        self.assertFalse(comms.has_local_control)
        self.assertTrue(comms.has_flight_computer)
        self.assertTrue(comms.has_connection)
        self.assertTrue(comms.has_connection_to_ground_station)

        self.assertGreater(comms.signal_delay, 0)
        self.assertGreater(comms.signal_delay_to_ground_station, 0)

        self.assertGreater(comms.signal_delay_to_vessel(self.vessel), 0)
        self.assertClose(comms.signal_delay_to_vessel(base), 0)

if __name__ == "__main__":
    unittest.main()
