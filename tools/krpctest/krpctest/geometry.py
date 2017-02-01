import math
import functools


def rad2deg(rad):
    return rad * 180 / math.pi


def norm(v):
    return math.sqrt(sum(x*x for x in v))


def normalize(v):
    mag = norm(v)
    return tuple(x/mag for x in v)


def dot(u, v):
    return sum(x*y for x, y in zip(u, v))


def cross(u, v):
    return (
        u[1]*v[2] - u[2]*v[1],
        u[2]*v[0] - u[0]*v[2],
        u[0]*v[1] - u[1]*v[0]
    )


def quaternion_axis_angle(axis, angle):
    return (
        axis[0] * math.sin(angle/2),
        axis[1] * math.sin(angle/2),
        axis[2] * math.sin(angle/2),
        math.cos(angle/2)
    )


def quaternion_vector_mult(q, v):
    r = quaternion_mult((v[0], v[1], v[2], 0), quaternion_conjugate(q))
    return quaternion_mult(q, r)[:3]


def quaternion_conjugate(q):
    return (-q[0], -q[1], -q[2], q[3])


def quaternion_mult(q, r):
    q0, q1, q2, q3 = q
    r0, r1, r2, r3 = r
    t0 = + r0*q3 - r1*q2 + r2*q1 + r3*q0
    t1 = + r0*q2 + r1*q3 - r2*q0 + r3*q1
    t2 = - r0*q1 + r1*q0 + r2*q3 + r3*q2
    t3 = - r0*q0 - r1*q1 - r2*q2 + r3*q3
    return (t0, t1, t2, t3)


def vector(v):
    return Vector(v)


@functools.total_ordering
class Vector(object):
    def __init__(self, v):
        self.v = v

    def __len__(self):
        return len(self.v)

    def __iter__(self):
        return self.v.__iter__()

    def __getitem__(self, key):
        return self.v[key]

    def __setitem__(self, key, value):
        self.v[key] = value

    def __mul__(self, u):
        return Vector([x*u for x in self])

    def __rmul__(self, u):
        return Vector([x*u for x in self])

    def __add__(self, u):
        try:
            return Vector([x+y for x, y in zip(self.v, u)])
        except TypeError:
            return Vector([x+u for x in self.v])

    def __sub__(self, u):
        try:
            return Vector([x-y for x, y in zip(self.v, u)])
        except TypeError:
            return Vector([x-u for x in self.v])

    def __div__(self, u):
        return Vector([x/u for x in self.v])

    def __neg__(self):
        return Vector([-x for x in self.v])

    def __eq__(self, u):
        return all(x == y for x, y in zip(self.v, u))

    def __lt__(self, u):
        return all(x < y for x, y in zip(self.v, u))

    def __str__(self):
        return str(self.v)

    def __repr__(self):
        return self.v.__repr__()


def compute_position(obj, ref):
    """ Compute the objects position in the given
        reference frame (in Mm) from its orbital elements """
    orbit = obj.orbit
    major_axis = orbit.semi_major_axis / 1000000
    minor_axis = orbit.semi_minor_axis / 1000000
    eccentric_anomaly = orbit.eccentric_anomaly

    x = major_axis * math.cos(eccentric_anomaly)
    z = minor_axis * math.sin(eccentric_anomaly)
    pos = (x, 0, z)

    angle = orbit.argument_of_periapsis
    rotation = quaternion_axis_angle((0, 1, 0), -angle)
    pos = quaternion_vector_mult(rotation, pos)

    angle = orbit.inclination
    rotation = quaternion_axis_angle((1, 0, 0), -angle)
    pos = quaternion_vector_mult(rotation, pos)

    angle = orbit.longitude_of_ascending_node
    rotation = quaternion_axis_angle((0, 1, 0), -angle)
    pos = quaternion_vector_mult(rotation, pos)

    reference_direction = orbit.reference_plane_direction(ref)
    reference_angle = math.acos(dot((1, 0, 0), reference_direction))
    reference_rotation = quaternion_axis_angle((0, 1, 0), reference_angle)
    return quaternion_vector_mult(reference_rotation, pos)
