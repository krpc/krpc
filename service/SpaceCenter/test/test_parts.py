import unittest
import krpctest
from krpctest.geometry import dot


class TestParts(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_all_parts(self):
        # Assert on language-independent internal names (part.name), not the
        # localized titles, so this passes regardless of KSP's language.
        self.assertCountEqual(
            [
                "MiniDrill",  # 'Drill-O-Matic Junior' Mining Excavator
                "fairingSize1",  # AE-FF1 Airstream Protective Shell (1.25m)
                "IntakeRadialLong",  # Adjustable Ramp Intake (Radial)
                "asasmodule1-2",  # Advanced Reaction Wheel Module, Large
                "noseCone",  # Aerodynamic Nose Cone
                "noseCone",
                "noseCone",
                "dockingPort2",  # Clamp-O-Tron Docking Port
                "dockingPort3",  # Clamp-O-Tron Docking Port Jr.
                "longAntenna",  # Communotron 16
                "ISRU",  # Convert-O-Tron 250
                "winglet3",  # Delta-Deluxe Winglet
                "strutConnector",  # EAS-4 Strut Connector
                "strutConnector",
                "strutConnector",
                "RCSTank1-2",  # FL-R750 RCS Fuel Tank
                "sensorGravimeter",  # GRAVMAX Negative Gravioli Detector
                "largeSolarPanel",  # Gigantor XL Solar Array
                "spotLight1",  # Illuminator Mk1
                "spotLight1",
                "spotLight1",
                "landingLeg1",  # LT-1 Landing Struts
                "landingLeg1",
                "landingLeg1",
                "SmallGearBay",  # LY-10 Small Landing Gear
                "mk1-3pod",  # Mk1-3 Command Pod
                "parachuteRadial",  # Mk2-R Radial-Mount Parachute
                "parachuteRadial",
                "parachuteRadial",
                "GooExperiment",  # Mystery Goo™ Containment Unit
                "GooExperiment",
                "GooExperiment",
                "solarPanels5",  # OX-STAT Photovoltaic Panels
                "sensorBarometer",  # PresMat Barometer
                "engineLargeSkipper.v2",  # RE-I5 "Skipper" Liquid Fuel Engine
                "liquidEngine2-2.v2",  # RE-L10 "Poodle" Liquid Fuel Engine
                "liquidEngineMainsail.v2",  # RE-M3 "Mainsail" Liquid Fuel Engine
                "RCSBlock.v2",  # RV-105 RCS Thruster Block
                "Rockomax64.BW",  # Rockomax Jumbo-64 Fuel Tank
                "Rockomax32.BW",  # Rockomax X200-32 Fuel Tank
                "Rockomax8BW",  # Rockomax X200-8 Fuel Tank
                "MassiveBooster",  # S1 SRB-KD25k "Kickback" Solid Fuel Booster
                "MassiveBooster",
                "MassiveBooster",
                "solarPanels2",  # SP-L 1x6 Photovoltaic Panels
                "solarPanels2",
                "ServiceBay.250.v2",  # Service Bay (2.5m)
                "Separator.2",  # TS-25 Stack Separator
                "Separator.2",
                "Separator.2",
                "radialDecoupler2",  # TT-70 Radial Decoupler
                "radialDecoupler2",
                "radialDecoupler2",
                "launchClamp1",  # TT18-A Launch Stability Enhancer
                "launchClamp1",
                "launchClamp1",
                "launchClamp1",
                "launchClamp1",
                "launchClamp1",
                "foldingRadSmall",  # Thermal Control System (small)
                "airScoop",  # XM-G50 Radial Air Intake
                "ksp.r.largeBatteryPack",  # Z-400 Rechargeable Battery
            ],
            [x.name for x in self.parts.all],
        )

    def test_root_part(self):
        root = self.parts.root
        self.assertEqual("mk1-3pod", root.name)
        self.assertEqual(self.vessel, root.vessel)
        self.assertIsNone(root.parent)
        self.assertGreater(len(root.children), 0)

    def test_controlling(self):
        commandpod = self.parts.root
        dockingport = self.parts.docking_ports[0].part
        part = self.parts.with_name("ksp.r.largeBatteryPack")[0]
        self.assertNotEqual(commandpod, dockingport)
        self.assertNotEqual(commandpod, part)

        self.assertEqual(commandpod, self.parts.controlling)
        self.parts.controlling = dockingport
        self.assertEqual(dockingport, self.parts.controlling)
        self.parts.controlling = part
        self.assertEqual(part, self.parts.controlling)
        self.parts.controlling = commandpod
        self.assertEqual(commandpod, self.parts.controlling)

    def test_controlling_orientation(self):
        ref = self.vessel.orbit.body.reference_frame
        root = self.parts.root
        port = self.parts.with_name("dockingPort2")[0]

        # Check vessel direction is in direction of root part
        # and perpendicular to the docking port
        vessel_dir = self.vessel.direction(ref)
        root_dir = root.direction(ref)
        port_dir = port.direction(ref)
        self.assertAlmostEqual(vessel_dir, root_dir, places=2)
        self.assertAlmostEqual(0, dot(vessel_dir, port_dir), places=2)

        # Control from the docking port
        self.parts.controlling = port

        # Check vessel direction is now the direction of the docking port
        vessel_dir = self.vessel.direction(ref)
        self.assertAlmostEqual(0, dot(vessel_dir, root_dir), places=2)
        self.assertAlmostEqual(vessel_dir, port_dir, places=2)

        # Control from the root part
        self.parts.controlling = root

        # Check vessel direction is now the direction of the root part
        vessel_dir = self.vessel.direction(ref)
        self.assertAlmostEqual(vessel_dir, root_dir, places=2)
        self.assertAlmostEqual(0, dot(vessel_dir, port_dir), places=2)

    def test_parts_with_name(self):
        parts = self.parts.with_name("spotLight1")
        self.assertCountEqual(["spotLight1"] * 3, [p.name for p in parts])
        parts = self.parts.with_name("doesntExist")
        self.assertCountEqual([], parts)

    def test_parts_with_title(self):
        # with_title is locale-dependent, so derive the expected title from a
        # part looked up by its language-independent name; this still exercises
        # the with_title API without hard-coding an English string.
        title = self.parts.with_name("spotLight1")[0].title
        parts = self.parts.with_title(title)
        self.assertCountEqual([title] * 3, [p.title for p in parts])
        self.assertCountEqual([], self.parts.with_title("Doesn't Exist"))

    def test_parts_with_module(self):
        parts = self.parts.with_module("ModuleLight")
        self.assertCountEqual(
            ["spotLight1"] * 3 + ["SmallGearBay"], [p.name for p in parts]
        )
        parts = self.parts.with_module("DoesntExist")
        self.assertCountEqual([], parts)

    def test_parts_in_stage(self):
        # Assert on language-independent internal names (part.name).
        def part_names_in_stage(stage):
            return [part.name for part in self.parts.in_stage(stage)]

        self.assertCountEqual(
            [
                "MiniDrill",  # 'Drill-O-Matic Junior' Mining Excavator
                "IntakeRadialLong",  # Adjustable Ramp Intake (Radial)
                "asasmodule1-2",  # Advanced Reaction Wheel Module, Large
                "noseCone",  # Aerodynamic Nose Cone
                "noseCone",
                "noseCone",
                "longAntenna",  # Communotron 16
                "ISRU",  # Convert-O-Tron 250
                "winglet3",  # Delta-Deluxe Winglet
                "strutConnector",  # EAS-4 Strut Connector
                "strutConnector",
                "strutConnector",
                "RCSTank1-2",  # FL-R750 RCS Fuel Tank
                "sensorGravimeter",  # GRAVMAX Negative Gravioli Detector
                "largeSolarPanel",  # Gigantor XL Solar Array
                "spotLight1",  # Illuminator Mk1
                "spotLight1",
                "spotLight1",
                "landingLeg1",  # LT-1 Landing Struts
                "landingLeg1",
                "landingLeg1",
                "SmallGearBay",  # LY-10 Small Landing Gear
                "mk1-3pod",  # Mk1-3 Command Pod
                "GooExperiment",  # Mystery Goo\u2122 Containment Unit
                "GooExperiment",
                "GooExperiment",
                "solarPanels5",  # OX-STAT Photovoltaic Panels
                "sensorBarometer",  # PresMat Barometer
                "RCSBlock.v2",  # RV-105 RCS Thruster Block
                "Rockomax64.BW",  # Rockomax Jumbo-64 Fuel Tank
                "Rockomax32.BW",  # Rockomax X200-32 Fuel Tank
                "Rockomax8BW",  # Rockomax X200-8 Fuel Tank
                "solarPanels2",  # SP-L 1x6 Photovoltaic Panels
                "solarPanels2",
                "ServiceBay.250.v2",  # Service Bay (2.5m)
                "foldingRadSmall",  # Thermal Control System (small)
                "airScoop",  # XM-G50 Radial Air Intake
                "ksp.r.largeBatteryPack",  # Z-400 Rechargeable Battery
            ],
            part_names_in_stage(-1),
        )
        self.assertCountEqual(["fairingSize1"], part_names_in_stage(0))
        self.assertCountEqual(["Separator.2"], part_names_in_stage(1))
        self.assertCountEqual(["parachuteRadial"] * 3, part_names_in_stage(2))
        self.assertCountEqual(
            [
                "dockingPort2",  # Clamp-O-Tron Docking Port
                "dockingPort3",  # Clamp-O-Tron Docking Port Jr.
                "liquidEngine2-2.v2",  # RE-L10 "Poodle" Liquid Fuel Engine
                "Separator.2",  # TS-25 Stack Separator
            ],
            part_names_in_stage(3),
        )
        self.assertCountEqual(
            ["engineLargeSkipper.v2", "Separator.2"],  # RE-I5 "Skipper", TS-25
            part_names_in_stage(4),
        )
        self.assertCountEqual(["radialDecoupler2"] * 3, part_names_in_stage(5))
        self.assertCountEqual(
            ["liquidEngineMainsail.v2"]  # RE-M3 "Mainsail" Liquid Fuel Engine
            + ["MassiveBooster"] * 3  # S1 SRB-KD25k "Kickback"
            + ["launchClamp1"] * 6,  # TT18-A Launch Stability Enhancer
            part_names_in_stage(6),
        )
        self.assertCountEqual([], part_names_in_stage(7))

    def test_parts_in_decouple_stage(self):
        # Assert on language-independent internal names (part.name).
        def part_names_in_decouple_stage(stage):
            return [part.name for part in self.parts.in_decouple_stage(stage)]

        self.assertCountEqual(
            ["fairingSize1"]  # AE-FF1 Airstream Protective Shell (1.25m)
            + ["landingLeg1"] * 3  # LT-1 Landing Struts
            + ["SmallGearBay", "mk1-3pod"],  # LY-10 Small Landing Gear, Mk1-3 pod
            part_names_in_decouple_stage(-1),
        )
        self.assertCountEqual([], part_names_in_decouple_stage(0))
        self.assertCountEqual(
            [
                "liquidEngineMainsail.v2",  # RE-M3 "Mainsail" Liquid Fuel Engine
                "Rockomax64.BW",  # Rockomax Jumbo-64 Fuel Tank
                "Separator.2",  # TS-25 Stack Separator
            ],
            part_names_in_decouple_stage(4),
        )
        self.assertCountEqual(
            ["noseCone"] * 3  # Aerodynamic Nose Cone
            + ["spotLight1"] * 3  # Illuminator Mk1
            + ["MassiveBooster"] * 3  # S1 SRB-KD25k "Kickback"
            + ["radialDecoupler2"] * 3,  # TT-70 Radial Decoupler
            part_names_in_decouple_stage(5),
        )
        self.assertCountEqual(
            ["launchClamp1"] * 6, part_names_in_decouple_stage(6)  # TT18-A
        )
        self.assertCountEqual([], part_names_in_decouple_stage(7))

    def test_modules_with_name(self):
        modules = self.parts.modules_with_name("ModuleLight")
        self.assertCountEqual(["ModuleLight"] * 4, [m.name for m in modules])
        modules = self.parts.modules_with_name("DoesntExist")
        self.assertCountEqual([], modules)

    def test_antennas(self):
        self.assertCountEqual(
            ["mk1-3pod", "longAntenna"],
            [p.part.name for p in self.parts.antennas],
        )

    def test_cargo_bays(self):
        self.assertCountEqual(
            ["ServiceBay.250.v2"], [p.part.name for p in self.parts.cargo_bays]
        )

    def test_control_surfaces(self):
        self.assertCountEqual(
            ["winglet3"],
            [p.part.name for p in self.parts.control_surfaces],
        )

    def test_decouplers(self):
        self.assertCountEqual(
            ["Separator.2", "radialDecoupler2"] * 3,
            [p.part.name for p in self.parts.decouplers],
        )

    def test_docking_ports(self):
        self.assertCountEqual(
            ["dockingPort2", "dockingPort3"],
            [p.part.name for p in self.parts.docking_ports],
        )

    def test_docking_port_with_name(self):
        port = self.parts.docking_ports[0]
        if "ModuleDockingNodeNamed" not in set(m.name for m in port.part.modules):
            # Docking Port Alignment Indicator mod not installed
            return
        name = port.name
        self.assertEqual(port, self.parts.docking_port_with_name(name))
        self.assertNone(self.parts.docking_port_with_name("Not the name"))
        port.name = "Jeb's port"
        self.assertEqual(port, self.parts.docking_port_with_name("Jeb's port"))
        self.assertNone(self.parts.docking_port_with_name(name))
        self.assertNone(self.parts.docking_port_with_name("Not the name"))
        port.name = name

    def test_engines(self):
        self.assertCountEqual(
            [
                "engineLargeSkipper.v2",  # RE-I5 "Skipper" Liquid Fuel Engine
                "liquidEngine2-2.v2",  # RE-L10 "Poodle" Liquid Fuel Engine
                "liquidEngineMainsail.v2",  # RE-M3 "Mainsail" Liquid Fuel Engine
            ]
            + ["MassiveBooster"] * 3,  # S1 SRB-KD25k "Kickback"
            [p.part.name for p in self.parts.engines],
        )

    def test_experiments(self):
        self.assertCountEqual(
            [
                "mk1-3pod",  # Mk1-3 Command Pod
                "sensorGravimeter",  # GRAVMAX Negative Gravioli Detector
                "sensorBarometer",  # PresMat Barometer
            ]
            + ["GooExperiment"] * 3,  # Mystery Goo\u2122 Containment Unit
            [p.part.name for p in self.parts.experiments],
        )

    def test_fairings(self):
        self.assertCountEqual(
            ["fairingSize1"],
            [p.part.name for p in self.parts.fairings],
        )

    def test_intakes(self):
        self.assertCountEqual(
            ["IntakeRadialLong", "airScoop"],
            [p.part.name for p in self.parts.intakes],
        )

    def test_legs(self):
        self.assertCountEqual(
            ["landingLeg1"] * 3, [p.part.name for p in self.parts.legs]
        )

    def test_launch_clamps(self):
        self.assertCountEqual(
            ["launchClamp1"] * 6,
            [p.part.name for p in self.parts.launch_clamps],
        )

    def test_lights(self):
        self.assertCountEqual(
            ["spotLight1"] * 3 + ["SmallGearBay"],
            [p.part.name for p in self.parts.lights],
        )

    def test_parachutes(self):
        self.assertCountEqual(
            ["parachuteRadial"] * 3,
            [p.part.name for p in self.parts.parachutes],
        )

    def test_radiators(self):
        self.assertCountEqual(
            ["foldingRadSmall"],
            [p.part.name for p in self.parts.radiators],
        )

    def test_rcs(self):
        self.assertCountEqual(
            ["mk1-3pod", "RCSBlock.v2"],
            [p.part.name for p in self.parts.rcs],
        )

    def test_reaction_wheels(self):
        self.assertCountEqual(
            ["asasmodule1-2", "mk1-3pod"],
            [p.part.name for p in self.parts.reaction_wheels],
        )

    def test_resource_converters(self):
        self.assertCountEqual(
            ["ISRU"],
            [p.part.name for p in self.parts.resource_converters],
        )

    def test_resource_harvesters(self):
        self.assertCountEqual(
            ["MiniDrill"],
            [p.part.name for p in self.parts.resource_harvesters],
        )

    def test_sensors(self):
        self.assertCountEqual(
            ["sensorGravimeter", "sensorBarometer"],
            [p.part.name for p in self.parts.sensors],
        )

    def test_solar_panels(self):
        self.assertCountEqual(
            ["largeSolarPanel", "solarPanels5"] + ["solarPanels2"] * 2,
            [p.part.name for p in self.parts.solar_panels],
        )

    def test_wheels(self):
        self.assertCountEqual(
            ["SmallGearBay"], [p.part.name for p in self.parts.wheels]
        )


if __name__ == "__main__":
    unittest.main()
