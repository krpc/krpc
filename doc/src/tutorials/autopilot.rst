AutoPilot
=========

kRPC provides an autopilot that can be used to hold a vessel in a chosen orientation. It
automatically tunes itself to cope with vessels of differing size and control authority.

This tutorial is in two parts. :ref:`Using the AutoPilot <using-the-autopilot>` covers
how to drive the autopilot and configure it for your craft, and is all most users need.
:ref:`How it works <how-it-works>` explains the control law and mathematics behind it,
for those who want to fully understand or extend it.

.. _using-the-autopilot:

Using the AutoPilot
-------------------

The autopilot holds a vessel pointing in a chosen orientation, automatically tuning
itself to the vessel's size and control authority. You give it:

* a reference frame defining where zero rotation is,
* target pitch and heading angles,
* an optional target roll angle.

When a roll angle is not specified, the autopilot zeroes out any rotation around the
roll axis but does not hold a specific roll angle.

A minimal use case looks like this:

.. code-block:: python

   ap = vessel.auto_pilot
   ap.reference_frame = vessel.surface_reference_frame
   ap.target_pitch_and_heading(90, 90)   # point straight up, facing east
   ap.engaged = True
   ap.wait()                             # blocks until the vessel is pointing at the target
   ap.engaged = False

While the autopilot is engaged it keeps SAS switched off, so that the two do not fight
each other for control of the vessel.

The autopilot handles structurally flexible craft automatically (see :ref:`Flexible
craft <flexible-craft>`) and can ease target changes in over time for smooth maneuvers
(see :ref:`Smoothing target changes <target-smoothing>`). Internally it is a two-loop
cascade controller; if you want to know how it turns an attitude error into control
inputs, see :ref:`How it works <how-it-works>`.

Tuning the AutoPilot
^^^^^^^^^^^^^^^^^^^^

The following parameters affect the behavior of the autopilot. The default values should
suffice in most cases, but they can be adjusted to fit your needs.

* The **roll start angle** (default 20°) and **roll engage angle** (default 15°) define
  the band of direction error over which roll control is blended in. Above the roll
  start angle the vessel's roll is ignored, so that all of the available control
  authority is used to point the nose. Below the roll engage angle roll is fully
  controlled. Between the two, roll is blended in linearly.

* The **maximum angular velocity** (default 1 rad/s along each axis) caps the target
  angular velocity used when slewing the vessel towards the target. It is a vector of
  three values, one for each of the pitch, roll and yaw axes. Vessels with low or
  moderate control authority rarely reach this cap; it mainly prevents very high-torque
  craft from rotating faster than desired.

* The **pitch/yaw attenuation angle** and the **roll attenuation angle** (both default
  to 1 degree) set the region in which the autopilot considers the vessel to be 'close'
  to the target. In this region the target velocity is smoothly attenuated towards zero,
  which helps prevent the controls from oscillating when the vessel is pointing in the
  correct direction.

* **PI auto-tuning** (default ``True``) controls whether the PI controller gains are
  automatically tuned from the vessel's available torque and moment of inertia, as
  described in :ref:`Controller gain tuning <tuning-the-controllers>`. When disabled,
  the gains can be set manually.

* The **time to peak** (default is 1 second per axis) is the time, in seconds, that the
  PI controllers take to adjust the vessel's angular velocity to the target angular
  velocity. Decreasing it makes the controllers match the target velocity more
  aggressively, at the cost of a higher control bandwidth. Increasing it lowers the
  control bandwidth, making the controller gentler — useful for a very large or
  structurally flexible craft, where too high a bandwidth can excite the structure into
  a wobble (the autopilot normally damps this on its own, so raising the time to peak is
  mainly a manual fallback for unusual cases; see :ref:`Flexible craft
  <flexible-craft>`). It is a vector of three times, one for each of the pitch, roll and
  yaw axes.

* The **overshoot** (default is 0.01 in each axis) is the fraction by which the PI
  controllers are allowed to overshoot the target angular velocity. Increasing it makes
  the controllers match the target velocity more aggressively, but causes more
  overshoot. It is a vector of three values, between 0 and 1, for each of the pitch,
  roll and yaw axes.

* **Oscillation control** parameters configure how the autopilot suppresses any
  oscillation or wobble in a structurally flexible vessel. By default this is fully
  automatic. It can be forced on or off, or tuned, as described in :ref:`Flexible craft
  <flexible-craft>`.

* The **soft-start time** (default 0.5 seconds) is the time over which the control
  output is faded in when the autopilot is engaged, so that engaging does not deliver a
  sudden kick.  Set it to zero to disable the fade-in. See :ref:`When first engaging
  <first-engaging>`.

* The **target smoothing time** (default 0 seconds) makes the autopilot slew towards a
  new target over the given time, at a constant angular rate, rather than acting on the
  change immediately. Zero (the default) means changes take effect at once. See
  :ref:`Smoothing target changes <target-smoothing>`.

.. _flexible-craft:

Flexible craft
^^^^^^^^^^^^^^

Large rockets built from many parts are not perfectly rigid: KSP joins parts with
springy joints, so a tall or heavy vehicle flexes, and the autopilot can pick up the
structural wobble and feed it back into the controls. The autopilot tries to
automatically detect this and damps it, with no tuning required, so a flexible craft
will normally settle and hold steadily with the default settings. The full mechanism is
described under :ref:`Oscillation detection and mitigation <oscillation>`.

The autopilot detects two kinds of oscillation. *Attitude oscillation* is the craft
physically wobbling — the structural bending, visible in the measured angular
velocity. This is the primary signal: detecting it is what marks a craft as flexible and
switches the damping on. *Control oscillation* is the autopilot's own control output
swinging back and forth; it is a secondary signal, watched only on a craft already found
to be flexible, and used to keep the damping engaged through a maneuver.

As soon as the autopilot sees a craft begin to wobble, it marks that craft as
flexible. From then on it filters the attitude oscillation out of its rate measurements,
so the control loop does not chase it. Additionally, while the craft is holding
attitude, it applies further mitigations (chiefly easing back the control bandwidth) so
that it stops feeding energy into the structure. To keep the craft responsive to new
targets, these additional mitigations are relaxed during a maneuver.  If the maneuver
sets the wobble going again, the autopilot detects the resulting control oscillation and
re-applies them until the craft settles.

