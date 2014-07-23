import math
import itertools
import functools

def rad2deg(rad):
    """ Convert radians to degrees """
    return rad * 180 / math.pi;

def norm(v):
    return math.sqrt(sum(x*x for x in v))

def normalize(v):
    m = norm(v)
    return tuple(x/m for x in v)

def dot(u,v):
    return sum(x*y for x,y in itertools.izip(u,v))

def cross(u, v):
    return tuple([u[1]*v[2] - u[2]*v[1],
                  u[2]*v[0] - u[0]*v[2],
                  u[0]*v[1] - u[1]*v[0]])

def quaternion_axis_angle(axis, angle):
    return tuple([axis[0] * math.sin(angle/2),
                  axis[1] * math.sin(angle/2),
                  axis[2] * math.sin(angle/2),
                  math.cos(angle/2)])

def quaternion_vector_mult(q, v):
    return quaternion_mult(q, quaternion_mult(tuple([v[0],v[1],v[2],0]), quaternion_conjugate(q)))[:3]

def quaternion_conjugate(q):
    return tuple([-q[0],-q[1],-q[2],q[3]])

def quaternion_mult(q, r):
    q0 = q[3]; q1 = q[0]; q2 = q[1]; q3 = q[2]
    r0 = r[3]; r1 = r[0]; r2 = r[1]; r3 = r[2]
    t0 = r0*q0 - r1*q1 - r2*q2 - r3*q3
    t1 = r0*q1 + r1*q0 - r2*q3 + r3*q2
    t2 = r0*q2 + r1*q3 + r2*q0 - r3*q1
    t3 = r0*q3 - r1*q2 + r2*q1 + r3*q0
    return tuple([t1, t2, t3, t0])

@functools.total_ordering
class vector(object):
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
        return vector([x*u for x in self])

    def __rmul__(self, u):
        return vector([x*u for x in self])

    def __add__(self, u):
        try:
            return vector([x+y for x,y in itertools.izip(self.v,u)])
        except TypeError:
            return vector([x+u for x in self.v])

    def __sub__(self, u):
        try:
            return vector([x-y for x,y in itertools.izip(self.v,u)])
        except TypeError:
            return vector([x-u for x in self.v])

    def __div__(self, u):
        return vector([x/u for x in self.v])

    def __neg__(self):
        return vector([-x for x in self.v])

    def __eq__(self, u):
        return all(x == y for x,y in itertools.izip(self.v,u))

    def __lt__(self, u):
        return all(x < y for x,y in itertools.izip(self.v,u))

    def __str__(self):
        return str(self.v)

    def __repr__(self):
        return self.v.__repr__()
