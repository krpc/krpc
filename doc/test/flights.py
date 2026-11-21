"""The monitors of the tutorials' flights, which check each flew as its tutorial says."""

from __future__ import annotations

import math
from typing import Any

from tools.doc_examples.monitor import Monitor, Sample, angle_difference, magnitude

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


class LaunchIntoOrbit(Monitor):
    """The AeroEquus flies a gravity turn from 250 m to 45 km, separates two stages as they
    run dry, throttles down near a 150 km apoapsis, coasts out of the atmosphere, and
    circularizes at apoapsis."""

    TURN_START = 250
    TURN_END = 45_000
    TARGET = 150_000

    def streams(self) -> dict[str, Any]:
        conn = self.test.connect()
        space_center = conn.space_center
        self.space_center = space_center
        self.vessel = vessel = space_center.active_vessel
        body = vessel.orbit.body
        frame = body.non_rotating_reference_frame
        surface = vessel.flight()
        add = conn.add_stream
        self._warping = False
        self._throttled_down = False
        return {
            "ut": add(getattr, space_center, "ut"),
            "stage": add(getattr, vessel.control, "current_stage"),
            "throttle": add(getattr, vessel.control, "throttle"),
            "solid_fuel": add(vessel.resources.amount, "SolidFuel"),
            "liquid_fuel": add(vessel.resources.amount, "LiquidFuel"),
            "mass": add(getattr, vessel, "mass"),
            "altitude": add(getattr, surface, "mean_altitude"),
            "pitch": add(getattr, surface, "pitch"),
            "heading": add(getattr, surface, "heading"),
            "dynamic_pressure": add(getattr, surface, "dynamic_pressure"),
            "apoapsis": add(getattr, vessel.orbit, "apoapsis_altitude"),
            "periapsis": add(getattr, vessel.orbit, "periapsis_altitude"),
            # In the non-rotating frame, for the angle to orbital prograde
            "direction": add(vessel.direction, frame),
            "velocity": add(getattr, vessel.flight(frame), "velocity"),
            "angular_velocity": add(vessel.angular_velocity, frame),
        }

    # Time warp through the coast out of the atmosphere, after the engines cut out. The
    # script warps to the circularization burn itself.

    def act(self, sample: Sample) -> None:
        if abs(sample.throttle - 0.25) < 0.01:
            self._throttled_down = True
        coasting = (
            self._throttled_down
            and sample.throttle == 0
            and WARP_ALTITUDE < sample.altitude < 70_000
        )
        if coasting != self._warping:
            self._warping = coasting
            self.space_center.physics_warp_factor = COAST_WARP if coasting else 0

    def finish(self) -> None:
        if self._warping:
            self.space_center.physics_warp_factor = 0

    # The checks

    def expected_pitch(self, altitude: float) -> float:
        fraction = (altitude - self.TURN_START) / (self.TURN_END - self.TURN_START)
        return 90 - 90 * min(1.0, max(0.0, fraction))

    def _stagings(self) -> list[int]:
        """The indices of the samples where staging started, with the stages activated
        within a second of game time of each other taken as one."""
        stagings: list[int] = []
        for i in range(1, len(self.samples)):
            if self.samples[i].stage == self.samples[i - 1].stage:
                continue
            if stagings and self.samples[i].ut - self.samples[stagings[-1]].ut < 1:
                continue
            stagings.append(i)
        return stagings

    def _phases(self) -> dict[str, int | None]:
        stagings = self._stagings() + [None, None, None]
        launch, first_separation, second_separation = stagings[:3]
        throttle_down = cut_off = space = burn = burn_end = None
        if launch is not None:
            throttle_down = self.first(lambda s: abs(s.throttle - 0.25) < 0.01, launch)
        if throttle_down is not None:
            cut_off = self.first(lambda s: s.throttle == 0, throttle_down)
        if cut_off is not None:
            space = self.first(lambda s: s.altitude >= 70_000, cut_off)
        if space is not None:
            burn = self.first(lambda s: s.throttle > 0, space)
        if burn is not None:
            end = self.first(lambda s: s.throttle == 0, burn)
            burn_end = len(self.samples) - 1 if end is None else end
        return {
            "launch": launch,
            "first_separation": first_separation,
            "second_separation": second_separation,
            "throttle_down": throttle_down,
            "cut_off": cut_off,
            "space": space,
            "burn": burn,
            "burn_end": burn_end,
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
        values["stagings"] = len(self._stagings())
        launch, cut_off = phases["launch"], phases["cut_off"]
        if launch is not None and cut_off is not None:
            ascent = self.between(launch, cut_off)
            # The turn, once the auto-pilot has followed it from straight up
            turn = [s for s in ascent if 2000 < s.altitude < self.TURN_END]
            values["turn_pitch_error"] = max(
                (abs(s.pitch - self.expected_pitch(s.altitude)) for s in turn),
                default=None,
            )
            # The heading is undefined pointing straight up
            values["ascent_heading_error"] = max(
                (angle_difference(s.heading, 90) for s in ascent if s.pitch < 88),
                default=None,
            )
            values["ascent_max_rate"] = max(
                magnitude(s.angular_velocity) for s in ascent
            )
            values["max_dynamic_pressure"] = max(s.dynamic_pressure for s in ascent)
            values["apoapsis_at_cut_off"] = samples[cut_off].apoapsis
        # The fuel the stage had left in the last sample before it separated, and the fuel
        # it burned in the sample before that. A stage that separates as it runs dry
        # separates within a sample of burning its last.
        for name, fuel, spare in (
            ("first_separation", "solid_fuel", 0),
            ("second_separation", "liquid_fuel", UPPER_STAGE_FUEL),
        ):
            index = phases[name]
            if index is not None and index >= 2:
                before, earlier = samples[index - 1], samples[index - 2]
                values[name + "_fuel_left"] = getattr(before, fuel) - spare
                values[name + "_fuel_burn"] = getattr(earlier, fuel) - getattr(
                    before, fuel
                )
                values[name + "_altitude"] = before.altitude
        burn, burn_end = phases["burn"], phases["burn_end"]
        if burn is not None and burn_end is not None:
            # Once the throttle is up and the vessel steady along the burn
            steady = self.between(self.after(burn, 1), burn_end)
            values["burn_samples"] = len(steady)
            values["burn_prograde_error"] = max(
                (_prograde_error(s) for s in steady), default=None
            )
            values["burn_max_rate"] = max(
                (magnitude(s.angular_velocity) for s in steady), default=None
            )
        values["final_apoapsis"] = samples[-1].apoapsis
        values["final_periapsis"] = samples[-1].periapsis
        values["final_liquid_fuel"] = samples[-1].liquid_fuel
        # The stock Mystery Goo title ends in a space in some games and not in others
        values["parts"] = sorted(p.title.strip() for p in self.vessel.parts.all)
        return values

    def check(self) -> None:
        phases = self._phases()
        missing = [name for name, index in phases.items() if index is None]
        self.require(not missing, "The flight did not reach " + ", ".join(missing))
        values = self.summary()
        self.require(
            self.space_center.active_vessel == self.vessel,
            "The vessel launched is not the active vessel",
        )
        self.require(values["stagings"] == 3, "The vessel did not stage three times")
        for stage in ("first", "second"):
            left = values[stage + "_separation_fuel_left"]
            burn = values[stage + "_separation_fuel_burn"]
            self.require(
                left < max(0.1, 1.5 * burn),
                "The %s stage separated with fuel left" % stage,
            )
        self.require(
            values["turn_pitch_error"] < 3 and values["ascent_heading_error"] < 3,
            "The auto-pilot did not follow the gravity turn east",
        )
        self.require(
            values["ascent_max_rate"] < 0.15,
            "The vessel turned or oscillated too fast on the ascent",
        )
        # The AeroEquus peaks at about 42 kPa
        self.require(
            values["max_dynamic_pressure"] < 55_000,
            "The ascent was too steep, through too much dynamic pressure",
        )
        self.require(
            self.TARGET <= values["apoapsis_at_cut_off"] < self.TARGET + 2000,
            "The engines did not cut off at the target apoapsis",
        )
        # The burn holds the node's direction, which is prograde at apoapsis, so prograde
        # turns away from it by about 3 degrees by either end of the burn
        self.require(
            values["burn_prograde_error"] < 8 and values["burn_max_rate"] < 0.15,
            "The vessel did not hold its direction through the circularization burn",
        )
        apoapsis, periapsis = values["final_apoapsis"], values["final_periapsis"]
        self.require(
            abs(apoapsis - self.TARGET) < 1000
            and abs(periapsis - self.TARGET) < 1000
            and apoapsis - periapsis < 1000,
            "The orbit is not circular at the target altitude",
        )
        self.require(values["final_liquid_fuel"] > 0, "The upper stage ran dry")
        self.require(
            values["parts"]
            == [
                "Communotron 16",
                "FL-T400 Fuel Tank",
                'LV-909 "Terrier" Liquid Fuel Engine',
                "Mk1 Command Pod",
                "Mk16 Parachute",
                "Mystery Goo\u2122 Containment Unit",
                "Mystery Goo\u2122 Containment Unit",
                "TD-12 Decoupler",
            ],
            "The upper stage does not have the parts expected",
        )


# The liquid fuel of the AeroEquus' upper stage, in its FL-T400 tank
UPPER_STAGE_FUEL = 180


class Docking(Monitor):
    """The vessel turns to face the target port, lines up on its axis 5 m or more out,
    closes in along the axis at up to 1 m/s, and docks."""

    def streams(self) -> dict[str, Any]:
        conn = self.test.connect()
        space_center = conn.space_center
        self.space_center = space_center
        vessel = space_center.active_vessel
        port = vessel.parts.controlling.docking_port
        target = space_center.target_docking_port
        self.port = port
        self.monopropellant = sum(
            v.resources.amount("MonoPropellant") for v in space_center.vessels
        )
        frame = target.reference_frame
        add = conn.add_stream
        # Streams of the ports' parts, which outlast the vessel merging into the other
        return {
            "ut": add(getattr, space_center, "ut"),
            "state": add(getattr, port, "state"),
            "target_state": add(getattr, target, "state"),
            "position": add(port.position, frame),
            "velocity": add(port.part.velocity, frame),
            # The direction the port faces, which docks along -y of the target's frame
            "facing": add(port.direction, frame),
        }

    def settle(self) -> None:
        # The script ends as the ports start to pull together, which takes a few seconds
        docked = self.space_center.DockingPortState.docked
        self.test.wait_until(
            lambda: bool(self.samples) and self.samples[-1].state == docked,
            timeout=30,
            message="the ports to dock",
        )

    def _facing_error(self, sample: Sample) -> float:
        cosine = -sample.facing[1] / magnitude(sample.facing)
        return math.degrees(math.acos(max(-1.0, min(1.0, cosine))))

    def _off_axis(self, sample: Sample) -> float:
        x, _, z = sample.position
        return math.hypot(x, z)

    def _phases(self) -> dict[str, int | None]:
        states = self.space_center.DockingPortState
        facing = self.first(lambda s: self._facing_error(s) < 2)
        lined_up = contact = docked = None
        if facing is not None:
            lined_up = self.first(lambda s: self._off_axis(s) < 0.5, facing)
        if lined_up is not None:
            contact = self.first(
                lambda s: s.state in (states.docking, states.docked), lined_up
            )
        if contact is not None:
            docked = self.first(lambda s: s.state == states.docked, contact)
        return {
            "facing": facing,
            "lined_up": lined_up,
            "contact": contact,
            "docked": docked,
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
        values["start_distance"] = magnitude(samples[0].position)
        facing, contact = phases["facing"], phases["contact"]
        if facing is not None and contact is not None:
            approach = self.between(self.after(facing, 5), contact)
            values["approach_facing_error"] = max(
                (self._facing_error(s) for s in approach), default=None
            )
            values["approach_max_speed"] = max(
                (magnitude(s.velocity) for s in approach), default=None
            )
            # Closing in along the axis, within 4 m of the target
            close = [s for s in approach if s.position[1] < 4]
            values["close_off_axis"] = max(
                (self._off_axis(s) for s in close), default=None
            )
            at_contact = samples[contact - 1]
            values["contact_off_axis"] = self._off_axis(at_contact)
            values["contact_speed"] = magnitude(at_contact.velocity)
            values["contact_facing_error"] = self._facing_error(at_contact)
        space_center = self.space_center
        values["vessels"] = len(space_center.vessels)
        values["monopropellant_used"] = self.monopropellant - sum(
            v.resources.amount("MonoPropellant") for v in space_center.vessels
        )
        values["final_state"] = samples[-1].state
        return values

    def check(self) -> None:
        phases = self._phases()
        missing = [name for name, index in phases.items() if index is None]
        self.require(not missing, "The docking did not reach " + ", ".join(missing))
        values = self.summary()
        states = self.space_center.DockingPortState
        self.require(
            values["final_state"] == states.docked and values["vessels"] == 1,
            "The vessels did not dock into one",
        )
        self.require(
            values["approach_facing_error"] < 2,
            "The vessel did not keep facing the target port",
        )
        # The approach from 42 m uses about 2.4 units
        self.require(
            values["monopropellant_used"] < 5,
            "The approach used too much monopropellant",
        )
        self.require(
            values["approach_max_speed"] < 1.2,
            "The vessel approached faster than 1 m/s",
        )
        self.require(
            values["close_off_axis"] < 0.6,
            "The vessel closed in off the docking axis",
        )
        self.require(
            values["contact_off_axis"] < 0.3
            and values["contact_speed"] < 0.3
            and values["contact_facing_error"] < 3,
            "The ports met off axis, too fast or at an angle",
        )
