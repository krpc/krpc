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
            self.assertClose(obj.orbit.radius, norm(p), error=10)
        # Check position agrees with that calculated from bodies orbit
        if obj.name in ('Kerbin','Mun','Minmus','Test'):
            ref = obj.orbit.body.reference_frame
            expected_pos = self.compute_position(obj, ref)
            actual_pos = tuple(x / 1000000 for x in obj.position(ref))
            self.assertClose(expected_pos, actual_pos, error=1)

    def compute_position(self, obj, ref):
        """ Compute the objects position in the given reference frame (in Mm) from it's orbital elements """
        orbit = obj.orbit

        major_axis = orbit.semi_major_axis / 1000000
        minor_axis = orbit.semi_minor_axis / 1000000

        eccentricity = orbit.eccentricity
        mean_anomaly = orbit.mean_anomaly
        eccentric_anomaly = orbit.eccentric_anomaly

        x = major_axis * math.cos(eccentric_anomaly)
        z = minor_axis * math.sin(eccentric_anomaly)
        pos = (x,0,z)
        pos_magnitude = norm(pos)
        pos_direction = normalize(pos)
        self.assertClose(1, norm(pos_direction))

        angle = orbit.argument_of_periapsis
        rotation = quaternion_axis_angle((0,1,0), -angle)
        pos = quaternion_vector_mult(rotation, pos)

        angle = orbit.inclination
        rotation = quaternion_axis_angle((1,0,0), -angle)
        pos = quaternion_vector_mult(rotation, pos)

        angle = orbit.longitude_of_ascending_node
        rotation = quaternion_axis_angle((0,1,0), -angle)
        pos = quaternion_vector_mult(rotation, pos)

        reference_normal = orbit.reference_plane_normal(ref)
        self.assertClose((0,1,0), reference_normal)
        reference_direction = orbit.reference_plane_direction(ref)
        reference_angle = math.acos(dot((1,0,0),reference_direction))
        reference_rotation = quaternion_axis_angle((0,1,0), reference_angle)
        x_rotated = quaternion_vector_mult(reference_rotation, (1,0,0))
        self.assertClose(x_rotated, reference_direction)
        return quaternion_vector_mult(reference_rotation, pos)

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
