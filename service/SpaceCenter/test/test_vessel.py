import unittest
import krpc
import krpctest


class TestVessel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Vessel')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 100000)
        cls.space_center = cls.connect().space_center
        cls.Type = cls.space_center.VesselType
        cls.Situation = cls.space_center.VesselSituation
        cls.vessel = cls.space_center.active_vessel
        cls.far = cls.space_center.far_available

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

    def test_recoverable(self):
        self.assertFalse(self.vessel.recoverable)
        self.assertRaises(krpc.client.RPCError, self.vessel.recover)

    def test_met(self):
        ut = self.space_center.ut
        met = self.vessel.met
        self.wait(1)
        self.assertAlmostEqual(ut+1, self.space_center.ut, delta=0.5)
        self.assertAlmostEqual(met+1, self.vessel.met, delta=0.5)
        self.assertGreater(self.space_center.ut, self.vessel.met)

    def test_mass(self):
        # 260 l of monoprop at 4 kg/l
        # 180 l of LiquidFuel at 5 kg/l
        # 220 l of Oxidizer at 5 kg/l
        dry_mass = 3492
        resource_mass = 260 * 4 + 180 * 5 + 220 * 5
        self.assertAlmostEqual(
            dry_mass + resource_mass, self.vessel.mass, places=3)

    def test_dry_mass(self):
        self.assertAlmostEqual(3492, self.vessel.dry_mass, places=3)

    def test_moment_of_inertia(self):
        self.assertAlmostEqual(
            (13394, 2255, 13348), self.vessel.moment_of_inertia, delta=1)

    def test_inertia_tensor(self):
        self.assertAlmostEqual(
            [13394, 0, 0,
             0, 2255, 0,
             0, 0, 13348],
            self.vessel.inertia_tensor, delta=1)

    def test_available_torque(self):
        self.assertAlmostEqual(
            (5000, 5000, 5000), self.vessel.available_torque[0], delta=5)
        self.assertAlmostEqual(
            (-5000, -5000, -5000), self.vessel.available_torque[1], delta=5)

    def test_available_reaction_wheel_torque(self):
        self.assertAlmostEqual(
            (5000, 5000, 5000),
            self.vessel.available_reaction_wheel_torque[0])
        self.assertAlmostEqual(
            (-5000, -5000, -5000),
            self.vessel.available_reaction_wheel_torque[1])
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = False
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)),
            self.vessel.available_reaction_wheel_torque)
        for rw in self.vessel.parts.reaction_wheels:
            rw.active = True

    def test_available_rcs_torque(self):
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)), self.vessel.available_rcs_torque)
        self.vessel.control.rcs = True
        self.wait()
        self.assertAlmostEqual(
            (6005, 5575, 6005),
            self.vessel.available_rcs_torque[0], delta=5)
        self.assertAlmostEqual(
            (-6005, -5575, -6005),
            self.vessel.available_rcs_torque[1], delta=5)
        self.vessel.control.rcs = False
        self.wait()
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)), self.vessel.available_rcs_torque)

    def test_available_engine_torque(self):
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)), self.vessel.available_engine_torque)

    def test_available_control_surface_torque(self):
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)),
            self.vessel.available_control_surface_torque)

    def test_bounding_box(self):
        box = self.vessel.bounding_box(self.vessel.reference_frame)
        self.assertAlmostEqual((-1.55, -2.57, -1.55), box[0], places=2)
        self.assertAlmostEqual((1.55, 2.67, 1.55), box[1], places=2)


