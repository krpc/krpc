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

#. It passes the pitch, yaw and roll components of this angular velocity to three
   PID controllers, one per control axis. The outputs of these controllers are
   used as the pitch, yaw and roll control inputs for the vessel.

Pitch and yaw are not controlled independently of each other. They are computed
together, in a frame from which the vessel's current roll has been removed (the
*roll-invariant frame*), so that the nose follows the shortest, great-circle path
to the target direction and so that rolling the vessel does not disturb that
path. Roll is controlled separately, and is only blended in once the vessel is
already pointing close to the target (see :ref:`below
<target-angular-velocity>`).

The behavior of the autopilot is governed by several parameters, covered in the
next section. The default values should suffice in most cases.

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
  included. It should be disabled for large, structurally flexible rockets — see
  :ref:`Corner Cases <corner-cases>`.

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
barely rotating. If the autopilot reacts to this oscillation it can pump energy
into the bending mode and drive a growing wobble. Two adjustments help.

First, **disable the deceleration lag correction** by setting deceleration lag
correction to ``False``. The linear term of the :ref:`stopping-distance feedforward
<stopping-distance-feedforward>` divides the measured angular velocity by the controller
bandwidth. At a bandwidth of a few rad/s, a 0.05 rad/s bending oscillation adds of the
order of a degree of spurious correction — enough to flip the sign of
:math:`\theta_{ff}` on each bending cycle, so that the autopilot commands braking torque
every cycle and excites the bending mode. The quadratic term, which is always used, adds
well under 0.1° at the same amplitude and cannot flip the sign, so the quadratic term
alone is well behaved.

Second, **increase the time to peak** on the pitch and yaw axes. This lowers the natural
frequency :math:`\omega_0 = \pi / (T_P\sqrt{1-\zeta^2})`, and hence the closed-loop
bandwidth :math:`2\zeta\omega_0`, keeping it well below the structural resonance
frequency so that the PI controller no longer responds to, and drives, the bending
oscillations.

For example, for a large, flexible launcher::

   ap = vessel.auto_pilot
   ap.decel_lag_correction = False
   ap.time_to_peak = (20, 1, 20)  # slower pitch and yaw; roll can stay fast

Roll is typically far less affected, as it is driven by reaction wheels
distributed throughout the vessel rather than by the engine gimbal at the base,
so its time to peak can usually be left short.

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
the approach on small, rigid craft, but must be disabled for large flexible rockets —
see :ref:`Corner Cases <corner-cases>`.

Pitch and yaw together
^^^^^^^^^^^^^^^^^^^^^^^^

If pitch and yaw were each driven by the one-dimensional profile above
independently, each axis would target a speed proportional to
:math:`\sqrt{\lvert\theta\rvert}` in that axis. The direction of the combined
rotation would then differ from the direction of the error, and the nose would
trace a curved path to the target.

To avoid this, pitch and yaw are treated as a single two-dimensional problem in
the roll-invariant frame. The autopilot computes the total direction error:

.. math::
   \theta_{2d} = \sqrt{\theta_{pitch}^2 + \theta_{yaw}^2}

applies the profile (including the feedforward and attenuation) once to
:math:`\theta_{2d}`, and then distributes the resulting speed back across the
pitch and yaw axes in proportion to the error in each. The acceleration
:math:`\alpha` and the cap :math:`\omega_{max}` used are likewise projected along
the error direction. This makes the nose follow the shortest, great-circle path
to the target.

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
