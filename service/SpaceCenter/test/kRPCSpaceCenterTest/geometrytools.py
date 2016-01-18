import math
from mathtools import *

def compute_position(obj, ref):
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
    #self.assertClose(1, norm(pos_direction))

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
    #self.assertClose((0,1,0), reference_normal)
    reference_direction = orbit.reference_plane_direction(ref)
    reference_angle = math.acos(dot((1,0,0),reference_direction))
    reference_rotation = quaternion_axis_angle((0,1,0), reference_angle)
    x_rotated = quaternion_vector_mult(reference_rotation, (1,0,0))
    #self.assertClose(x_rotated, reference_direction)
    return quaternion_vector_mult(reference_rotation, pos)
