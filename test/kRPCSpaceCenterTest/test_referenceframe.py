#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
from mathtools import *
import krpc
import time
import itertools
import math

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

        cls.bodies = cls.conn.space_center.bodies

    def check_object_position(self, obj, ref):
        # Check (0,0,0) position is at object position
        self.assertClose((0,0,0), obj.position(ref))
        # Check norm of object position is same as objects orbital radius
        if obj.orbit is not None:
            p = obj.orbit.body.position(ref)
            self.assertClose(obj.orbit.radius, norm(p), error=5)

    def test_celestial_body_position(self):
        for body in self.bodies.values():
            self.check_object_position(body, body.reference_frame)

    def test_celestial_body_orbital_position(self):
        for body in self.bodies.values():
            if body.orbit is not None:
                self.check_object_position(body, body.orbital_reference_frame)
            else:
                self.assertRaises(krpc.client.RPCError, getattr, body, 'orbital_reference_frame')

    def test_celestial_body_surface_position(self):
        for body in self.bodies.values():
            if body.orbit is not None:
                self.check_object_position(body, body.surface_reference_frame)
            else:
                self.assertRaises(krpc.client.RPCError, getattr, body, 'surface_reference_frame')

    def test_vessel_position(self):
        self.check_object_position(self.vessel, self.vessel.reference_frame)

    def test_vessel_orbital_position(self):
        self.check_object_position(self.vessel, self.vessel.orbital_reference_frame)

    def test_vessel_surface_position(self):
        self.check_object_position(self.vessel, self.vessel.surface_reference_frame)

    def test_part_position(self):
        # TODO: implement
        pass

    def test_part_orbital_position(self):
        # TODO: implement
        pass

    def test_part_surface_position(self):
        # TODO: implement
        pass

    def test_maneuver_position(self):
        node = self.vessel.control.add_node(self.sc.ut, 100, 0, 0)
        p = self.vessel.position(node.reference_frame)
        # TODO: large error
        self.assertClose((0,0,0), p, error=50)

if __name__ == "__main__":
    unittest.main()
