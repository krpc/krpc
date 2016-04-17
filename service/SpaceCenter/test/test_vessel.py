import unittest
import testingtools
import krpc
import time

class TestVessel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Basic':
            testingtools.new_save()
            testingtools.remove_other_vessels()
            testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = testingtools.connect(name='TestVessel')
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

    def test_moment_of_inertia(self):
        self.assertClose((7696, 928, 7675), self.vessel.moment_of_inertia, error=1)

    def test_inertia_tensor(self):
        self.assertClose(
            [7696, 0, 0,
             0, 928, 0,
             0, 0, 7675],
            self.vessel.inertia_tensor, error=1)

    #def test_torque(self):
    #    self.assertEqual((5000,5000,5000), self.vessel.torque)

    def test_reaction_wheel_torque(self):
        self.assertEqual((5000,5000,5000), self.vessel.reaction_wheel_torque)
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = False
        self.assertEqual((0,0,0), self.vessel.reaction_wheel_torque)
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = True

    #def test_rcs_torque(self):
    #    self.assertEqual((0,0,0), self.vessel.rcs_torque)

    #def test_engine_torque(self):
    #    self.assertEqual((0,0,0), self.vessel.engine_torque)

    #def test_control_surface_torque(self):
    #    self.assertEqual((0,0,0), self.vessel.control_surface_torque)

class TestVesselEngines(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsEngine')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = testingtools.connect(name='TestVesselEngines')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.control = cls.vessel.control

        cls.engines = []
        for engine in cls.vessel.parts.engines:
            if 'IntakeAir' not in engine.propellants and engine.can_shutdown:
                cls.engines.append(engine)

        cls.engine_info = {
            'IX-6315 "Dawn" Electric Propulsion System': {
                'max_thrust': 2000,
                'available_thrust': 2000,
                'isp': 4200,
                'vac_isp': 4200,
                'msl_isp': 100
            },
            'LV-T45 "Swivel" Liquid Fuel Engine': {
                'max_thrust': 200000,
                'available_thrust': 200000,
                'isp': 320,
                'vac_isp': 320,
                'msl_isp': 270
            },
            'LV-T30 "Reliant" Liquid Fuel Engine': {
                'max_thrust': 215000,
                'available_thrust': 215000,
                'isp': 300,
                'vac_isp': 300,
                'msl_isp': 280
            },
            'LV-N "Nerv" Atomic Rocket Motor': {
                'max_thrust': 60000,
                'available_thrust': 60000,
                'isp': 800,
                'vac_isp': 800,
                'msl_isp': 185
            },
            'O-10 "Puff" MonoPropellant Fuel Engine': {
                'max_thrust': 20000,
                'available_thrust': 20000,
                'isp': 250,
                'vac_isp': 250,
                'msl_isp': 120
            },
            'RT-10 "Hammer" Solid Fuel Booster': {
                'max_thrust': 0,
                'available_thrust': 0,
                'isp': 195,
                'vac_isp': 195,
                'msl_isp': 170
            },
            'LV-909 "Terrier" Liquid Fuel Engine': {
                'max_thrust': 60000,
                'available_thrust': 0,
                'isp': 345,
                'vac_isp': 345,
                'msl_isp': 85
            },
            'J-33 "Wheesley" Basic Jet Engine': {
                'max_thrust': 0,
                'available_thrust': 0,
                'isp': 0,
                'vac_isp': 0,
                'msl_isp': 0
            }
        }
        max_thrusts = [x['max_thrust'] for x in cls.engine_info.values()]
        available_thrusts = [x['available_thrust'] for x in cls.engine_info.values()]
        isps = [x['isp'] for x in cls.engine_info.values()]
        vac_isps = [x['vac_isp'] for x in cls.engine_info.values()]
        msl_isps = [x['msl_isp'] for x in cls.engine_info.values()]
        cls.max_thrust = sum(max_thrusts)
        cls.available_thrust = sum(available_thrusts)
        cls.combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t,i in zip(max_thrusts, isps))
        cls.vac_combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t,i in zip(max_thrusts, vac_isps))
        cls.msl_combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t,i in zip(max_thrusts, msl_isps))

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

    def test_one_idle(self):
        self.control.throttle = 0
        title = 'LV-N "Nerv" Atomic Rocket Motor'
        engine = next(iter(filter(lambda x: x.part.title == title, self.vessel.parts.engines)))
        engine.active = True
        time.sleep(0.5)

        #FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        time.sleep(0.5)
        self.control.throttle = 0
        time.sleep(0.5)

        info = self.engine_info[title]
        self.assertClose(self.vessel.thrust, 0)
        self.assertClose(self.vessel.available_thrust, info['available_thrust'])
        self.assertClose(self.vessel.max_thrust, info['max_thrust'])
        self.assertClose(self.vessel.specific_impulse, info['isp'])
        self.assertClose(self.vessel.vacuum_specific_impulse, info['vac_isp'])
        self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, info['msl_isp'])
        engine.active = False
        time.sleep(0.5)

    def test_all_idle(self):
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = True
        time.sleep(0.5)

        #FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        time.sleep(0.5)
        self.control.throttle = 0
        time.sleep(0.5)

        self.assertClose(self.vessel.thrust, 0, 1)
        self.assertClose(self.vessel.available_thrust, self.available_thrust, 1)
        self.assertClose(self.vessel.max_thrust, self.max_thrust, 1)
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
            self.assertClose(self.vessel.thrust, throttle*self.available_thrust, 1)
            self.assertClose(self.vessel.available_thrust, self.available_thrust, 1)
            self.assertClose(self.vessel.max_thrust, self.max_thrust, 1)
            self.assertClose(self.vessel.specific_impulse, self.combined_isp, 1)
            self.assertClose(self.vessel.vacuum_specific_impulse, self.vac_combined_isp, 1)
            self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, self.msl_combined_isp, 1)
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = False
        time.sleep(1)

if __name__ == "__main__":
    unittest.main()
