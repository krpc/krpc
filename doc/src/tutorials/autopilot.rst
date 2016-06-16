Auto Pilot
==========

kRPC provides an autopilot that can be used to hold a vessel in a chosen
orientation. It automatically tunes itself to cope with vessels of differing
size and control authority. This tutorial explains the operation of the
autopilot and the mathematics behind it.

Overview
--------

The inputs to the autopilot are:

* A reference frame defining where zero rotation is,
* pitch and heading angles,
* and an optional roll angle.

When a roll angle is not specified, the autopilot will try to zero out any
rotation around the roll axis but will not try to hold a specific roll
angle.

The diagram below shows a high level overview of autopilots operation. First,
the current and target rotations are used to compute the :rst:ref:`target
angular velocity <target-angular-velocity>` that is needed to rotate the vessel
to the target rotation. Next, the components of this angular velocity in the
pitch, yaw and roll axes of the vessel are passed to three PID controllers. The
outputs of these controllers are used as the control inputs for the vessel.

.. image:: /images/tutorials/autopilot-schematic.png
   :align: center

Configuring the AutoPilot
-------------------------

The default settings should suffice for most vessels, but there are various
parameters that affect the behavior of the autopilot:

* Max Rotational Speed - the maximum angular speed of the vessel.
* Time to peak - the time, in seconds, that the PID controllers take to adjust
  the angular velocity of the vessel to the target angular velocity. The default
  is 3 seconds.
* Overshoot - the amount by which the PID controllers are allowed to overshoot
  the target velocity, as a percentage (a value between 0 and 1). The default is
  0.01.

.. _target-angular-velocity:

Computing the Target Angular Velocity
-------------------------------------

First, a quaternion :math:`R` representing the rotation from the vessels current
rotation to the target rotation is calculated. If a target roll angle is not
specified, or if the vessel is not pointing close to the target direction,
:math:`R` is the rotation from the vessels current direction to the direction
defined by the target pitch and heading angles. Otherwise, :math:`R` is the
rotation from the vessels current rotation to the rotation defined by the target
pitch, heading and roll angles. A vessel is considered to be close to the target
direction when it is within 5 degrees of it.

This means that the autopilot will only roll when the vessel is pointing close
to its target direction. This is done because if the vessel is not pointing
close to the target direction, trying to achieve a target roll angle could be
problematic. For example, if the vessel is spinning around its pitch axis, the
roll angle will be changing very rapidly. Trying to correct the roll angle would
have no effect, as the rotation in the pitch axis needs to be corrected first.

The target angular velocity, denoted :math:`\vec{\omega_T}`, is then calculated from
:math:`R` as follows:

.. math::
   \vec{x}, \theta &= \text{axisangle}(R) \\
   \vec{y} &= \theta \vec{x} \\
   \vec{\omega_T} &= \frac{v_{max}}{2\sqrt{3}\pi} \vec{y}

The components of :math:`\vec{y}` are the rotations described by :math:`R`
around the pitch, roll and yaw axes. :math:`\vec{\omega_T}` is calculated from
this, by converting these angles to angular velocities in the range
:math:`[v_{max},-v_{max}]`. :math:`v_{max}` is the maximum rotation speed and is
configurable using
:csharp:prop:`KRPC.Client.Services.SpaceCenter.AutoPilot.MaxRotationSpeed`.

.. _tuning-the-controllers:

Tuning the Controllers
----------------------

Three PID controllers, one for each of the pitch, roll and yaw control axes, are
used to control the vessel. Each controller takes the relevant component of the
target angular velocity as input. The following describes how the gains for
these controllers are automatically tuned based on the vessels available torque
and moment of inertia.

The schematic for the entire system, in a single control axis, is as follows:

.. image:: /images/tutorials/autopilot-system.png
   :align: center

The input to the system is the angular speed around the control axis, denoted
:math:`\dot{\theta}`. The error in the angular speed
:math:`\dot{\theta_\epsilon}` is calculated from this and passed to controller
:math:`C`. This is a PID controller that we need to tune. The output of the
controller is the control input, :math:`x`, that is passed to the vessel. The
plant :math:`H` describes the physical system, i.e. how the control input
affects the angular acceleration of the vessel. The derivative of this is
computed to get the new angular speed of the vessel, which is then fed back to
compute the new error.

