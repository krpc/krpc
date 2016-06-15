Auto Pilot
==========

kRPC provides an autopilot that can be used to point a vessel in chosen
direction and hold a chosen roll angle. It automatically tunes itself to cope
with vessels with differing size and control torque.

On each physics tick, the autopilot computes the :rst:ref:`angular velocity
<target-angular-velocity>` that is needed to rotate the vessel to the chosen
direction and roll angle. This angular velocity is passed to a
:rst:ref:`rotation rate controller <rotation-rate-controller>` which uses three
PID controllers (one for each control axis) to compute the control inputs to
achieve this rotation.

.. _target-angular-velocity:

Computing the Target Angular Velocity
-------------------------------------

The target angular velocity is calculated in two parts. First, the angular
velocity necessary to rotate the vessel to face the target direction is
calculated. This is done as follows:

.. math::
   &\vec{d_C} = \text{vessels current direction vector} \\
   &\vec{d_T} = \text{the target direction vector} \\
   &\vec{x} = \vec{d_C} \times \vec{d_T} \\
   &\vec{y} = \vec{x} / \|\vec{x}\| \\
   &\text{if } \vec{d_T} \cdot \vec{d_C} < 0 \\
   &\hspace{1em} \text{angular velocity} = \alpha \cdot \vec{y} \\
   &\text{else} \\
   &\hspace{1em} \theta = \sin^{-1}(\|\vec{x}\|) \\
   &\hspace{1em} \text{angular velocity} = \alpha \cdot \frac{2}{\pi}\theta \cdot \vec{y}

The magnitude of this is between 0 and :math:`\alpha`, and it linearly decreases
the closer the vessel gets to the target direction. :math:`\alpha` is the
maximum rotation rate and is configurable using
:csharp:prop:`KRPC.Client.Services.SpaceCenter.AutoPilot.MaxRotationSpeed`.

Second, the angular velocity needed to roll the vessel to the target roll angle
is calculated:

.. math::
   \rho_C &= \text{current roll angle in radians} \\
   \rho_T &= \text{target roll angle in radians} \\
   \text{angular velocity} &= \beta \cdot \text{normangle}(\rho_T - \rho_C) \cdot \vec{d_T}

where the normangle function clamps the angle to the interval
:math:`[-\pi,+\pi]`. The magnitude of this is between 0 and :math:`\beta`, and
linearly decreases the closer the vessel gets to the target roll
angle. :math:`\beta` is the maximum rotation rate and is configurable using
KRPC.Client.Services.SpaceCenter.AutoPilot.MaxRollSpeed.

These two components are then summed to get the target angular velocity, which
is passed to the rotation rate controller.

.. _rotation-rate-controller:

Rotation Rate Controller
------------------------

The rotation rate controller uses three PID controllers (one for each axis of
rotation) to try and maintain the target angular velocity. The gains for these
PID controllers are automatically tuned based on the moment of inertia and
available torque of the current vessel.

The following diagram shows the schematic for the system in each axis:

.. image:: /images/tutorials/autopilot-controller.png
   :align: center

Given a target angular velocity :math:`\dot{\theta}`, the error in the angular
velocity is calculated and passed to controller :math:`C`. This computes the
control input :math:`x` (equivalent to the pitch/roll/yaw controls on the
vessel). The plant :math:`H` models the physical system, i.e. how this control
input affects the rotational acceleration of the vessel. The derivative of this
is computed to get the new angular velocity of the vessel, which is then fed
back to compute the new error to be input to the controller.

For the controller, a PI controller suffices. The derivative gain :math:`P_D` is
set to 0. The controllers transfer function is:

.. math::
   C(s) &= K_P + K_I s^{-1} \\

The transfer function for the plant :math:`H` is:

.. math::
   H(s) &= \frac{\dot{\theta}(s)}{X(s)}

:math:`x` is the pitch/roll/yaw input to the vessel, and is therefore the
percentage of the available torque :math:`\tau` being applied to the vessel. The
current torque being applied to the vessel :math:`\tau_C` can be written as:

.. math::
   \tau_C &= x\tau

This can be combined with the rotational equation of motion to get the
rotational acceleration in terms of the control input:

.. math::
   I &= \text{vessels moment of inertia} \\
   \tau_C &= I \dot{\theta} \\
   \Rightarrow \dot{\theta} &= \frac{x\tau}{I}

Taking the laplace transform to convert to the s domain we get:

.. math::
   \mathcal{L}(\dot{\theta}(t)) &= s\dot{\theta}(s) \\
                                &= \frac{sX(s)\tau}{I} \\
   \Rightarrow \frac{\dot{\theta}(s)}{X(s)} &= \frac{\tau}{I}

The transfer function for :math:`H` is therefore:

.. math::
   H(s) = \frac{\tau}{I}

The open loop transfer function for the entire system is:

.. math::
   G_{OL}(s) &= C(S) \cdot H(s) \cdot s^{-1} \\
             &= (K_P + K_I s^{-1}) \frac{\tau}{Is}

The closed loop transfer function is then:

.. math::
   G(s) &= \frac{G_{OL}(s)}{1 + G_{OL}(s)} \\
        &= \frac{\gamma K_P s + \gamma  K_I}{s^2 + \gamma K_P s + \gamma K_I}
           \text{ where } \gamma = \frac{\tau}{I} \\

This has characteristic equation:

.. math::
   \Phi &= s^2 + \gamma K_P s + \gamma K_I \\

The characteristic function for a standard second order system is:

.. math::
   \Phi_{standard} &= s^2 + 2\zeta \omega s + \omega^2 \\

where :math:`\zeta` is the damping ratio and :math:`\omega` is the systems
natural frequency. The system is overdamped (converges quickly but with some
overshoot) when :math:`0 < \zeta < 1`.

Equating coefficients and rearranging gives us the PI gains in terms of
:math:`\zeta` and :math:`\omega`:

.. math::
   K_P &= \frac{2 \zeta \omega I}{\tau} \\
   K_I &= \frac{I\omega^2}{\tau}

We now need to choose some performance requirements to place on the system,
which will determine the values of the PI gains.

The percentage by which a second order system overshoots is:

.. math::
   O &= e^{-\frac{\pi\zeta}{\sqrt{1-\zeta^2}}}

And the time take to reach the first peak is:

.. math::
   T_P &= \frac{\pi}{\omega\sqrt{1-\zeta^2}}

These formulas can be rearranged to get the values of :math:`\zeta` and
:math:`\omega` in terms of overshoot and time to peak, which can in turn be used
to calculate the PI gains. By default, kRPC uses the values :math:`O = 0.01` and
:math:`T_P = 3`.
