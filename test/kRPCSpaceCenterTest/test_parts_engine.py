import unittest
import testingtools
import krpc
import time

class EngineTestBase(object):

    engine_data = {
        'LV-T30 "Reliant" Liquid Fuel Engine': {
            'propellants': ['LiquidFuel', 'Oxidizer'],
            'gimballed': False,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 215000,
            'msl_isp': 280,
            'vac_isp': 300
        },
        'LV-T45 "Swivel" Liquid Fuel Engine': {
            'propellants': ['LiquidFuel', 'Oxidizer'],
            'gimballed': True,
            'gimbal_range': 3,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 200000,
            'msl_isp': 270,
            'vac_isp': 320
        },
        'LV-N "Nerv" Atomic Rocket Motor': {
            'propellants': ['LiquidFuel'],
            'gimballed': False,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 60000,
            'msl_isp': 185,
            'vac_isp': 800
        },
        'IX-6315 "Dawn" Electric Propulsion System': {
            'propellants': ['XenonGas', 'ElectricCharge'],
            'gimballed': False,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 2000,
            'msl_isp': 100,
            'vac_isp': 4200
        },
        'O-10 "Puff" MonoPropellant Fuel Engine': {
            'propellants': ['MonoPropellant'],
            'gimballed': False,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 20000,
            'msl_isp': 120,
            'vac_isp': 250
        },
        'RT-10 "Hammer" Solid Fuel Booster': {
            'propellants': ['SolidFuel'],
            'gimballed': False,
            'throttle_locked': True,
            'can_restart': False,
            'can_shutdown': False,
            'max_vac_thrust': 227000,
            'msl_isp': 150,
            'vac_isp': 162
        },
        'J-33 "Wheesley" Basic Jet Engine': {
            'propellants': ['IntakeAir', 'LiquidFuel'],
            'gimballed': True,
            'gimbal_range': 1,
            'throttle_locked': False,
            'can_restart': True,
            'can_shutdown': True,
            'max_vac_thrust': 115000,
            'msl_isp': 19200,
            'vac_isp': 19200
        }
    }

    @classmethod
    def add_engine_data(cls, title, data):
        for k,v in data.items():
            cls.engine_data[title][k] = v

    def get_engine(self, title):
        return next(iter(filter(lambda e: e.part.title == title, self.parts.engines)))

    def set_idle(self, engine):
        data = self.engine_data[engine.part.title]
        self.vessel.control.throttle = 0
        engine.active = False
        time.sleep(1)
        if not data['throttle_locked']:
            self.assertClose(0, engine.throttle)

    def set_throttle(self, engine, value):
        data = self.engine_data[engine.part.title]
        self.vessel.control.throttle = value
        engine.active = True
        time.sleep(1)
        if not data['throttle_locked']:
            self.assertClose(value, engine.throttle)

