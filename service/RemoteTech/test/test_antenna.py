import unittest
import krpctest


@unittest.skipIf(not krpctest.TestCase.connect().remote_tech.available,
                 "RemoteTech not installed")
class TestAntenna(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('RemoteTech')
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.rt = cls.connect().remote_tech
        cls.vessel = cls.space_center.active_vessel
        cls.other_vessel = next(iter(cls.vessel.parts.decouplers)).decouple()
        cls.antenna = cls.rt.antenna(
            next(iter(cls.vessel.parts.with_title('Reflectron KR-7'))))

    def test_antenna(self):
        self.assertEqual('Reflectron KR-7', self.antenna.part.title)
        self.assertFalse(self.antenna.has_connection)
        self.assertEqual(self.rt.Target.none, self.antenna.target)

    def test_target_active_vessel(self):
        self.assertEqual(self.rt.Target.none, self.antenna.target)
        self.antenna.target = self.rt.Target.active_vessel
        self.wait()
        self.assertEqual(self.rt.Target.active_vessel, self.antenna.target)
        self.wait()
        self.antenna.target = self.rt.Target.none

    def test_target_body(self):
        self.assertEqual(self.rt.Target.none, self.antenna.target)
        self.antenna.target_body = self.space_center.bodies['Jool']
        self.wait()
        self.assertEqual(self.rt.Target.celestial_body, self.antenna.target)
        self.assertEqual('Jool', self.antenna.target_body.name)
        self.wait()
        self.antenna.target = self.rt.Target.none

    def test_target_ground_station(self):
        self.assertEqual(self.rt.Target.none, self.antenna.target)
        self.antenna.target_ground_station = 'Mission Control'
        self.wait()
        self.assertEqual(self.rt.Target.ground_station, self.antenna.target)
        self.assertEqual('Mission Control', self.antenna.target_ground_station)
        self.wait()
        self.antenna.target = self.rt.Target.none

    def test_target_vessel(self):
        self.assertEqual('RemoteTech Ship', self.other_vessel.name)
        self.assertEqual(self.rt.Target.none, self.antenna.target)
        self.antenna.target_vessel = self.other_vessel
        self.wait()
        self.assertEqual(self.rt.Target.vessel, self.antenna.target)
        self.assertEqual('RemoteTech Ship', self.antenna.target_vessel.name)
        self.wait()
        self.antenna.target = self.rt.Target.none


if __name__ == '__main__':
    unittest.main()