Overriding the automatic behavior
"""""""""""""""""""""""""""""""""

The **pitch/yaw oscillation control** and **roll oscillation control** properties select
how *attitude oscillation* is suppressed. You can override the automatic behavior when
you know your craft in advance; each property takes one of the following values:

* ``AUTOMATIC`` (the default) — detect, latch and suppress oscillation automatically,
  choosing the notch or low-pass filter at the estimated frequency.
* ``OFF`` — never suppress. The vessel is controlled with full authority, which is the
  most responsive but allows a flexible craft to wobble. Use this if you would rather
  accept the wobble than give up control authority.
* ``NOTCH`` — always apply the notch filter. Use this when you already know the craft
  has a low-frequency structural mode near the control band.
* ``LOW_PASS`` — always apply the low-pass filter. Use this when you already know the
  craft has a high-frequency structural mode.

When forcing a tool, set the mode frequency for that axis group with the **pitch/yaw
oscillation frequency** or **roll oscillation frequency** property (in Hz); the same
properties seed the automatic estimator before it acquires:

.. code-block:: python

   ap = vessel.auto_pilot
   # treat the craft as flexible from the start, notching a 1.4 Hz bending mode
   ap.pitch_yaw_oscillation_control = conn.space_center.OscillationControl.notch
   ap.pitch_yaw_oscillation_frequency = 1.4

The **oscillation control level** lets you force the holding-attitude damping on ahead
of time. Normally the autopilot eases that damping off during a maneuver so the craft
stays responsive, and re-applies it by itself — from the control oscillation — only once
a maneuver has set the wobble going again; this override holds it fully on throughout
instead. It is a manual per-axis value from 0 (no override; leave it to the automatic
detection) to 1 (damping forced fully on), and takes effect once the craft has been
detected as flexible. Use it when you already know a maneuver will excite the craft.

Monitoring and tuning
"""""""""""""""""""""

The two signals are observed and tuned through separate sets of properties.

*Attitude oscillation* — the craft wobbling — is reported by the read-only **oscillation
level** (a value between 0 and 1 per axis measuring how strongly structural oscillation
is detected), the **pitch/yaw oscillation latched** and **roll oscillation latched**
flags (whether the craft has been confirmed flexible), and the **pitch/yaw oscillation
detected frequency** and **roll oscillation detected frequency** (the estimated mode
frequency in Hz, or not-a-number until the estimator has acquired it). Its handling is
tuned with the **oscillation detection threshold** (a higher threshold is less
sensitive), the **oscillation notch Q** (how narrow the notch is), the **oscillation
bandwidth floor** (the inner-loop bandwidth, in rad/s, a flexible axis is reduced
towards while holding attitude), and — as a manual override for unusual cases — the time
to peak (see below).

*Control oscillation* — the autopilot's own command swinging — is reported by the read-only
**pitch/yaw control oscillation** and **roll control oscillation** (the amplitude of
oscillation in the control output). It is tuned with the **oscillation control threshold** (the
control-output amplitude above which the maneuver damping is re-engaged) and the **oscillation
control level** described above.

.. _target-smoothing:

Smoothing target changes
^^^^^^^^^^^^^^^^^^^^^^^^^

By default, changing the target — its pitch, heading, roll, direction or rotation —
takes effect immediately, and the autopilot reorients to it as quickly as the vessel's
control authority allows. Sometimes you instead want a slow, deliberate reorientation,
moving to the new attitude gradually rather than as fast as possible. You could script
that yourself by feeding the autopilot a series of small intermediate target
changes. Such a loop risks exciting a bending mode of the craft, and consumes RPC
throughput that could be used for other tasks. The autopilot can do this smoothing for
you from a single command.

Setting the **target smoothing time** to a value greater than zero makes the autopilot
slew towards a new target instead of jumping to it. The control loop no longer tracks
the **commanded** target you set directly; it tracks an **effective** target that moves
towards the commanded one at a constant angular rate, reaching it in exactly the target
smoothing time. A single change to the target therefore produces a smooth reorientation
spread over that time, with no per-tick scripting on your part.

Properties related to the commanded and effective targets are accessible via the API:

* The target properties (**target pitch**, **target heading**, **target roll**,
  **target direction**, **target rotation**) and the error properties (**error**, **pitch
  error**, and so on) all refer to the **commanded** target. Reading a target back therefore
  returns exactly what you set, and waiting for the autopilot blocks until the vessel reaches
  the final commanded attitude.

* A similar set of read-only properties — **current target pitch**, **current target
  heading**, **current target roll**, **current target direction**, **current target
  rotation**, and **current error**, **current pitch error**, and so on — expose the
  **effective** target and the error to it. These stay small while a smoothed change is
  being fed in, since the vessel is tracking the effective target closely.

When the target smoothing time is zero (the default), the effective target always equals
the commanded target and both sets of properties read the same.

.. _corner-cases:

Corner cases
^^^^^^^^^^^^

.. _first-engaging:

When first engaging
"""""""""""""""""""

When the autopilot is engaged it may be pointing far from its target, and its gains
may not yet reflect the vessel's current torque. Driving the full correction on the
very first tick would deliver a sudden kick to the controls, which on a flexible
vehicle can excite a structural oscillation.

To avoid this, the control output is faded in smoothly from zero over the **soft-start
time** (default 0.5 seconds), using a ramp with zero slope at the start so the onset is
gentlest exactly when the loop is furthest from steady state. The integral terms are
held at zero throughout the fade, so they only begin to accumulate once the ramp is
complete — and the controls are therefore not kicked when it finishes. Setting the
soft-start time to zero disables the fade-in.

When sitting on the launchpad
"""""""""""""""""""""""""""""

While the vessel is held on the launch clamps it cannot rotate, and with its engines
unlit its available torque is near zero. The autopilot therefore does not try to control
it at all: it holds the pitch, yaw and roll controls at zero and keeps its internal state
in the freshly-engaged condition, until the clamps release.

This avoids two problems. First, because the vessel cannot move, the integral terms in the
controllers would otherwise wind up to a large value — even when the vessel is pointing
correctly, since small floating-point variations in the computed error still accumulate.
Second, the gain auto-tuning scales the gains by the moment of inertia divided by the
available torque, so against the near-zero pad torque it would compute enormous gains and
saturate the controls against sensor jitter — a full-deflection command that would then be
delivered as a violent kick the instant the clamps release and the gains collapse onto the
now-large engine torque.

The :ref:`engagement soft-start <first-engaging>` begins the moment the clamps release:
holding on the pad is equivalent to engaging the autopilot the instant the clamps drop.

When the available torque is zero
"""""""""""""""""""""""""""""""""

