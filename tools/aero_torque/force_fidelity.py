#!/usr/bin/env python3
"""
force_fidelity.py

Grade Flight.SimulateAerodynamicForceAt against a logged re-entry, as a
function of angle of attack.

Reads a reentry_predictor run (meta + actual-flight CSV) and compares three
force estimates per sample, all along the airflow direction (pure drag):

  measured   from telemetry: F = m * (dv/dt - g(r)), non-rotating frame,
             central differences. The independent oracle.
  simulated  the force or wrench endpoint evaluated at the logged state. From
             the log's fsx/fsy/fsz columns when present (newer logs record it
             in-flight; fully offline), else via one legacy-force RPC per
             sample (game must be running with the same craft loaded).
  live       Flight.AerodynamicForce at the same instant (flx/fly/flz
             columns), i.e. the per-part dragScalar forces the game actually
             applied. Only available in newer logs.

The ratio tables isolate the failure mode: sim/meas is the end-to-end error;
live/meas checks that the game's applied-force fields account for the true
deceleration (if not, the game applies forces outside those fields); sim/live
is the pure formula difference with zero telemetry noise.

Usage:
    python force_fidelity.py --prefix step4            # uses step4_meta.json
    python force_fidelity.py --prefix step4 --stride 2 # halve the RPCs
"""

import argparse
import json
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import load_traj, q_normalize, total_aoa_deg  # noqa: E402

AOA_BINS = [0, 2, 5, 10, 15, 20, 30, 45, 60, 91]