class TestVesselEngines(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsEngine')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 100000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control

        cls.engines = []
        for engine in cls.vessel.parts.engines:
            if 'IntakeAir' not in engine.propellant_names and \
               engine.can_shutdown:
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
                'max_thrust': 215000,
                'available_thrust': 215000,
                'isp': 320,
                'vac_isp': 320,
                'msl_isp': 250
            },
            'LV-T30 "Reliant" Liquid Fuel Engine': {
                'max_thrust': 240000,
                'available_thrust': 240000,
                'isp': 310,
                'vac_isp': 310,
                'msl_isp': 265
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
            },
            'CR-7 R.A.P.I.E.R. Engine': {
                'max_thrust': 0,
                'available_thrust': 0,
                'isp': 0,
                'vac_isp': 0,
                'msl_isp': 0
            }
        }
        max_thrusts = [x['max_thrust'] for x in cls.engine_info.values()]
        available_thrusts = [x['available_thrust']
                             for x in cls.engine_info.values()]
        isps = [x['isp'] for x in cls.engine_info.values()]
        vac_isps = [x['vac_isp'] for x in cls.engine_info.values()]
        msl_isps = [x['msl_isp'] for x in cls.engine_info.values()]
        cls.max_thrust = sum(max_thrusts)
        cls.available_thrust = sum(available_thrusts)
        cls.combined_isp = \
            sum(max_thrusts) / sum(t/i if i > 0 else 0
                                   for t, i in zip(max_thrusts, isps))
        cls.vac_combined_isp = \
            sum(max_thrusts) / sum(t/i if i > 0 else 0
                                   for t, i in zip(max_thrusts, vac_isps))
        cls.msl_combined_isp = \
            sum(max_thrusts) / sum(t/i if i > 0 else 0
                                   for t, i in zip(max_thrusts, msl_isps))

    def test_inactive(self):
        self.control.throttle = 0.0
        for engine in self.engines:
            engine.active = False
        self.wait(0.5)
        self.assertAlmostEqual(0, self.vessel.thrust)
        self.assertAlmostEqual(0, self.vessel.available_thrust)
        self.assertAlmostEqual(0, self.vessel.max_thrust)
        self.assertAlmostEqual(0, self.vessel.specific_impulse)
        self.assertAlmostEqual(0, self.vessel.vacuum_specific_impulse)
        self.assertAlmostEqual(
            0, self.vessel.kerbin_sea_level_specific_impulse)
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)), self.vessel.available_engine_torque)

    def test_one_idle(self):
        self.control.throttle = 0.0
        title = 'LV-N "Nerv" Atomic Rocket Motor'
        engine = self.vessel.parts.with_title(title)[0].engine
        engine.active = True
        self.wait(0.5)

        # FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        self.wait(0.5)
        self.control.throttle = 0.0
        self.wait(0.5)

        info = self.engine_info[title]
        self.assertAlmostEqual(0, self.vessel.thrust, places=3)
        self.assertAlmostEqual(
            info['available_thrust'], self.vessel.available_thrust, places=3)
        self.assertAlmostEqual(
            info['max_thrust'], self.vessel.max_thrust, places=3)
        self.assertAlmostEqual(
            info['isp'], self.vessel.specific_impulse, places=3)
        self.assertAlmostEqual(
            info['vac_isp'], self.vessel.vacuum_specific_impulse, places=3)
        self.assertAlmostEqual(
            info['msl_isp'],
            self.vessel.kerbin_sea_level_specific_impulse, places=3)
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)),
            self.vessel.available_engine_torque, places=3)
        engine.active = False
        self.wait(0.5)

    def test_all_idle(self):
        self.control.throttle = 0.0
        for engine in self.engines:
            engine.active = True
        self.wait(0.5)

        # FIXME: need to run the engines to update their has fuel status
        self.control.throttle = 0.1
        self.wait(0.5)
        self.control.throttle = 0.0
        self.wait(0.5)

        self.assertAlmostEqual(0, self.vessel.thrust, delta=1)
        self.assertAlmostEqual(
            self.available_thrust, self.vessel.available_thrust, delta=1)
        self.assertAlmostEqual(
            self.max_thrust, self.vessel.max_thrust, delta=1)
        self.assertAlmostEqual(
            self.combined_isp, self.vessel.specific_impulse, delta=1)
        self.assertAlmostEqual(
            self.vac_combined_isp,
            self.vessel.vacuum_specific_impulse, delta=1)
        self.assertAlmostEqual(
            self.msl_combined_isp,
            self.vessel.kerbin_sea_level_specific_impulse, delta=1)
        self.assertAlmostEqual(
            ((0, 0, 0), (0, 0, 0)), self.vessel.available_engine_torque)
        for engine in self.engines:
            engine.active = False
        self.wait(0.5)

    def test_throttle(self):
        for engine in self.engines:
            engine.active = True
        for throttle in (0.3, 0.7, 1):
            self.control.throttle = throttle
            self.wait(1)
            self.assertAlmostEqual(
                throttle*self.available_thrust, self.vessel.thrust, delta=1)
            self.assertAlmostEqual(
                self.available_thrust, self.vessel.available_thrust, delta=1)
            self.assertAlmostEqual(
                self.max_thrust, self.vessel.max_thrust, delta=1)
            self.assertAlmostEqual(
                self.combined_isp, self.vessel.specific_impulse, delta=1)
            self.assertAlmostEqual(
                self.vac_combined_isp,
                self.vessel.vacuum_specific_impulse, delta=1)
            self.assertAlmostEqual(
                self.msl_combined_isp,
                self.vessel.kerbin_sea_level_specific_impulse, delta=1)
            self.assertGreater(self.vessel.available_engine_torque, (0, 0, 0))
        self.control.throttle = 0
        for engine in self.engines:
            engine.active = False
        self.wait(1)


if __name__ == '__main__':
    unittest.main()
