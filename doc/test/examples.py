"""The manifest: how to run each documentation example, and what it must do.

Each Example covers the files of every language that share its key."""

from __future__ import annotations

from doc.test import flights
from doc.test.spec import Example, Exit, Pad, Setup

# The game's own craft, in its Ships folder
STOCK = ""

# In the order the test classes run: the shared setups first, then those that relaunch for
# every example
SETUPS = {
    "launch-into-orbit": Setup(
        craft="AeroEquus", directory=STOCK, placement=Pad(), fresh=True
    ),
    "suborbital-flight": Setup(
        craft="Kerbal 2", directory=STOCK, placement=Pad(), fresh=True
    ),
    # Two vessels joined port to port, which _place_docking_vessels undocks
    "docking": Setup(craft="PartsDockingPortInFlight", fresh=True),
}


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
]

EXAMPLES = [*TUTORIALS]
