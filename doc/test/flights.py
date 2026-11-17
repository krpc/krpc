"""The monitors of the tutorials' flights, which check each flew as its tutorial says."""

from __future__ import annotations

import math
from typing import Any

from doc.test.monitor import Monitor, Sample, angle_difference, magnitude

# The physics warp the coasts run at, 4x, the highest in an atmosphere
COAST_WARP = 3

# The altitude above which the coast up warps, in meters, where the aerodynamic forces are
# small enough for the auto-pilot to hold prograde under warp
WARP_ALTITUDE = 16_000


class SubOrbitalFlight(Monitor):
    """The Kerbal 2 climbs at half throttle, pitches over to 70 degrees at 500 m, holds
    prograde from flame out to apoapsis, separates, and lands under its parachute."""

    # Game time for the auto-pilot to settle on prograde after flame out, in seconds
    SETTLE = 15

    def streams(self) -> dict[str, Any]:
        conn = self.test.connect()
        space_center = conn.space_center
        self.vessel = vessel = space_center.active_vessel
        body = vessel.orbit.body
        surface = vessel.flight()
        inertial = vessel.flight(body.reference_frame)
        add = conn.add_stream
        self.space_center = space_center
        # The stage changes so far, which act counts as the samples arrive
        self._stages = 0
        return {
            "ut": add(getattr, space_center, "ut"),
            "stage": add(getattr, vessel.control, "current_stage"),
            "throttle": add(getattr, vessel.control, "throttle"),
            "engaged": add(getattr, vessel.auto_pilot, "engaged"),
            "situation": add(getattr, vessel, "situation"),
            "fuel": add(vessel.resources.amount, "LiquidFuel"),
            "altitude": add(getattr, surface, "mean_altitude"),
            "surface_altitude": add(getattr, surface, "surface_altitude"),
            "pitch": add(getattr, surface, "pitch"),
            "heading": add(getattr, surface, "heading"),
            "speed": add(getattr, inertial, "speed"),
            "vertical_speed": add(getattr, inertial, "vertical_speed"),
            # Relative to the surface, for the angle to surface prograde. The surface
            # velocity frame is singular at rest, which a stream would fail on.
            "direction": add(vessel.direction, body.reference_frame),
            "velocity": add(getattr, inertial, "velocity"),
            "angular_velocity": add(
                vessel.angular_velocity, body.non_rotating_reference_frame
            ),
        }

    # Time warp through the coasts

    def act(self, sample: Sample) -> None:
        if len(self.samples) > 1 and sample.stage != self.samples[-2].stage:
            self._stages += 1
        stages = self._stages
        coasting = (
            # Flame out to just before apoapsis, where the script separates. Only high up,
            # as the auto-pilot fails to hold prograde under warp in the lower atmosphere.
            (
                stages == 1
                and sample.throttle == 0
                and sample.altitude > WARP_ALTITUDE
                and sample.vertical_speed > 30
            )
            # Released after the turn to retrograde, to the parachute altitude
            or (stages == 2 and not sample.engaged and sample.surface_altitude > 2200)
            # Under the parachute
            or stages == 3
        )
        landed = sample.situation in self._landed()
        warp = COAST_WARP if coasting and not landed else 0
        if self.space_center.physics_warp_factor != warp:
            self.space_center.physics_warp_factor = warp

    def finish(self) -> None:
        self.space_center.physics_warp_factor = 0

    # The checks

    def _landed(self):
        situations = self.space_center.VesselSituation
        return (situations.landed, situations.splashed)

    def _stage_changes(self) -> list[int]:
        """The indices of the samples where the stage changed."""
        return [
            i
            for i in range(1, len(self.samples))
            if self.samples[i].stage != self.samples[i - 1].stage
        ]

    def _phases(self) -> dict[str, int | None]:
        stages = self._stage_changes() + [None, None, None]
        launch, separation, parachute = stages[:3]
        pitch_over = flame_out = landed = None
        if launch is not None:
            pitch_over = self.first(lambda s: s.altitude > 500, launch)
            flame_out = self.first(lambda s: s.fuel < 0.1, launch)
        if parachute is not None:
            landed = self.first(lambda s: s.situation in self._landed(), parachute)
        return {
            "launch": launch,
            "pitch_over": pitch_over,
            "flame_out": flame_out,
            "separation": separation,
            "parachute": parachute,
            "landed": landed,
        }

    def summary(self) -> dict[str, Any]:
        samples = self.samples
        if not samples:
            return {"samples": 0}
        phases = self._phases()
        values: dict[str, Any] = {"samples": len(samples)}
        start = samples[0].ut
        for name, index in phases.items():
            values[name + "_time"] = (
                None if index is None else samples[index].ut - start
            )
        values["max_altitude"] = max(s.altitude for s in samples)
        values["max_speed"] = max(s.speed for s in samples)
        launch, pitch_over = phases["launch"], phases["pitch_over"]
        flame_out, separation = phases["flame_out"], phases["separation"]
        if launch is not None and pitch_over is not None:
            climb = self.between(self.after(launch, 2), pitch_over)
            values["climb_min_pitch"] = min((s.pitch for s in climb), default=None)
        if pitch_over is not None and flame_out is not None:
            # The burn ends before the smoothed pitch-over settles, so the ascent is
            # checked for a turn east that does not overshoot 70 degrees
            turn = self.between(pitch_over, flame_out)
            values["ascent_samples"] = len(turn)
            values["ascent_min_pitch"] = min((s.pitch for s in turn), default=None)
            values["ascent_final_pitch"] = samples[flame_out].pitch
            # The heading is undefined pointing straight up
            values["ascent_heading_error"] = max(
                (angle_difference(s.heading, 90) for s in turn if s.pitch < 88),
                default=None,
            )
            values["ascent_max_rate"] = max(
                (magnitude(s.angular_velocity) for s in turn), default=None
            )
        if flame_out is not None and separation is not None:
            hold = self.between(self.after(flame_out, self.SETTLE), separation)
            values["coast_samples"] = len(hold)
            values["coast_prograde_error"] = max(
                (_prograde_error(s) for s in hold), default=None
            )
            values["coast_max_rate"] = max(
                (magnitude(s.angular_velocity) for s in hold), default=None
            )
            values["fuel_at_separation"] = samples[separation].fuel
            values["separation_altitude"] = samples[separation].altitude
        landed = phases["landed"]
        if landed is not None:
            values["touchdown_speed"] = max(
                s.speed for s in samples[max(0, landed - RATE_WINDOW) : landed + 1]
            )
        values["parts"] = sorted(p.title for p in self.vessel.parts.all)
        return values

    def check(self) -> None:
        phases = self._phases()
        missing = [name for name, index in phases.items() if index is None]
        self.require(not missing, "The flight did not reach " + ", ".join(missing))
        values = self.summary()
        self.require(
            self.space_center.active_vessel == self.vessel,
            "The capsule is not the active vessel",
        )
        self.require(
            values["parts"] == ["Mk1-3 Command Pod", "Mk16-XL Parachute"],
            "The capsule has other parts left",
        )
        self.require(values["climb_min_pitch"] > 85, "The climb did not go straight up")
        self.require(
            values["ascent_min_pitch"] > 67 and values["ascent_heading_error"] < 3,
            "The auto-pilot did not turn to 70 degrees pitch, heading east",
        )
        self.require(
            values["ascent_max_rate"] < 0.15,
            "The vessel turned or oscillated too fast on the ascent",
        )
        self.require(
            values["coast_prograde_error"] < 2 and values["coast_max_rate"] < 0.15,
            "The auto-pilot did not hold prograde through the coast",
        )
        # The Kerbal 2 reaches about 21 km at about 630 m/s
        self.require(
            18_000 < values["max_altitude"] < 25_000,
            "The flight did not reach the expected apoapsis",
        )
        self.require(
            540 < values["max_speed"] < 720,
            "The flight did not reach the expected speed",
        )
        self.require(
            values["fuel_at_separation"] < 0.1,
            "The fuel was not used before separating",
        )
        self.require(
            values["max_altitude"] - values["separation_altitude"] < 200,
            "The vessel did not separate at apoapsis",
        )
        self.require(values["touchdown_speed"] < 10, "The capsule landed too fast")


# Samples before touchdown to take the landing speed from
RATE_WINDOW = 10


def _prograde_error(sample: Sample) -> float:
    """The angle between the vessel and its surface prograde, in degrees."""
    speed = magnitude(sample.velocity)
    cosine = sum(d * v for d, v in zip(sample.direction, sample.velocity)) / speed
    return math.degrees(math.acos(max(-1.0, min(1.0, cosine))))