def _bin_table(title, aoa, qdyn, num, den):
    """Print median num/den per AoA bin; returns (centers, medians)."""
    ratio = num / den
    print(f"\n{title}")
    print(f"{'AoA bin':>10} {'n':>5} {'q kPa':>7} {'median ratio':>13} " f"{'IQR':>15}")
    centers, meds = [], []
    for lo, hi in zip(AOA_BINS[:-1], AOA_BINS[1:]):
        m = (aoa >= lo) & (aoa < hi) & (den > 0) & np.isfinite(ratio)
        if m.sum() < 5:
            continue
        med = float(np.median(ratio[m]))
        q1, q3 = np.percentile(ratio[m], [25, 75])
        centers.append(0.5 * (lo + hi))
        meds.append(med)
        print(
            f"{f'{lo}-{hi} deg':>10} {int(m.sum()):5d} "
            f"{np.mean(qdyn[m]) / 1000.0:7.2f} {med:13.4f} "
            f"{f'[{q1:.3f},{q3:.3f}]':>15}"
        )
    return centers, meds


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--prefix",
        default="step4",
        help="run prefix; reads <prefix>_meta.json and its actual log",
    )
    ap.add_argument(
        "--stride", type=int, default=1, help="grade every Nth sample (default all)"
    )
    ap.add_argument(
        "--min-decel",
        type=float,
        default=0.5,
        help="grade only samples with measured aero acceleration "
        "above this (m/s^2); excludes the vacuum/noise-floor phase",
    )
    ap.add_argument(
        "--force-rpc",
        action="store_true",
        help="re-evaluate the endpoint via RPC even when the log "
        "already contains sim force columns",
    )
    ap.add_argument("--png", default=None)
    ap.add_argument("--name", default="force-fidelity")
    args = ap.parse_args()

    with open(args.prefix + "_meta.json") as f:
        meta = json.load(f)
    logged_api = meta.get("actual_aero_api", meta.get("aero_api", "legacy endpoints"))
    act = load_traj(meta["files"]["actual"])

    # Trim the frozen post-destruction tail (same rule as the analyzer).
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
    mass = act["mass"]
    mu = meta["mu"]
    w_planet = np.asarray(meta["w_vec"])

    # Measured aero force from telemetry.
    a = np.gradient(v, t, axis=0)
    rn = np.linalg.norm(r, axis=1)
    g = -mu / rn[:, None] ** 3 * r
    f_meas = mass[:, None] * (a - g)

    have_live = "flx" in act
    have_logged_sim = "fsx" in act
    have_sim = have_logged_sim and not args.force_rpc
    f_live_all = (
        np.column_stack([act["flx"], act["fly"], act["flz"]]) if have_live else None
    )
    f_slog_all = (
        np.column_stack([act["fsx"], act["fsy"], act["fsz"]])
        if have_logged_sim
        else None
    )
    f_sim_all = f_slog_all if have_sim else None

    flight = body = None
    if not have_sim:
        import krpc

        conn = krpc.connect(name=args.name)
        sc = conn.space_center
        vessel = sc.active_vessel
        if vessel.name != meta["craft"]:
            print(
                f"WARNING: active vessel '{vessel.name}' != logged craft "
                f"'{meta['craft']}'; the sim runs on the active vessel's parts."
            )
        if abs(vessel.mass - meta["mass"]) > 0.01 * meta["mass"]:
            print(
                f"WARNING: active vessel mass {vessel.mass:.0f} kg != logged "
                f"{meta['mass']:.0f} kg -- this is probably NOT the same "
                "craft (names alone don't distinguish unsaved craft); the "
                "graded sim would reflect the wrong drag cubes."
            )
        body = sc.bodies[meta["body"]]
        flight = vessel.flight(body.non_rotating_reference_frame)
        print("Log has no sim-force columns; evaluating the endpoint via RPC.")
    else:
        print(
            "Using sim/live force columns recorded in the log "
            f"(offline, aero API: {logged_api})."
        )

    rows = []
    for i in range(2, len(t) - 2, args.stride):  # skip noisy gradient endpoints
        v_air = v[i] - np.cross(w_planet, r[i])
        speed = np.linalg.norm(v_air)
        if speed < 1.0:
            continue
        if np.linalg.norm(f_meas[i]) / mass[i] < args.min_decel:
            continue
        if have_sim:
            f_sim = f_sim_all[i]
            if not np.isfinite(f_sim).all():
                continue
        else:
            f_sim = np.array(
                flight.simulate_aerodynamic_force_at(
                    body, tuple(r[i]), tuple(v[i]), tuple(q_normalize(q[i]))
                )
            )
        f_live = f_live_all[i] if have_live else np.full(3, np.nan)
        f_slog = f_slog_all[i] if have_logged_sim else np.full(3, np.nan)
        dhat = -v_air / speed
        aoa = total_aoa_deg(q_normalize(q[i]), r[i], v[i], w_planet)
        rows.append(
            [
                t[i] - meta["ut0"],
                act["alt"][i],
                aoa,
                act["qdyn"][i],
                float(np.dot(f_meas[i], dhat)),
                float(np.dot(f_sim, dhat)),
                float(np.dot(f_live, dhat)),
                float(np.dot(f_slog, dhat)),
            ]
        )
    if not rows:
        sys.exit("No samples above the deceleration threshold.")
    data = np.array(rows)
    tt, alt_g, aoa, qdyn, drag_m, drag_s, drag_l, drag_sl = data.T
    has_live_data = np.isfinite(drag_l).any()

    # RPC re-grades of a log that already has in-flight sim data are a
    # different experiment; write them to a different file so the two can't
    # overwrite each other.
    suffix = "_force_rpc" if (args.force_rpc and have_logged_sim) else "_force"
    out_csv = args.prefix + suffix + ".csv"
    np.savetxt(
        out_csv,
        data,
        delimiter=",",
        header=("t,alt,aoa,qdyn,drag_meas,drag_sim,drag_live,drag_sim_inflight"),
        comments="",
    )
    print(f"Wrote {len(rows)} graded samples to {out_csv}")

    c_sm, m_sm = _bin_table(
        "sim / measured (end-to-end endpoint error):", aoa, qdyn, drag_s, drag_m
    )
    if has_live_data:
        _bin_table(
            "live / measured (game-applied fields vs telemetry; ~1.0 "
            "means dragScalar accounts for the true deceleration):",
            aoa,
            qdyn,
            drag_l,
            drag_m,
        )
        _bin_table(
            "sim / live (pure formula difference, no telemetry noise):",
            aoa,
            qdyn,
            drag_s,
            drag_l,
        )
    if not have_sim and have_logged_sim:
        _bin_table(
            "sim NOW (RPC, current craft context) / sim IN-FLIGHT "
            "(logged): same states, same craft -- any deviation is "
            "evaluation-context (drag cube state) dependence:",
            aoa,
            qdyn,
            drag_s,
            drag_sl,
        )

    # Plot.
    import matplotlib

    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    fig, axes = plt.subplots(3, 1, figsize=(11, 11))
    ax = axes[0]
    ax.plot(tt, drag_m / 1000.0, color="#111", lw=1.4, label="measured")
    ax.plot(tt, drag_s / 1000.0, color="#d1495b", lw=1.0, label="simulated")
    if has_live_data:
        ax.plot(
            tt,
            drag_l / 1000.0,
            color="#3a7ca5",
            lw=1.0,
            ls="--",
            label="live (game-applied)",
        )
    ax.set_ylabel("drag (kN)")
    ax.set_xlabel("time since capture (s)")
    ax.set_title("Drag along the airflow")
    ax.legend()
    ax.grid(alpha=0.3)
    ax = axes[1]
    sc_ = ax.scatter(aoa, drag_s / drag_m, s=6, alpha=0.3, c=tt, cmap="viridis")
    fig.colorbar(sc_, ax=ax, label="time (s)")
    ax.plot(c_sm, m_sm, "o-", color="#d1495b", lw=2, ms=6, label="sim/meas bin median")
    if has_live_data:
        c_lm, m_lm = [], []
        ratio_lm = drag_l / drag_m
        for lo, hi in zip(AOA_BINS[:-1], AOA_BINS[1:]):
            m = (aoa >= lo) & (aoa < hi) & np.isfinite(ratio_lm)
            if m.sum() >= 5:
                c_lm.append(0.5 * (lo + hi))
                m_lm.append(float(np.median(ratio_lm[m])))
        ax.plot(
            c_lm, m_lm, "s-", color="#3a7ca5", lw=2, ms=6, label="live/meas bin median"
        )
    ax.axhline(1.0, color="#111", lw=0.8, ls="--")
    ax.set_xlabel("total AoA (deg)")
    ax.set_ylabel("drag ratio vs measured")
    ax.set_ylim(0.8, 1.3)
    ax.set_title("Drag ratio vs angle of attack (flat 1.0 = exact)")
    ax.legend()
    ax.grid(alpha=0.3)
    ax = axes[2]
    ax.plot(tt, drag_s / drag_m, color="#d1495b", lw=0.8, alpha=0.7, label="sim/meas")
    if has_live_data:
        ax.plot(
            tt, drag_s / drag_l, color="#7b2d8b", lw=0.8, alpha=0.7, label="sim/live"
        )
    ax.axhline(1.0, color="#111", lw=0.8, ls="--")
    ax.set_xlabel("time since capture (s)")
    ax.set_ylabel("drag ratio")
    ax.set_ylim(0.8, 1.3)
    ax.set_title("Drag ratio over the descent")
    ax.legend()
    ax.grid(alpha=0.3)
    fig.tight_layout()
    png = args.png or (args.prefix + suffix + ".png")
    fig.savefig(png, dpi=130)
    print(f"Wrote plot to {png}")


if __name__ == "__main__":
    main()