An axis can lose all of its control authority — for example if the reaction wheels run out
of electric charge — leaving it with no available torque to control attitude.

Two things are then done on any axis whose available torque is zero. First, its controller
output is forced to zero and its integral term is cleared. With no torque to act on, the
controller cannot reduce the error, so its integral would otherwise wind up to a large value
and deliver a kick the moment authority returned. Second, gain auto-tuning is skipped for that
axis: the auto-tuned gains scale as the moment of inertia divided by the available torque, so
they grow without bound as the torque falls towards zero — and would divide by zero at zero.
The gains are left at their last values until torque is available again.

When the controlling client disconnects
""""""""""""""""""""""""""""""""""""""""

The autopilot is engaged by a client over the network. If that client disconnects
while the autopilot is still engaged, the autopilot resets its target to zero pitch
and heading in the vessel's surface reference frame, with roll left uncontrolled, and
disengages. This ensures that a dropped connection does not leave the vessel locked to
a stale target with no way to release it.

Limitations
^^^^^^^^^^^

The autopilot models each axis as having a single maximum torque, which it reads afresh
every physics tick. That figure already reflects the current state of the vessel — the
engine gimbals at the current throttle, RCS at its current thrust limiter, control
surfaces at the current dynamic pressure — so the model follows these as they change
rather than assuming a fixed value. What it does not capture is finer structure within
that figure. It is a single number per axis, taken in one direction only, so it is exact
for reaction wheels but only approximate for a vessel whose authority is asymmetric
(different in the two directions of an axis). Because the torque is read reactively
rather than predicted, the auto-tuner adapts to a change in authority only as fast as
the inner loop can respond.

While the autopilot detects and damps structural flexibility automatically, it leans
most heavily on **reducing the affected craft's control bandwidth** while it holds
attitude, rather than on the notch filter it also applies. In principle a notch placed
exactly at the bending frequency would damp the mode without reducing the bandwidth at
all, but the autopilot's online estimate of that frequency is not reliable enough on the
craft tested to make the notch carry the damping on its own, so the bandwidth reduction
does the heavy lifting. As a result a flexible craft is held more gently than a rigid
one. The bandwidth reduction is applied only while holding attitude — released during a
commanded slew so the craft stays responsive, and re-applied once it has settled — so
while holding, the craft rejects a *small* disturbance (a light nudge, or a gust) more
slowly than a rigid craft would, because it is deliberately running at a reduced
bandwidth. For the same reason, recovering from a maneuver that has excited the bending
mode runs at the reduced bandwidth and can take several seconds.

The autopilot is primarily designed for, and tested in, vacuum. It is a closed-loop
attitude tracker: aerodynamic torques appear to it as disturbances that the inner loop
only rejects once an error has built up. In a thick atmosphere — during ascent through
maximum dynamic pressure, or re-entry at a high angle of attack — the aerodynamic torque
can be large and fast-changing, and may even exceed the vessel's control authority. The
autopilot will still drive towards the target, but a low-authority craft can lag the
target by many degrees, or fail to hold attitude at all. It also does **not** manage
angle of attack: keeping the vessel within safe structural and thermal limits in
atmosphere is the responsibility of your script, not the autopilot.

.. _how-it-works:

How it works
------------

This part explains the control law in detail: how the autopilot turns an attitude error
into control inputs, and how it tunes itself. It is not needed in order to use the
autopilot, but is useful if you want a deeper understanding of how it works, or want to
extend it.

The control cascade
^^^^^^^^^^^^^^^^^^^

Each physics tick the autopilot does the following:

#. It compares the current rotation with the target rotation to compute the
   :ref:`target angular velocity <target-angular-velocity>` needed to rotate
   the vessel towards the target.

#. It passes the target angular velocity to three PI controllers (one per axis),
   which compare it against the vessel's measured angular velocity. Two feedforward
   terms are added to each controller's output: an :ref:`acceleration feedforward
   <acceleration-feedforward>` — the time derivative of the target angular velocity,
   normalized by the maximum angular acceleration :math:`\alpha = \tau_{max} / I` — and
   a :ref:`gyroscopic feedforward <gyroscopic-feedforward>` that cancels the rigid-body
   cross-coupling between the axes. The sum is clamped to :math:`[-1, 1]` to give the
   pitch, yaw and roll control inputs for the vessel.

Together these two steps form a *cascade*: an outer loop converts the attitude error
into a target angular velocity, and an inner loop of three PI controllers drives the
vessel's measured angular velocity onto that target. The outer loop is the velocity
profile described in :ref:`Computing the target angular velocity
<target-angular-velocity>`; the inner loop is the set of PI controllers described in
:ref:`Controller gain tuning <tuning-the-controllers>`.

Pitch and yaw are not controlled independently of each other. They are computed
together, in a frame from which the vessel's current roll has been removed (the
:ref:`roll-invariant frame <roll-invariant-frame>`), so that the nose follows the
shortest, great-circle path to the target direction and so that rolling the vessel does
not disturb that path. Roll is controlled separately, and is only blended in once the
vessel is already pointing close to the target.

.. _roll-invariant-frame:

The roll-invariant frame
^^^^^^^^^^^^^^^^^^^^^^^^^

Pitch and yaw are computed in a *roll-invariant frame*: the vessel body frame with the
vessel's current roll removed. It shares the vessel's nose direction, but its pitch and
yaw axes are held fixed relative to the autopilot reference frame for the current
pointing direction, rather than turning with the vessel as it rolls.

The reason for this is decoupling. If the pitch and yaw controllers ran directly in the
body frame, then as the vessel rolled its body pitch and yaw axes would rotate around
the nose. A roll correction would then change which axis a given direction error
projects onto, disturbing the path the nose takes towards the target. Removing the roll
means that roll corrections and pitch/yaw corrections do not interfere with one another.

The roll angle :math:`\varphi` is recovered each tick from the vessel's body x-axis
expressed in the roll-invariant frame. Pitch/yaw quantities are rotated from the body
frame into the roll-invariant frame by a rotation of :math:`\varphi` about the nose:

.. math::
   \begin{pmatrix} p \\ y \end{pmatrix}_{\!ri} =
   \begin{pmatrix} \cos\varphi & \sin\varphi \\ -\sin\varphi & \cos\varphi \end{pmatrix}
   \begin{pmatrix} p \\ y \end{pmatrix}_{\!body}

The pitch and yaw controllers run in this frame, and their outputs are rotated back into
the body frame (by :math:`-\varphi`) to become the actual pitch and yaw control inputs.
Roll is measured about the nose itself, which the rotation leaves unchanged, so the roll
controller needs no transformation.

Continuity through the antipode
"""""""""""""""""""""""""""""""

To measure :math:`\varphi` the autopilot first needs the roll-invariant frame itself. That
frame is the autopilot reference frame turned by a *pointing-only rotation*: the shortest
rotation taking a fixed reference axis :math:`\hat{\boldsymbol{u}}` of the reference frame
onto the current nose direction :math:`\hat{\boldsymbol{c}}`. Applied to the reference
frame it produces a frame that shares the nose but adds no twist about it.

Computed directly, that shortest rotation is singular when the nose reaches the *antipode*
of the reference axis — pointing exactly opposite :math:`\hat{\boldsymbol{u}}`. (For the
surface reference frame, whose reference axis points north, that is due south along the
horizon.) There it is a half-turn whose axis is undefined, and — the same hyper-sensitivity
to transverse motion that makes a 180° flip's plane ill-defined (:ref:`Flipping to the
antipode <antipodal-flip>`) — a tiny jitter in the nose direction swings the whole frame
through nearly 180°. That is not cosmetic: the frame carries stateful quantities — the
integral terms and the :ref:`acceleration feedforward <acceleration-feedforward>` are
expressed in it — so reinterpreting it by half a turn in a single tick delivers a
full-deflection kick to the controls.

The frame is therefore not rebuilt from scratch each tick. Writing
:math:`\rho(\boldsymbol{a}, \boldsymbol{b})` for the shortest rotation taking
:math:`\boldsymbol{a}` onto :math:`\boldsymbol{b}`, the pointing rotation is seeded once,
at engagement, as :math:`\rho(\hat{\boldsymbol{u}}, \hat{\boldsymbol{c}})`, and thereafter
carried forward by composing it with the small rotation between the previous and current
nose directions:

.. math::
   R_{point}(t) = \rho\big(\hat{\boldsymbol{c}}_{t-1},\ \hat{\boldsymbol{c}}_{t}\big)\;
                  R_{point}(t-1)

The nose cannot reverse within a single physics tick, so this incremental rotation is
always through a tiny, well-conditioned angle and never approaches its own singularity. The
frame slides smoothly across the reference antipode instead of flipping across it.

Separating the direction and roll errors
"""""""""""""""""""""""""""""""""""""""""

