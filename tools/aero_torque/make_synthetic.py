"""Generate a synthetic re-entry log in the exact schema compare_aero_torque.py expects.
Purpose: exercise the analysis/plot pipeline and preview output. NOT real game data.

Construction: pick a clean angular-velocity history omega_clean(t) that only spins up
while there is airflow, define the 'true' aero torque as I.omega_clean' + omega x I omega,
then log omega WITH measurement noise (so the differentiated 'measured' series shows the
finite-difference noise you'd see in-game). simulated = 0.90*true + bias + noise (a
deliberately ~10% low, slightly biased simulator), live = 0.98*true + small noise.
"""

import csv
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from compare_aero_torque import COLUMNS  # noqa: E402

rng = np.random.default_rng(7)
dt = 0.1
t = np.arange(0.0, 140.0 + dt, dt)
N = t.size

# Re-entry dynamic-pressure bump (Pa) and rough altitude/speed for context.
qn = np.exp(-(((t - 65.0) / 28.0) ** 2))  # 0..1 gaussian
q = 12000.0 * qn
altitude = 70000.0 - 65000.0 * (t / 140.0)
speed = 2200.0 - 1900.0 * (t / 140.0)
mach = speed / 300.0

# Inertia tensor (kg m^2), symmetric with a little pitch<->yaw coupling.
I3 = np.array([[1500.0, 0.0, 120.0], [0.0, 900.0, 0.0], [120.0, 0.0, 1500.0]])

# Clean body-frame angular velocity: a capsule oscillating about trim, amplitude
# gated by airflow (qn) and slowly damping; plus a small decaying roll.
env = qn * np.exp(-0.006 * t)
wx = 0.8 * env * np.cos(2 * np.pi * 0.30 * t)
wz = 0.5 * env * np.sin(2 * np.pi * 0.22 * t)
wy = 0.15 * np.exp(-0.02 * t) * qn
w_clean = np.column_stack([wx, wy, wz])

# 'True' aero torque implied by that motion (Euler's equation).
alpha = np.gradient(w_clean, t, axis=0)
true = np.empty_like(w_clean)
for k in range(N):
    true[k] = I3.dot(alpha[k]) + np.cross(w_clean[k], I3.dot(w_clean[k]))

# Log omega with sensor/differentiation noise.
w_log = w_clean + rng.normal(0.0, 0.010, w_clean.shape)

# Simulator: ~10% low, small constant pitch bias, plus noise.
sim = 0.90 * true + np.array([1.5, 0.0, 0.0]) + rng.normal(0.0, 2.0, true.shape)
# Live per-part sum: nearly true, small noise.
live = 0.98 * true + rng.normal(0.0, 1.0, true.shape)

Iflat = I3.reshape(-1)
out = sys.argv[1] if len(sys.argv) > 1 else "reentry_synth.csv"
with open(out, "w", newline="") as f:
    wtr = csv.writer(f)
    wtr.writerow(COLUMNS)
    for k in range(N):
        wtr.writerow(
            [
                t[k],
                altitude[k],
                speed[k],
                q[k],
                mach[k],
                w_log[k, 0],
                w_log[k, 1],
                w_log[k, 2],
                *Iflat,
                sim[k, 0],
                sim[k, 1],
                sim[k, 2],
                live[k, 0],
                live[k, 1],
                live[k, 2],
            ]
        )
print("wrote", out, "rows:", N)
