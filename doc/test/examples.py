"""The manifest: how to run each documentation example, and what it must do.

Each Example covers the files of every language that share its key. coverage_test.py checks
that every example file is either declared here or listed in unsupported.txt."""

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
    Splashed,
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
    # Below the altitudes the event examples wait for
    "low-flight": Setup(placement=Flight(altitude=800, speed=100)),
    "two-vessels": Setup(vessels=2),
    "out-of-fuel": Setup(craft="PartsEngine", placement=Pad()),
    "parts-tree": Setup(craft="PartsTree", directory=DOC_CRAFT, placement=Pad()),
    "docking-ports": Setup(craft="PartsDockingPort", placement=Pad()),
    "splashed": Setup(craft="Buoyancy", placement=Splashed()),
    "editor": Setup(craft="Buoyancy", scene="editor"),
    "orbit-fresh": Setup(fresh=True),
    "parts-tree-fresh": Setup(
        craft="PartsTree", directory=DOC_CRAFT, placement=Pad(), fresh=True
    ),
    "quicksave": Setup(
        craft="PartsTree",
        directory=DOC_CRAFT,
        placement=Pad(),
        quicksave=True,
        fresh=True,
    ),
    "pad": Setup(placement=Pad(), fresh=True),
    # The examples launch without clearing the pad, so it must hold no vessel, including in
    # the save's record of its vessels
    "launch-my-rocket": Setup(fresh=True, reload=True, ships=(("My Rocket", "Basic"),)),
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
    "docking-camera": Setup(
        craft="DockingCamera",
        directory="service/DockingCamera/test/craft",
        mods=("DockingCamera",),
    ),
    "infernal-robotics": Setup(
        craft="InfernalRobotics",
        directory="service/InfernalRobotics/test/craft",
        placement=Pad(),
        mods=("InfernalRobotics",),
    ),
    "kerbal-alarm-clock": Setup(
        craft="KerbalAlarmClock",
        directory="service/KerbalAlarmClock/test/craft",
        mods=("KerbalAlarmClock",),
    ),
    "kerbal-engineer-redux": Setup(
        craft="Staging", placement=Pad(), mods=("KerbalEngineerRedux",)
    ),
    "mechjeb": Setup(
        craft="Kerbal X",
        directory=STOCK,
        placement=Pad(),
        fresh=True,
        mods=("MechJeb",),
    ),
    "mechjeb-tutorial": Setup(
        craft="PT Series Munsplorer",
        directory=STOCK,
        placement=Pad(),
        fresh=True,
        mods=("MechJeb",),
    ),
    "raster-prop-monitor": Setup(
        craft="RasterPropMonitor",
        directory="service/RasterPropMonitor/test/craft",
        placement=Pad(),
        mods=("RasterPropMonitor",),
    ),
    "remote-tech": Setup(
        craft="RemoteTech",
        directory="service/RemoteTech/test/craft",
        mods=("RemoteTech",),
    ),
    "scansat": Setup(
        craft="ScanSat", directory="service/ScanSat/test/craft", mods=("SCANsat",)
    ),
    "trajectories": Setup(mods=("Trajectories",)),
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
_quickload = checks.Quickload()
_landing_site_calls = checks.CallCount(3)
_visual_debugging_calls = checks.CallCount(3)

# The combined specific impulse, as the parts tutorial prints it
_ISP = Values(checks.specific_impulse, pattern=r"Isp = (%s)" % NUMBER)


def _activate_engines(test) -> None:
    for engine in test.connect().space_center.active_vessel.parts.engines:
        engine.active = True


def _fire_engines(test) -> None:
    vessel = test.connect().space_center.active_vessel
    for engine in vessel.parts.engines:
        engine.active = True
    vessel.control.throttle = 1
    # The thrust builds over a few frames, and a compiled example reads it at once
    test.wait_until(lambda: vessel.thrust > 0, message="the engines to fire")


def _rename_servo_group(test) -> None:
    # Infernal Robotics builds the groups again from the parts shortly after a launch,
    # which undoes a rename made before then, so rename until the name holds
    conn = test.connect()
    ir = conn.infernal_robotics
    vessel = conn.space_center.active_vessel
    held = 0

    def renamed() -> bool:
        nonlocal held
        group = ir.servo_group_with_name(vessel, "Group1")
        if group is not None:
            group.name = "MyGroup"
            held = 0
        elif ir.servo_group_with_name(vessel, "MyGroup") is not None:
            held += 1
        return held >= 20

    test.wait_until(renamed, message="the servo group to stay renamed MyGroup")


