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
}


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
]

EXAMPLES = [*TUTORIALS]
