"""The manifest: how to run each documentation example, and what it must do.

Each Example covers the files of every language that share its key."""

from __future__ import annotations

from doc.test import checks, flights
from doc.test.spec import (
    NUMBER,
    Example,
    Exit,
    Flight,
    Interactive,
    Loop,
    Pad,
    Setup,
    Values,
)

DOC_CRAFT = "doc/src/crafts"
# The game's own craft, in its Ships folder
STOCK = ""

# In the order the test classes run: the shared setups first, then those that relaunch for
# every example
SETUPS = {
    "orbit": Setup(),
    "flight": Setup(placement=Flight()),
    "parts-tree": Setup(craft="PartsTree", directory=DOC_CRAFT, placement=Pad()),
    "docking-ports": Setup(craft="PartsDockingPort", placement=Pad()),
    "orbit-fresh": Setup(fresh=True),
    "parts-tree-fresh": Setup(
        craft="PartsTree", directory=DOC_CRAFT, placement=Pad(), fresh=True
    ),
    "launch-kerbal-x": Setup(fresh=True, ships=(("Kerbal X", "Kerbal X"),)),
    "launch-into-orbit": Setup(
        craft="AeroEquus", directory=STOCK, placement=Pad(), fresh=True
    ),
    "suborbital-flight": Setup(
        craft="Kerbal 2", directory=STOCK, placement=Pad(), fresh=True
    ),
    # Two vessels joined port to port, which _place_docking_vessels undocks
    "docking": Setup(craft="PartsDockingPortInFlight", fresh=True),
    # Mods
    "raster-prop-monitor": Setup(
        craft="RasterPropMonitor",
        directory="service/RasterPropMonitor/test/craft",
        placement=Pad(),
        mods=("RasterPropMonitor",),
    ),
}


def _exits(setup: str, *keys: str, **fields) -> list[Example]:
    return [Example(key, setup, **fields) for key in keys]


def _loops(setup: str, *keys: str, **fields) -> list[Example]:
    return [Example(key, setup, run=Loop(), **fields) for key in keys]


def _place_docking_vessels(test) -> None:
    # Undock the pair, and place the active vessel 40 m out from the other vessel's port and
    # off its axis, at rest relative to it. Then pick the ports, as the tutorial asks the
    # player to.
    conn = test.connect()
    space_center = conn.space_center
    vessel = space_center.active_vessel
    other = vessel.parts.docking_ports[0].undock()
    port = vessel.parts.docking_ports[0]
    target = other.parts.docking_ports[0]
    body = vessel.orbit.body
    conn.debug.set_position(
        space_center.transform_position(
            (15, 40, 10), target.reference_frame, body.reference_frame
        )
    )
    frame = body.non_rotating_reference_frame
    conn.debug.set_velocity(other.velocity(frame), frame)
    for each in (vessel, other):
        conn.debug.set_angular_velocity((0, 0, 0), None, each)
    vessel.parts.controlling = port.part
    space_center.target_docking_port = target
    ready = space_center.DockingPortState.ready
    test.wait_until(
        lambda: port.state == ready and target.state == ready,
        message="the docking ports to be ready",
    )


_warp_to = checks.WarpToApoapsis()
_landing_site_calls = checks.CallCount(3)
_visual_debugging_calls = checks.CallCount(3)

# The combined specific impulse, as the parts tutorial prints it
_ISP = Values(checks.specific_impulse, pattern=r"Isp = (%s)" % NUMBER)


def _fire_engines(test) -> None:
    vessel = test.connect().space_center.active_vessel
    for engine in vessel.parts.engines:
        engine.active = True
    vessel.control.throttle = 1
    # The thrust builds over a few frames, and a compiled example reads it at once
    test.wait_until(lambda: vessel.thrust > 0, message="the engines to fire")


TUTORIALS = [
    Example(
        "tutorials/suborbital-flight/SubOrbitalFlight",
        "suborbital-flight",
        run=Exit(timeout=1200),
        monitor=flights.SubOrbitalFlight,
        slow=True,
    ),
    Example(
        "tutorials/launch-into-orbit/LaunchIntoOrbit",
        "launch-into-orbit",
        run=Exit(timeout=1200),
        monitor=flights.LaunchIntoOrbit,
        slow=True,
    ),
    Example(
        "tutorials/docking/Docking",
        "docking",
        prepare=_place_docking_vessels,
        run=Exit(timeout=600),
        monitor=flights.Docking,
        slow=True,
    ),
    Example(
        "tutorials/parts/CombinedIsp",
        "orbit",
        prepare=_fire_engines,
        values=_ISP,
    ),
    Example(
        "tutorials/parts/CombinedIspFunction",
        "orbit",
        prepare=_fire_engines,
        values=_ISP,
    ),
    Example(
        "tutorials/parts/ControlFromHere",
        "docking-ports",
        check=checks.controlling("Clamp-O-Tron Docking Port"),
    ),
    *_exits(
        "parts-tree-fresh",
        "tutorials/parts/DeployParachutes",
        "tutorials/parts/DeployParachutesFunction",
        check=checks.parachutes_deployed,
    ),
    *_exits(
        "orbit",
        "tutorials/user-interface/UserInterface",
        "tutorials/drawing/DrawingExample",
        run=Interactive(checks.keeps_calling, then=Loop(prints=False)),
    ),
    Example(
        "tutorials/raster-prop-monitor/ScreenMenu",
        "raster-prop-monitor",
        run=Interactive(checks.screen_menu, then=Loop(prints=False)),
    ),
]

