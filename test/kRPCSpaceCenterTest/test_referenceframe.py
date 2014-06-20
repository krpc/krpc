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
        cls.sc = cls.conn.space_center
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ref_vessel = cls.vessel.reference_frame
        cls.ref_obt_vessel = cls.vessel.orbital_reference_frame
        cls.ref_srf_vessel = cls.vessel.surface_reference_frame
        bodies = cls.conn.space_center.bodies
        cls.sun = bodies['Sun']
        cls.kerbin = bodies['Kerbin']
        cls.mun = bodies['Mun']
        cls.minmus = bodies['Minmus']
        cls.duna = bodies['Duna']
        cls.ike = bodies['Ike']
        cls.ref_sun = cls.sun.reference_frame
        cls.ref_srf_sun = cls.sun.surface_reference_frame
        cls.ref_kerbin = cls.kerbin.reference_frame
        cls.ref_obt_kerbin = cls.kerbin.orbital_reference_frame
        cls.ref_srf_kerbin = cls.kerbin.surface_reference_frame
        cls.ref_mun = cls.mun.reference_frame
        cls.ref_obt_mun = cls.mun.orbital_reference_frame
        cls.ref_srf_mun = cls.mun.surface_reference_frame
        cls.ref_minmus = cls.minmus.reference_frame
        cls.ref_obt_minmus = cls.minmus.orbital_reference_frame
        cls.ref_srf_minmus = cls.minmus.surface_reference_frame
        cls.ref_duna = cls.duna.reference_frame
        cls.ref_obt_duna = cls.duna.orbital_reference_frame
        cls.ref_srf_duna = cls.duna.surface_reference_frame
        cls.ref_ike = cls.ike.reference_frame
        cls.ref_obt_ike = cls.ike.orbital_reference_frame
        cls.ref_srf_ike = cls.ike.surface_reference_frame

    def test_celestial_body_position(self):
        self.assertClose((0,0,0), self.sun.position(self.ref_sun))
        self.assertClose((0,0,0), self.kerbin.position(self.ref_kerbin))
        self.assertClose((0,0,0), self.mun.position(self.ref_mun))
        p = self.kerbin.position(self.ref_sun)
        self.assertClose(self.kerbin.orbit.radius, norm(p), error=0.5)
        p = self.duna.position(self.ref_sun)
        self.assertClose(self.duna.orbit.radius, norm(p), error=0.5)
        p = self.mun.position(self.ref_kerbin)
        self.assertClose(self.mun.orbit.radius, norm(p))
        p = self.minmus.position(self.ref_kerbin)
        self.assertClose(self.minmus.orbit.radius, norm(p))
        p = self.ike.position(self.ref_duna)
        self.assertClose(self.ike.orbit.radius, norm(p), error=0.5)

    def test_celestial_body_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_sun, self.ref_sun))
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_kerbin, self.ref_kerbin))
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_mun, self.ref_mun))
        v = self.sc.transform_velocity((0,0,0), self.ref_kerbin, self.ref_sun)
        self.assertClose(norm(v), self.kerbin.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_mun, self.ref_kerbin)
        self.assertClose(norm(v), self.mun.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_minmus, self.ref_kerbin)
        self.assertClose(norm(v), self.minmus.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_duna, self.ref_sun)
        self.assertClose(norm(v), self.duna.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_ike, self.ref_duna)
        self.assertClose(norm(v), self.ike.orbit.speed)

    def test_celestial_body_surface_position(self):
        self.assertClose((0,0,0), self.sun.position(self.ref_srf_sun))
        self.assertClose((0,0,0), self.kerbin.position(self.ref_srf_kerbin))
        self.assertClose((0,0,0), self.mun.position(self.ref_srf_mun))
        p = self.kerbin.position(self.ref_srf_sun)
        self.assertClose(self.kerbin.orbit.radius, norm(p), error=0.5)
        p = self.duna.position(self.ref_srf_sun)
        self.assertClose(self.duna.orbit.radius, norm(p), error=0.5)
        p = self.mun.position(self.ref_srf_kerbin)
        self.assertClose(self.mun.orbit.radius, norm(p))
        p = self.minmus.position(self.ref_srf_kerbin)
        self.assertClose(self.minmus.orbit.radius, norm(p))
        p = self.ike.position(self.ref_srf_duna)
        self.assertClose(self.ike.orbit.radius, norm(p), error=0.5)

    def test_celestial_body_surface_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_srf_sun, self.ref_srf_sun))
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_srf_kerbin, self.ref_srf_kerbin))
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_srf_mun, self.ref_srf_mun))
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_kerbin, self.ref_srf_sun)
        self.assertClose(norm(v), self.kerbin.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_mun, self.ref_srf_kerbin)
        self.assertClose(norm(v), self.mun.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_minmus, self.ref_srf_kerbin)
        self.assertClose(norm(v), self.minmus.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_duna, self.ref_srf_sun)
        self.assertClose(norm(v), self.duna.orbit.speed)
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_ike, self.ref_srf_duna)
        self.assertClose(norm(v), self.ike.orbit.speed)

    def test_vessel_position(self):
        self.assertClose((0,0,0), self.vessel.position(self.ref_vessel))
        p = self.vessel.position(self.ref_kerbin)
        self.assertClose(self.kerbin.equatorial_radius + 200000, norm(p), error = 10)

    def test_vessel_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_vessel, self.ref_vessel))
        v = self.sc.transform_velocity((0,0,0), self.ref_vessel, self.ref_kerbin)
        self.assertClose(self.vessel.orbit.speed, norm(v))

    def test_vessel_surface_position(self):
        self.assertClose((0,0,0), self.vessel.position(self.ref_srf_vessel))
        p = self.vessel.position(self.ref_kerbin)
        self.assertClose(self.kerbin.equatorial_radius + 200000, norm(p), error = 10)

    def test_vessel_surface_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_srf_vessel, self.ref_srf_vessel))
        rotational_speed = self.kerbin.rotational_speed * self.vessel.orbit.radius
        v = self.sc.transform_velocity((0,0,0), self.ref_srf_vessel, self.ref_kerbin)
        self.assertClose(rotational_speed, norm(v))
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_vessel, self.ref_srf_vessel)
        self.assertClose(self.vessel.orbit.speed - rotational_speed, norm(v))

    def test_orbital_body_position(self):
        self.assertRaises(krpc.client.RPCError, getattr, self.sun, 'orbital_reference_frame')
        self.assertClose((0,0,0), self.kerbin.position(self.ref_obt_mun))
        p = self.kerbin.position(self.ref_obt_kerbin)
        self.assertClose(self.kerbin.orbit.radius, norm(p))
        p = self.duna.position(self.ref_obt_duna)
        self.assertClose(self.duna.orbit.radius, norm(p), error=0.1)
        p = self.mun.position(self.ref_obt_mun)
        self.assertClose(self.mun.orbit.radius, norm(p))
        p = self.minmus.position(self.ref_obt_minmus)
        self.assertClose(self.minmus.orbit.radius, norm(p))
        p = self.ike.position(self.ref_obt_ike)
        self.assertClose(self.ike.orbit.radius, norm(p), error=0.5)

    def test_orbital_body_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_obt_kerbin, self.ref_obt_kerbin))
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_mun, self.ref_obt_kerbin)
        self.assertClose(self.mun.orbit.speed, norm(v))
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_minmus, self.ref_obt_kerbin)
        self.assertClose(self.minmus.orbit.speed, norm(v))
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_ike, self.ref_obt_duna)
        self.assertClose(self.ike.orbit.speed, norm(v))

    def test_orbital_vessel_position(self):
        p = self.vessel.position(self.ref_obt_vessel)
        self.assertClose(self.kerbin.equatorial_radius + 200000, norm(p), error = 10)

    def test_orbital_vessel_velocity(self):
        self.assertClose((1,2,3), self.sc.transform_velocity((1,2,3), self.ref_obt_vessel, self.ref_obt_vessel))
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_vessel, self.ref_vessel)
        self.assertClose((0,0,0), v)
        v = self.sc.transform_velocity((0,0,0), self.ref_obt_vessel, self.ref_srf_vessel)
        rotational_speed = self.kerbin.rotational_speed * self.vessel.orbit.radius
        self.assertClose(self.vessel.orbit.speed - rotational_speed, norm(v))

    def test_maneuver_position(self):
        node = self.vessel.control.add_node(self.sc.ut, 100, 0, 0)
        p = self.vessel.position(node.reference_frame)
        # TODO: large error
        self.assertClose((0,0,0), p, error=100)

    def test_maneuver_velocity(self):
        node = self.vessel.control.add_node(self.sc.ut, 100, 0, 0)
        v = self.sc.transform_velocity((0,0,0), node.reference_frame, self.ref_obt_vessel)
        self.assertClose((0,0,0), v)
        v = self.sc.transform_velocity((0,0,0), node.reference_frame, self.ref_srf_vessel)
        rotational_speed = self.kerbin.rotational_speed * self.vessel.orbit.radius
        self.assertClose(self.vessel.orbit.speed - rotational_speed, norm(v))

    def test_part_position(self):
        # TODO: implement
        pass

    def test_part_velocity(self):
        # TODO: implement
        pass

    def test_maneuver_velocity_same_as_orbit(self):
        vessel = self.conn.space_center.active_vessel
        node = vessel.control.add_node(self.conn.space_center.ut, 100, 0, 0)
        v = self.conn.space_center.transform_velocity((1,2,3), node.reference_frame, vessel.orbit.reference_frame)
        self.assertClose((1,2,3), v)

if __name__ == "__main__":
    unittest.main()
