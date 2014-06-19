#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
from mathtools import norm
import krpc
import time
import itertools

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
        self.assertEqual(6.673e-11, self.conn.space_center.g)

    def test_warp_to(self):
        t = self.conn.space_center.ut + (5*60)
        self.conn.space_center.warp_to(t)
        self.assertClose(t, self.conn.space_center.ut, error=2)

    def test_transform_position_same_reference_frame(self):
        r = self.conn.space_center.active_vessel.reference_frame
        self.assertEqual((1,2,3), self.conn.space_center.transform_position((1,2,3), r, r))

    def test_transform_position_between_celestial_bodies(self):
        bodies = self.conn.space_center.bodies
        sun = bodies['Sun']
        kerbin = bodies['Kerbin']
        mun = bodies['Mun']
        ref_sun = sun.reference_frame
        ref_kerbin = kerbin.reference_frame
        ref_mun = mun.reference_frame

        p = self.conn.space_center.transform_position((0,0,0), ref_kerbin, ref_mun)
        self.assertClose(mun.orbit.radius, norm(p))

        p = self.conn.space_center.transform_position((0,0,0), ref_sun, ref_kerbin)
        self.assertClose(kerbin.orbit.radius, norm(p))

    def test_transform_position_between_vessel_and_celestial_body(self):
        bodies = self.conn.space_center.bodies
        vessel = self.conn.space_center.active_vessel
        kerbin = bodies['Kerbin']
        ref_vessel = vessel.reference_frame
        ref_kerbin = kerbin.reference_frame

        p = self.conn.space_center.transform_position((0,0,0), ref_vessel, ref_kerbin)
        self.assertClose(vessel.orbit.radius, norm(p), error=0.01)

    def test_transform_position_between_vessel_and_celestial_bodies(self):
        bodies = self.conn.space_center.bodies
        vessel = self.conn.space_center.active_vessel
        sun = bodies['Sun']
        kerbin = bodies['Kerbin']
        ref_vessel = vessel.reference_frame
        ref_sun = sun.reference_frame
        ref_kerbin = kerbin.reference_frame

        p0 = self.conn.space_center.transform_position((0,0,0), ref_vessel, ref_kerbin)
        p1 = self.conn.space_center.transform_position((0,0,0), ref_vessel, ref_sun)
        p2 = self.conn.space_center.transform_position((0,0,0), ref_kerbin, ref_sun)

        p3 = tuple(x-y for (x,y) in itertools.izip(p1,p2))
        #TODO: large error
        self.assertClose(norm(p0), norm(p3), error=500)

    def test_transform_velocity_same_reference_frame(self):
        r = self.conn.space_center.active_vessel.reference_frame
        self.assertEqual((1,2,3), self.conn.space_center.transform_velocity((1,2,3), r, r))

    def test_transform_position_between_celestial_bodies(self):
        bodies = self.conn.space_center.bodies
        sun = bodies['Sun']
        kerbin = bodies['Kerbin']
        mun = bodies['Mun']
        ref_sun = sun.reference_frame
        ref_kerbin = kerbin.reference_frame
        ref_mun = mun.reference_frame

        v1 = self.conn.space_center.transform_velocity((0,0,0), ref_mun, ref_kerbin)
        v2 = self.conn.space_center.transform_velocity((0,0,0), ref_kerbin, ref_mun)
        self.assertClose(mun.orbit.speed, norm(v1))
        self.assertClose(mun.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

        v1 = self.conn.space_center.transform_velocity((0,0,0), ref_kerbin, ref_sun)
        v2 = self.conn.space_center.transform_velocity((0,0,0), ref_sun, ref_kerbin)
        self.assertClose(kerbin.orbit.speed, norm(v1))
        self.assertClose(kerbin.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

    def test_transform_velocity_between_vessel_and_celestial_body(self):
        bodies = self.conn.space_center.bodies
        vessel = self.conn.space_center.active_vessel
        kerbin = bodies['Kerbin']
        ref_vessel = vessel.reference_frame
        ref_kerbin = kerbin.reference_frame

        v = self.conn.space_center.transform_velocity((0,0,0), ref_vessel, ref_kerbin)
        self.assertClose(vessel.orbit.speed, norm(v))

    def test_transform_velocity_between_vessel_and_celestial_bodies(self):
        bodies = self.conn.space_center.bodies
        vessel = self.conn.space_center.active_vessel
        sun = bodies['Sun']
        kerbin = bodies['Kerbin']
        ref_vessel = vessel.reference_frame
        ref_sun = sun.reference_frame
        ref_kerbin = kerbin.reference_frame

        v0 = self.conn.space_center.transform_velocity((0,0,0), ref_vessel, ref_kerbin)
        v1 = self.conn.space_center.transform_velocity((0,0,0), ref_vessel, ref_sun)
        v2 = self.conn.space_center.transform_velocity((0,0,0), ref_kerbin, ref_sun)

        v3 = tuple(x-y for (x,y) in itertools.izip(v1,v2))
        self.assertClose(norm(v0), norm(v3))

if __name__ == "__main__":
    unittest.main()