For the controller, :math:`C`, we use a proportional-integral controller. Note
that the controller does not have a derivative term, so that the system behaves
like a second order system and is therefore easy to tune.

The transfer function for the controller in the :math:`s` domain is:

.. math::
   C(s) &= K_P + K_I s^{-1}

From the schematic, the transfer function for the plant :math:`H` is:

.. math::
   H(s) &= \frac{\dot{\theta_\epsilon}(s)}{X(s)}

:math:`x` is the control input to the vessel, which is the percentage of the
available torque :math:`\tau_{max}` that is being applied to the vessel. Call
this the current torque, denoted :math:`\tau`. This can be written
mathematically as:

.. math::
   \tau &= x \tau_{max}

Combining this with the angular equation of motion gives the angular
acceleration in terms of the control input:

.. math::
   I &= \text{moment of inertia of the vessel} \\
   \tau &= I \dot{\theta_\epsilon} \\
   \Rightarrow \dot{\theta_\epsilon} &= \frac{x\tau_{max}}{I}

Taking the laplace transform of this gives us:

.. math::
   \mathcal{L}(\dot{\theta_\epsilon}(t)) &= s\dot{\theta_\epsilon}(s) \\
                                &= \frac{sX(s)\tau_{max}}{I} \\
   \Rightarrow \frac{\dot{\theta_\epsilon}(s)}{X(s)} &= \frac{\tau_{max}}{I}

We can now rewrite the transfer function for :math:`H` as:

.. math::
   H(s) = \frac{\tau_{max}}{I}

The open loop transfer function for the entire system is:

.. math::
   G_{OL}(s) &= C(S) \cdot H(s) \cdot s^{-1} \\
             &= (K_P + K_I s^{-1}) \frac{\tau_{max}}{Is}

The closed loop transfer function is then:

.. math::
   G(s) &= \frac{G_{OL}(s)}{1 + G_{OL}(s)} \\
        &= \frac{a K_P s + a  K_I}{s^2 + a K_P s + a K_I}
           \text{ where } a = \frac{\tau_{max}}{I}

The characteristic equation for the system is therefore:

.. math::
   \Phi &= s^2 + \frac{\tau_{max}}{I} K_P s + \frac{\tau_{max}}{I} K_I

The characteristic equation for a standard second order system is:

.. math::
   \Phi_{standard} &= s^2 + 2 \zeta \omega_0 s + \omega_0^2 \\

where :math:`\zeta` is the damping ratio and :math:`\omega_0` is the natural
frequency of the system.

Equating coefficients between these equations, and rearranging, gives us the
gains for the PI controller in terms of :math:`\zeta` and :math:`\omega_0`:

.. math::
   K_P &= \frac{2 \zeta \omega_0 I}{\tau_{max}} \\
   K_I &= \frac{I\omega_0^2}{\tau_{max}}

We now need to choose some performance requirements to place on the system,
which will allow us to determine the values of :math:`\zeta` and
:math:`\omega_0`, and therefore the gains for the controller.

The percentage by which a second order system overshoots is:

.. math::
   O &= e^{-\frac{\pi\zeta}{\sqrt{1-\zeta^2}}}

And the time it takes to reach the first peak in its output is:

.. math::
   T_P &= \frac{\pi}{\omega_0\sqrt{1-\zeta^2}}

These can be rearranged to give us :math:`\zeta` and :math:`\omega_0` in terms
of overshoot and time to peak:

.. math::
   \zeta = \sqrt{\frac{\ln^2(O)}{\pi^2+\ln^2(O)}} \\
   \omega_0 = \frac{\pi}{T_P\sqrt{1-\zeta^2}}

By default, kRPC uses the values :math:`O = 0.01` and :math:`T_P = 3`.

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

This could be caused, for example, by the reaction wheels on a vessel running
out of electricity resulting in the vessel having no torque.

In this situation, the autopilot also has little or no control over the
vessel. This means that the integral terms in the controllers will build up to a
large value over time. This is overcome by fixing the integral terms to zero
when the available angular acceleration falls below a small threshold.

This situation also causes an issue with the controller gain auto-tuning: as the
available angular acceleration tends towards zero, the controller gains tend
towards infinity. When it equals zero, the auto-tuning would cause a division by
zero. Therefore, auto-tuning is also disabled when the available acceleration
falls below the threshold. This leaves the controller gains at their current
values until the available acceleration rises again.
