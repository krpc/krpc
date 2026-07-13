#!/usr/bin/env python3
"""
damping_probe.py

Measure the central-difference aerodynamic force and torque derivatives with
respect to angular velocity that Flight.SimulateAerodynamicWrenchAt returns,
across a grid of (altitude, speed). Compare the pitch-torque derivative against
the damping stock KSP applies via per-part rigid-body angular drag:

    rb.angularDrag = part.angularDrag * dynamicPressure(atm)
                     * PhysicsGlobals.AngularDragMultiplier      (FlightIntegrator)
    equivalent torque = -rb.angularDrag * I_part * omega

For a SINGLE-PART craft the kinematic (omega x r) damping is ~zero, so the
endpoint's measured c should equal the angular-drag term alone:

    c_expected = angularDrag(2) * q_atm * multiplier(2) * I_part
               ~ 4 * q_atm * I_vessel        (single part: I_part = I_vessel)

Run anywhere with the probe craft loaded (launchpad is fine; the probe passes
hypothetical positions). Interpretation:

    -dtau_pitch/domega ~ c_expected
                            the endpoint's angular-drag term is correct
    c_sim ~ c_expected/1000 unit bug: the term uses tonne-scale inertia with
                            an extra 1/1000
    c_sim ~ 0               the term is missing entirely (stale server build?)

Usage:
    python damping_probe.py [--spin 0.3] [--name damping-probe]
"""

import argparse

import numpy as np


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--spin",
        type=float,
        default=0.3,
        help="probe angular rate about the pitch axis (rad/s)",
    )
    ap.add_argument("--name", default="damping-probe")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)

    up = np.array(vessel.position(ref), dtype=float)
    radius = body.equatorial_radius
    up /= np.linalg.norm(up)
    nose = np.array(vessel.direction(ref), dtype=float)
    pitch = np.cross(nose, up)
    if np.linalg.norm(pitch) < 0.1:
        pitch = np.cross(nose, np.array([1.0, 0.0, 0.0]))
    pitch /= np.linalg.norm(pitch)
    rot = vessel.rotation(ref)

    moi = vessel.moment_of_inertia  # kg m^2, vessel frame (x = pitch)
    i_pitch = moi[0]
    print(
        f"Craft: {vessel.name}, {len(vessel.parts.all)} part(s), "
        f"I_pitch = {i_pitch:.1f} kg m^2, probe spin {args.spin} rad/s"
    )
    print(
        f"{'alt':>4} {'speed':>5} {'q_atm':>9} {'|dF/dw|':>10} "
        f"{'dF_pitch':>10} {'|dT/dw|':>10} {'dT_pitch':>10} "
        f"{'c_expect':>10} {'ratio':>8}"
    )

    w0 = args.spin
    probe_ut = sc.ut
    for alt in (30000.0, 20000.0, 10000.0, 5000.0):
        pos = tuple(up * (radius + alt))
        rho = body.atmospheric_density_at_position(pos, ref)
        for speed in (1500.0, 800.0, 300.0):
            q_atm = 0.5 * rho * speed * speed / 101325.0
            vel = tuple(speed * nose)  # head-on wind, AoA 0
            force_p, torque_p = flight.simulate_aerodynamic_wrench_at(
                body, pos, vel, rot, tuple(w0 * pitch), probe_ut
            )
            force_m, torque_m = flight.simulate_aerodynamic_wrench_at(
                body, pos, vel, rot, tuple(-w0 * pitch), probe_ut
            )
            dforce = (np.array(force_p) - np.array(force_m)) / (2.0 * w0)
            dtorque = (np.array(torque_p) - np.array(torque_m)) / (2.0 * w0)
            dforce_pitch = float(np.dot(dforce, pitch))
            dtorque_pitch = float(np.dot(dtorque, pitch))
            # tau = -c * omega about the pitch axis.
            c_sim = -dtorque_pitch
            c_exp = 4.0 * q_atm * i_pitch  # angularDrag(2) * mult(2) * q_atm * I
            ratio = c_sim / c_exp if c_exp > 0 else float("nan")
            print(
                f"{alt / 1000.0:4.0f} {speed:5.0f} {q_atm:9.5f} "
                f"{np.linalg.norm(dforce):10.3f} {dforce_pitch:10.3f} "
                f"{np.linalg.norm(dtorque):10.3f} {dtorque_pitch:10.3f} "
                f"{c_exp:10.3f} {ratio:8.4f}"
            )

    print(
        "\nDerivatives use [+spin - (-spin)] / (2*spin); force units are "
        "N/(rad/s), torque units are N m/(rad/s).\n"
        "ratio = -dT_pitch/dw / c_expect. ratio ~ (0.5-1): term correct "
        "(I_part vs I_vessel difference is "
        "expected).\nratio ~ 0.001: tonne/kg unit bug (drop the 1/1000)."
        "\nratio ~ 0: term missing (stale build?)."
    )
    print(
        "NOTE: expects a SINGLE-PART craft; with more parts the kinematic "
        "omega x r damping adds to c_sim."
    )


if __name__ == "__main__":
    main()