def _switch_on_camera(test) -> None:
    conn = test.connect()
    part = conn.space_center.active_vessel.parts.with_name("OnboardCamera")[0]
    module = next(m for m in part.modules if m.name == "PartCameraModule")
    enabled = next(f for f in module.field_list if f.name == "IsEnabled")
    enabled.bool_value = True
    camera = conn.docking_camera.camera(part)

    def started() -> bool:
        try:
            camera.image()
            return True
        except RuntimeError:
            return False

    test.wait_until(started, message="the camera to switch on")


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
        "tutorials/mechjeb/LaunchToMinmus",
        "mechjeb-tutorial",
        run=Exit(timeout=3600),
        check=checks.landed_on("Minmus"),
        slow=True,
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
    # Boats and submarines
    *_exits(
        "splashed",
        "user-guide/boats-and-submarines/BuoyancyTrim",
        "user-guide/boats-and-submarines/BuoyantForce",
        "user-guide/boats-and-submarines/BuoyantTorque",
        "user-guide/boats-and-submarines/CenterOfBuoyancy",
        "user-guide/boats-and-submarines/PressureLimits",
        "user-guide/boats-and-submarines/PressureLimitsFunction",
    ),
    Example("user-guide/boats-and-submarines/EditorTorque", "editor"),
    # Streams and events
    Example(
        "user-guide/streams-and-events/Event",
        "low-flight",
        run=Interactive(checks.climb_past(10_000)),
    ),
    Example("user-guide/streams-and-events/StreamRate", "orbit", run=Loop()),
    # Object lifetime
    Example("user-guide/object-lifetime/DestroyedPart", "orbit"),
    Example(
        "user-guide/object-lifetime/Quickload",
        "quicksave",
        prepare=_quickload.prepare,
        check=_quickload.check,
    ),
    # Server side functions
    *_exits(
        "orbit",
        "user-guide/server-side-functions/AltitudeCheck",
        "user-guide/server-side-functions/AltitudeStream",
        "user-guide/server-side-functions/BurnRatings",
        "user-guide/server-side-functions/CapturedValue",
        "user-guide/server-side-functions/CheckFuel",
        "user-guide/server-side-functions/Event",
        "user-guide/server-side-functions/FiringCounter",
        "user-guide/server-side-functions/FuelFraction",
        "user-guide/server-side-functions/FunctionStatements",
        "user-guide/server-side-functions/HeaviestEngine",
        "user-guide/server-side-functions/NotLoadedCheck",
        "user-guide/server-side-functions/PrintMass",
        "user-guide/server-side-functions/RunOnce",
        "user-guide/server-side-functions/StructureField",
        "user-guide/server-side-functions/SurfaceAngle",
        "user-guide/server-side-functions/SwitchModes",
        "user-guide/server-side-functions/SwitchStatement",
        "user-guide/server-side-functions/TruncateAltitude",
        "user-guide/server-side-functions/UpperCaseName",
    ),
    # The examples that wait for the vessel to climb above 1000 m
    *_exits(
        "low-flight",
        "user-guide/server-side-functions/AltitudeEvent",
        "user-guide/server-side-functions/CustomEvent",
        run=Interactive(checks.climb_past(1000)),
    ),
    Example(
        "user-guide/server-side-functions/NotLoadedEvent",
        "low-flight",
        run=Interactive(checks.climb_past(1000)),
        # The C-nano example checks once, as the client has no events
        per_language={"cnano": {"run": Exit()}},
    ),
    Example(
        "user-guide/server-side-functions/FunctionStream",
        "orbit",
        run=Loop(),
    ),
    Example(
        "user-guide/server-side-functions/CompiledStep",
        "orbit",
        run=Interactive(checks.toggles_action_group, then=Loop(prints=False)),
    ),
    Example(
        "user-guide/server-side-functions/BurnTime",
        "orbit",
        prepare=_activate_engines,
        values=Values(checks.burn_times),
    ),
    Example(
        "user-guide/server-side-functions/EngineThrusts",
        "orbit",
        prepare=_fire_engines,
        values=Values(checks.engine_thrusts),
    ),
    Example(
        "user-guide/server-side-functions/TotalThrust",
        "orbit",
        prepare=_fire_engines,
        values=Values(checks.thrust),
    ),
    Example(
        "user-guide/server-side-functions/ThrottleLimit",
        "orbit",
        run=Exit(timeout=120),
    ),
    # The examples watch for some vessel becoming the target, which must be one other than
    # the active vessel
    Example("user-guide/server-side-functions/TargetCheck", "two-vessels"),
    Example(
        "user-guide/server-side-functions/TargetEvent",
        "two-vessels",
        # Only the JavaScript example waits for the event
        per_language={"js": {"run": Interactive(checks.target_first_vessel)}},
    ),
    # Needs more than 100 units of liquid fuel, or it throws
    Example("user-guide/server-side-functions/ThrowException", "parts-tree"),
    *_exits(
        "out-of-fuel",
        "user-guide/server-side-functions/CollectionEvent",
        "user-guide/server-side-functions/OutOfFuelCheck",
        "user-guide/server-side-functions/OutOfFuelEvent",
    ),
    Example("user-guide/server-performance/Statistics", "orbit"),
]

