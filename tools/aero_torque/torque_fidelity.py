#!/usr/bin/env python3
"""
torque_fidelity.py

Grade Flight.SimulateAerodynamicTorqueAt against a logged UNCONTROLLED
re-entry (SAS/RCS/wheels off, so the measured net torque is purely
aerodynamic), focusing on the trim: the zero crossing of pitch torque vs
angle of attack. A bias in the simulated trim angle shows up directly as the
offset between the measured and simulated zero crossings.

Per sample:
    measured   tau = I.alpha + omega x (I.omega), body frame, from telemetry
    simulated  SimulateAerodynamicTorqueAt at the logged state (via RPC)

Both are projected on the instantaneous pitch axis (normal to the plane
containing the airflow and the nose), against the SIGNED in-plane angle of
attack, then binned to expose the torque-vs-AoA curves and their crossings.

Needs the game running with the SAME craft loaded. Use a log flown with
--hold none/retro-release and NO rate damping (wheel torque would contaminate
the measured series).

Usage:
    python torque_fidelity.py --prefix step5f
"""

import argparse
import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import load_traj, q_normalize, q_conj, q_rot, NOSE  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--prefix", default="step5f")
    ap.add_argument("--stride", type=int, default=1)
    ap.add_argument(
        "--min-q",
        type=float,
        default=200.0,
        help="grade only samples with dynamic pressure above this (Pa)",
    )
    ap.add_argument("--name", default="torque-fidelity")
    args = ap.parse_args()

    with open(args.prefix + "_meta.json") as f:
        meta = json.load(f)
    if meta.get("rate_damp", 0.0) > 0.0:
        sys.exit(
            "This log was flown WITH rate damping -- wheel torque "
            "contaminates the measured aero torque. Use an undamped log "
            "(e.g. step5f)."
        )
    act = load_traj(meta["files"]["actual"])

    # Trim frozen post-impact tail.
    alt = act["alt"]
    last = len(alt) - 1
    while last > 0 and alt[last] == alt[last - 1]:
        last -= 1
    while last > 0 and act["mass"][last] == 0.0:
        last -= 1
    act = {k: v[: last + 1] for k, v in act.items()}

    t = act["ut"]
    r = np.column_stack([act["x"], act["y"], act["z"]])
    v = np.column_stack([act["vx"], act["vy"], act["vz"]])
    q = np.column_stack([act["qx"], act["qy"], act["qz"], act["qw"]])
    w = np.column_stack([act["wx"], act["wy"], act["wz"]])
    inertia = np.asarray(meta["inertia"], dtype=float).reshape(3, 3)
    w_planet = np.asarray(meta["w_vec"])

    # Measured torque (body frame): I.alpha + omega x (I.omega), with
    # alpha_body = R^-1 * d(omega_world)/dt.
    w_dot = np.gradient(w, t, axis=0)

    have_logged = "tsx" in act
    t_sim_all = t_live_all = None
    flight = body = None
    if have_logged:
        t_sim_all = np.column_stack([act["tsx"], act["tsy"], act["tsz"]])
        t_live_all = np.column_stack([act["tlx"], act["tly"], act["tlz"]])
        print("Using sim/live torque columns recorded in the log (offline).")
    else:
        import krpc

        conn = krpc.connect(name=args.name)
        sc = conn.space_center
        vessel = sc.active_vessel
        if abs(vessel.mass - meta["mass"]) > 0.01 * meta["mass"]:
            print(
                f"WARNING: active vessel mass {vessel.mass:.0f} != logged "
                f"{meta['mass']:.0f} kg -- probably the wrong craft!"
            )
        body = sc.bodies[meta["body"]]
        flight = vessel.flight(body.non_rotating_reference_frame)

    rows = []
    idx = range(2, len(t) - 2, args.stride)
    print(f"Grading {len(list(idx))} samples...")
    for i in idx:
        if act["qdyn"][i] < args.min_q:
            continue
        qi = q_normalize(q[i])
        qc = q_conj(qi)
        v_air = v[i] - np.cross(w_planet, r[i])
        speed = np.linalg.norm(v_air)
        if speed < 1.0:
            continue
        w_hat = v_air / speed
        nose = q_rot(qi, NOSE)
        # Signed in-plane AoA and the pitch axis (normal of the AoA plane).
        e = nose - np.dot(nose, w_hat) * w_hat
        if np.linalg.norm(e) < 1e-3:
            continue
        e /= np.linalg.norm(e)
        pitch_axis = np.cross(w_hat, e)  # world frame
        aoa = math.degrees(
            math.atan2(float(np.dot(nose, e)), float(np.dot(nose, -w_hat)))
        )

        w_body = q_rot(qc, w[i])
        alpha_body = q_rot(qc, w_dot[i])
        tau_meas_body = inertia.dot(alpha_body) + np.cross(w_body, inertia.dot(w_body))
        tau_meas = float(np.dot(q_rot(qi, tau_meas_body), pitch_axis))

        if have_logged:
            tau_sim_w = t_sim_all[i]
            tau_live = float(np.dot(t_live_all[i], pitch_axis))
        else:
            tau_sim_w = np.array(
                flight.simulate_aerodynamic_torque_at(
                    body, tuple(r[i]), tuple(v[i]), tuple(qi), tuple(w[i])
                )
            )
            tau_live = float("nan")
        tau_sim = float(np.dot(np.asarray(tau_sim_w), pitch_axis))

        rows.append(
            [
                t[i] - meta["ut0"],
                act["alt"][i],
                act["qdyn"][i],
                aoa,
                tau_meas,
                tau_sim,
                tau_live,
            ]
        )

    if len(rows) < 20:
        sys.exit("Too few samples above the q threshold.")
    data = np.array(rows)
    tt, alt_g, qdyn, aoa, tau_m, tau_s, tau_l = data.T
    has_live = np.isfinite(tau_l).any()
    out_csv = args.prefix + "_torque.csv"
    np.savetxt(
        out_csv,
        data,
        delimiter=",",
        header="t,alt,qdyn,aoa,tau_meas,tau_sim,tau_live",
        comments="",
    )
    print(f"Wrote {len(rows)} samples to {out_csv}")

    # Torque normalized by q so different flight phases can share bins, then
    # binned vs signed AoA; the zero crossing of each curve is the trim.
    norm_m = tau_m / qdyn
    norm_s = tau_s / qdyn
    norm_l = tau_l / qdyn
    print(
        f"\n{'AoA bin':>10} {'n':>5} {'tau/q meas':>11} {'tau/q sim':>11}"
        + (f" {'tau/q live':>11}" if has_live else "")
    )
    edges = np.arange(math.floor(aoa.min()), math.ceil(aoa.max()) + 1.0, 1.0)
    centers, m_med, s_med, l_med = [], [], [], []
    for lo, hi in zip(edges[:-1], edges[1:]):
        m = (aoa >= lo) & (aoa < hi)
        if m.sum() < 8:
            continue
        centers.append(0.5 * (lo + hi))
        m_med.append(float(np.median(norm_m[m])))
        s_med.append(float(np.median(norm_s[m])))
        l_med.append(float(np.median(norm_l[m])) if has_live else float("nan"))
        line = (
            f"{f'{lo:.0f}..{hi:.0f}':>10} {int(m.sum()):5d} "
            f"{m_med[-1]:11.4f} {s_med[-1]:11.4f}"
        )
        if has_live:
            line += f" {l_med[-1]:11.4f}"
        print(line)

    def zero_crossing(x, y):
        for j in range(len(x) - 1):
            if y[j] * y[j + 1] < 0:
                return x[j] + (x[j + 1] - x[j]) * y[j] / (y[j] - y[j + 1])
        return float("nan")

    trim_m = zero_crossing(centers, m_med)
    trim_s = zero_crossing(centers, s_med)
    print(
        f"\nTrim (zero crossing): measured {trim_m:.2f} deg, "
        f"simulated {trim_s:.2f} deg, bias {trim_s - trim_m:+.2f} deg"
    )
    if has_live:
        trim_l = zero_crossing(centers, l_med)
        print(
            f"Live (game-applied) trim: {trim_l:.2f} deg -- "
            "live==measured & sim off: formula/state issue in the endpoint; "
            "sim==live & both off measured: the game applies torque outside "
            "the per-part force fields (e.g. joint-flex geometry)."
        )
    fit_m = np.polyfit(centers, m_med, 1)[0]
    fit_s = np.polyfit(centers, s_med, 1)[0]
    print(
        f"Stiffness d(tau/q)/dAoA: measured {fit_m:.4f}, simulated "
        f"{fit_s:.4f} (ratio {fit_s / fit_m:.3f})"
    )


if __name__ == "__main__":
    main()
