import unittest
import time
import krpctest

class TestAntenna(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('RemoteTech')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.other_vessel = next(iter(cls.vessel.parts.decouplers)).decouple()
        cls.rt = cls.conn.remote_tech
        cls.antenna = cls.rt.antenna(next(iter(cls.vessel.parts.with_title('Reflectron KR-7'))))
        cls.Target = cls.rt.Target

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_antenna(self):
        self.assertEquals('Reflectron KR-7', self.antenna.part.title)
        self.assertFalse(self.antenna.has_connection)
        self.assertEqual(self.Target.none, self.antenna.target)

    def test_target_active_vessel(self):
        self.assertEqual(self.Target.none, self.antenna.target)
        self.antenna.target = self.Target.active_vessel
        time.sleep(0.1)
        self.assertEqual(self.Target.active_vessel, self.antenna.target)
        time.sleep(0.1)
        self.antenna.target = self.Target.none

    def test_target_body(self):
        self.assertEqual(self.Target.none, self.antenna.target)
        self.antenna.target_body = self.conn.space_center.bodies['Jool']
        time.sleep(0.1)
        self.assertEqual(self.Target.celestial_body, self.antenna.target)
        self.assertEqual('Jool', self.antenna.target_body.name)
        time.sleep(0.1)
        self.antenna.target = self.Target.none

    def test_target_ground_station(self):
        self.assertEqual(self.Target.none, self.antenna.target)
        self.antenna.target_ground_station = 'Mission Control'
        time.sleep(0.1)
        self.assertEqual(self.Target.ground_station, self.antenna.target)
        self.assertEqual('Mission Control', self.antenna.target_ground_station)
        time.sleep(0.1)
        self.antenna.target = self.Target.none

    def test_target_vessel(self):
        self.assertEqual('RemoteTech Ship', self.other_vessel.name)
        self.assertEqual(self.Target.none, self.antenna.target)
        self.antenna.target_vessel = self.other_vessel
        time.sleep(0.1)
        self.assertEqual(self.Target.vessel, self.antenna.target)
        self.assertEqual('RemoteTech Ship', self.antenna.target_vessel.name)
        time.sleep(0.1)
        self.antenna.target = self.Target.none

if __name__ == '__main__':
    unittest.main()
