import unittest
import krpctest


class TestPartsPart(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
            # TODO: wait needed to allow dynamic
            # pressure calculations to settle
            cls.wait(3)
        cls.sc = cls.connect().space_center
        cls.vessel = cls.sc.active_vessel
        cls.parts = cls.vessel.parts
        cls.far_available = cls.sc.far_available

    def test_root_part(self):
        part = self.parts.root
        self.assertEqual("mk1-3pod", part.name)
        self.assertEqual(3800, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertIsNone(part.parent)
        self.assertCountEqual(
            ["fairingSize1"] + ["landingLeg1"] * 3 + ["SmallGearBay", "Separator.2"],
            [x.name for x in part.children],
        )
        self.assertFalse(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(2720, part.mass, places=2)
        self.assertAlmostEqual(2600, part.dry_mass, places=2)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(20, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "FlagDecal",
            "ModuleColorChanger",
            "ModuleCommand",
            "ModuleConductionMultiplier",
            "ModuleReactionWheel",
            "ModuleScienceContainer",
            "ModuleScienceExperiment",
            "ModuleDataTransmitter",
            "ModuleTripLogger",
            "ModuleProbeControlPoint",
            "ModuleLiftingSurface",
            "ModuleRCSFX",
            "ModuleInventoryPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.extend(["FARBasicDragModel", "FARControlSys"])
        # MechJebCore is injected by the MechJeb mod when installed; strip it so
        # the assertion holds with or without MechJeb (cf. the ModuleRTAntennaPassive
        # handling in test_launch_clamp).
        actual_modules = [x.name for x in part.modules]
        if "MechJebCore" in actual_modules:
            actual_modules.remove("MechJebCore")
        self.assertCountEqual(modules, actual_modules)
        box = part.bounding_box(part.reference_frame)
        self.assertAlmostEqual((-1.223, -0.574, -1.223), box[0], places=2)
        self.assertAlmostEqual((1.223, 1.273, 1.223), box[1], places=2)
        self.assertIsNotNone(part.antenna)
        self.assertIsNone(part.cargo_bay)
        self.assertIsNone(part.control_surface)
        self.assertIsNone(part.decoupler)
        self.assertIsNone(part.docking_port)
        self.assertIsNone(part.engine)
        self.assertIsNotNone(part.experiment)
        self.assertIsNone(part.fairing)
        self.assertIsNone(part.intake)
        self.assertIsNone(part.leg)
        self.assertIsNone(part.launch_clamp)
        self.assertIsNone(part.light)
        self.assertIsNone(part.parachute)
        self.assertIsNone(part.radiator)
        self.assertIsNotNone(part.rcs)
        self.assertIsNone(part.resource_converter)
        self.assertIsNone(part.resource_harvester)
        self.assertIsNotNone(part.reaction_wheel)
        self.assertIsNone(part.sensor)
        self.assertIsNone(part.solar_panel)
        self.assertIsNone(part.wheel)

    def test_thermal(self):
        part = self.parts.root
        self.assertAlmostEqual(300, part.temperature, delta=50)
        self.assertAlmostEqual(300, part.skin_temperature, delta=50)
        self.assertEqual(1400, part.max_temperature)
        self.assertEqual(2400, part.max_skin_temperature)
        self.assertAlmostEqual(2.427, part.thermal_mass, places=2)
        self.assertAlmostEqual(0.01343, part.thermal_skin_mass, places=4)
        self.assertAlmostEqual(0.36, part.thermal_resource_mass, places=2)
        self.assertAlmostEqual(0, part.thermal_conduction_flux, places=2)
        self.assertAlmostEqual(0, part.thermal_convection_flux, places=2)
        self.assertAlmostEqual(0, part.thermal_radiation_flux, places=2)
        self.assertAlmostEqual(0, part.thermal_internal_flux, places=2)
        self.assertAlmostEqual(0, part.thermal_skin_to_internal_flux, places=2)

    def test_antenna(self):
        part = self.parts.with_name("longAntenna")[0]
        self.assertEqual("longAntenna", part.name)
        self.assertEqual(300, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertCountEqual([], [p.name for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertAlmostEqual(0, part.mass, places=4)
        self.assertAlmostEqual(0, part.dry_mass, places=4)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleDataTransmitter",
            "ModuleDeployableAntenna",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.antenna)

    def test_cargo_bay(self):
        part = self.parts.with_name("ServiceBay.250.v2")[0]
        self.assertEqual("ServiceBay.250.v2", part.name)
        self.assertEqual(500, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertCountEqual(["liquidEngine2-2.v2"], [p.name for p in part.children])
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(300, part.mass, places=4)
        self.assertAlmostEqual(300, part.dry_mass, places=4)
        self.assertEqual(14, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleAnimateGeneric",
            "ModuleCargoBay",
            "ModuleConductionMultiplier",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.cargo_bay)

    def test_control_surface(self):
        part = self.parts.with_name("winglet3")[0]
        self.assertEqual("winglet3", part.name)
        self.assertEqual(600, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax32.BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(78, part.mass, places=4)
        self.assertAlmostEqual(78, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["SyncModuleControlSurface", "ModuleCargoPart", "KOSNameTag"]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.control_surface)

    def test_decoupler(self):
        part = self.parts.with_name("radialDecoupler2")[0]
        self.assertEqual("radialDecoupler2", part.name)
        self.assertEqual(700, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax64.BW", part.parent.name)
        self.assertCountEqual(
            ["MassiveBooster"],
            [p.name for p in part.children],
        )
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(5, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(50, part.mass, places=4)
        self.assertAlmostEqual(50, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(8, part.impact_tolerance)
        self.assertFalse(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleAnchoredDecoupler",
            "ModuleTestSubject",
            "ModuleToggleCrossfeed",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.decoupler)

    def test_docking_port(self):
        part = self.parts.with_name("dockingPort2")[0]
        self.assertEqual("dockingPort2", part.name)
        self.assertEqual(280, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax32.BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        # TODO: why is this not -1? Docking ports aren't activated in stages?
        self.assertEqual(3, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(50, part.mass, places=4)
        self.assertAlmostEqual(50, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(10, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleDockingNode",
            "ModuleColorChanger",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.docking_port)

    def test_engine(self):
        part = self.parts.with_name("MassiveBooster")[0]
        self.assertEqual("MassiveBooster", part.name)
        self.assertEqual(2700, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("radialDecoupler2", part.parent.name)
        self.assertCountEqual(["noseCone"], [p.name for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(6, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(23250, part.mass, places=4)
        self.assertAlmostEqual(4500, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "FXModuleAnimateThrottle",
            "ModuleEnginesFX",
            "ModuleSurfaceFX",
            "ModuleTestSubject",
            "ModulePartVariants",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.engine)

    def test_experiment(self):
        part = self.parts.with_name("GooExperiment")[0]
        self.assertEqual("GooExperiment", part.name)
        self.assertEqual(800, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax32.BW", part.parent.name)
        self.assertCountEqual([], [p.name for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(50, part.mass, places=4)
        self.assertAlmostEqual(50, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleScienceExperiment",
            "ModuleAnimateGeneric",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.experiment)

    def test_fairing(self):
        part = self.parts.with_name("fairingSize1")[0]
        self.assertEqual("fairingSize1", part.name)
        self.assertEqual(300, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("mk1-3pod", part.parent.name)
        self.assertEqual([], part.children)
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(0, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(106.16, part.mass, places=2)
        self.assertAlmostEqual(106.16, part.dry_mass, places=2)
        self.assertEqual(9, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleCargoBay",
            "ModuleProceduralFairing",
            "ModuleTestSubject",
            "ModuleStructuralNodeToggle",
            "ModulePartVariants",
            "ModuleCargoPart",
            "KOSNameTag",
        ] + ["ModuleStructuralNode"] * 12
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.fairing)

    def test_intake(self):
        part = self.parts.with_name("airScoop")[0]
        self.assertEqual("airScoop", part.name)
        self.assertEqual(250, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(30, part.mass, places=4)
        # TODO: why is the dry mass != total mass,
        # part doens't have any resources!?
        self.assertAlmostEqual(20, part.dry_mass, places=4)
        self.assertEqual(10, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["ModuleResourceIntake", "ModuleCargoPart", "KOSNameTag"]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.intake)

    def test_leg(self):
        part = self.parts.with_name("landingLeg1")[0]
        self.assertEqual("landingLeg1", part.name)
        self.assertEqual(440, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("mk1-3pod", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(50, part.mass, places=4)
        self.assertAlmostEqual(50, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleWheelBase",
            "ModuleWheelBogey",
            "ModuleWheelDamage",
            "ModuleWheelDeployment",
            "ModuleWheelLock",
            "ModuleWheelSuspension",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.leg)
        box = part.bounding_box(part.reference_frame)
        self.assertAlmostEqual((-0.150, -1.016, -0.279), box[0], places=2)
        self.assertAlmostEqual((0.150, 0.239, 0.377), box[1], places=2)

    def test_launch_clamp(self):
        part = self.parts.with_name("launchClamp1")[0]
        self.assertEqual("launchClamp1", part.name)
        self.assertEqual(200, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax64.BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(6, part.stage)
        self.assertEqual(6, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertAlmostEqual(0, part.mass, places=4)
        self.assertAlmostEqual(0, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(100, part.impact_tolerance)
        self.assertFalse(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["LaunchClamp", "ModuleGenerator", "ModuleTestSubject", "KOSNameTag"]
        actual_modules = [x.name for x in part.modules]
        if "ModuleRTAntennaPassive" in actual_modules:
            actual_modules.remove("ModuleRTAntennaPassive")
        self.assertCountEqual(modules, actual_modules)
        self.assertIsNotNone(part.launch_clamp)

    def test_light(self):
        part = self.parts.with_name("spotLight1")[0]
        self.assertEqual("spotLight1", part.name)
        self.assertEqual(100, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("noseCone", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(5, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertAlmostEqual(0, part.mass, places=4)
        self.assertAlmostEqual(0, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["ModuleLight", "KOSNameTag"]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.light)

    def test_parachute(self):
        part = self.parts.with_name("parachuteRadial")[0]
        self.assertEqual("parachuteRadial", part.name)
        self.assertEqual(400, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(2, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(100, part.mass, places=4)
        self.assertAlmostEqual(100, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        self.assertCountEqual(
            [
                "ModuleDragModifier",
                "ModuleDragModifier",
                "ModuleParachute",
                "ModuleTestSubject",
                "ModuleCargoPart",
                "KOSNameTag",
            ],
            [x.name for x in part.modules],
        )
        self.assertIsNotNone(part.parachute)

    def test_radiator(self):
        part = self.parts.with_name("foldingRadSmall")[0]
        self.assertEqual("foldingRadSmall", part.name)
        self.assertEqual(450, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("asasmodule1-2", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(50, part.mass, places=4)
        self.assertAlmostEqual(50, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(12, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleActiveRadiator",
            "ModuleDeployableRadiator",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.radiator)

    def test_rcs(self):
        part = self.parts.with_name("RCSBlock.v2")[0]
        self.assertEqual("RCSBlock.v2", part.name)
        self.assertEqual(45, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertAlmostEqual(0, part.mass, places=4)
        self.assertAlmostEqual(0, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(15, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "FXModuleAnimateRCS",
            "ModuleRCSFX",
            "ModulePartVariants",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.rcs)

    def test_reaction_wheel(self):
        part = self.parts.with_name("asasmodule1-2")[0]
        self.assertEqual("asasmodule1-2", part.name)
        self.assertEqual(2100, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("RCSTank1-2", part.parent.name)
        self.assertCountEqual(
            ["foldingRadSmall", "ISRU"],
            [p.name for p in part.children],
        )
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(200, part.mass, places=4)
        self.assertAlmostEqual(200, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(9, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["ModuleReactionWheel", "ModuleCargoPart", "KOSNameTag"]
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.reaction_wheel)

    def test_resource_converter(self):
        part = self.parts.with_name("ISRU")[0]
        self.assertEqual("ISRU", part.name)
        self.assertEqual(8000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("asasmodule1-2", part.parent.name)
        self.assertCountEqual(["Rockomax32.BW"], [p.name for p in part.children])
        self.assertTrue(part.axially_attached)
        self.assertFalse(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(4250, part.mass, places=4)
        self.assertAlmostEqual(4250, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleAnimationGroup",
            "ModuleCoreHeat",
            "ModuleOverheatDisplay",
            "KOSNameTag",
        ] + ["ModuleResourceConverter"] * 4
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.resource_converter)

    def test_resource_harvester(self):
        part = self.parts.with_name("MiniDrill")[0]
        self.assertEqual("MiniDrill", part.name)
        self.assertEqual(1000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax32.BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(250, part.mass, places=4)
        self.assertAlmostEqual(250, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(7, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleAnimationGroup",
            "ModuleAsteroidDrill",
            "ModuleCometDrill",
            "ModuleCoreHeat",
            "ModuleOverheatDisplay",
            "ModuleResourceHarvester",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.resource_harvester)

    def test_sensor(self):
        part = self.parts.with_name("sensorBarometer")[0]
        self.assertEqual("sensorBarometer", part.name)
        self.assertEqual(880, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("Rockomax8BW", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(1, part.decouple_stage)
        self.assertTrue(part.massless)
        self.assertAlmostEqual(0, part.mass, places=4)
        self.assertAlmostEqual(0, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "ModuleEnviroSensor",
            "ModuleScienceExperiment",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.sensor)

    def test_solar_panel(self):
        part = self.parts.with_name("largeSolarPanel")[0]
        self.assertEqual("largeSolarPanel", part.name)
        self.assertEqual(3000, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("RCSTank1-2", part.parent.name)
        self.assertEqual([], part.children)
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(3, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(300, part.mass, places=4)
        self.assertAlmostEqual(300, part.dry_mass, places=4)
        self.assertFalse(part.shielded)
        self.assertAlmostEqual(0, part.dynamic_pressure, places=3)
        self.assertEqual(8, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = ["ModuleDeployableSolarPanel", "ModuleCargoPart", "KOSNameTag"]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.solar_panel)

    def test_wheel(self):
        part = self.parts.with_name("SmallGearBay")[0]
        self.assertEqual("SmallGearBay", part.name)
        self.assertEqual(600, part.cost)
        self.assertEqual(self.vessel, part.vessel)
        self.assertEqual("mk1-3pod", part.parent.name)
        self.assertEqual([], [p.name for p in part.children])
        self.assertFalse(part.axially_attached)
        self.assertTrue(part.radially_attached)
        self.assertEqual(-1, part.stage)
        self.assertEqual(-1, part.decouple_stage)
        self.assertFalse(part.massless)
        self.assertAlmostEqual(45, part.mass, places=4)
        self.assertAlmostEqual(45, part.dry_mass, places=4)
        self.assertEqual(50, part.impact_tolerance)
        self.assertTrue(part.crossfeed)
        self.assertFalse(part.is_fuel_line)
        self.assertEqual([], part.fuel_lines_from)
        self.assertEqual([], part.fuel_lines_to)
        modules = [
            "FXModuleConstrainPosition",
            "FXModuleLookAtConstraint",
            "ModuleLight",
            "ModuleStatusLight",
            "ModuleTestSubject",
            "ModuleWheelBase",
            "ModuleWheelBrakes",
            "ModuleWheelDamage",
            "ModuleWheelDeployment",
            "ModuleWheelSteering",
            "ModuleWheelSuspension",
            "ModuleDragModifier",
            "ModuleDragModifier",
            "ModuleCargoPart",
            "KOSNameTag",
        ]
        if self.far_available:
            modules.append("FARBasicDragModel")
        self.assertCountEqual(modules, [x.name for x in part.modules])
        self.assertIsNotNone(part.wheel)
        box = part.bounding_box(part.reference_frame)
        self.assertAlmostEqual((-0.495, -1.122, -0.569), box[0], places=2)
        self.assertAlmostEqual((0.495, 0.232, 0.679), box[1], places=2)

    def test_highlighting(self):
        part = self.parts.with_name("Rockomax64.BW")[0]
        init_color = part.highlight_color
        self.assertEqual((0, 1, 0), init_color)
        self.assertFalse(part.highlighted)
        colors = [(1, 0, 0), (0, 1, 0), (0, 0, 1)]
        for color in colors:
            part.highlight_color = color
            part.highlighted = True
            self.wait(0.5)
            part.highlighted = False
            self.wait(0.5)
        part.highlight_color = init_color

    def test_rotation(self):
        part = self.parts.root
        for target_frame in [
            part.reference_frame,
            self.vessel.reference_frame,
            self.vessel.orbit.body.reference_frame,
        ]:
            expected = self.sc.transform_rotation(
                (0, 0, 0, 1), part.reference_frame, target_frame
            )
            self.assertQuaternionsAlmostEqual(
                expected, part.rotation(target_frame), places=5
            )


class TestPartsPartDecoupleStage(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "PartsDecoupleStage":
            cls.launch_vessel_from_vab("PartsDecoupleStage")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def check(self, part, name, stage, decouple_stage):
        self.assertEqual(name, part.name)
        self.assertEqual(stage, part.stage)
        self.assertEqual(decouple_stage, part.decouple_stage)

    def test_stage_numbers(self):
        stage_numbering = []
        stack = [(0, self.parts.root)]
        while stack:
            level, part = stack.pop()
            stage_numbering.append(
                " " * (level * 2)
                + "%s %d %d" % (part.name, part.stage, part.decouple_stage)
            )
            stack.extend(
                (level + 1, part)
                for part in sorted(part.children, key=lambda part: part.name)
            )
        # Internal part names (part.name), sorted by name for stable, locale-
        # independent ordering. rcsTankMini = "FL-R10 RCS Fuel Tank",
        # RCSFuelTank = "FL-R25 RCS Fuel Tank".
        expected_stage_numbering = [
            "mk1pod.v2 -1 -1",
            "  Separator.1 1 1",
            "    fuelTank -1 1",
            "      radialDecoupler2 0 1",
            "        rcsTankMini -1 1",
            "      radialDecoupler2 0 1",
            "        rcsTankMini -1 1",
            "      launchClamp1 5 5",
            "      launchClamp1 5 5",
            "      Decoupler.1 2 1",
            "        fuelTank.long -1 2",
            "          radialDecoupler2 3 3",
            "            fuelTankSmall -1 3",
            "              Decoupler.1 4 4",
            "                RCSFuelTank -1 4",
            "          radialDecoupler2 3 3",
            "            fuelTankSmall -1 3",
            "              Decoupler.1 4 4",
            "                RCSFuelTank -1 4",
            "          radialDecoupler2 3 3",
            "            fuelTankSmall -1 3",
            "              Decoupler.1 4 4",
            "                RCSFuelTank -1 4",
            "          radialDecoupler2 3 3",
            "            fuelTankSmall -1 3",
            "              Decoupler.1 4 4",
            "                RCSFuelTank -1 4",
        ]
        self.assertEqual(expected_stage_numbering, stage_numbering)


class TestPartsPartForce(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.part = cls.vessel.parts.root

    def test_add_force(self):
        force = self.part.add_force((1, 2, 3), (4, 5, 6), self.part.reference_frame)
        self.assertEqual(force.part.name, self.part.name)
        self.assertEqual((1, 2, 3), force.force_vector)
        self.assertEqual((4, 5, 6), force.position)
        self.assertEqual(self.part.reference_frame, force.reference_frame)
        force.remove()

    def test_instantaneous_force(self):
        self.part.instantaneous_force((1, 2, 3), (4, 5, 6), self.part.reference_frame)


if __name__ == "__main__":
    unittest.main()
