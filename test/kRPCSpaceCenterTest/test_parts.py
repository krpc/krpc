import unittest
import testingtools
import krpc

class TestParts(testingtools.TestCase):

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

    def test_all_parts(self):
        parts = self.parts.all
        part_titles = sorted(p.title for p in parts)
        self.assertEquals(
            part_titles,
            ['Advanced Reaction Wheel Module, Large', 'Aerodynamic Nose Cone',
             'Aerodynamic Nose Cone', 'Aerodynamic Nose Cone',
             'Clamp-O-Tron Docking Port', 'Clamp-O-Tron Docking Port Jr.',
             'Communotron 16', 'EAS-4 Strut Connector', 'EAS-4 Strut Connector',
             'EAS-4 Strut Connector', 'FL-R1 RCS Fuel Tank',
             'GRAVMAX Negative Gravioli Detector', 'Gigantor XL Solar Array',
             'Illuminator Mk1', 'Illuminator Mk1', 'Illuminator Mk1',
             'LT-1 Landing Struts', 'LT-1 Landing Struts', 'LT-1 Landing Struts',
             'Mk1-2 Command Pod', 'Mk16-XL Parachute', 'Mk2-R Radial-Mount Parachute',
             'Mk2-R Radial-Mount Parachute', 'Mk2-R Radial-Mount Parachute',
             u'Mystery Goo\u2122 Containment Unit', u'Mystery Goo\u2122 Containment Unit',
             u'Mystery Goo\u2122 Containment Unit', 'OX-STAT Photovoltaic Panels',
             'PresMat Barometer', 'Reflectron DP-10',
             'Reflectron KR-7', 'Rockomax "Mainsail" Liquid Engine',
             'Rockomax "Poodle" Liquid Engine', 'Rockomax "Skipper" Liquid Engine',
             'Rockomax Jumbo-64 Fuel Tank', 'Rockomax X200-32 Fuel Tank',
             'Rockomax X200-8 Fuel Tank', 'S1 SRB-KD25k', 'S1 SRB-KD25k', 'S1 SRB-KD25k',
             'SP-L 1x6 Photovoltaic Panels', 'SP-L 1x6 Photovoltaic Panels',
             'TR-XL Stack Separator', 'TR-XL Stack Separator', 'TR-XL Stack Separator',
             'TT-70 Radial Decoupler', 'TT-70 Radial Decoupler', 'TT-70 Radial Decoupler',
             'TT18-A Launch Stability Enhancer', 'TT18-A Launch Stability Enhancer',
             'TT18-A Launch Stability Enhancer', 'TT18-A Launch Stability Enhancer',
             'TT18-A Launch Stability Enhancer', 'TT18-A Launch Stability Enhancer',
             'Z-400 Rechargeable Battery'])

    def test_root_part(self):
        root = self.parts.root
        self.assertEqual('Mark1-2Pod', root.name)
        self.assertEqual('Mk1-2 Command Pod', root.title)
        self.assertEqual(self.vessel, root.vessel)
        self.assertEqual(None, root.parent)
        self.assertGreater(len(root.children), 0)

    def test_parts_with_name(self):
        parts = self.parts.with_name('spotLight1')
        self.assertEqual(['spotLight1', 'spotLight1', 'spotLight1'], [p.name for p in parts])
        parts = self.parts.with_name('doesntExist')
        self.assertEqual(len(parts), 0)

    def test_parts_with_title(self):
        parts = self.parts.with_title('Illuminator Mk1')
        self.assertEqual(
            ['Illuminator Mk1', 'Illuminator Mk1', 'Illuminator Mk1'],
            [p.title for p in parts])
        parts = self.parts.with_title('Doesn\'t Exist')
        self.assertEqual(len(parts), 0)

    def test_parts_with_module(self):
        parts = self.parts.with_module('ModuleLight')
        self.assertEqual(['spotLight1', 'spotLight1', 'spotLight1'], [p.name for p in parts])
        parts = self.parts.with_module('DoesntExist')
        self.assertEqual(len(parts), 0)

    def test_parts_in_stage(self):
        #TODO: implement
        pass

    def test_modules_with_name(self):
        modules = self.parts.modules_with_name('ModuleLight')
        self.assertEqual(['ModuleLight', 'ModuleLight', 'ModuleLight'], [m.name for m in modules])
        modules = self.parts.modules_with_name('DoesntExist')
        self.assertEqual(len(modules), 0)

    def test_modules_in_stage(self):
        #TODO: implement
        pass

    def test_engines(self):
        self.assertEqual(
            ['Rockomax "Mainsail" Liquid Engine', 'Rockomax "Poodle" Liquid Engine',
             'Rockomax "Skipper" Liquid Engine', 'S1 SRB-KD25k', 'S1 SRB-KD25k', 'S1 SRB-KD25k'],
            sorted(e.part.title for e in self.parts.engines))

    def test_engines_in_stage(self):
        #TODO: implement
        pass

    def test_solar_panels(self):
        self.assertEqual(
            ['Gigantor XL Solar Array', 'OX-STAT Photovoltaic Panels',
             'SP-L 1x6 Photovoltaic Panels', 'SP-L 1x6 Photovoltaic Panels'],
            sorted(e.part.title for e in self.parts.solar_panels))

    def test_solar_panels_in_stage(self):
        #TODO: implement
        pass

    def test_sensors(self):
        self.assertEqual(
            ['GRAVMAX Negative Gravioli Detector', 'PresMat Barometer'],
            sorted(e.part.title for e in self.parts.sensors))

    def test_sensors_in_stage(self):
        #TODO: implement
        pass

    def test_decouplers(self):
        self.assertEqual(
            ['TR-XL Stack Separator', 'TR-XL Stack Separator', 'TR-XL Stack Separator',
             'TT-70 Radial Decoupler', 'TT-70 Radial Decoupler', 'TT-70 Radial Decoupler'],
            sorted(e.part.title for e in self.parts.decouplers))

    def test_decouplers_in_stage(self):
        #TODO: implement
        pass

    def test_lights(self):
        self.assertEqual(
            ['Illuminator Mk1', 'Illuminator Mk1', 'Illuminator Mk1'],
            sorted(e.part.title for e in self.parts.lights))

    def test_lights_in_stage(self):
        #TODO: implement
        pass

    def test_parachutes(self):
        self.assertEqual(
            ['Mk16-XL Parachute', 'Mk2-R Radial-Mount Parachute',
             'Mk2-R Radial-Mount Parachute', 'Mk2-R Radial-Mount Parachute'],
            sorted(e.part.title for e in self.parts.parachutes))

    def test_parachutes_in_stage(self):
        #TODO: implement
        pass

if __name__ == "__main__":
    unittest.main()
