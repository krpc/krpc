import math
import itertools
import functools
from krpc.schema.Geometry import Vector3

def rad2deg(rad):
    """ Convert radians to degrees """
    return rad * 180 / math.pi;

def norm(v):
    return math.sqrt(sum(x*x for x in v))

def normalize(v):
    m = norm(v)
    return vector([x/m for x in v])

def dot(u,v):
    return sum(x*y for x,y in itertools.izip(u,v))

@functools.total_ordering
class vector(object):
    def __init__(self, v):
        if type(v) == Vector3:
            self.v = [v.x, v.y, v.z]
        else:
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
