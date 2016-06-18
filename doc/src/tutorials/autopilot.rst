Auto Pilot
==========

kRPC provides an autopilot that can be used to hold a vessel in a chosen
orientation. It automatically tunes itself to cope with vessels of differing
size and control authority. This tutorial explains the operation of the
autopilot, how to configure it and mathematics behind it.

Overview
--------

The inputs to the autopilot are:

* A reference frame defining where zero rotation is,
* pitch and heading angles,
* and an optional roll angle.

When a roll angle is not specified, the autopilot will try to zero out any
rotation around the roll axis but will not try to hold a specific roll
angle.

The diagram below shows a high level overview of the autopilot. First, the
current and target rotations are used to compute the :rst:ref:`target angular
velocity <target-angular-velocity>` that is needed to rotate the vessel to the
target rotation. Next, the components of this angular velocity in the pitch, yaw
and roll axes of the vessel are passed to three PID controllers. The outputs of
these controllers are used as the control inputs for the vessel.

..
   TODO: add stopping time, acceleration factor, dead zone etc. to diagram

.. image:: /images/tutorials/autopilot-schematic.png
   :align: center

Configuring the AutoPilot
-------------------------

The default settings should suffice in most cases, but there are various
parameters that affect the behavior of the autopilot:

* The **stopping time** is the maximum amount of time that the vessel will need
  to come to a complete stop. In other words, the autopilot will not rotate
  rotate the vessel faster than a maximum angular speed such that it can stop
  its rotation within this time. Decreasing this parameter will allow the
  autopilot to rotate the vessel more quickly, but risks overshooting the
  target.

  This parameter is a vector of three stopping times, one for each of the pitch,
  roll and yaw axes. The default value is 1 second for each axis.

* The **acceleration factor** is how quickly the autopilot will try to
  decelerate the vessel as it approaches the target direction. More
  specifically, it is the percentage of the vessels angular acceleration used to
  decelerate the vessel.

  This parameter is a vector of three acceleration factors, each between 0 and
  1, for each of the pitch, roll and yaw axes. The default value is 0.8 for each
  axis.

  A value of 1 means that the vessel will try to decelerate the vessel as late
  as possible, as it approaches the target direction. This will result in the
  vessel turning faster but risks overshooting. Setting it to a smaller value
  will cause the vessel to slow down sooner, which gives it more time to avoid
  overshoot.

* The **dead zone** angle sets the region in which the vessel is considered to
  be pointing in the correct direction.

  This parameter is a vector of three angles, one for each of the pitch, roll
  and yaw axes. The default value is 0 degrees in each axis - i.e. no dead zone.

  A value of 1 means that when the vessel is within 1 degree of the target
  direction, the autopilot will stop rotating the vessel. This value defaults to
  zero, as it is not needed for most vessels. However, if your vessel
  experiences oscillation when trying to hold the target direction, increasing
  this value might help resolve the issue.

* The **time to peak**, in seconds, that the PID controllers take to adjust the
  angular velocity of the vessel to the target angular velocity.

  This parameter is a vector of three times, one for each of the pitch, roll and
  yaw axes. The default is 3 seconds in each axis.

  Decreasing this value will make the controllers try to match the target
  velocity more aggressively.

* The **overshoot**, as a percentage, which is the amount by which the PID
  controllers are allowed to overshoot the target angular velocity.

  This parameter is a vector of three values, between 0 and 1, and one for each
  of the pitch, roll and yaw axes. The default is 0.01 in each axis.

  Increasing this value will make the controllers try to match the target
  velocity more aggressively, but will cause some overshoot.

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
   \vec{\Theta} &= \theta \vec{x} \\
   \vec{\omega_T} &= \big( f(\vec{\Theta}_x), f(\vec{\Theta}_y), f(\vec{\Theta}_z) \big)

The components of :math:`\vec{\Theta}` are the rotations described by :math:`R`
around the pitch, roll and yaw axes. :math:`\vec{\omega_T}` is calculated from
this, by applying a function :math:`f` to its elements. This function converts
the angles to rotational speeds, and is defined as follows:

.. image:: /images/tutorials/autopilot-angular-speed.png
   :align: center

.. math::
   f(\theta)
        &= \text{min} \big(
               \dot{\theta}_{max},
               m \cdot (\theta + \theta_{dead}
           \big) \text{ if } \theta < -\theta_{dead} \\
        &= 0 \text{ if } -\theta_{dead} \leq \theta \leq \theta_{dead} \\
        &= \text{max} \big(
               -\dot{\theta}_{max},
               m \cdot (\theta - \theta_{dead}
           \big) \text{ if } \theta > \theta_{dead} \\
   \text{where} & \\
   m &= -\alpha \frac{2}{t_{stop}} \\
   \dot{\theta}_{max} &= \frac{\tau_{max}t_{stop}}{I}

The reasoning for this is as follows:

* We want the vessel to rotate towards :math:`\theta = 0`. This means that the
  target angular velocity :math:`f(\theta)` needs to be positive when
  :math:`\theta` is negative, and negative when :math:`\theta` is positive.

* As the vessel approaches the target :math:`\theta = 0` we want its velocity to
  decrease, so that the vessel stops rotating at the target. This means we need
  :math:`f(\theta)` to tend to zero as :math:`\theta` tends to zero.

* :math:`t_{stop}` is the maximum stopping time in seconds, which determines the
  vessels maximum angular speed :math:`\dot{\theta}_{max}`. This is the maximum
  speed the vessel should rotate at so that it is able to stop within
  :math:`t_{stop}` seconds. To derive it, assume the vessel is at initially at
  rest and then accelerates as fast as it can for :math:`t_{stop}` seconds. The
  resulting velocity is :math:`\dot{\theta}_{max}`. Using the equation of motion
  under constant acceleration:

  .. math::
     \dot{\theta}_{max} &= \ddot{\theta} \cdot t_{stop} \\
                        &= \frac{\tau_{max}}{I} \cdot t_{stop}

* :math:`\alpha` is the percentage of the vessels angular acceleration that
  should used to decelerate the vessel, called the 'acceleration factor'. A
  value of 1 means that the autopilot will start decelerating the vessels
  rotation as late as possible. Smaller values make the deceleration less
  aggressive.

  This parameter controls the gradient :math:`m` of :math:`f` which is:

  .. math::
     m = -\alpha \frac{\dot{\theta}_{max}}{\theta_{max}}

  :math:`\dot{\theta}_{max}` is as defined above, and :math:`\theta_{max}` is
  the angle at which the vessel must start decelerating in order to stop
  rotating when it reaches :math:`\theta = 0` when using all of the vessels
  available angular acceleration.

  To derive :math:`\theta_{max}` imagine that the vessel is at rest at angle
  :math:`\theta = 0`. It then accelerates fully for :math:`t_{stop}`
  seconds. The angle it reaches is :math:`\theta_{max}`. From the equation of
  motion under constant acceleration we have:

  .. math::
     \theta_{max} &= \frac{1}{2}\ddot{\theta}t_{stop}^2 \\
                  &= \frac{1}{2} \cdot \frac{\tau_{max}}{I} \cdot t_{stop}^2 \\
                  &= \frac{\tau_{max}t_{stop}^2}{2I}

  We can now substitute these into the equation for the gradient:

  .. math::
     m &= -\alpha \frac{\dot{\theta}_{max}}{\theta_{max}} \\
       &= -\alpha \frac{\tau_{max}t_{stop}}{I} \big/ \frac{\tau_{max}t_{stop}^2}{2I} \\
       &= -\alpha \frac{2}{t_{stop}}

* :math:`\theta_{dead}` specifies the dead zone. If the rotational error is less
  than this threshold angle, the vessel is assumed to be pointing in the target
  direction so the target angular speed is 0. This is used to prevent small
  magnitude oscillations in the target velocity when the vessel is pointing very
  close to the correct direction. It should usually be set to a very small
  value.

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
