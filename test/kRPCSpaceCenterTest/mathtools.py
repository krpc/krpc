import math
import itertools
from krpc.schema.Geometry import Vector3

def v3(v):
    """ Convert a Geometry.Vector3 to a list of 3-elements """
    return [v.x, v.y, v.z]

def to_vector(v):
    r = Vector3()
    r.x = v[0]
    r.y = v[1]
    r.z = v[2]
    return r

def normalize(v):
    m = norm(v)
    return [x/m for x in v]

def rad2deg(rad):
    """ Convert radians to degrees """
    return rad * 180 / math.pi;

def norm(v):
    return math.sqrt(sum(x*x for x in v))

def dot(u,v):
    return sum(x*y for x,y in itertools.izip(u,v))

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
        return vector([x+y for x,y in itertools.izip(self.v,u)])

    def __sub__(self, u):
        return vector([x-y for x,y in itertools.izip(self.v,u)])

    def __div__(self, u):
        return vector([x/u for x in self.v])
