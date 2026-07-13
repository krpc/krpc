# Demo: attitude-aware re-entry prediction with an aerodynamic wrench

This document describes the flight-validated demo for the stock
`Flight.SimulateAerodynamicWrenchAt` endpoint (#914): a 6-DOF re-entry landing
predictor that obtains force and torque (attitude + angular rate) together,
compared against the best predictor that was possible before, a 3-DOF
force-only model that must assume an attitude.

Everything below was measured against real KSP flights (stock aero, KSP
1.12.5), using `reentry_predictor.py` (see `README.md` in this directory for
tool usage).

## Method

- The predictor captures the vessel state once during the pre-entry coast
  (position, velocity, attitude, angular velocity, mass, inertia tensor,
  reaction wheel torque) and integrates offline with RK4. Only aerodynamics
  comes from the game, sampled via RPC at each step; gravity, mass and the
  control law are modeled client-side.
- `baseline` mode assumes a permanent retrograde hold and samples only
  `SimulateAerodynamicForceAt`. `6dof` mode integrates the full rigid-body
  attitude and samples `SimulateAerodynamicWrenchAt` once per RK4 stage. Each
  call receives the actual RK4 stage UT, so stock atmospheric temperature,
  density, speed of sound and Mach follow the predicted position and solar
  geometry.
- Comparisons are horizontal miss distance between the prediction and the
  logged flight at fixed altitude checkpoints (40 to 2 km), in body-fixed
  coordinates. Repeat trials from quicksaves reproduce to ~0.01 km, so the
  numbers below are signal, not noise.

## Results

All entries: Kerbin, ~100 x 30 km arc, capture at ~95 km, roughly 1000 km of
downrange between capture and impact.

| Scenario | Craft | Baseline miss | 6-DOF miss | Runs |
|---|---|---|---|---|
| Ballistic capsule, uncontrolled | Mk1 pod + heatshield + chute | 2.2-2.5 km | 0.5-0.6 km | step3e |
| Ballistic capsule, uncontrolled | bare Mk1 pod | 2.1-2.3 km | 0.5 km | step4c |
| Lifting trim capsule, uncontrolled | + girder-mounted ore ballast | 3.5-7.6 km | 1.6-3.1 km | step5k |
| Lifting trim capsule, rate-damped | same | 4.0-8.3 km | 0.6-1.1 km | step5l |

The headline: a rate-damped, trim-flying capsule (a lifting entry, the
configuration a precision landing would actually use) is predicted to about
1 km over a 1060 km entry, 5x to 7.5x better than the force-only baseline at
every checkpoint. The baseline cannot do better on this craft by any tuning:
the landing point depends on the trim attitude and its lift, and trim is by
definition the zero crossing of torque, which the force endpoint cannot see.

Attitude fidelity, separately from landing accuracy: on uncontrolled entries
the predicted angle-of-attack envelope (capture swing, oscillation decay,
trim) overlays the telemetry envelope through the descent. This is the direct
visualization of the torque endpoint working, including its angular-rate
damping term.

## Reproduction

Craft: build the craft in the table (ablator tweaked low, chutes not armed,
aim at ocean). Design the trim ballast against `trim_probe.py` (target a trim
of 10-15 degrees at the 25-45 km conditions; stock caps capsule L/D at about
0.03-0.08, which is enough).

```
# ballistic validation
python reentry_predictor.py run --mode both --hold none --predict-altitude 95000 --out runA

# lifting entry, deterministic release, modeled rate damping
python reentry_predictor.py run --mode both --hold retro-release --rate-damp 0.5 --rate 10 --predict-altitude 95000 --out runB

# re-grade a stored run against a modified server build, no re-flying
python reentry_predictor.py predict --replay runB --out runB2
```

`selftest` validates the integrator, attitude dynamics and geometry offline
(no game). `force_fidelity.py` and `torque_fidelity.py` grade the endpoints
against a run's telemetry.

## Physics findings from the campaign

- Stock KSP applies an intrinsic per-part rotational damping (Unity rigidbody
  angular drag, `part.angularDrag x dynamicPressure(atm) x
  AngularDragMultiplier`) in addition to the kinematic damping from per-part
  translation. The torque endpoint now includes it; without it, predicted
  attitude dynamics are visibly under-damped versus flight.
- An uncontrolled trimmed capsule released tumbling is roll-chaotic: the roll
  orientation (and therefore the lift azimuth) settles into a basin selected
  by the capture swing, and predictions cannot reliably pick the same basin.
  A gentle release at the atmosphere interface makes the roll deterministic.
  This mirrors why real lifting entries use active roll control.
- Oscillation phase decoheres over hundreds of cycles even with correct
  physics; the envelope is the predictable quantity. Rate damping (torque
  proportional to minus the body rate, no attitude setpoint) removes the
  phase-sensitive term entirely and is exactly modelable in the predictor,
  which is why the damped configuration gives the best landing accuracy.
- The trim angle of an asymmetric capsule, its oscillation frequency, and its
  static stability are all measurable from the launchpad with the torque
  endpoint alone (`trim_probe.py`); these were the other demo candidates from
  the issue and fall out of the same machinery.

## Known limitations

- Constant mass (tweak ablator low), no terrain (aim at ocean), no staging
  between capture and impact.
- Undamped strongly-lifting entries retain a residual (2-3 km here) from
  under-damped oscillation regimes, partly attributable to a tracked lead:
  the heatshield's CapsuleBottom module lift simulates 1.48x to 1.68x its
  live value (mach-correlated). See README_BEFORE_MERGE.md.
- The rigid-body idealization is documented on the endpoint; per-part flow
  tracing during the campaign showed real flexing contributes negligibly for
  stiff stacks (per-part flow angles ~0.03 degrees).