The attitude error has two parts — a *direction* error (where the nose points) and a
*roll* error (the rotation about the nose) — and the autopilot computes them separately
rather than from a single combined error rotation.

The direction error is the shortest rotation that brings the nose from its current
direction onto the target direction. The axis of this rotation is perpendicular to the
nose, so it represents pure pitch and yaw with no roll component. This is what the
pitch/yaw velocity profile acts on.

The roll error is whatever rotation remains once the direction has been aligned. Its
axis is *not* generally aligned with the nose while the direction error is large, so
folding it into the direction error would contaminate the pitch and yaw errors and curve
the nose's path. Instead the residual rotation is projected onto the nose axis,
extracting only the part that is a genuine roll, and that component is scaled by the roll
blend weight (see :ref:`Roll <roll-control>` below) before being used.

.. _target-angular-velocity:

Computing the target angular velocity
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The target angular velocity is the angular velocity that will rotate the vessel
towards the target direction. The core of the calculation is a one-dimensional
function that, given the angular error :math:`\theta` about an axis, returns the
target angular speed :math:`\omega` for that axis:

.. math::
   \omega &= -\frac{\theta}{\lvert\theta\rvert}
             \min \big(
                 \omega_{max},
                 \sqrt{2 \alpha \lvert\theta\rvert}
             \big) \cdot f_a(\theta) \\
   \text{where} & \\
   \alpha &= \frac{\tau_{max}}{I} \\
   f_a(\theta) &= \frac{1}{1 + e^{-6/\theta_a(\lvert\theta\rvert - \theta_a)}}

The reasoning and derivation for this is as follows.

Decelerating to the target
""""""""""""""""""""""""""

The vessel needs to rotate towards :math:`\theta = 0`. The target speed
:math:`\omega` must therefore be positive when :math:`\theta` is negative, and
negative when :math:`\theta` is positive. This is the role of the term
:math:`-\frac{\theta}{\lvert\theta\rvert}`, which is :math:`+1` when
:math:`\theta < 0` and :math:`-1` when :math:`\theta > 0`.

The autopilot plans to decelerate the vessel to rest exactly as it reaches the
target, using the maximum angular acceleration the vessel can produce. This is a
*bang-bang* deceleration profile. The maximum angular acceleration is:

.. math::
   \alpha = \frac{\tau_{max}}{I}

where :math:`\tau_{max}` is the maximum torque the vessel can generate and
:math:`I` is its moment of inertia.

Under a constant deceleration :math:`\alpha`, a vessel rotating at speed
:math:`\omega` comes to rest after rotating through an angle:

.. math::
   \theta = \frac{\omega^2}{2\alpha}

Rearranging gives the largest speed from which the vessel can still stop within
the remaining error :math:`\theta`:

.. math::
   \omega = \sqrt{2 \alpha \theta}

This is used as the target speed. It is the minimum-time profile: the vessel
accelerates up to this speed and then decelerates at full torque, coming to rest
on the target.

The target speed is capped at the maximum angular velocity :math:`\omega_{max}`.

Attenuation near the target
"""""""""""""""""""""""""""

To prevent the vessel from oscillating when it is pointing at the target, the
target speed curve must have zero gradient at :math:`\theta = 0` and rise
smoothly with increasing :math:`\lvert\theta\rvert`. The square-root profile does
not have this property near the origin, so it is multiplied by an attenuation
function — a logistic function with the required shape:

.. math::
   f_a(\theta) = \frac{1}{1 + e^{-6/\theta_a(\lvert\theta\rvert - \theta_a)}}

where :math:`\theta_a` is the attenuation angle. The attenuation function is
close to 1 except within a few multiples of :math:`\theta_a` of the target, so
the autopilot uses the full bang-bang profile until the vessel is close to the
target direction, then eases the target speed down to zero.

.. _stopping-distance-feedforward:

Stopping-distance feedforward
"""""""""""""""""""""""""""""