USER_GUIDE = [
    # Parts
    Example(
        "user-guide/parts/AttachmentModes",
        "parts-tree",
        lines=checks.attachment_modes,
    ),
    Example("user-guide/parts/TreeTraversal", "parts-tree", lines=checks.part_titles),
    Example(
        "user-guide/parts/PartModules",
        "orbit-fresh",
        check=checks.all_of(
            checks.engines_at_half_thrust, checks.solar_panels_extended
        ),
    ),
    # Game scenes and saves
    Example(
        "user-guide/game-scenes-and-saves/ChangeScene",
        "orbit-fresh",
        check=checks.scene("space_center"),
    ),
    Example(
        "user-guide/game-scenes-and-saves/LaunchVessel",
        "launch-kerbal-x",
        check=checks.vessel_named("Kerbal X"),
    ),
    # Time warp
    Example(
        "user-guide/time-warp/WarpFactor",
        "orbit",
        values=Values(checks.maximum_rails_warp_factor),
        check=checks.rails_warp_factor(3),
    ),
    Example(
        "user-guide/time-warp/WarpTo",
        "orbit",
        prepare=_warp_to.prepare,
        run=Exit(timeout=600),
        check=_warp_to.check,
    ),
    # Vectors and rotations
    # The vessel turns slowly relative to the frames, between the print and the check
    Example(
        "user-guide/vectors-and-rotations/AngleToPrograde",
        "orbit",
        values=Values(checks.angle_to_prograde, relative=0, absolute=1),
    ),
    Example(
        "user-guide/vectors-and-rotations/Quaternion",
        "orbit",
        values=Values(checks.rotation, relative=0, absolute=0.02),
    ),
    Example(
        "user-guide/vectors-and-rotations/Vector3",
        "orbit",
        values=Values(checks.prograde, relative=0, absolute=0.02),
    ),
    # Reference frames
    *_exits(
        "orbit",
        "user-guide/reference-frames/VesselPosition",
    ),
    *_exits(
        "orbit",
        "user-guide/reference-frames/NavballDirections",
        "user-guide/reference-frames/OrbitalDirections",
        run=Exit(timeout=600),
    ),
    # Surface prograde is the direction of flight through the atmosphere
    Example(
        "user-guide/reference-frames/SurfacePrograde",
        "flight",
        run=Exit(timeout=600),
    ),
    *_loops(
        "orbit",
        "user-guide/reference-frames/AngleOfAttack",
        "user-guide/reference-frames/NavballSpeed",
        "user-guide/reference-frames/VesselSpeed",
        "user-guide/reference-frames/VesselVelocity",
    ),
    # The examples draw, then wait
    Example(
        "user-guide/reference-frames/LandingSite",
        "orbit",
        prepare=_landing_site_calls.prepare,
        run=Loop(prints=False),
        check=_landing_site_calls.check,
    ),
    Example(
        "user-guide/reference-frames/VisualDebugging",
        "orbit",
        prepare=_visual_debugging_calls.prepare,
        run=Loop(prints=False),
        check=_visual_debugging_calls.check,
    ),
    # Auto-pilot
    Example(
        "user-guide/autopilot/InfoUi",
        "orbit",
        check=checks.autopilot(show_info_ui=True),
    ),
    Example(
        "user-guide/autopilot/LimitAngularVelocity",
        "orbit",
        check=checks.autopilot(
            limit_angular_velocity=False, max_angular_velocity=(0.3, 0.5, 0.3)
        ),
    ),
    Example(
        "user-guide/autopilot/RateFilter",
        "orbit",
        check=checks.autopilot(
            pitch_yaw_rate_filter_mode=lambda conn: (
                conn.space_center.RateFilterMode.notch
            ),
            pitch_yaw_oscillation_frequency=1.4,
        ),
    ),
    # Each ends on its last target, pitch and heading in the surface frame
    Example(
        "user-guide/autopilot/DirectionAndUp",
        "orbit",
        run=Exit(timeout=600),
        check=checks.pointing(0, 90),
    ),
    Example(
        "user-guide/autopilot/TargetPitchAndHeading",
        "orbit",
        run=Exit(timeout=600),
        check=checks.pointing(80, 90),
    ),
    Example(
        "user-guide/autopilot/TargetSmoothing",
        "orbit",
        run=Exit(timeout=600),
        check=checks.pointing(80, 90),
    ),
    Example("user-guide/autopilot/ManualUpdate", "flight", run=Loop()),
    # Control loops
    *_exits(
        "flight",
        "user-guide/control-loops/CompiledStep",
        "user-guide/control-loops/HoldTick",
        "user-guide/control-loops/NextTick",
        run=Interactive(checks.pitches_up, then=Loop(prints=False)),
    ),
    Example(
        "user-guide/control-loops/PitchHold",
        "flight",
        run=Interactive(checks.pitches_up, then=Exit(timeout=120)),
    ),
]

EXAMPLES = [*TUTORIALS, *USER_GUIDE]
