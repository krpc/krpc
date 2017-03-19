import unittest
import krpctest
from krpctest.geometry import dot


class TestParts(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_all_parts(self):
        self.assertItemsEqual([
            '\'Drill-O-Matic Junior\' Mining Excavator',
            'AE-FF1 Airstream Protective Shell (1.25m)',
            'Adjustable Ramp Intake (Radial)',
            'Advanced Reaction Wheel Module, Large',
            'Aerodynamic Nose Cone',
            'Aerodynamic Nose Cone',
            'Aerodynamic Nose Cone',
            'Clamp-O-Tron Docking Port',
            'Clamp-O-Tron Docking Port Jr.',
            'Communotron 16',
            'Convert-O-Tron 250',
            'Delta-Deluxe Winglet',
            'EAS-4 Strut Connector',
            'EAS-4 Strut Connector',
            'EAS-4 Strut Connector',
            'FL-R1 RCS Fuel Tank',
            'GRAVMAX Negative Gravioli Detector',
            'Gigantor XL Solar Array',
            'Illuminator Mk1',
            'Illuminator Mk1',
            'Illuminator Mk1',
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LY-10 Small Landing Gear',
            'Mk1-2 Command Pod',
            'Mk2-R Radial-Mount Parachute',
            'Mk2-R Radial-Mount Parachute',
            'Mk2-R Radial-Mount Parachute',
            u'Mystery Goo\u2122 Containment Unit',
            u'Mystery Goo\u2122 Containment Unit',
            u'Mystery Goo\u2122 Containment Unit',
            'OX-STAT Photovoltaic Panels',
            'PresMat Barometer',
            'RE-I5 "Skipper" Liquid Fuel Engine',
            'RE-L10 "Poodle" Liquid Fuel Engine',
            'RE-M3 "Mainsail" Liquid Fuel Engine',
            'RV-105 RCS Thruster Block',
            'Rockomax Jumbo-64 Fuel Tank',
            'Rockomax X200-32 Fuel Tank',
            'Rockomax X200-8 Fuel Tank',
            'S1 SRB-KD25k "Kickback" Solid Fuel Booster',
            'S1 SRB-KD25k "Kickback" Solid Fuel Booster',
            'S1 SRB-KD25k "Kickback" Solid Fuel Booster',
            'SP-L 1x6 Photovoltaic Panels',
            'SP-L 1x6 Photovoltaic Panels',
            'Service Bay (2.5m)',
            'TR-XL Stack Separator',
            'TR-XL Stack Separator',
            'TR-XL Stack Separator',
            'TT-70 Radial Decoupler',
            'TT-70 Radial Decoupler',
            'TT-70 Radial Decoupler',
            'TT18-A Launch Stability Enhancer',
            'TT18-A Launch Stability Enhancer',
            'TT18-A Launch Stability Enhancer',
            'TT18-A Launch Stability Enhancer',
            'TT18-A Launch Stability Enhancer',
            'TT18-A Launch Stability Enhancer',
            'Thermal Control System (small)',
            'XM-G50 Radial Air Intake',
            'Z-400 Rechargeable Battery'
        ], [x.title for x in self.parts.all])

    def test_root_part(self):
        root = self.parts.root
        self.assertEqual('Mark1-2Pod', root.name)
        self.assertEqual('Mk1-2 Command Pod', root.title)
        self.assertEqual(self.vessel, root.vessel)
        self.assertIsNone(root.parent)
        self.assertGreater(len(root.children), 0)

    def test_controlling(self):
        commandpod = self.parts.root
        dockingport = self.parts.docking_ports[0].part
        part = self.parts.with_title('Z-400 Rechargeable Battery')[0]
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
        port = self.parts.with_title('Clamp-O-Tron Docking Port')[0]

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
        parts = self.parts.with_name('spotLight1')
        self.assertItemsEqual(['spotLight1']*3, [p.name for p in parts])
        parts = self.parts.with_name('doesntExist')
        self.assertItemsEqual([], parts)

    def test_parts_with_title(self):
        parts = self.parts.with_title('Illuminator Mk1')
        self.assertItemsEqual(['Illuminator Mk1']*3, [p.title for p in parts])
        parts = self.parts.with_title('Doesn\'t Exist')
        self.assertItemsEqual([], parts)

    def test_parts_with_module(self):
        parts = self.parts.with_module('ModuleLight')
        self.assertItemsEqual(
            ['spotLight1']*3 + ['SmallGearBay'], [p.name for p in parts])
        parts = self.parts.with_module('DoesntExist')
        self.assertItemsEqual([], parts)

    def test_parts_in_stage(self):
        def part_titles_in_stage(stage):
            return [part.title for part in self.parts.in_stage(stage)]
        self.assertItemsEqual([
            '\'Drill-O-Matic Junior\' Mining Excavator',
            'Adjustable Ramp Intake (Radial)',
            'Advanced Reaction Wheel Module, Large',
            'Aerodynamic Nose Cone',
            'Aerodynamic Nose Cone',
            'Aerodynamic Nose Cone',
            'Communotron 16',
            'Convert-O-Tron 250',
            'Delta-Deluxe Winglet',
            'EAS-4 Strut Connector',
            'EAS-4 Strut Connector',
            'EAS-4 Strut Connector',
            'FL-R1 RCS Fuel Tank',
            'GRAVMAX Negative Gravioli Detector',
            'Gigantor XL Solar Array',
            'Illuminator Mk1',
            'Illuminator Mk1',
            'Illuminator Mk1',
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LT-1 Landing Struts',
            'LY-10 Small Landing Gear',
            'Mk1-2 Command Pod',
            u'Mystery Goo\u2122 Containment Unit',
            u'Mystery Goo\u2122 Containment Unit',
            u'Mystery Goo\u2122 Containment Unit',
            'OX-STAT Photovoltaic Panels',
            'PresMat Barometer',
            'RV-105 RCS Thruster Block',
            'Rockomax Jumbo-64 Fuel Tank',
            'Rockomax X200-32 Fuel Tank',
            'Rockomax X200-8 Fuel Tank',
            'SP-L 1x6 Photovoltaic Panels',
            'SP-L 1x6 Photovoltaic Panels',
            'Service Bay (2.5m)',
            'Thermal Control System (small)',
            'XM-G50 Radial Air Intake',
            'Z-400 Rechargeable Battery'
        ], part_titles_in_stage(-1))
        self.assertItemsEqual(
            ['AE-FF1 Airstream Protective Shell (1.25m)'],
            part_titles_in_stage(0))
        self.assertItemsEqual(
            ['TR-XL Stack Separator'], part_titles_in_stage(1))
        self.assertItemsEqual(
            ['Mk2-R Radial-Mount Parachute']*3, part_titles_in_stage(2))
        self.assertItemsEqual(
            ['Clamp-O-Tron Docking Port', 'Clamp-O-Tron Docking Port Jr.',
             'RE-L10 "Poodle" Liquid Fuel Engine', 'TR-XL Stack Separator'],
            part_titles_in_stage(3))
        self.assertItemsEqual([
            'RE-I5 "Skipper" Liquid Fuel Engine',
            'TR-XL Stack Separator'
        ], part_titles_in_stage(4))
        self.assertItemsEqual(
            ['TT-70 Radial Decoupler']*3, part_titles_in_stage(5))
        self.assertItemsEqual(
            ['RE-M3 "Mainsail" Liquid Fuel Engine'] +
            ['S1 SRB-KD25k "Kickback" Solid Fuel Booster']*3 +
            ['TT18-A Launch Stability Enhancer']*6,
            part_titles_in_stage(6))
        self.assertItemsEqual([], part_titles_in_stage(7))

    def test_parts_in_decouple_stage(self):
        def part_titles_in_decouple_stage(stage):
            return [part.title for part in self.parts.in_decouple_stage(stage)]
        self.assertItemsEqual(
            ['AE-FF1 Airstream Protective Shell (1.25m)'] +
            ['LT-1 Landing Struts']*3 +
            ['LY-10 Small Landing Gear', 'Mk1-2 Command Pod'],
            part_titles_in_decouple_stage(-1))
        self.assertItemsEqual([], part_titles_in_decouple_stage(0))
        self.assertItemsEqual([
            'RE-M3 "Mainsail" Liquid Fuel Engine',
            'Rockomax Jumbo-64 Fuel Tank',
            'TR-XL Stack Separator'
        ], part_titles_in_decouple_stage(4))
        self.assertItemsEqual(
            ['Aerodynamic Nose Cone']*3 +
            ['Illuminator Mk1']*3 +
            ['S1 SRB-KD25k "Kickback" Solid Fuel Booster']*3 +
            ['TT-70 Radial Decoupler']*3,
            part_titles_in_decouple_stage(5))
        self.assertItemsEqual(
            ['TT18-A Launch Stability Enhancer']*6,
            part_titles_in_decouple_stage(6))
        self.assertItemsEqual([], part_titles_in_decouple_stage(7))

    def test_modules_with_name(self):
        modules = self.parts.modules_with_name('ModuleLight')
        self.assertItemsEqual(['ModuleLight']*4, [m.name for m in modules])
        modules = self.parts.modules_with_name('DoesntExist')
        self.assertItemsEqual([], modules)

    def test_antennas(self):
        self.assertItemsEqual(
            ['Mk1-2 Command Pod', 'Communotron 16'],
            [p.part.title for p in self.parts.antennas])

    def test_cargo_bays(self):
        self.assertItemsEqual(
            ['Service Bay (2.5m)'],
            [p.part.title for p in self.parts.cargo_bays])

    def test_control_surfaces(self):
        self.assertItemsEqual(
            ['Delta-Deluxe Winglet'],
            [p.part.title for p in self.parts.control_surfaces])

    def test_decouplers(self):
        self.assertItemsEqual(
            ['TR-XL Stack Separator', 'TT-70 Radial Decoupler'] * 3,
            [p.part.title for p in self.parts.decouplers])

    def test_docking_ports(self):
        self.assertItemsEqual(
            ['Clamp-O-Tron Docking Port', 'Clamp-O-Tron Docking Port Jr.'],
            [p.part.title for p in self.parts.docking_ports])

    def test_docking_port_with_name(self):
        port = self.parts.docking_ports[0]
        if 'ModuleDockingNodeNamed' not in set(
                m.name for m in port.part.modules):
            # Docking Port Alignment Indicator mod not installed
            return
        name = port.name
        self.assertEqual(port, self.parts.docking_port_with_name(name))
        self.assertNone(self.parts.docking_port_with_name('Not the name'))
        port.name = 'Jeb\'s port'
        self.assertEqual(
            port, self.parts.docking_port_with_name('Jeb\'s port'))
        self.assertNone(self.parts.docking_port_with_name(name))
        self.assertNone(self.parts.docking_port_with_name('Not the name'))
        port.name = name

    def test_engines(self):
        self.assertItemsEqual(
            ['RE-I5 "Skipper" Liquid Fuel Engine',
             'RE-L10 "Poodle" Liquid Fuel Engine',
             'RE-M3 "Mainsail" Liquid Fuel Engine'] +
            ['S1 SRB-KD25k "Kickback" Solid Fuel Booster']*3,
            [p.part.title for p in self.parts.engines])

    def test_experiments(self):
        self.assertItemsEqual(
            ['Mk1-2 Command Pod',
             'GRAVMAX Negative Gravioli Detector',
             'PresMat Barometer'] +
            [u'Mystery Goo\u2122 Containment Unit']*3,
            [p.part.title for p in self.parts.experiments])

    def test_fairings(self):
        self.assertItemsEqual(
            ['AE-FF1 Airstream Protective Shell (1.25m)'],
            [p.part.title for p in self.parts.fairings])

    def test_intakes(self):
        self.assertItemsEqual(
            ['Adjustable Ramp Intake (Radial)', 'XM-G50 Radial Air Intake'],
            [p.part.title for p in self.parts.intakes])

    def test_legs(self):
        self.assertItemsEqual(
            ['LT-1 Landing Struts']*3,
            [p.part.title for p in self.parts.legs])

    def test_launch_clamps(self):
        self.assertItemsEqual(
            ['TT18-A Launch Stability Enhancer']*6,
            [p.part.title for p in self.parts.launch_clamps])

    def test_lights(self):
        self.assertItemsEqual(
            ['Illuminator Mk1']*3 + ['LY-10 Small Landing Gear'],
            [p.part.title for p in self.parts.lights])

    def test_parachutes(self):
        self.assertItemsEqual(
            ['Mk2-R Radial-Mount Parachute']*3,
            [p.part.title for p in self.parts.parachutes])

    def test_radiators(self):
        self.assertItemsEqual(
            ['Thermal Control System (small)'],
            [p.part.title for p in self.parts.radiators])

    def test_rcs(self):
        self.assertItemsEqual(
            ['RV-105 RCS Thruster Block'],
            [p.part.title for p in self.parts.rcs])

    def test_reaction_wheels(self):
        self.assertItemsEqual(
            ['Advanced Reaction Wheel Module, Large', 'Mk1-2 Command Pod'],
            [p.part.title for p in self.parts.reaction_wheels])

    def test_resource_converters(self):
        self.assertItemsEqual(
            ['Convert-O-Tron 250'],
            [p.part.title for p in self.parts.resource_converters])

    def test_resource_harvesters(self):
        self.assertItemsEqual(
            ['\'Drill-O-Matic Junior\' Mining Excavator'],
            [p.part.title for p in self.parts.resource_harvesters])

    def test_sensors(self):
        self.assertItemsEqual(
            ['GRAVMAX Negative Gravioli Detector', 'PresMat Barometer'],
            [p.part.title for p in self.parts.sensors])

    def test_solar_panels(self):
        self.assertItemsEqual(
            ['Gigantor XL Solar Array', 'OX-STAT Photovoltaic Panels'] +
            ['SP-L 1x6 Photovoltaic Panels'] * 2,
            [p.part.title for p in self.parts.solar_panels])

    def test_wheels(self):
        self.assertItemsEqual(
            ['LY-10 Small Landing Gear'],
            [p.part.title for p in self.parts.wheels])


if __name__ == '__main__':
    unittest.main()