The profile above gives the speed to aim for assuming the vessel starts from
rest. In practice the vessel is usually already rotating — towards or away from
the target. Feeding the raw error :math:`\theta` into the profile would then
cause overshoot (when already rotating towards the target) or sluggishness (when
rotating away). To correct for this, the autopilot predicts how far the vessel
will rotate before it can stop, and evaluates the profile at that predicted
stopping point :math:`\theta_{ff}` instead of the current error :math:`\theta`:

.. math::
   \theta_{ff} = \theta + \Delta\theta

Two estimates of the stopping distance are computed, and the one with the larger
magnitude is used:

.. math::
   \Delta\theta = \max\nolimits_{\lvert\cdot\rvert}\big(
       \Delta\theta_{quad},\ \Delta\theta_{lin} \big)

The quadratic term is the stopping distance under full-torque (bang-bang)
deceleration, obtained from the same kinematic relation as above (signed to keep
the direction of the current angular velocity :math:`\omega`):

.. math::
   \Delta\theta_{quad} = \frac{\omega\lvert\omega\rvert}{2\alpha}

The linear term accounts for the finite bandwidth of the closed loop: a PI
controller that is not saturated decelerates more gently than full torque, so its
true stopping distance is larger. It is approximated by:

.. math::
   \Delta\theta_{lin} = \frac{\omega}{\text{bw}}, \quad
   \text{bw} = K_P \frac{\tau_{max}}{I} = 2\zeta\omega_0

where :math:`\text{bw}` is the closed-loop bandwidth (see :ref:`Controller gain tuning
<tuning-the-controllers>` for :math:`K_P`, :math:`\zeta` and :math:`\omega_0`). This term
improves the approach on small, rigid craft; on large flexible rockets the autopilot
suppresses the bending mode automatically — see :ref:`Oscillation detection and mitigation
<oscillation>`.

Pitch and yaw together
""""""""""""""""""""""

If pitch and yaw were each driven by the one-dimensional profile above
independently, each axis would target a speed proportional to
:math:`\sqrt{\lvert\theta\rvert}` in that axis. The direction of the combined
rotation would then differ from the direction of the error, and the nose would
trace a curved path to the target.

To avoid this, pitch and yaw are treated as a single two-dimensional problem in
the roll-invariant frame. Rather than building a setpoint along the current error
direction, the autopilot predicts the **stopping point as a vector** — where the nose
would coast to if the current angular velocity :math:`\boldsymbol{\omega}` were braked
at full authority — and aims the velocity profile straight at it:

.. math::
   \boldsymbol{e}_{stop} &= \boldsymbol{\theta} + c\,\boldsymbol{\omega} \\
   \hat{s} &= \frac{\boldsymbol{e}_{stop}}{\lvert\boldsymbol{e}_{stop}\rvert} \\
   (\omega_{pitch},\,\omega_{yaw}) &= -\hat{s}\;
       \min\big(\omega_{max},\ \sqrt{2\alpha\lvert\boldsymbol{e}_{stop}\rvert}\big)\,
       f_a(\lvert\boldsymbol{e}_{stop}\rvert)

where :math:`\boldsymbol{\theta} = (\theta_{pitch}, \theta_{yaw})` is the direction
error and :math:`c\,\boldsymbol{\omega}` is the predicted coasting displacement. The
coefficient :math:`c` is the same stopping distance as in the one-dimensional case —
the larger in magnitude of the quadratic (bang-bang) and linear (PI-lag) terms, which
are both collinear with :math:`\boldsymbol{\omega}` and so collapse to a single scalar.
The acceleration :math:`\alpha` and the cap :math:`\omega_{max}` are projected along
:math:`\hat{\boldsymbol{\omega}}` for the prediction and along :math:`\hat{s}` for the
speed profile. This makes the nose follow the shortest, great-circle path to the target.

Tangential damping
""""""""""""""""""

Aiming at the predicted stopping point gives tangential damping for free, with no
separate term. When the vessel has an angular velocity component *perpendicular* to the
error direction — for example after a keyboard nudge — the coasting displacement
:math:`c\,\boldsymbol{\omega}` tilts :math:`\boldsymbol{e}_{stop}` off-axis, rotating
:math:`\hat{s}` so that the command :math:`-\hat{s}\,(\dots)` acquires a component
opposing the sideways drift. The autopilot therefore leads the turn to place the
predicted stopping point on the target, rather than letting the nose enter a
self-sustaining circular orbit around the target direction and correcting it after the
fact.

When the angular velocity is purely along the error direction
(:math:`\boldsymbol{\omega}_{\perp} = 0`) the law reduces exactly to the
one-dimensional profile applied along that direction, so great-circle slews and ordinary
settling are unchanged; the off-axis behavior appears only in the nudge/orbit regime.
The singularity guard is on :math:`\lvert\boldsymbol{e}_{stop}\rvert` rather than
:math:`\theta`: only when both the error and the predicted drift vanish is there nothing
to command.

.. _antipodal-flip:

Flipping to the antipode
""""""""""""""""""""""""

Everything above acts on the direction error :math:`\boldsymbol{\theta}`, whose axis is
the rotation that carries the nose onto the target — equivalently, the normal of the
great-circle plane the nose should sweep along. Writing the current nose direction as
:math:`\boldsymbol{c}`, the target direction as :math:`\boldsymbol{t}`, and the angle
between them as :math:`\theta`, that plane normal is

.. math::
   \hat{\boldsymbol{n}} = \frac{\boldsymbol{c} \times \boldsymbol{t}}
                               {\lvert \boldsymbol{c} \times \boldsymbol{t} \rvert},
   \qquad
   \lvert \boldsymbol{c} \times \boldsymbol{t} \rvert = \sin\theta

This is well behaved for an ordinary slew, but it degenerates for a **180° flip** —
commanding the nose to the point exactly opposite where it currently points.
There :math:`\boldsymbol{c}` and :math:`\boldsymbol{t}` are antipodal,
:math:`\sin\theta \to 0`, and *every* plane through the antipodal pair is an equally
valid geodesic, so :math:`\hat{\boldsymbol{n}}` is undefined.

The difficulty is not confined to exactly 180°. Suppose the nose has drifted a small
angle :math:`\delta` out of the intended plane. The cross product then gains an
out-of-plane component, and because it is divided by :math:`\sin\theta`, the resulting
tilt of :math:`\hat{\boldsymbol{n}}` away from the intended plane is

.. math::
   \Delta\hat{\boldsymbol{n}} \sim \frac{\delta}{\sin\theta}

which diverges as :math:`\theta \to 180°`. Near the antipode the geodesic axis is
therefore *hyper-sensitive*: a tiny out-of-plane offset swings the commanded rotation
plane by a large angle.

This interacts badly with the fact that a large flip is slow to leave the antipode. The
rate loop can only build angular velocity at the available acceleration :math:`\alpha`,
so the vessel unavoidably crawls through the near-antipodal region while it accelerates.
During that crawl, if the profile tracks the live :math:`\hat{\boldsymbol{n}}`, each
small out-of-plane disturbance swings the hyper-sensitive axis, the commanded plane
rotates under the nose, and the disturbance is fed back and amplified — the nose bows out
of the intended plane, pitching away from the great circle. The slower the crawl the more
it accumulates, so a flip started from rest bows the worst, while a 90° slew — which never
comes near the antipode — is entirely unaffected.

To avoid this, within a band of the antipode the autopilot stops tracking the live
geodesic axis and instead **latches a fixed flip plane**, commanding the rotation about
it. The normal is captured once, on entering the band, from the vessel's perpendicular
angular velocity :math:`\boldsymbol{\omega}_{\perp}` if it is already committed to a
rotation, or from the (arbitrary but consistent) geodesic axis for a flip started from
rest:

.. math::
   \hat{\boldsymbol{n}}_{latched} = \begin{cases}
       \hat{\boldsymbol{\omega}}_{\perp}
           & \lvert \boldsymbol{\omega}_{\perp} \rvert \ge \omega_{lead} \\
       \hat{\boldsymbol{n}} & \text{otherwise}
   \end{cases}

Because the latched plane is a great circle through the nose and its antipode — which
*is* the target — rotating within it still carries the nose all the way to the target;
the flip simply commits to one plane instead of chasing a singular one. The latched
normal is held in full within the hold angle :math:`\theta_{hold}` of the antipode
(spanning the crawl), then blended smoothly back to the live geodesic axis by the blend
angle :math:`\theta_{blend}`, using a weight :math:`w` on the latched plane:

.. math::
   w = \begin{cases}
       1 & 180° - \theta \le \theta_{hold} \\
       \text{smoothstep}\!\left(
           \dfrac{\theta_{blend} - (180° - \theta)}{\theta_{blend} - \theta_{hold}}
       \right) & \theta_{hold} < 180° - \theta < \theta_{blend} \\
       0 & 180° - \theta \ge \theta_{blend}
   \end{cases}

The commanded axis is rebuilt from the latched-plane and geodesic tangents blended by
:math:`w`. Outside the band (:math:`w = 0`) the live geodesic axis is recovered exactly,
so ordinary slews are untouched — and there :math:`\sin\theta` is no longer small, so
:math:`\hat{\boldsymbol{n}}` is well conditioned and correctly steers to a target that is
no longer antipodal. The mitigation changes only the *plane* the flip is commanded in, not
the speed profile, so the flip still runs at the full :math:`\omega_{max}`; no control
authority is given up.

The thresholds are the hold angle :math:`\theta_{hold} = 35°` and blend angle
:math:`\theta_{blend} = 50°` (both measured from the antipode), and the perpendicular-rate
threshold :math:`\omega_{lead} = 0.004` rad/s, above which the plane is latched from the
vessel's own rotation rather than from the geodesic axis.

One caveat follows from holding the plane only *partially* across the blend band. A slew
that starts within the band while rotating **towards** the target simply moves away from
the antipode and holds plane cleanly. But if the vessel enters the band rotating *away*
from the target — for example when a near-full reversal is commanded while it is already
slewing the other way — it must decelerate and pass back through the near-antipodal region,
where the partial latch does not fully suppress the out-of-plane growth. The nose can then
wobble a few degrees out of plane before settling. This is bounded and self-correcting,
unlike the unmitigated case, and it only arises from the unusual combination of a
near-180° reversal commanded in the middle of a slew.

.. _roll-control:

Roll
""""

