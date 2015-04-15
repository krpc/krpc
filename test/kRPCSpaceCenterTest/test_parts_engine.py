import unittest
import testingtools
import krpc
import time

class TestPartsEngine(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsEngine')
        testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestPartsEngine')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def set_idle(self, engine):
        self.vessel.control.throttle = 0
        engine.active = False
        time.sleep(0.5)

    def set_throttle(self, engine, value):
        self.vessel.control.throttle = value
        engine.active = True
        time.sleep(0.5)

    def check_engine_idle(self, engine, thrust, vac_isp, msl_isp, propellants, srb=False, gimballed=False):
        self.assertFalse(engine.active)
        self.assertEqual(engine.thrust, 0)
        self.assertEqual(engine.available_thrust, thrust)
        self.assertEqual(engine.max_thrust, thrust)
        self.assertEqual(engine.thrust_limit, 1)
        self.assertEqual(engine.specific_impulse, 0)
        self.assertEqual(engine.vacuum_specific_impulse, vac_isp)
        self.assertEqual(engine.kerbin_sea_level_specific_impulse, msl_isp)
        self.assertEqual(set(engine.propellants), set(propellants))
        self.assertTrue(engine.has_fuel)

        self.assertEqual(engine.throttle_locked, srb)
        self.assertEqual(engine.can_restart, not srb)
        self.assertEqual(engine.can_shutdown, not srb)

        self.assertEqual(engine.gimballed, gimballed)
        if not gimballed:
            self.assertEqual(engine.gimbal_range, 0)
            self.assertFalse(engine.gimbal_locked)

    def check_engine_throttle(self, engine, throttle, thrust, isp, vac_isp, msl_isp, propellants, srb=False, jet=False, gimballed=False):
        if jet:
            time.sleep(10)
        self.assertTrue(engine.active)
        if not jet:
            self.assertClose(engine.thrust, thrust*throttle)
        else:
            self.assertGreater(engine.thrust, thrust*throttle*0.66)
            self.assertGreater(thrust*throttle*1.33, engine.thrust)
        self.assertEqual(engine.available_thrust, thrust)
        self.assertEqual(engine.max_thrust, thrust)
        self.assertEqual(engine.thrust_limit, 1)
        self.assertClose(engine.specific_impulse, isp, 1)
        self.assertEqual(engine.vacuum_specific_impulse, vac_isp)
        self.assertEqual(engine.kerbin_sea_level_specific_impulse, msl_isp)
        self.assertEqual(set(engine.propellants), set(propellants))
        self.assertTrue(engine.has_fuel)

        self.assertEqual(engine.throttle_locked, srb)
        self.assertEqual(engine.can_restart, not srb)
        self.assertEqual(engine.can_shutdown, not srb)

        self.assertEqual(engine.gimballed, gimballed)
        if not gimballed:
            self.assertEqual(engine.gimbal_range, 0)
            self.assertFalse(engine.gimbal_locked)

    def test_test(self):
        engine = filter(lambda e: e.part.title == 'LV-T30 Liquid Fuel Engine', self.parts.engines)[0]
        self.set_throttle(engine, 1)
        engine.active = False

    def test_lfo_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-T30 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 215000
        isp = 321
        vac_isp = 370
        msl_isp = 320
        propellants = ['LiquidFuel', 'Oxidizer']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, vac_isp, msl_isp, propellants)
        self.set_idle(engine)
        engine.active = False

    def test_gimballed_lfo_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-T45 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 200000
        isp = 321
        vac_isp = 370
        msl_isp = 320
        propellants = ['LiquidFuel', 'Oxidizer']
        def check_gimbal():
            self.assertEqual(engine.gimbal_range, 1)
            self.assertFalse(engine.gimbal_locked)
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        check_gimbal()
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, vac_isp, msl_isp, propellants, gimballed=True)
            check_gimbal()
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        engine.active = False

    def test_gimballed_nuclear_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-N Atomic Rocket Motor', self.parts.engines)[0]
        thrust = 60000
        isp = 230
        vac_isp = 800
        msl_isp = 220
        propellants = ['LiquidFuel', 'Oxidizer']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, vac_isp, msl_isp, propellants, gimballed=True)
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        engine.active = False

    def test_jet_engine(self):
        engine = filter(lambda e: e.part.title == 'Basic Jet Engine', self.parts.engines)[0]
        thrust = 150000
        isp = 1995
        vac_isp = 1000
        msl_isp = 2000
        propellants = ['LiquidFuel', 'IntakeAir']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, vac_isp, msl_isp, propellants, gimballed=True, jet=True)
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, gimballed=True)
        engine.active = False

    def test_ion_engine(self):
        engine = filter(lambda e: e.part.title == 'PB-ION Electric Propulsion System', self.parts.engines)[0]
        thrust = 2000
        isp = 4200
        propellants = ['ElectricCharge', 'XenonGas']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, isp, isp, propellants)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, isp, isp, propellants)
        self.set_idle(engine)
        engine.active = False

    def test_rcs_engine(self):
        engine = filter(lambda e: e.part.title == 'O-10 MonoPropellant Engine', self.parts.engines)[0]
        thrust = 20000
        isp = 222
        vac_isp = 290
        msl_isp = 220
        propellants = ['MonoPropellant']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, vac_isp, msl_isp, propellants)
        self.set_idle(engine)
        engine.active = False

    def test_srb_engine(self):
        engine = filter(lambda e: e.part.title == 'RT-10 Solid Fuel Booster', self.parts.engines)[0]
        thrust = 250000
        isp = 225
        vac_isp = 240
        msl_isp = 225
        propellants = ['SolidFuel']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, srb=True)
        self.set_throttle(engine, 1)
        self.check_engine_throttle(engine, 1, thrust, isp, vac_isp, msl_isp, propellants, srb=True)
        engine.active = False
        self.assertEqual(engine.active, True)
        self.set_idle(engine)

    def test_thrust_limit(self):
        engine = filter(lambda e: e.part.title == 'LV-T30 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 215000

        engine.thrust_limit = 1
        self.vessel.control.throttle = 1
        self.assertEqual(engine.thrust_limit, 1)
        self.assertEqual(engine.thrust, 0)
        self.assertEqual(engine.available_thrust, thrust)
        self.assertEqual(engine.max_thrust, thrust)
        engine.active = True

        for throttle in [1,0.666,0.2,0]:
            self.vessel.control.throttle = throttle
            for thrust_limit in [1,0.8,0.333,0.1,0]:
                engine.thrust_limit = thrust_limit
                self.assertClose(engine.thrust_limit, thrust_limit, 1)
                time.sleep(0.5)
                self.assertClose(engine.thrust, throttle * thrust_limit * thrust, 1)
                self.assertClose(engine.available_thrust, thrust_limit * thrust, 1)
                self.assertClose(engine.max_thrust, thrust, 1)

        engine.active = False
        engine.thrust_limit = 1
        self.assertEqual(engine.thrust_limit, 1)

    def test_gimbal(self):
        engine = filter(lambda e: e.part.title == 'LV-T45 Liquid Fuel Engine', self.parts.engines)[0]
        self.assertTrue(engine.gimballed)
        self.assertEquals(1, engine.gimbal_range)
        self.assertFalse(engine.gimbal_locked)
        engine.gimbal_locked = True
        time.sleep(0.1)
        self.assertTrue(engine.gimbal_locked)
        engine.gimbal_locked = False
        time.sleep(0.1)
        self.assertFalse(engine.gimbal_locked)

if __name__ == "__main__":
    unittest.main()
