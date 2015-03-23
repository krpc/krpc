import unittest
import testingtools
import krpc

class TestPartsPart(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpc.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestParts')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_root_part(self):
        part = self.parts.root
        self.assertEqual('Mark1-2Pod', part.name)
        self.assertEqual('Mk1-2 Command Pod', part.title)
        self.assertEqual(3800, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual(None, part.parent)
        self.assertEqual(
            ['LT-1 Landing Struts', 'LT-1 Landing Struts', 'LT-1 Landing Struts',
             'Mk16-XL Parachute', 'Reflectron DP-10', 'TR-XL Stack Separator'],
            sorted(p.title for p in part.children))
        #stage
        #decouple stage
        self.assertFalse(part.massless)
        self.assertClose(4120, part.mass)
        self.assertClose(4000, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertEqual(45, part.impact_tolerance)
        self.assertClose(3400, part.max_temperature, 0.5)
        #resources
        modules = ['FlagDecal', 'ModuleCommand', 'ModuleReactionWheel',
                   'ModuleScienceContainer', 'ModuleScienceExperiment', 'ModuleTripLogger']
        if self.conn.space_center.far_available:
            modules.extend(['FARBasicDragModel', 'FARControlSys'])
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_engine(self):
        part = self.parts.with_title('S1 SRB-KD25k')[0]
        self.assertEqual('MassiveBooster', part.name)
        self.assertEqual('S1 SRB-KD25k', part.title)
        self.assertEqual(1800, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('TT-70 Radial Decoupler', part.parent.title)
        self.assertEqual(['Aerodynamic Nose Cone'], [p.title for p in part.children])
        #stage
        #decouple stage
        self.assertFalse(part.massless)
        self.assertClose(21750, part.mass)
        self.assertClose(3000, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3900, part.max_temperature, 0.5)
        self.assertEqual(7, part.impact_tolerance)
        #resources
        modules = ['FlagDecal', 'ModuleAnimateHeat', 'ModuleEnginesFX', 'ModuleTestSubject']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertNotEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_solar_panel(self):
        part = self.parts.with_title('Gigantor XL Solar Array')[0]
        self.assertEqual('largeSolarPanel', part.name)
        self.assertEqual('Gigantor XL Solar Array', part.title)
        self.assertEqual(3000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('FL-R1 RCS Fuel Tank', part.parent.title)
        self.assertEqual(0, len(part.children))
        #stage
        #decouple stage
        self.assertFalse(part.massless)
        self.assertClose(350, part.mass)
        self.assertClose(350, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3200, part.max_temperature, 0.5)
        self.assertEqual(8, part.impact_tolerance)
        #resources
        modules = ['ModuleDeployableSolarPanel']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertNotEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_sensor(self):
        part = self.parts.with_title('PresMat Barometer')[0]
        self.assertEqual('sensorBarometer', part.name)
        self.assertEqual('PresMat Barometer', part.title)
        self.assertEqual(3300, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax X200-8 Fuel Tank', part.parent.title)
        self.assertEqual(0, len(part.children))
        #stage
        #decouple stage
        self.assertTrue(part.massless)
        self.assertClose(0, part.mass)
        self.assertClose(0, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3200, part.max_temperature, 0.5)
        self.assertEqual(8, part.impact_tolerance)
        #resources
        modules = ['ModuleEnviroSensor', 'ModuleScienceExperiment']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertNotEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_decoupler(self):
        part = self.parts.with_title('TT-70 Radial Decoupler')[0]
        self.assertEqual('radialDecoupler2', part.name)
        self.assertEqual('TT-70 Radial Decoupler', part.title)
        self.assertEqual(700, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax Jumbo-64 Fuel Tank', part.parent.title)
        self.assertEqual(['S1 SRB-KD25k'], [p.title for p in part.children])
        #stage
        #decouple stage
        self.assertFalse(part.massless)
        self.assertClose(50, part.mass)
        self.assertClose(50, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3200, part.max_temperature, 0.5)
        self.assertEqual(8, part.impact_tolerance)
        #resources
        modules = ['ModuleAnchoredDecoupler', 'ModuleTestSubject']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertNotEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_light(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        self.assertEqual('spotLight1', part.name)
        self.assertEqual('Illuminator Mk1', part.title)
        self.assertEqual(100, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Aerodynamic Nose Cone', part.parent.title)
        self.assertEqual(0, len(part.children))
        #stage
        #decouple stage
        self.assertTrue(part.massless)
        self.assertClose(0, part.mass)
        self.assertClose(0, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3200, part.max_temperature, 0.5)
        self.assertEqual(8, part.impact_tolerance)
        #resources
        modules = ['ModuleLight']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertNotEqual(None, part.light)
        self.assertEqual(None, part.parachute)

    def test_parachute(self):
        part = self.parts.with_title('Mk16-XL Parachute')[0]
        self.assertEqual('parachuteLarge', part.name)
        self.assertEqual('Mk16-XL Parachute', part.title)
        self.assertEqual(850, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Mk1-2 Command Pod', part.parent.title)
        self.assertEqual(0, len(part.children))
        #stage
        #decouple stage
        self.assertFalse(part.massless)
        self.assertClose(300, part.mass)
        self.assertClose(300, part.dry_mass)
        self.assertGreater(part.temperature, 15)
        self.assertLess(part.temperature, 25)
        self.assertClose(3100, part.max_temperature, 0.5)
        self.assertEqual(12, part.impact_tolerance)
        #resources
        self.assertEqual(['ModuleParachute', 'ModuleTestSubject'], sorted(m.name for m in part.modules))

        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.solar_panel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.light)
        self.assertNotEqual(None, part.parachute)

if __name__ == "__main__":
    unittest.main()
