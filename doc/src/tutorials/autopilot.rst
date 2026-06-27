AutoPilot
=========

kRPC provides an autopilot that can be used to hold a vessel in a chosen
orientation. It automatically tunes itself to cope with vessels of differing
size and control authority. This tutorial explains how the autopilot works, how
to configure it and the mathematics behind it.

Overview
--------

The inputs to the autopilot are:

* A reference frame defining where zero rotation is,
* target pitch and heading angles,
* and an (optional) target roll angle.

When a roll angle is not specified, the autopilot will try to zero out any
rotation around the roll axis but will not try to hold a specific roll angle.

Each physics tick the autopilot does the following:

#. It compares the current rotation with the target rotation to compute the
   :ref:`target angular velocity <target-angular-velocity>` needed to rotate
   the vessel towards the target.

#. It differentiates the target angular velocity setpoint to obtain a target angular
   acceleration, normalizes it by the maximum angular acceleration
   :math:`\alpha = \tau_{max} / I` to produce a feedforward control fraction, and
   passes the target angular velocity to three PI controllers (one per axis). Each
   controller's output is summed with the feedforward and clamped to
   :math:`[-1, 1]` to give the pitch, yaw and roll control inputs for the vessel.

Together these two steps form a *cascade*: an outer loop converts the attitude error
into a target angular velocity, and an inner loop of three PI controllers drives the
vessel's measured angular velocity onto that target. The outer loop is the velocity
profile described in :ref:`Computing the Target Angular Velocity
<target-angular-velocity>`; the inner loop is the set of PI controllers described in
:ref:`Tuning the Controllers <tuning-the-controllers>`. The autopilot also continuously
watches for structurally flexible craft and adapts itself when it detects bending
oscillation, as described under :ref:`Corner Cases <corner-cases>`.

While the autopilot is engaged it keeps the stock SAS system switched off, so that the
two do not fight each other for control of the vessel.

Pitch and yaw are not controlled independently of each other. They are computed
together, in a frame from which the vessel's current roll has been removed (the
:ref:`roll-invariant frame <roll-invariant-frame>`), so that the nose follows the shortest, great-circle path
to the target direction and so that rolling the vessel does not disturb that
path. Roll is controlled separately, and is only blended in once the vessel is
already pointing close to the target (see :ref:`below
<target-angular-velocity>`).

Configuring the AutoPilot
-------------------------

The following parameters affect the behavior of the autopilot. The default
values should suffice in most cases, but they can be adjusted to fit your needs.

* The **maximum angular velocity** (default 1 rad/s along each axis) caps the target
  angular velocity used when slewing the vessel towards the target. It is a vector of
  three values, one for each of the pitch, roll and yaw axes. Vessels with low or
  moderate control authority rarely reach this cap; it mainly prevents very high-torque
  craft from spinning faster than desired.

* The **attenuation angle** (default to 1 degree per axis) sets the region in which the
  autopilot considers the vessel to be 'close' to the target direction. In this region
  the target velocity is smoothly attenuated towards zero, based on how close the vessel
  is to the target. It is an angle, in degrees, for each of the pitch, roll and yaw
  axes. This prevents the controls from oscillating when the vessel is pointing in the
  correct direction. If you find that the vessel still oscillates, try increasing this
  value.

* The **time to peak** (default is 1 second per axis) is the time, in seconds, that the
  PID controllers take to adjust the vessel's angular velocity to the target angular
  velocity. Decreasing it makes the controllers match the target velocity more
  aggressively, at the cost of a higher control bandwidth. It is a vector of three
  times, one for each of the pitch, roll and yaw axes.

* The **overshoot** (default is 0.01 in each axis) is the fraction by which the PID
  controllers are allowed to overshoot the target angular velocity. Increasing it makes
  the controllers match the target velocity more aggressively, but causes more
  overshoot. It is a vector of three values, between 0 and 1, for each of the pitch,
  roll and yaw axes.

* The **roll start angle** (default 20°) and **roll engage angle** (default 15°) define
  the band of direction error over which roll control is blended in. Above the roll
  start angle the vessel's roll is ignored, so that all of the available control
  authority is used to point the nose. Below the roll engage angle roll is fully
  controlled. Between the two, roll is blended in linearly.

* The **deceleration lag correction** (default ``True``) controls whether the linear
  term of the :ref:`stopping-distance feedforward <target-angular-velocity>` is
  included. Structurally flexible rockets are handled automatically and do not normally
  need this changed; it remains available as an override — see
  :ref:`Corner Cases <corner-cases>`.