class EngineTest(EngineTestBase):

    def check_engine_properties(self, engine):
        """ Check engine properties independent of activity/throttle """
        data = self.engine_data[engine.part.title]
        self.assertClose(data['max_vac_thrust'], engine.max_vacuum_thrust)
        self.assertEqual(set(data['propellants']), set(engine.propellants))
        self.assertEqual(data['throttle_locked'], engine.throttle_locked)
        self.assertClose(data['msl_isp'], engine.kerbin_sea_level_specific_impulse)
        self.assertClose(data['vac_isp'], engine.vacuum_specific_impulse)
        self.assertEqual(data['can_restart'], engine.can_restart)
        self.assertEqual(data['can_shutdown'], engine.can_shutdown)
        self.assertEqual(data['gimballed'], engine.gimballed)
        self.assertFalse(engine.gimbal_locked)
        if not data['gimballed']:
            self.assertClose(engine.gimbal_range, 0)
        else:
            self.assertClose(engine.gimbal_range, data['gimbal_range'])

    def check_engine_idle(self, engine):
        """ Check engine properties when engine is deactivated """
        return
        data = self.engine_data[engine.part.title]
        self.assertFalse(engine.active)
        self.assertClose(engine.thrust_limit, 1)
        self.assertEqual(engine.thrust, 0)
        self.assertClose(engine.available_thrust, data['max_thrust'])
        self.assertClose(engine.max_thrust, data['max_thrust'])
        self.assertClose(engine.specific_impulse, 0)
        self.assertTrue(engine.has_fuel)

    def check_engine_active(self, engine, throttle):
        """ Check engine properties when engine is activated """
        data = self.engine_data[engine.part.title]
        self.assertTrue(engine.active)
        self.assertClose(throttle, engine.throttle)
        self.assertClose(engine.thrust_limit, 1)
        self.assertClose(engine.thrust, data['max_thrust'] * throttle, 500)
        self.assertClose(engine.available_thrust, data['max_thrust'], 500)
        self.assertClose(engine.max_thrust, data['max_thrust'], 500)
        self.assertClose(engine.specific_impulse, data['isp'], 1)

    def check_engine(self, engine):
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        for throttle in [1,0.6,0.2]:
            self.set_throttle(engine, throttle)
            self.check_engine_properties(engine)
            self.check_engine_active(engine, throttle)
        self.set_idle(engine)
        self.check_engine_properties(engine)
        engine.active = False

    def test_lfo_engine(self):
        engine = self.get_engine('LV-T30 "Reliant" Liquid Fuel Engine')
        self.check_engine(engine)

    def test_gimballed_lfo_engine(self):
        engine = self.get_engine('LV-T45 "Swivel" Liquid Fuel Engine')
        self.check_engine(engine)

    def test_nuclear_engine(self):
        engine = self.get_engine('LV-N "Nerv" Atomic Rocket Motor')
        self.check_engine(engine)

    def test_ion_engine(self):
        engine = self.get_engine('IX-6315 "Dawn" Electric Propulsion System')
        self.check_engine(engine)

    def test_rcs_engine(self):
        engine = self.get_engine('O-10 "Puff" MonoPropellant Fuel Engine')
        self.check_engine(engine)

    def test_srb_engine(self):
        engine = self.get_engine('RT-10 "Hammer" Solid Fuel Booster')
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        self.set_throttle(engine, 1)
        self.check_engine_properties(engine)
        self.check_engine_active(engine, 1)
        engine.active = False
        self.assertEqual(engine.active, True)
        self.check_engine_properties(engine)
        self.check_engine_active(engine, 1)
        self.set_idle(engine)

class TestPartsEngine(testingtools.TestCase, EngineTestBase):

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

    def test_thrust_limit(self):
        engine = self.get_engine('LV-T30 "Reliant" Liquid Fuel Engine')
        thrust = 201000

        engine.active = False
        engine.thrust_limit = 1
        self.vessel.control.throttle = 1
        self.assertEqual(engine.thrust_limit, 1)
        self.assertClose(engine.thrust, 0, 500)
        self.assertClose(engine.available_thrust, thrust, 500)
        self.assertClose(engine.max_thrust, thrust, 500)
        engine.active = True

        for throttle in [1,0.666,0.2,0]:
            self.vessel.control.throttle = throttle
            for thrust_limit in [1,0.8,0.333,0.1,0]:
                engine.thrust_limit = thrust_limit
                self.assertClose(engine.thrust_limit, thrust_limit)
                time.sleep(0.5)
                self.assertClose(engine.thrust, throttle * thrust_limit * thrust, 500)
                self.assertClose(engine.available_thrust, thrust_limit * thrust, 500)
                self.assertClose(engine.max_thrust, thrust, 500)

        engine.active = False
        engine.thrust_limit = 1
        self.assertClose(engine.thrust_limit, 1)

    def test_gimbal_lock(self):
        engine = self.get_engine('LV-T45 "Swivel" Liquid Fuel Engine')
        self.assertTrue(engine.gimballed)
        self.assertEqual(3, engine.gimbal_range)
        self.assertFalse(engine.gimbal_locked)
        engine.gimbal_locked = True
        time.sleep(0.1)
        self.assertTrue(engine.gimbal_locked)
        engine.gimbal_locked = False
        time.sleep(0.1)
        self.assertFalse(engine.gimbal_locked)

    def test_gimbal_limit(self):
        engine = self.get_engine('LV-T45 "Swivel" Liquid Fuel Engine')
        self.assertTrue(engine.gimballed)
        self.assertFalse(engine.gimbal_locked)
        self.assertEqual(1, engine.gimbal_limit)
        for limit in [1,0.6,0.234,0]:
            engine.gimbal_limit = limit
            self.assertClose(limit, engine.gimbal_limit)
            time.sleep(1)
        engine.gimbal_limit = 1
        engine.gimbal_locked = True
        self.assertEqual(0, engine.gimbal_limit)
        engine.gimbal_locked = False
        self.assertEqual(1, engine.gimbal_limit)

