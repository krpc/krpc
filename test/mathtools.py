import math
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

def length(v):
    return math.sqrt(sum(x*x for x in v))

def normalize(v):
    m = length(v)
    return [x/m for x in v]

def rad2deg(rad):
    """ Convert radians to degrees """
    return rad * 180 / math.pi;