* The **gyroscopic compensation** (default ``True``) controls whether the autopilot
  cancels the gyroscopic cross-coupling term :math:`\boldsymbol{\omega}\times(I\boldsymbol{\omega})`
  with a feedforward, as described in :ref:`Gyroscopic feedforward
  <gyroscopic-feedforward>`. It is negligible during normal attitude holding and only
  matters for fast rotations or vessels with strongly asymmetric moments of inertia.

* **Auto-tuning** (default ``True``) controls whether the PID controller gains are
  automatically tuned from the vessel's available torque and moment of inertia, as
  described in :ref:`Tuning the Controllers <tuning-the-controllers>`. When disabled,
  the gains can be set manually.

.. _corner-cases:

Corner Cases
------------

When sitting on the launchpad
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

In this situation, the autopilot cannot rotate the vessel. This means that the
integral term in the controllers will build up to a large value. This is even
true if the vessel is pointing in the correct direction, as small floating point
variations in the computed error will also cause the integral term to
increase. The integral terms are therefore fixed at zero to overcome this.

When the available angular acceleration is zero
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This could be caused, for example, by the reaction wheels on a vessel running out
of electricity, resulting in the vessel having no torque on one or more axes.

In this situation, the autopilot has little or no control over the vessel, so the
integral term in the affected controller would build up to a large value over
time. This is overcome by fixing the integral term to zero, and zeroing the
output, when an axis has no available torque.

This situation also causes an issue with the controller gain auto-tuning: as the
available torque tends towards zero, the controller gains tend towards
infinity. When it equals zero, the auto-tuning would divide by zero. Auto-tuning
is therefore skipped on an axis when its available torque is zero, leaving the
gains at their current values until torque is available again.

Wobbly or structurally flexible rockets
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Large rockets built from many parts behave less like a rigid body and more like a
flexible beam: KSP joins parts with springy joints, so the vessel bends. The
autopilot measures angular velocity at the vessel's root part, so this measured
value contains an oscillating component from the structural bending modes —
typically of the order of 0.01 to 0.1 rad/s — even when the vessel as a whole is
barely rotating. If the autopilot reacted to this oscillation it would pump energy
into the bending mode and drive a growing wobble.

**This is handled automatically — no tuning is required.** The autopilot watches for
the tell-tale signature of a bending-mode limit cycle (the measured rate changing from
one tick to the next by more than the available torque could physically cause) and, on
the affected axes only, adapts itself: it low-pass filters the measured rate, computes
its feedforward from the commanded trajectory rather than the noisy measurement, and
lowers the inner-loop bandwidth so the loop no longer responds to — and drives — the
bending oscillation. A rigid or high-authority craft never triggers this and is left
completely unchanged, so the adaptation is safe to leave always on. A flexible craft
will therefore settle and hold steadily on the default parameters.

In more detail, the detector tracks each axis independently and produces a level between
0 and 1 measuring how strongly that axis is oscillating. This level rises quickly when
excitation appears but decays slowly once it subsides, so the quiet, well-behaved state
that the adaptation produces does not immediately switch the adaptation back off and let
the oscillation return. The level also persists if the autopilot is briefly disengaged
and re-engaged, so a craft known to be flexible stays damped. The three adaptations are
blended in proportion to this level rather than switched abruptly on and off, so the
transition between the rigid and flexible regimes is smooth.

The older manual levers remain available as overrides for unusual cases. Disabling the
:ref:`deceleration lag correction <stopping-distance-feedforward>` drops the linear
stopping-distance term (whose division of the measured rate by the controller bandwidth
amplifies bending content), and increasing the time to peak on the pitch and yaw axes
lowers the closed-loop bandwidth :math:`2\zeta\omega_0`, keeping it below the structural
resonance::

   ap = vessel.auto_pilot
   ap.decel_lag_correction = False
   ap.time_to_peak = (20, 1, 20)  # slower pitch and yaw; roll can stay fast

Roll is typically far less affected, as it is driven by reaction wheels
distributed throughout the vessel rather than by the engine gimbal at the base.

When the controlling client disconnects
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The autopilot is engaged by a client over the network. If that client disconnects
while the autopilot is still engaged, the autopilot resets its target to zero pitch
and heading in the vessel's surface reference frame, with roll left uncontrolled, and
disengages. This ensures that a dropped connection does not leave the vessel locked to
a stale target with no way to release it.

.. _roll-invariant-frame:

The roll-invariant frame
------------------------

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

Separating the direction and roll errors
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

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

Computing the Target Angular Velocity
-------------------------------------

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
^^^^^^^^^^^^^^^^^^^^^^^^^^

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
^^^^^^^^^^^^^^^^^^^^^^^^^^^

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
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

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