class TestPartsEngineMSL(testingtools.TestCase, EngineTest):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsEngine')
        testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestPartsEngineMSL')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.add_engine_data(
            'LV-T30 "Reliant" Liquid Fuel Engine',
            {'max_thrust': 201000, 'isp': 280.5})
        cls.add_engine_data(
            'LV-T45 "Swivel" Liquid Fuel Engine',
            {'max_thrust': 169200, 'isp': 270.7})
        cls.add_engine_data(
            'LV-N "Nerv" Atomic Rocket Motor',
            {'max_thrust': 14300, 'isp': 190.6})
        cls.add_engine_data(
            'IX-6315 "Dawn" Electric Propulsion System',
            {'max_thrust': 63, 'isp': 132.1})
        cls.add_engine_data(
            'O-10 "Puff" MonoPropellant Fuel Engine',
            {'max_thrust': 9700, 'isp': 121.2})
        cls.add_engine_data(
            'RT-10 "Hammer" Solid Fuel Booster',
            {'max_thrust': 210600, 'isp': 150.3})
        cls.add_engine_data(
            'J-33 "Wheesley" Basic Jet Engine',
            {'max_thrust': 115000, 'isp': 19200})

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_jet_engine(self):
        engine = self.get_engine('J-33 "Wheesley" Basic Jet Engine')
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        engine.active = True
        for throttle in [1,0.6,0.2]:
            self.vessel.control.throttle = throttle
            while abs(engine.throttle - throttle) > 0.1:
                time.sleep(1)
            self.check_engine_properties(engine)
            self.check_engine_active(engine, engine.throttle)
        engine.active = False

class TestPartsEngineVacuum(testingtools.TestCase, EngineTest):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsEngine')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 250000)
        cls.conn = krpc.connect(name='TestPartsEngineMSL')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.add_engine_data(
            'LV-T30 "Reliant" Liquid Fuel Engine',
            {'max_thrust': 215000, 'isp': 300})
        cls.add_engine_data(
            'LV-T45 "Swivel" Liquid Fuel Engine',
            {'max_thrust': 200000, 'isp': 320})
        cls.add_engine_data(
            'LV-N "Nerv" Atomic Rocket Motor',
            {'max_thrust': 60000, 'isp': 800})
        cls.add_engine_data(
            'IX-6315 "Dawn" Electric Propulsion System',
            {'max_thrust': 2000, 'isp': 4200})
        cls.add_engine_data(
            'O-10 "Puff" MonoPropellant Fuel Engine',
            {'max_thrust': 20000, 'isp': 250})
        cls.add_engine_data(
            'RT-10 "Hammer" Solid Fuel Booster',
            {'max_thrust': 227000, 'isp': 162})

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

if __name__ == "__main__":
    unittest.main()
