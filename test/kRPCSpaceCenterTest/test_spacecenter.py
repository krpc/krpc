#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc

class TestBody(testingtools.TestCase):

    def test_basic(self):
        load_save('basic')
        ksp = krpc.connect()
        self.assertEqual(6.673, ksp.space_center.g)
        self.assertEqual(set([
            'Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
            'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
            'Bop', 'Pol', 'Eeloo']), set(ksp.space_center.bodies.keys()))
        self.assertEqual(set(['Test']), set(v.name for v in ksp.space_center.vessels))

if __name__ == "__main__":
    unittest.main()