where :math:`\text{bw}` is the closed-loop bandwidth (see :ref:`Tuning the Controllers
<tuning-the-controllers>` for :math:`K_P`, :math:`\zeta` and :math:`\omega_0`). The
linear term is only included when deceleration lag correction is enabled. It improves
the approach on small, rigid craft; on large flexible rockets the autopilot suppresses
the bending mode automatically (the correction can also be disabled manually) —
see :ref:`Corner Cases <corner-cases>`.

Pitch and yaw together
^^^^^^^^^^^^^^^^^^^^^^^^

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
the larger in magnitude of the quadratic (bang-bang) and linear (PID-lag) terms, which
are both collinear with :math:`\boldsymbol{\omega}` and so collapse to a single scalar.
The acceleration :math:`\alpha` and the cap :math:`\omega_{max}` are projected along
:math:`\hat{\boldsymbol{\omega}}` for the prediction and along :math:`\hat{s}` for the
speed profile. This makes the nose follow the shortest, great-circle path to the target.

Tangential damping
^^^^^^^^^^^^^^^^^^

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

.. _acceleration-feedforward:

Acceleration feedforward
^^^^^^^^^^^^^^^^^^^^^^^^

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

.. _gyroscopic-feedforward:

Gyroscopic feedforward
^^^^^^^^^^^^^^^^^^^^^^^

The :ref:`controller tuning <tuning-the-controllers>` models each axis as an independent
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
strongly asymmetric inertia. It can be disabled with the gyroscopic compensation
parameter.

.. _roll-control:

Roll
^^^^

Roll is controlled separately, using the one-dimensional profile directly on the roll
axis. It is only worth controlling once the vessel is pointing close to the target;
holding a roll angle while the nose is still slewing wastes control authority and can
curve the path. The target roll speed is therefore scaled by a weight that is 0 above
the roll start angle, 1 below the roll engage angle, and blends linearly between the
two. When no target roll is set, the roll target speed is simply 0, so that the
autopilot damps roll rotation without holding a specific angle.

.. _tuning-the-controllers:

Tuning the Controllers
----------------------

Three PID controllers, one for each of the pitch, roll and yaw control axes, are
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
^^^^^^^^^^^^^^^^^^^^^

The integral term of a PI controller can *wind up* — accumulate a large value — whenever
the controller is unable to reduce the error for a sustained period, for example during a
long slew while the output is already saturated. A wound-up integral then overshoots
badly once the error finally clears, because it takes time to unwind. To prevent this,
the controllers use *conditional integration*: the integral stops accumulating whenever
it is already at the :math:`[-1, 1]` output limit and the new contribution would only
push it further into saturation. The integral term is also reset outright in the special
cases described under :ref:`Corner Cases <corner-cases>` — on the launchpad, and on any
axis that currently has no available torque.

Limitations
-----------

Some limitations remain, and are worth being aware of when relying on the autopilot.

The torque model assumes a constant maximum torque on each axis. This is accurate for
reaction wheels, but only approximate for RCS thrusters (which are discrete and often
asymmetric) and for engine gimbals and control surfaces (whose authority varies with
throttle and with dynamic pressure). The fine-control mode (Caps Lock), which halves RCS
and reaction-wheel authority, is also not modeled. The auto-tuner recomputes the gains
every tick from the currently available torque, so it adapts as these change, but only as
fast as the inner loop can respond.

While the autopilot detects and damps structural flexibility automatically, it does so by
reducing the affected craft's control bandwidth rather than by notching out the specific
bending frequency, so a very flexible craft is held a little more gently than a rigid one.
This also produces a brief transient during a large slew: as the craft accelerates quietly
the bandwidth drifts back up, then re-tightens when full-torque braking begins, giving a
few seconds of mild oscillation mid-slew before a clean hold. Estimating the bending
frequency and applying a notch filter at it — which would damp the mode without reducing
the bandwidth at all — is the preferred long-term improvement.

The flexibility detector's thresholds are fixed constants, tuned against the craft tested
so far. A craft with an unusually high or low bending frequency, or whose dominant bending
mode is on the roll axis, has not been exercised and may not be handled as well.

Finally, the autopilot is designed for, and tested in, vacuum. It is a closed-loop
attitude tracker: aerodynamic torques appear to it as disturbances that the inner loop
only rejects once an error has built up. In a thick atmosphere — during ascent through
maximum dynamic pressure, or re-entry at a high angle of attack — the aerodynamic torque
can be large and fast-changing, and may even exceed the vessel's control authority. The
autopilot will still drive towards the target, but a low-authority craft can lag the
target by many degrees, or fail to hold attitude at all. It also does **not** manage angle
of attack: keeping the vessel within safe structural and thermal limits in atmosphere is
the responsibility of your script, not the autopilot. A planned improvement is an
aerodynamic disturbance feedforward, which would estimate the current aerodynamic torque
and cancel it before it becomes a tracking error, collapsing the lag even under heavy
aerodynamic loading.
