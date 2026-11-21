"""The monitor of the launch tutorials' flights, which speeds the simulation up where the
flight allows it."""

from __future__ import annotations

from typing import Any

from tools.doc_examples.monitor import Monitor, Sample

# The altitude above which the ascent runs faster, in meters. Lower down, the scripts'
# control loops, which poll in real time, steer through the dense atmosphere.
FAST_ALTITUDE = 20_000

# The simulation speed above that altitude, as a multiple of real time
FAST_SPEED = 10

# How far below the top of the atmosphere the ascent returns to normal speed, in meters, so
# the speed is set before a script's warp at the top starts. At FAST_SPEED, the vehicle
# climbs about a kilometer between samples.
MARGIN = 5_000

# The game time before the apoapsis reaches its target at which the ascent returns to normal
# speed, in seconds, so the script cuts the engines on time. A sample at FAST_SPEED is about
# a second of game time.
CUTOFF_LEAD = 10


class FastAscent(Monitor):
    """Runs the simulation at FAST_SPEED while the active vehicle climbs from FAST_ALTITUDE
    to near the top of the atmosphere, except around the engine cutoff at the target
    apoapsis. Above the atmosphere the scripts warp themselves, and a warp passes through
    normal speed as it starts, where setting the speed would stop it."""

    def __init__(self, test: Any, target: float):
        super().__init__(test)
        # The apoapsis at which the script cuts the engines, in meters
        self.target = target
        self._last: tuple[float, float] | None = None

    def streams(self) -> dict[str, Any]:
        conn = self.test.connect()
        self.ksa = ksa = conn.ksa
        earth = ksa.bodies["Earth"]
        self.top = earth.atmosphere_radius - earth.radius - MARGIN
        return {
            "speed": conn.add_stream(getattr, ksa, "simulation_speed"),
            "ut": conn.add_stream(getattr, ksa, "ut"),
        }

    def act(self, sample: Sample) -> None:
        if sample.speed not in (1, FAST_SPEED):
            return
        vehicle = self.ksa.active_vehicle
        if vehicle is None:
            return
        altitude = vehicle.flight.mean_altitude
        apoapsis = vehicle.orbit.apoapsis_altitude
        fast = FAST_ALTITUDE < altitude < self.top and not self._near_cutoff(
            sample.ut, apoapsis
        )
        speed = FAST_SPEED if fast else 1
        if sample.speed != speed:
            self.ksa.simulation_speed = speed

    def _near_cutoff(self, ut: float, apoapsis: float) -> bool:
        """Whether the apoapsis reaches the target within CUTOFF_LEAD of game time, at the
        rate it rose since the last sample."""
        last, self._last = self._last, (ut, apoapsis)
        if apoapsis >= self.target or last is None or ut <= last[0]:
            return False
        rate = (apoapsis - last[1]) / (ut - last[0])
        return rate > 0 and (self.target - apoapsis) / rate < CUTOFF_LEAD

    def finish(self) -> None:
        if self.ksa.simulation_speed == FAST_SPEED:
            self.ksa.simulation_speed = 1

    def check(self) -> None:
        pass


def fast_ascent(target: float):
    """A FastAscent monitor for a launch to an apoapsis, in meters."""
    return lambda test: FastAscent(test, target)
