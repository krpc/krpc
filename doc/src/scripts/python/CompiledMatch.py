import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
VesselSituation = conn.space_center.VesselSituation


def where() -> str:
    match vessel.situation:
        case (
            VesselSituation.pre_launch
            | VesselSituation.landed
            | VesselSituation.splashed
        ):
            return "on the surface"
        case VesselSituation.orbiting | VesselSituation.escaping:
            return "in space"
        case _ if vessel.flight().mean_altitude < 1000:
            return "near the ground"
        case _:
            return "in flight"


with conn.add_stream(where) as situation:
    print("The vessel is", situation())