Roll is controlled separately, using the one-dimensional profile directly on the roll
axis. It is only worth controlling once the vessel is pointing close to the target;
holding a roll angle while the nose is still slewing wastes control authority and can
curve the path. The target roll speed is therefore scaled by a weight that is 0 above
the roll start angle, 1 below the roll engage angle, and blends linearly between the
two. When no target roll is set, the roll target speed is simply 0, so that the
autopilot damps roll rotation without holding a specific angle.

Feedforward
^^^^^^^^^^^

Two feedforward terms are added to the PI controllers' output before the final clamp to
:math:`[-1, 1]`: an *acceleration feedforward* derived from the velocity setpoint, and a
*gyroscopic feedforward* that cancels the rigid-body cross-coupling between the axes.

.. _acceleration-feedforward:

Acceleration feedforward
""""""""""""""""""""""""

The velocity setpoint :math:`\omega_{ref}` changes from tick to tick as the vessel
rotates and the bang-bang profile is re-evaluated. Its time derivative is the angular
acceleration the vessel must produce to stay on the trajectory:

.. math::
   \alpha_{ref} = \frac{d\omega_{ref}}{dt}

The autopilot approximates this numerically from the change in setpoint between the
current and previous physics tick, then normalizes by the maximum angular acceleration:

.. math::
   x_{ff} = \frac{\alpha_{ref}}{\alpha} =
       \frac{\omega_{ref}(t) - \omega_{ref}(t - \Delta t)}{\Delta t \cdot \alpha}

This feedforward term :math:`x_{ff}` is added to the PI controller's output before
clamping to :math:`[-1, 1]`:

.. math::
   x = \operatorname{clamp}(x_{ff} + x_{PI},\ -1,\ 1)

When the vessel is following the trajectory perfectly, the PI error
:math:`\omega_{ref} - \omega` is near zero and the feedforward alone drives the
actuators. The PI controller then only needs to correct for disturbances (atmospheric
drag, center-of-mass offsets) rather than the trajectory itself, cleanly separating the
two concerns so they can be tuned independently.

On the first physics tick after the autopilot starts,
:math:`\omega_{ref}(t - \Delta t)` is undefined, so the feedforward is skipped for
that tick to avoid a transient spike.

