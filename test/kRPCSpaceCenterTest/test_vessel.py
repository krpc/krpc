import unittest
import testingtools
import krpc
import time

class TestVessel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpc.connect(name='TestVessel')
        cls.vtype = cls.conn.space_center.VesselType
        cls.vsituation = cls.conn.space_center.VesselSituation
        cls.vessel = cls.conn.space_center.active_vessel
        cls.far = cls.conn.space_center.far_available

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_name(self):
        self.assertEqual('Basic', self.vessel.name)
        self.vessel.name = 'Foo Bar Baz';
        self.assertEqual('Foo Bar Baz', self.vessel.name)
        self.vessel.name = 'Basic';

    def test_type(self):
        self.assertEqual(self.vtype.ship, self.vessel.type)
        self.vessel.type = self.vtype.station
        self.assertEqual(self.vtype.station, self.vessel.type)
        self.vessel.type = self.vtype.ship

    def test_situation(self):
        self.assertEqual(self.vsituation.orbiting, self.vessel.situation)

    def test_met(self):
        ut = self.conn.space_center.ut
        met = self.vessel.met
        time.sleep(1)
        self.assertClose(ut+1, self.conn.space_center.ut, error=0.5)
        self.assertClose(met+1, self.vessel.met, error=0.5)
        self.assertGreater(self.conn.space_center.ut, self.vessel.met)

    def test_mass(self):
        # 2645 kg dry mass
        # 10 l of monoprop at 4 kg/l
        # 180 l of LiquidFueld at 5 kg/l
        # 220 l of Oxidizer at 5 kg/l
        dry_mass = 2645
        resource_mass = 10 * 4 + 180 * 5 + 220 * 5
        self.assertEqual(dry_mass + resource_mass, self.vessel.mass)

    def test_dry_mass(self):
        # 2645 kg dry mass
        self.assertEqual(2645, self.vessel.dry_mass)

class TestVesselEngines(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsEngine')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpc.connect(name='TestVesselEngines')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.control = cls.vessel.control

        cls.engines = []
        for engine in cls.vessel.parts.engines:
            if 'IntakeAir' not in engine.propellants and engine.can_shutdown:
                cls.engines.append(engine)

        thrusts = [200000, 215000, 20000, 2000, 60000]
        isps = [320, 300, 250, 4200, 800]
        vac_isps = [320, 300, 250, 4200, 800]
        msl_isps = [270, 280, 120, 100, 185]
        cls.thrust = sum(thrusts)
        cls.combined_isp = sum(thrusts) / sum(t/i for t,i in zip(thrusts, isps))
        cls.vac_combined_isp = sum(thrusts) / sum(t/i for t,i in zip(thrusts, vac_isps))
        cls.msl_combined_isp = sum(thrusts) / sum(t/i for t,i in zip(thrusts, msl_isps))

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_inactive(self):
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = False
        time.sleep(0.5)
        self.assertClose(self.vessel.thrust, 0)
        self.assertClose(self.vessel.available_thrust, 0)
        self.assertClose(self.vessel.max_thrust, 0)
        self.assertClose(self.vessel.specific_impulse, 0)
        self.assertClose(self.vessel.vacuum_specific_impulse, 0)
        self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, 0)

    def test_idle(self):
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = True
        time.sleep(0.5)
        self.assertClose(self.vessel.thrust, 0, 1)
        self.assertClose(self.vessel.available_thrust, self.thrust, 1)
        self.assertClose(self.vessel.max_thrust, self.thrust, 1)
        self.assertClose(self.vessel.specific_impulse, self.combined_isp, 1)
        self.assertClose(self.vessel.vacuum_specific_impulse, self.vac_combined_isp, 1)
        self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, self.msl_combined_isp, 1)
        for engine in self.engines:
            engine.active = False
        time.sleep(0.5)

    def test_throttle(self):
        for engine in self.engines:
            engine.active = True
        for throttle in [0.3,0.7,1]:
            self.control.throttle = throttle
            time.sleep(1)
            self.assertClose(self.vessel.thrust, throttle*self.thrust, 1)
            self.assertClose(self.vessel.available_thrust, self.thrust, 1)
            self.assertClose(self.vessel.max_thrust, self.thrust, 1)
            self.assertClose(self.vessel.specific_impulse, self.combined_isp, 1)
            self.assertClose(self.vessel.vacuum_specific_impulse, self.vac_combined_isp, 1)
            self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, self.msl_combined_isp, 1)
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = False
        time.sleep(1)

if __name__ == "__main__":
    unittest.main()
