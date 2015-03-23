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
        engine.activated = False
        time.sleep(0.5)

    def set_throttle(self, engine, value):
        self.vessel.control.throttle = value
        engine.activated = True
        time.sleep(0.5)

    def check_engine_idle(self, engine, thrust, vac_isp, msl_isp, propellants, srb=False, gimballed=False):
        self.assertFalse(engine.activated)
        self.assertEqual(engine.thrust_limit, 1)
        self.assertEqual(engine.max_thrust, thrust)
        self.assertEqual(engine.thrust, 0)
        self.assertEqual(engine.specific_impulse, 0)
        self.assertEqual(engine.vacuum_specific_impulse, vac_isp)
        self.assertEqual(engine.kerbin_sea_level_specific_impulse, msl_isp)
        self.assertEqual(set(engine.propellants), set(propellants))
        self.assertTrue(engine.has_fuel)

        self.assertEqual(engine.is_throttle_locked, srb)
        self.assertEqual(engine.can_restart, not srb)
        self.assertEqual(engine.can_shutdown, not srb)

        self.assertEqual(engine.is_gimballed, gimballed)
        if not gimballed:
            self.assertEqual(engine.gimbal_range, 0)
            self.assertEqual(engine.gimbal_speed, 0)
            self.assertEqual(engine.gimbal_pitch, 0)
            self.assertEqual(engine.gimbal_yaw, 0)
            self.assertEqual(engine.gimbal_roll, 0)
            self.assertFalse(engine.gimbal_locked)

    def check_engine_throttle(self, engine, throttle, thrust, isp, vac_isp, msl_isp, propellants, srb=False, jet=False, gimballed=False):
        if jet:
            time.sleep(10)
        self.assertTrue(engine.activated)
        self.assertEqual(engine.thrust_limit, 1)
        self.assertEqual(engine.max_thrust, thrust)
        if not jet:
            self.assertClose(engine.thrust, thrust*throttle)
        else:
            self.assertGreater(engine.thrust, thrust*throttle*0.66)
            self.assertGreater(thrust*throttle*1.33, engine.thrust)
        self.assertClose(engine.specific_impulse, isp, 1)
        self.assertEqual(engine.vacuum_specific_impulse, vac_isp)
        self.assertEqual(engine.kerbin_sea_level_specific_impulse, msl_isp)
        self.assertEqual(set(engine.propellants), set(propellants))
        self.assertTrue(engine.has_fuel)

        self.assertEqual(engine.is_throttle_locked, srb)
        self.assertEqual(engine.can_restart, not srb)
        self.assertEqual(engine.can_shutdown, not srb)

        self.assertEqual(engine.is_gimballed, gimballed)
        if not gimballed:
            self.assertEqual(engine.gimbal_range, 0)
            self.assertEqual(engine.gimbal_speed, 0)
            self.assertEqual(engine.gimbal_pitch, 0)
            self.assertEqual(engine.gimbal_yaw, 0)
            self.assertEqual(engine.gimbal_roll, 0)
            self.assertFalse(engine.gimbal_locked)

    def test_lfo_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-T30 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 215
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

    def test_gimballed_lfo_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-T45 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 200
        isp = 321
        vac_isp = 370
        msl_isp = 320
        propellants = ['LiquidFuel', 'Oxidizer']
        def check_gimbal():
            self.assertEqual(engine.gimbal_range, 1)
            self.assertEqual(engine.gimbal_speed, 10)
            self.assertEqual(engine.gimbal_pitch, 0)
            self.assertEqual(engine.gimbal_yaw, 0)
            self.assertEqual(engine.gimbal_roll, 0)
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

    def test_gimballed_nuclear_engine(self):
        engine = filter(lambda e: e.part.title == 'LV-N Atomic Rocket Motor', self.parts.engines)[0]
        thrust = 60
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

    def test_jet_engine(self):
        engine = filter(lambda e: e.part.title == 'Basic Jet Engine', self.parts.engines)[0]
        thrust = 150
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

    def test_ion_engine(self):
        engine = filter(lambda e: e.part.title == 'PB-ION Electric Propulsion System', self.parts.engines)[0]
        thrust = 2
        isp = 4200
        propellants = ['ElectricCharge', 'XenonGas']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, isp, isp, propellants)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_throttle(engine, throttle, thrust, isp, isp, isp, propellants)
        self.set_idle(engine)

    def test_rcs_engine(self):
        engine = filter(lambda e: e.part.title == 'O-10 MonoPropellant Engine', self.parts.engines)[0]
        thrust = 20
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

    def test_srb_engine(self):
        engine = filter(lambda e: e.part.title == 'RT-10 Solid Fuel Booster', self.parts.engines)[0]
        thrust = 250
        isp = 225
        vac_isp = 240
        msl_isp = 225
        propellants = ['SolidFuel']
        self.set_idle(engine)
        self.check_engine_idle(engine, thrust, vac_isp, msl_isp, propellants, srb=True)
        self.set_throttle(engine, 1)
        self.check_engine_throttle(engine, 1, thrust, isp, vac_isp, msl_isp, propellants, srb=True)
        engine.activated = False
        self.assertEqual(engine.activated, True)
        self.set_idle(engine)

    def test_thrust_limit(self):
        engine = filter(lambda e: e.part.title == 'LV-T30 Liquid Fuel Engine', self.parts.engines)[0]
        thrust = 215

        engine.thrust_limit = 1
        self.assertEqual(engine.thrust_limit, 1)
        self.vessel.control.throttle = 1
        engine.activated = True

        for thrust_limit in [1,0.8,0.333,0.1,0]:
            engine.thrust_limit = thrust_limit
            self.assertClose(engine.thrust_limit, thrust_limit, 1)
            time.sleep(0.5)
            self.assertClose(engine.thrust, thrust_limit * thrust, 1)

        engine.activated = False
        engine.thrust_limit = 1
        self.assertEqual(engine.thrust_limit, 1)

if __name__ == "__main__":
    unittest.main()
