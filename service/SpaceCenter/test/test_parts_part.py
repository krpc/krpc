import unittest
import testingtools
import krpc

class TestPartsPart(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestParts')
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
        self.assertEqual([
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LY-10 Small Landing Gear',
            'Mk16-XL Parachute',
            'TR-XL Stack Separator'],
            sorted(p.title for p in part.children))
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(4120, part.mass)
        self.assertClose(4000, part.dry_mass)
        self.assertEqual(45, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = [
            'FlagDecal',
            'ModuleCommand',
            'ModuleConductionMultiplier',
            'ModuleReactionWheel',
            'ModuleScienceContainer',
            'ModuleScienceExperiment',
            'ModuleTripLogger',
            'TransferDialogSpawner'
        ]
        if self.conn.space_center.far_available:
            modules.extend(['FARBasicDragModel', 'FARControlSys'])
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertEqual(None, part.decoupler)
        self.assertEqual(None, part.docking_port)
        self.assertEqual(None, part.engine)
        self.assertEqual(None, part.landing_leg)
        self.assertEqual(None, part.launch_clamp)
        self.assertEqual(None, part.light)
        self.assertEqual(None, part.parachute)
        self.assertEqual(None, part.radiator)
        self.assertEqual(None, part.resource_converter)
        self.assertEqual(None, part.resource_harvester)
        self.assertNotEqual(None, part.reaction_wheel)
        self.assertEqual(None, part.sensor)
        self.assertEqual(None, part.solar_panel)

    def test_thermal(self):
        part = self.parts.root
        self.assertClose(300, part.temperature, error=50)
        self.assertClose(300, part.skin_temperature, error=50)
        self.assertEqual(1400, part.max_temperature)
        self.assertEqual(2400, part.max_skin_temperature)
        #TODO: not implemented. see issue #174
        #self.assertClose(300, part.external_temperature, error=50)
        self.assertClose(3546.5, part.thermal_mass, error=0.1)
        self.assertClose(13.5, part.thermal_skin_mass, error=0.1)
        self.assertClose(360, part.thermal_resource_mass, error=0.1)
        #TODO: add these property tests
        #part.thermal_conduction_flux
        #part.thermal_convection_flux
        #part.thermal_radiation_flux
        #part.thermal_internal_flux
        #part.thermal_skin_to_internal_flux

    def test_decoupler(self):
        part = self.parts.with_title('TT-70 Radial Decoupler')[0]
        self.assertEqual('radialDecoupler2', part.name)
        self.assertEqual('TT-70 Radial Decoupler', part.title)
        self.assertEqual(700, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax Jumbo-64 Fuel Tank', part.parent.title)
        self.assertEqual(['S1 SRB-KD25k "Kickback" Solid Fuel Booster'], [p.title for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(5, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(50, part.mass)
        self.assertClose(50, part.dry_mass)
        self.assertEqual(8, part.impact_tolerance)
        self.assertFalse(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleAnchoredDecoupler', 'ModuleTestSubject', 'ModuleToggleCrossfeed']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.decoupler)

    def test_docking_port(self):
        part = self.parts.with_title('Clamp-O-Tron Docking Port')[0]
        self.assertEqual('dockingPort2', part.name)
        self.assertEqual('Clamp-O-Tron Docking Port', part.title)
        self.assertEqual(280, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax X200-32 Fuel Tank', part.parent.title)
        self.assertEqual([], [p.title for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(4, part.stage) #TODO: why is this not -1? Docking ports aren't activated in stages?
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(50, part.mass)
        self.assertClose(50, part.dry_mass)
        self.assertEqual(10, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleDockingNode']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.docking_port)

    def test_engine(self):
        part = self.parts.with_title('S1 SRB-KD25k "Kickback" Solid Fuel Booster')[0]
        self.assertEqual('MassiveBooster', part.name)
        self.assertEqual('S1 SRB-KD25k "Kickback" Solid Fuel Booster', part.title)
        self.assertEqual(2700, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('TT-70 Radial Decoupler', part.parent.title)
        self.assertEqual(['Aerodynamic Nose Cone'], [p.title for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(6, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(23250, part.mass)
        self.assertClose(4500, part.dry_mass)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['FXModuleAnimateThrottle', 'ModuleEnginesFX', 'ModuleSurfaceFX', 'ModuleTestSubject']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.engine)

    def test_landing_leg(self):
        part = self.parts.with_title('LT-1 Landing Struts')[0]
        self.assertEqual('landingLeg1', part.name)
        self.assertEqual('LT-1 Landing Struts', part.title)
        self.assertEqual(440, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Mk1-2 Command Pod', part.parent.title)
        self.assertEqual([], [p.title for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(50, part.mass)
        self.assertClose(50, part.dry_mass)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleLandingLeg']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.landing_leg)

    def test_launch_clamp(self):
        part = self.parts.with_title('TT18-A Launch Stability Enhancer')[0]
        self.assertEqual('launchClamp1', part.name)
        self.assertEqual('TT18-A Launch Stability Enhancer', part.title)
        self.assertEqual(200, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax Jumbo-64 Fuel Tank', part.parent.title)
        self.assertEqual(len(part.children), 0)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(6, part.stage)
        self.assertEqual(6, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(100, part.mass)
        self.assertClose(100, part.dry_mass)
        self.assertEqual(100, part.impact_tolerance)
        self.assertFalse(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['LaunchClamp', 'ModuleGenerator', 'ModuleTestSubject']
        if self.conn.space_center.remote_tech_available:
            modules.extend(['ModuleRTDataTransmitter', 'ModuleRTAntennaPassive'])
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.launch_clamp)

    def test_light(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        self.assertEqual('spotLight1', part.name)
        self.assertEqual('Illuminator Mk1', part.title)
        self.assertEqual(100, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Aerodynamic Nose Cone', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertClose(0, part.mass)
        self.assertClose(0, part.dry_mass)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleLight']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.light)

    def test_parachute(self):
        part = self.parts.with_title('Mk16-XL Parachute')[0]
        self.assertEqual('parachuteLarge', part.name)
        self.assertEqual('Mk16-XL Parachute', part.title)
        self.assertEqual(850, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Mk1-2 Command Pod', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(0, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(300, part.mass)
        self.assertClose(300, part.dry_mass)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        self.assertEqual([
            'ModuleDragModifier', 'ModuleDragModifier',
            'ModuleParachute', 'ModuleTestSubject'],
            sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.parachute)

    def test_radiator(self):
        part = self.parts.with_title('Thermal Control System (small)')[0]
        self.assertEqual('foldingRadSmall', part.name)
        self.assertEqual('Thermal Control System (small)', part.title)
        self.assertEqual(450, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Advanced Reaction Wheel Module, Large', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(50, part.mass)
        self.assertClose(50, part.dry_mass)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleActiveRadiator', 'ModuleDeployableRadiator']
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.radiator)

    def test_reaction_wheel(self):
        part = self.parts.with_title('Advanced Reaction Wheel Module, Large')[0]
        self.assertEqual('asasmodule1-2', part.name)
        self.assertEqual('Advanced Reaction Wheel Module, Large', part.title)
        self.assertEqual(2100, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('FL-R1 RCS Fuel Tank', part.parent.title)
        self.assertEqual(2, len(part.children))
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(200, part.mass)
        self.assertClose(200, part.dry_mass)
        self.assertEqual(9, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleReactionWheel']
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.reaction_wheel)

    def test_resource_converter(self):
        part = self.parts.with_title('Convert-O-Tron 250')[0]
        self.assertEqual('ISRU', part.name)
        self.assertEqual('Convert-O-Tron 250', part.title)
        self.assertEqual(8000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Advanced Reaction Wheel Module, Large', part.parent.title)
        self.assertEqual(1, len(part.children))
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(4250, part.mass)
        self.assertClose(4250, part.dry_mass)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleAnimationGroup',
                   'ModuleCoreHeat',
                   'ModuleOverheatDisplay'] + \
                   ['ModuleResourceConverter']*4
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.resource_converter)

    def test_resource_harvester(self):
        part = self.parts.with_title('\'Drill-O-Matic Junior\' Mining Excavator')[0]
        self.assertEqual('MiniDrill', part.name)
        self.assertEqual('\'Drill-O-Matic Junior\' Mining Excavator', part.title)
        self.assertEqual(1000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax X200-32 Fuel Tank', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(250, part.mass)
        self.assertClose(250, part.dry_mass)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = [
            'ModuleAnimationGroup',
            'ModuleAsteroidDrill',
            'ModuleCoreHeat',
            'ModuleOverheatDisplay',
            'ModuleResourceHarvester'
        ]
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.resource_harvester)

    def test_sensor(self):
        part = self.parts.with_title('PresMat Barometer')[0]
        self.assertEqual('sensorBarometer', part.name)
        self.assertEqual('PresMat Barometer', part.title)
        self.assertEqual(3300, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('Rockomax X200-8 Fuel Tank', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertClose(0, part.mass)
        self.assertClose(0, part.dry_mass)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleEnviroSensor', 'ModuleScienceExperiment']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.sensor)

    def test_solar_panel(self):
        part = self.parts.with_title('Gigantor XL Solar Array')[0]
        self.assertEqual('largeSolarPanel', part.name)
        self.assertEqual('Gigantor XL Solar Array', part.title)
        self.assertEqual(3000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual('FL-R1 RCS Fuel Tank', part.parent.title)
        self.assertEqual(0, len(part.children))
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertClose(300, part.mass)
        self.assertClose(300, part.dry_mass)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual(0, len(part.fuel_lines_from))
        self.assertEqual(0, len(part.fuel_lines_to))
        modules = ['ModuleDeployableSolarPanel']
        if self.conn.space_center.far_available:
            modules.append('FARBasicDragModel')
        self.assertEqual(sorted(modules), sorted(m.name for m in part.modules))
        self.assertNotEqual(None, part.solar_panel)

if __name__ == "__main__":
    unittest.main()
