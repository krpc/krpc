#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time

class TestBody(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('basic')
        cls.conn = krpc.connect()

    def test_active_vessel(self):
        active = self.conn.space_center.active_vessel
        self.assertEqual(active.name, 'Test')
        self.assertEqual(self.conn.space_center.active_vessel, active)

    def test_vessels(self):
        vessels = self.conn.space_center.vessels
        self.assertEqual(set(['Test']), set(v.name for v in vessels))
        self.assertEqual(self.conn.space_center.vessels, vessels)

    def test_bodies(self):
        self.assertEqual(set([
            'Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
            'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
            'Bop', 'Pol', 'Eeloo']), set(self.conn.space_center.bodies.keys()))

    def test_ut(self):
        self.assertClose(290, self.conn.space_center.ut, error=5)
        time.sleep(1)
        self.assertClose(291, self.conn.space_center.ut, error=5)

    def test_g(self):
        self.assertEqual(6.673, self.conn.space_center.g)

    def test_warp_to(self):
        t = self.conn.space_center.ut + (5*60)
        self.conn.space_center.warp_to(t)
        self.assertClose(t, self.conn.space_center.ut, error=2)

if __name__ == "__main__":
    unittest.main()
