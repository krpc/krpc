def v3(v):
    """ Convert a Geometry.Vector3 to a list of 3-elements """
    return [v.x, v.y, v.z]

def rad2deg(rad):
    """ Convert radians to degrees """
    return rad * 180 / math.pi;