The velocity setpoint :math:`\omega_{ref}` is continuous but not smooth: it has slope
discontinuities where the bang-bang profile switches (the velocity cap engaging, the
quadratic and linear stopping terms swapping, and the sign change through the target).
Differentiating across one of these kinks produces a single-tick step in :math:`x_{ff}`
that, once clamped, momentarily saturates the actuators. To suppress these transients the
feedforward is passed through a short first-order low-pass filter, with a time constant of
a few physics ticks. The lag this introduces is small and is absorbed by the PI
controller, which sees the unfiltered velocity error.

On a craft the autopilot has detected as structurally flexible, this feedforward is faded
out while the craft is holding attitude. Because it is a numerical derivative its gain is
largest at high frequency, making it the control path most able to re-excite the bending
mode once the inner-loop bandwidth has been reduced (see :ref:`Oscillation detection and
mitigation <oscillation>`). It is restored in full while the craft is slewing, and
is always present on a rigid craft.

.. _gyroscopic-feedforward:

Gyroscopic feedforward
""""""""""""""""""""""

The :ref:`controller gain tuning <tuning-the-controllers>` models each axis as an independent
plant :math:`\tau = I\dot\omega`. The true rigid-body equation of motion, however,
couples the axes through a gyroscopic term:

.. math::
   \boldsymbol{\tau} = I\dot{\boldsymbol{\omega}} +
       \boldsymbol{\omega} \times (I\boldsymbol{\omega})

The cross term :math:`\boldsymbol{\omega}\times(I\boldsymbol{\omega})` is a torque that
the per-axis model ignores; left uncorrected the PI controllers would have to reject it
as a disturbance. The autopilot instead cancels it directly, adding a feedforward control
fraction equal to the negative of that torque normalized by the available torque on each
axis:

.. math::
   x_{gyro} = -\frac{\boldsymbol{\omega} \times (I\boldsymbol{\omega})}{\tau_{max}}

This is computed in the body frame, where the moment of inertia and available torque are
defined per axis, and summed with the other control terms before clamping to
:math:`[-1, 1]`. A diagonal moment of inertia is assumed, matching the rest of the
controller.

Because the term is quadratic in :math:`\boldsymbol{\omega}`, it is negligible at the low
angular rates of normal attitude holding — including the structural bending oscillation
that affects flexible craft — and only becomes significant for fast slews or vessels with
strongly asymmetric inertia.

.. _tuning-the-controllers:

Controller gain tuning
^^^^^^^^^^^^^^^^^^^^^^^

Three PI controllers, one for each of the pitch, roll and yaw control axes, are
used to control the vessel. Each controller takes the relevant component of the
target angular velocity as its setpoint and the vessel's current angular velocity
as its input, and outputs a control input in the range :math:`[-1, 1]`. The
following describes how the gains for these controllers are automatically tuned
based on the vessel's available torque and moment of inertia.

Consider the system in a single control axis. The input to the system is the
target angular speed :math:`\omega`. The error in the angular speed
:math:`\omega_\epsilon` is passed to the controller :math:`C`, whose output is
the control input :math:`x` passed to the vessel. The plant :math:`H` describes
how that control input affects the vessel's angular acceleration; integrating it
gives the new angular speed, which is fed back to compute the new error.

For the controller :math:`C` we use a proportional-integral controller. It has no
derivative term, so that the closed loop behaves like a second order system and
is therefore easy to tune. Its transfer function in the :math:`s` domain is:

.. math::
   C(s) = K_P + K_I s^{-1}

The control input :math:`x` is the fraction of the available torque
:math:`\tau_{max}` being applied, so the applied torque is
:math:`\tau = x\tau_{max}`. Combining this with the angular equation of motion
gives the angular acceleration in terms of the control input:

.. math::
   I &= \text{moment of inertia of the vessel} \\
   \tau &= I \dot\omega_\epsilon \\
   \Rightarrow \dot\omega_\epsilon &= \frac{x\tau_{max}}{I}

Taking the Laplace transform of this gives the transfer function for the plant
:math:`H`:

.. math::
   \mathcal{L}(\dot\omega_\epsilon(t)) &= s\omega_\epsilon(s)
                                = \frac{X(s)\tau_{max}}{I} \\
   \Rightarrow H(s) = \frac{\omega_\epsilon(s)}{X(s)} &= \frac{\tau_{max}}{Is}

The open loop transfer function for the entire system is:

.. math::
   G_{OL}(s) &= C(s) \cdot H(s) \\
             &= (K_P + K_I s^{-1}) \frac{\tau_{max}}{Is}

The closed loop transfer function is then:

.. math::
   G(s) &= \frac{G_{OL}(s)}{1 + G_{OL}(s)} \\
        &= \frac{a K_P s + a  K_I}{s^2 + a K_P s + a K_I}
           \text{ where } a = \frac{\tau_{max}}{I}

The characteristic equation for the system is therefore:

.. math::
   \Phi = s^2 + \frac{\tau_{max}}{I} K_P s + \frac{\tau_{max}}{I} K_I

The characteristic equation for a standard second order system is:

.. math::
   \Phi_{standard} &= s^2 + 2 \zeta \omega_0 s + \omega_0^2 \\

where :math:`\zeta` is the damping ratio and :math:`\omega_0` is the natural
frequency of the system.

Equating coefficients between these equations, and rearranging, gives the gains
for the PI controller in terms of :math:`\zeta` and :math:`\omega_0`:

.. math::
   K_P &= \frac{2 \zeta \omega_0 I}{\tau_{max}} \\
   K_I &= \frac{I\omega_0^2}{\tau_{max}}

We now need some performance requirements to place on the system, which determine
the values of :math:`\zeta` and :math:`\omega_0`, and therefore the gains. The
fraction by which a second order system overshoots is:

.. math::
   O = e^{-\frac{\pi\zeta}{\sqrt{1-\zeta^2}}}

and the time it takes to reach the first peak in its output is:

.. math::
   T_P = \frac{\pi}{\omega_0\sqrt{1-\zeta^2}}

These can be rearranged to give :math:`\zeta` and :math:`\omega_0` in terms of
the overshoot and time to peak:

.. math::
   \zeta &= \sqrt{\frac{\ln^2(O)}{\pi^2+\ln^2(O)}} \\
   \omega_0 &= \frac{\pi}{T_P\sqrt{1-\zeta^2}}

By default, kRPC uses the values :math:`O = 0.01` and :math:`T_P = 1`. Note that
the closed-loop bandwidth :math:`\text{bw} = 2\zeta\omega_0` used in the
:ref:`stopping-distance feedforward <stopping-distance-feedforward>` is
proportional to :math:`\omega_0`; increasing the time to peak :math:`T_P` lowers
both.

The gains are recomputed every physics tick, so the autopilot adapts as the
vessel's available torque and moment of inertia change — for example as fuel is
burned, stages are dropped, or engines are throttled. The available torque is
smoothed slightly before being used, so that a sudden drop (such as an engine
shutting down while a reaction wheel keeps a small amount of torque available)
does not cause a momentary spike in the gains.

Because the :ref:`acceleration feedforward <acceleration-feedforward>` carries the
nominal trajectory demand, the PI controller only needs to reject disturbances — the
small, slowly-varying corrections from atmospheric drag, off-axis thrust, or mass
changes. The gains :math:`K_P` and :math:`K_I` derived from the overshoot and
time-to-peak parameters remain valid for this purpose, and the same settings produce a
less loaded controller that relies more on the feedforward for large maneuvers.

Integral anti-windup
""""""""""""""""""""

The integral term of a PI controller can *wind up* — accumulate a large value — whenever
the controller is unable to reduce the error for a sustained period, for example during a
long slew while the output is already saturated. A wound-up integral then overshoots
badly once the error finally clears, because it takes time to unwind. To prevent this,
the controllers use *conditional integration*: the integral stops accumulating whenever
it is already at the :math:`[-1, 1]` output limit and the new contribution would only
push it further into saturation. The integral term is also reset outright in the special
cases described under :ref:`Corner cases <corner-cases>` — on the launchpad, and on any
axis that currently has no available torque.

.. _oscillation:

Oscillation detection and mitigation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The autopilot measures angular velocity at the vessel's root part, so on a structurally
flexible craft (see :ref:`Flexible craft <flexible-craft>`) this measured value contains
an oscillating component from the structural bending modes — typically of the order of
0.01 to 0.1 rad/s — even when the vessel as a whole is barely rotating. If the autopilot
reacted to this oscillation it would pump energy into the bending mode and drive a growing
wobble.

To handle this the autopilot watches two signals: the *measured angular velocity* — the
craft's attitude oscillation, the primary signal that detects flexibility — and its own
*control output* — the control oscillation, a secondary signal used only for maneuver
recovery. They are covered in turn.

Attitude oscillation
""""""""""""""""""""

The autopilot watches for the tell-tale signature of a bending-mode limit cycle: the
measured rate changing from one tick to the next by more than the available torque could
physically cause. The detector tracks each axis independently and produces a level between
0 and 1 measuring how strongly that axis is oscillating; this level rises quickly when
excitation appears but decays slowly once it subsides. Once it is high enough the axis is
*latched* as flexible, and the latch persists if the autopilot is briefly disengaged and
re-engaged, so a craft known to be flexible stays damped. Pitch and yaw are treated together
— a long vehicle's bending mode appears in both, so if one is found to be flexible the other
is too.

On a latched axis the autopilot takes two measures so that the control loops no longer drive
the mode.

First, it removes the oscillation from the *measured* angular velocity before the control
loops act on it. It continuously estimates the oscillation frequency and uses it to pick
the right filter: a **notch** filter for a low-frequency mode near the control band (such
as the roughly 1 Hz bending mode of a tall launch vehicle held vertical on ascent), or a
**low-pass** filter for a high-frequency mode well above the band. A notch is transparent
everywhere except at the mode frequency, so it can stay on permanently without slowing the
craft's response.

The notch is always placed at the estimated frequency, and the low-pass corner is likewise
derived from it (at roughly a third of the estimated frequency). Until the estimator has
actually acquired a frequency, though, the autopilot does not guess: rather than risk a notch
at the wrong frequency, it falls back to a fixed, broadband low-pass with a corner of about
2 Hz. This fixed fallback — together with the bandwidth reduction below — is what keeps the
suppression robust when the frequency estimate is poor.

Second, while the craft is *holding* attitude, the affected axes are switched into a quieter
control regime. Its centerpiece is a reduction of the inner-loop bandwidth towards the
oscillation bandwidth floor, dropping the loop's crossover well below the bending frequency so
the loop has little authority left with which to excite the mode — the primary stabilizer.
Lowering the bandwidth alone is not enough, though, because the residual wobble can still leak
back into the actuators through other paths, so three things happen alongside it:

* The :ref:`acceleration feedforward <acceleration-feedforward>` is cut. Being a numerical
  derivative it has the most gain at high frequency, which makes it the part of the controller
  most able to re-drive the bending mode once the bandwidth has been lowered.
* The loop tracks a *rate-independent* target — the velocity profile evaluated as if the
  measured rate were zero — so the residual wobble in the measured rate is not fed back into
  the velocity setpoint (which, at the floored bandwidth, the large autotuned gain would
  otherwise amplify).
* A light low-pass is applied to the final control output, as a frequency-independent backstop
  on any residual chatter.

This whole regime is released whenever the vessel is commanded to slew — so that maneuvers stay
fully responsive — and re-applied once the craft has settled.

A rigid or high-authority craft never triggers any of this and is left completely
unchanged, so the adaptation is safe to leave always on. On a craft that has latched, the
filtering of the measured rate is kept up whether it is slewing or holding, while the
bandwidth reduction and its companions apply only while holding.

Control oscillation
"""""""""""""""""""

The second signal the autopilot watches is its *own control output*, and it exists to cover a
blind spot in the attitude-oscillation handling above. The holding regime — the bandwidth floor
and the feedforward cut, rate-independent target and output low-pass that accompany it — is
released during a slew so that maneuvers stay responsive. On a very flexible craft, though, a
large maneuver can itself excite the bending mode into a sustained oscillation that holds the
pointing error just large enough to keep that regime released — so the craft never settles. The
autopilot guards against this by watching the amplitude of its control output about its
slowly-varying mean: a genuine slew is a steady, one-sided push, whereas a limit cycle shows up
as a large oscillating command. When an axis shows such a control oscillation, the full holding
regime is re-engaged regardless of the pointing error, until the oscillation dies away. This
recovery runs at the reduced bandwidth and so takes a few seconds.

This is strictly a secondary signal: it acts only on an axis that the attitude-oscillation
detector has *already* latched as flexible, so a rigid craft never invokes it. The
control-output oscillation, the threshold above which it triggers, and the manual override that
forces the damping on are all exposed; see :ref:`Flexible craft <flexible-craft>`.