CLIENT = [
    *_exits(
        "orbit",
        "client/BoundStaticMethods",
        "client/CompilationErrors",
        "client/CompiledFunctionArguments",
        "client/CompiledMatch",
        "client/CompiledParameters",
        "client/CompiledStatements",
        "client/CompileDecorator",
        "client/Concurrent",
        "client/Connecting1",
        "client/Connecting3",
        "client/ConnectingLocal",
        "client/ConnectingSerial",
        "client/ConnectingTcp",
        "client/FlagsFunction",
        "client/Imports",
        "client/NotLoadedStream",
        "client/NullValues",
        "client/RemoteObjects",
        "client/RemoteProcedures",
        "client/RunFunction",
        "client/RunFunctionArguments",
        "client/StateVariable",
        "client/StaticMethods",
        "client/StreamRate",
        "client/StreamScope",
        "client/Threads",
    ),
    # The C-nano client prints each error, including the one the example shows
    Example(
        "client/Errors",
        "orbit",
        per_language={"cnano": {"expect_errors": ("kRPC error",)}},
    ),
    *_exits(
        "low-flight",
        "client/CompiledEvent",
        "client/CustomEvent",
        "client/Event",
        "client/EventScope",
        "client/FunctionEvent",
        run=Interactive(checks.climb_past(1000)),
    ),
    Example("client/Flags", "orbit", check=checks.map_filter_includes_debris),
    *_loops(
        "orbit",
        "client/CompiledFunction",
        "client/CompiledFunctionStream",
        "client/FunctionStream",
        "client/Streaming",
        "client/Streaming1",
        "client/Streaming2",
        "client/Streaming3",
        "client/Streaming4",
    ),
    Example(
        "client/EventCallback",
        "orbit",
        run=Loop(),
        expect_output=("Altitude reached 1000m",),
    ),
    Example(
        "client/Callbacks",
        "orbit",
        run=Interactive(checks.toggle_abort, then=Loop()),
        expect_output=(
            "Abort 1 called with a value of",
            "Abort 2 called with a value of",
        ),
    ),
    Example(
        "client/ConditionVariables",
        "orbit",
        run=Interactive(checks.toggle_abort),
    ),
    Example(
        "client/EndCallback",
        "orbit",
        run=Interactive(checks.replace_vessel, then=Loop(), ready="Temperature"),
        expect_output=("The stream ended",),
    ),
    Example(
        "client/StreamEnd",
        "orbit",
        run=Interactive(checks.replace_vessel, ready="Temperature"),
        expect_output=("The part was destroyed",),
    ),
    *_exits(
        "launch-my-rocket",
        "client/Arguments",
        "client/OmittedArguments",
        # The craft launched beside the setup's vessel, which it did not recover
        check=checks.vessels(2),
    ),
]

API = [
    Example("api/debug/DebugExample", "orbit", check=checks.infinite_propellant),
    *_exits(
        "orbit",
        "api/drawing/DrawingExample",
        "api/krpc/KRPCExample",
        "api/lidar/RaycastExample",
        "api/space-center/SpaceCenterExample",
        "api/ui/UIExample",
    ),
    Example(
        "api/docking-camera/DockingCameraExample",
        "docking-camera",
        prepare=_switch_on_camera,
    ),
    Example(
        "api/infernal-robotics/InfernalRoboticsExample",
        "infernal-robotics",
        prepare=_rename_servo_group,
    ),
    Example("api/kerbal-alarm-clock/KerbalAlarmClockExample", "kerbal-alarm-clock"),
    Example(
        "api/kerbal-engineer-redux/KerbalEngineerReduxExample",
        "kerbal-engineer-redux",
    ),
    Example(
        "api/mechjeb/MechJebExample",
        "mechjeb",
        run=Exit(timeout=900),
        check=checks.in_orbit,
        slow=True,
    ),
    Example(
        "api/raster-prop-monitor/RasterPropMonitorExample",
        "raster-prop-monitor",
        run=Loop(prints=False),
        # These clients have no events, so their examples set up the page and stop
        per_language={"cnano": {"run": Exit()}, "lua": {"run": Exit()}},
    ),
    Example("api/remote-tech/RemoteTechExample", "remote-tech"),
    Example("api/scansat/ScanSatExample", "scansat"),
    Example("api/trajectories/TrajectoriesExample", "trajectories"),
]

PAGES = [
    Example("getting-started/HelloWorld", "orbit"),
    Example("index/Launch", "pad", run=Exit(timeout=300), slow=True),
    *_exits(
        "orbit",
        "communication-protocols/localsocket/RPC",
        "communication-protocols/tcpip/RPC",
    ),
    Example("communication-protocols/tcpip/Stream", "orbit", run=Loop()),
]

EXAMPLES = [*TUTORIALS, *USER_GUIDE, *CLIENT, *API, *PAGES]
