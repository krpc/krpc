#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc

class TestBody(testingtools.TestCase):

    def test_basic(self):
        load_save('basic')
        ksp = krpc.connect()

        active = ksp.space_center.active_vessel
        self.assertEqual(ksp.space_center.active_vessel, active)
        self.assertEqual(active.name, 'Test')

        vessels = ksp.space_center.vessels
        self.assertEqual(set(['Test']), set(v.name for v in vessels))
        self.assertEqual(ksp.space_center.vessels, vessels)

        self.assertEqual(set([
            'Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
            'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
            'Bop', 'Pol', 'Eeloo']), set(ksp.space_center.bodies.keys()))

        self.assertClose(290, ksp.space_center.ut, error=5)

        self.assertEqual(6.673, ksp.space_center.g)

if __name__ == "__main__":
    unittest.main()
