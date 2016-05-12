import unittest
import time
import krpctest

class TestVessel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Vessel')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpctest.connect(cls)
        cls.Type = cls.conn.space_center.VesselType
        cls.Situation = cls.conn.space_center.VesselSituation
        cls.vessel = cls.conn.space_center.active_vessel
        cls.far = cls.conn.space_center.far_available

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_name(self):
        self.assertEqual('Vessel', self.vessel.name)
        self.vessel.name = 'Foo Bar Baz'
        self.assertEqual('Foo Bar Baz', self.vessel.name)
        self.vessel.name = 'Vessel'

    def test_type(self):
        self.assertEqual(self.Type.ship, self.vessel.type)
        self.vessel.type = self.Type.station
        self.assertEqual(self.Type.station, self.vessel.type)
        self.vessel.type = self.Type.ship

    def test_situation(self):
        self.assertEqual(self.Situation.orbiting, self.vessel.situation)

    def test_met(self):
        ut = self.conn.space_center.ut
        met = self.vessel.met
        time.sleep(1)
        self.assertClose(ut+1, self.conn.space_center.ut, error=0.5)
        self.assertClose(met+1, self.vessel.met, error=0.5)
        self.assertGreater(self.conn.space_center.ut, self.vessel.met)

    def test_mass(self):
        # 2645 kg dry mass
        # 260 l of monoprop at 4 kg/l
        # 180 l of LiquidFueld at 5 kg/l
        # 220 l of Oxidizer at 5 kg/l
        dry_mass = 3082
        resource_mass = 260 * 4 + 180 * 5 + 220 * 5
        self.assertEqual(dry_mass + resource_mass, self.vessel.mass)

    def test_dry_mass(self):
        # 2645 kg dry mass
        self.assertEqual(3082, self.vessel.dry_mass)

    def test_moment_of_inertia(self):
        self.assertClose((13411, 2219, 13366), self.vessel.moment_of_inertia, error=10)

    def test_inertia_tensor(self):
        self.assertClose(
            [13411, 0, 0,
             0, 2219, 0,
             0, 0, 13366],
            self.vessel.inertia_tensor, error=10)

    def test_available_torque(self):
        self.assertClose((5000, 5000, 5000), self.vessel.available_torque, error=1)

    def test_available_reaction_wheel_torque(self):
        self.assertClose((5000, 5000, 5000), self.vessel.available_reaction_wheel_torque)
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = False
        self.assertClose((0, 0, 0), self.vessel.available_reaction_wheel_torque)
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = True

    def test_available_rcs_torque(self):
        self.assertClose((0, 0, 0), self.vessel.available_rcs_torque)
        self.vessel.control.rcs = True
        time.sleep(0.1)
        self.assertClose((6005, 5575, 6005), self.vessel.available_rcs_torque, error=1)
        self.vessel.control.rcs = False
        time.sleep(0.1)
        self.assertClose((0, 0, 0), self.vessel.available_rcs_torque)

    def test_available_engine_torque(self):
        self.assertClose((0, 0, 0), self.vessel.available_engine_torque)

    def test_available_control_surface_torque(self):
        self.assertClose((0, 0, 0), self.vessel.available_control_surface_torque)

class TestVesselEngines(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsEngine')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpctest.connect(cls)
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
        cls.combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t, i in zip(max_thrusts, isps))
        cls.vac_combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t, i in zip(max_thrusts, vac_isps))
        cls.msl_combined_isp = sum(max_thrusts) / sum(t/i if i > 0 else 0 for t, i in zip(max_thrusts, msl_isps))

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_inactive(self):
        self.control.throttle = 0.0
        for engine in self.engines:
            engine.active = False
        time.sleep(0.5)
        self.assertClose(0, self.vessel.thrust)
        self.assertClose(0, self.vessel.available_thrust)
        self.assertClose(0, self.vessel.max_thrust)
        self.assertClose(0, self.vessel.specific_impulse)
        self.assertClose(0, self.vessel.vacuum_specific_impulse)
        self.assertClose(0, self.vessel.kerbin_sea_level_specific_impulse)
        self.assertClose((0, 0, 0), self.vessel.available_engine_torque)

    def test_one_idle(self):
        self.control.throttle = 0.0
        title = 'LV-N "Nerv" Atomic Rocket Motor'
        engine = self.vessel.parts.with_title(title)[0].engine
        engine.active = True
        time.sleep(0.5)

        #FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        time.sleep(0.5)
        self.control.throttle = 0.0
        time.sleep(0.5)

        info = self.engine_info[title]
        self.assertClose(0, self.vessel.thrust)
        self.assertClose(info['available_thrust'], self.vessel.available_thrust)
        self.assertClose(info['max_thrust'], self.vessel.max_thrust)
        self.assertClose(info['isp'], self.vessel.specific_impulse)
        self.assertClose(info['vac_isp'], self.vessel.vacuum_specific_impulse)
        self.assertClose(info['msl_isp'], self.vessel.kerbin_sea_level_specific_impulse)
        self.assertClose((0, 0, 0), self.vessel.available_engine_torque)
        engine.active = False
        time.sleep(0.5)

    def test_all_idle(self):
        self.control.throttle = 0.0
        for engine in self.engines:
            engine.active = True
        time.sleep(0.5)

        #FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        time.sleep(0.5)
        self.control.throttle = 0.0
        time.sleep(0.5)

        self.assertClose(0, self.vessel.thrust, 1)
        self.assertClose(self.available_thrust, self.vessel.available_thrust, 1)
        self.assertClose(self.max_thrust, self.vessel.max_thrust, 1)
        self.assertClose(self.combined_isp, self.vessel.specific_impulse, 1)
        self.assertClose(self.vac_combined_isp, self.vessel.vacuum_specific_impulse, 1)
        self.assertClose(self.msl_combined_isp, self.vessel.kerbin_sea_level_specific_impulse, 1)
        self.assertClose((0, 0, 0), self.vessel.available_engine_torque)
        for engine in self.engines:
            engine.active = False
        time.sleep(0.5)

    def test_throttle(self):
        for engine in self.engines:
            engine.active = True
        for throttle in (0.3, 0.7, 1):
            self.control.throttle = throttle
            time.sleep(1)
            self.assertClose(throttle*self.available_thrust, self.vessel.thrust, 1)
            self.assertClose(self.available_thrust, self.vessel.available_thrust, 1)
            self.assertClose(self.max_thrust, self.vessel.max_thrust, 1)
            self.assertClose(self.combined_isp, self.vessel.specific_impulse, 1)
            self.assertClose(self.vac_combined_isp, self.vessel.vacuum_specific_impulse, 1)
            self.assertClose(self.msl_combined_isp, self.vessel.kerbin_sea_level_specific_impulse, 1)
            self.assertGreater((0, 0, 0), self.vessel.available_engine_torque)
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = False
        time.sleep(1)

if __name__ == '__main__':
    unittest.main()
