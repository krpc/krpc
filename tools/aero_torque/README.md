# aero_torque tools

Tools for validating and demonstrating the stock aerodynamic simulation
endpoints (`Flight.SimulateAerodynamicForceAt`,
`Flight.SimulateAerodynamicTorqueAt`, `Flight.SimulateAerodynamicWrenchAt`,
and the live `Flight.AerodynamicTorque` property), built during the #914
flight-validation campaign.

Requirements: `krpc`, `numpy`, `matplotlib` (analysis/plot paths run without
the game). Teleport-based scenarios use the TestingTools plugin when
available and fall back to a deorbit burn.

## The predictor (the demo)

`reentry_predictor.py`: attitude-aware re-entry landing prediction. One RK4
integrator, two modes:

- `baseline`: 3-DOF point mass assuming a permanent retrograde hold, force
  endpoint only. This is what was possible before the torque endpoint.
- `6dof`: integrates translation and rotation; samples both force and torque
  with one wrench RPC at each RK4 stage, passing that stage's UT for the stock
  solar-exposure-dependent atmospheric state. Discovers trim, lift and
  oscillation on its own.

Subcommands: `run` (teleport onto an entry arc, capture, predict, fly, log,
compare), `predict` (capture and predict only; `--replay PREFIX` re-predicts
a stored capture against its logged flight with zero flight-to-flight
variance), `plot` (offline re-analysis), `selftest` (offline integrator,
attitude and geometry checks; no game needed).

The replay plot retains the total-AoA envelope and also resolves the 6-DOF
state into signed pitch AoA, sideslip, bank about the relative wind, and the
vertical/crossrange components of lift acceleration. These panels expose a
bank or phase error that total AoA alone can hide.

Key options: `--hold {retrograde,retro-release,none}` (retro-release gives a
deterministic gentle release at the atmosphere interface, best for trim
craft), `--rate-damp K` (reaction-wheel rate damping applied to the flight
AND modeled identically in the prediction; suppresses the phase-sensitive
trim oscillation), `--predict-altitude` (capture point; give the prediction
enough coast time).

Typical results (Kerbin, ~1000 km entries): ballistic capsule 0.5 km vs
2.2 km baseline; rate-damped lifting trim capsule 1.0 km vs 8.3 km baseline.
See `DEMO.md`.

## Fidelity graders (grade the endpoints against telemetry)

- `force_fidelity.py --prefix RUN`: sim/measured/live drag ratios binned by
  angle of attack. Newer flight logs record the sim and live forces in-flight
  (`fsx`/`flx` columns), making the analysis fully offline. Current logs use
  the wrench force component while old logs retain the legacy-force samples;
  both formats are accepted. `--force-rpc`
  re-evaluates via RPC in the current game context and compares against the
  in-flight values (this is how evaluation-context bugs were caught). For a
  wrench-format log it reuses each sample's logged UT and angular velocity;
  legacy logs retain the legacy force endpoint path.
- `torque_fidelity.py --prefix RUN`: measured/live/sim pitch torque vs signed
  angle of attack, with trim zero crossings and stiffness. Requires an
  UNDAMPED, uncontrolled log (wheel torque contaminates the measured series).
- `compare_aero_torque.py`: the original torque-fidelity tool (measured vs
  simulated vs live during an entry, plus a static launchpad AoA probe).
  `make_synthetic.py` generates a synthetic log for its plot pipeline.

## Launchpad probes (no flight needed)

- `trim_probe.py`: finds the torque zero crossing vs AoA per flight condition
  (trim angle, lift, drag, L/D). Use to design trim-ballast craft. Note that
  stock crushes hypersonic body lift (BodyLift liftMach = 0.0625 above
  mach 5): optimize the trim angle (10-15 deg), not L/D.
- `damping_probe.py`: measures central-difference force and torque derivatives
  with respect to angular velocity, and compares pitch damping against the
  stock rigid-body angular-drag formula (single-part craft). Paired calls use
  one frozen atmospheric UT.
- `invariance_probe.py`: tests hypothetical-attitude invariance and compares
  the wrench for one physical state expressed in rotating-body,
  non-rotating-body and vessel reference frames at one frozen UT.
- `cube_probe.py`: fingerprints the effective drag cube state via forces along
  the six body axes plus a mixing sweep; `--compare A.json B.json` diffs two
  contexts.

## Craft conventions

Few parts; ablator tweaked low (the predictor assumes constant mass); aim
entries at ocean (no terrain model; impact is graded at `--stop-altitude`
above sea level); do not stage between capture and impact; a pod with a
heatshield on its bottom node has no cube body lift and an inert
CapsuleBottom module, both of which the simulation now models.
