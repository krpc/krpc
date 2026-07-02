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
   ap.wait()                             # blocks until the vessel has settled on the target
   ap.engaged = False

``wait()`` returns once the vessel has *settled* on the target: both the pointing error
and the vessel's angular velocity must fall below a threshold, so it does not return while
the nose is still swinging through the target. The two thresholds are the **stopping angle
threshold** (default 1°) and the **stopping velocity threshold** (default 0.05 rad/s), and
both can be adjusted. ``wait()`` also takes an optional ``timeout`` (in seconds), after
which it raises rather than blocking indefinitely.

While the autopilot is engaged it keeps SAS switched off, so that the two do not fight
each other for control of the vessel.

The autopilot handles structurally flexible craft automatically, detecting and
suppressing any structural oscillation (wobble) with no tuning required (see
:ref:`Flexible craft <flexible-craft>`), and can ease target changes in over time for
smooth maneuvers (see :ref:`Smoothing target changes <target-smoothing>`). Internally it
is a two-loop cascade controller; if you want to know how it turns an attitude error into
control inputs, see :ref:`How it works <how-it-works>`.

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
  to the target. The target velocity is at full above this angle and ramps linearly to
  zero at half of it — a *pointing deadband* — so the vessel coasts to a stop rather than
  the controls chasing sub-degree jitter when the vessel is pointing in the correct
  direction.

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

* **Oscillation mitigations** suppress any oscillation or wobble in a structurally
  flexible vessel. By default they are fully automatic, and a rigid craft is left
  untouched. Each individual mitigation can be inspected, forced on or off, or tuned
  separately, as described in :ref:`Flexible craft <flexible-craft>`.

* The **soft-start time** (default 0.5 seconds) is the time over which the control
  output is faded in when the autopilot is engaged, so that engaging does not deliver a
  sudden kick.  Set it to zero to disable the fade-in. See :ref:`When first engaging
  <first-engaging>`.

* The **target smoothing time** (default 0 seconds) makes the autopilot slew towards a
  new target over the given time, at a constant angular rate, rather than acting on the
  change immediately. Zero (the default) means changes take effect at once. See
  :ref:`Smoothing target changes <target-smoothing>`.

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

For example, to pitch the nose over by 10° across 20 seconds:

.. code-block:: python

   ap = vessel.auto_pilot
   ap.reference_frame = vessel.surface_reference_frame
   ap.target_pitch_and_heading(90, 90)   # start pointing straight up
   ap.engaged = True
   ap.wait()

   # ease a 10° pitch-over (90° → 80°) in over 20 seconds
   ap.target_smoothing_time = 20
   ap.target_pitch_and_heading(80, 90)
   ap.wait()                             # returns once the 80° target is reached

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
swinging back and forth. On a craft already marked flexible it keeps the damping engaged
through a maneuver; and on its own, a sustained control oscillation also switches on the
bandwidth reduction directly, catching a craft that limit-cycles for a non-structural
reason — typically an aggressively-tuned loop driving slow gimbal or aerodynamic actuators
(see :ref:`Limitations <autopilot-limitations>`).

As soon as the autopilot sees a craft begin to wobble, it *latches* that craft as
flexible and switches on up to four independent mitigations. The first — a :ref:`rate
filter <rate-filter>` — removes the attitude oscillation from its rate measurements so the
control loop does not chase it, and stays on whether the craft is holding or slewing. The
other three — a :ref:`bandwidth floor <bandwidth-floor>`, a :ref:`feedforward cut
<feedforward-cut>` and an :ref:`output filter <output-filter>` — stop the loop feeding
energy back into the structure, and apply only while the craft is *holding* attitude, so
that a
commanded maneuver stays responsive. If a maneuver sets the wobble going again, the
autopilot detects the resulting control oscillation and re-applies the holding
mitigations until the craft settles.

Each mitigation is exposed separately, so you can inspect, force, disable or tune any one
of them without disturbing the others (they are *orthogonal*). The remainder of this
section covers each in turn; the mechanism behind them is described under
:ref:`Oscillation detection and mitigation <oscillation>`.

.. _rate-filter:

Rate filter
"""""""""""

The rate filter removes the structural oscillation from the *measured* angular velocity
before the control loops act on it, so the loop has no gain at the bending frequency. It
is selected per axis group by the **pitch/yaw rate filter mode** and **roll rate filter
mode** properties, each taking one of:

* ``AUTOMATIC`` (the default) — detect and latch the oscillation, estimate its frequency,
  and route it to the right filter automatically: a notch for a low-frequency mode near
  the control band, a low-pass for a high-frequency mode, or a broadband low-pass while
  the frequency is not yet known. A rigid craft is left untouched.
* ``OFF`` — no rate filtering. The other mitigations are unaffected.
* ``NOTCH`` — always apply a notch filter at the manually set frequency. Use this when you
  already know the craft has a low-frequency structural mode near the control band.
* ``LOW_PASS`` — always apply a low-pass filter derived from the manually set frequency.
  Use this when you already know the craft has a high-frequency structural mode.

When forcing a tool, set the mode frequency for that axis group with the **pitch/yaw
oscillation frequency** or **roll oscillation frequency** property (in Hz, default 1.5);
the same properties also seed the automatic estimator before it acquires. The **oscillation
notch Q** (default 2.5) sets how narrow the notch is — higher is narrower, with less
in-band lag but less tolerance to the mode frequency drifting.

.. code-block:: python

   ap = vessel.auto_pilot
   # treat the craft as flexible from the start, notching a 1.4 Hz bending mode
   ap.pitch_yaw_rate_filter_mode = conn.space_center.RateFilterMode.notch
   ap.pitch_yaw_oscillation_frequency = 1.4

.. _bandwidth-floor:

Bandwidth floor
"""""""""""""""

The bandwidth floor reduces the inner control loop's bandwidth on a flexible axis while
it holds attitude, dropping the loop's crossover well below the bending frequency so it
has little authority left with which to excite the mode. This is the *primary* stabilizer
— it needs no accurate frequency estimate — so a flexible craft is held more gently than a
rigid one. It is controlled by the **oscillation bandwidth floor mode** property, taking
``AUTOMATIC`` (the default; engage on a latched axis while holding), ``OFF`` (never reduce
the bandwidth) or ``FORCED`` (keep it fully reduced at all times). The **oscillation
bandwidth floor** property (in rad/s, default 1) sets the bandwidth an axis is reduced
towards: lower suppresses oscillation more strongly, higher keeps more control authority
at the cost of allowing more wobble.

.. _feedforward-cut:

Feedforward cut
"""""""""""""""

The feedforward cut removes the :ref:`acceleration feedforward <acceleration-feedforward>`
on a flexible axis while it holds attitude. Being an open-loop plant inversion whose gain
rises with frequency, the feedforward is the path most able to re-excite the mode once the
bandwidth has been floored. It is controlled by the **oscillation feedforward mode**
property, taking ``AUTOMATIC`` (the default; follow the hold gate on a latched axis),
``OFF`` (never cut the feedforward) or ``FORCED`` (always cut it fully).

.. _output-filter:

Output filter
"""""""""""""

The output filter is a light low-pass on the delivered actuator command — a
frequency-independent backstop that caps any residual control chatter left by the other
mitigations.

The other three mitigations are each *targeted*: the rate filter depends on a good
frequency estimate, and the bandwidth floor and feedforward cut each close off one
particular path by which the wobble reaches the actuators. Between them they can still
leave a little high-frequency buzz on the command. The output filter is a catch-all for
that remainder — applied directly to the command the game actuates, it needs neither a
frequency estimate nor any particular gain, so it removes residual chatter whatever its
source. Because a flexible craft is already held at a reduced bandwidth, the small phase
lag it adds costs almost nothing.

It is controlled by the **oscillation output filter mode** property, taking
``AUTOMATIC`` (the default; engage on a latched axis, and lightly while the detector is
firing on an unlatched one), ``OFF`` (never smooth) or ``FORCED`` (smooth fully at all
times).

Holding versus slewing
""""""""""""""""""""""

The bandwidth floor, feedforward cut and output filter engage only while a latched axis is
*holding* attitude, and are released while it is *slewing*, so that a commanded maneuver
runs at full responsiveness. The autopilot decides holding versus slewing from the
pointing error, keyed per axis group: pitch/yaw on the pointing error, and roll on the
larger of the pointing error and the roll error (so a pure roll maneuver releases the roll
axis). This is the *hold gate*.

The gate has a blind spot: a maneuver on a very flexible craft can excite a limit cycle
that parks the pointing error just large enough to keep the gate released, so the craft
never settles. To close it, the autopilot watches its own control output for a sustained
oscillation and, on a latched axis, re-engages the holding mitigations regardless of the
pointing error until the oscillation dies away. This recovery runs at the floored
bandwidth and can take a few seconds.

Setting a mitigation's mode to ``FORCED`` engages it at all times regardless of the hold
gate — use this when you know in advance that a maneuver will excite the craft and want
the holding mitigations held on throughout.

Monitoring
""""""""""

Several read-only properties expose the detector and estimator state, and run in every
mode (so they remain observable even when a mitigation is forced or off):

* **oscillation level** — a value between 0 and 1 per axis measuring how strongly
  structural oscillation is currently detected.
* **pitch/yaw oscillation latched** and **roll oscillation latched** — whether the axis
  group has been confirmed *structurally* flexible from the measured rate (the attitude
  oscillation), which is what switches the damping on.
* **pitch/yaw oscillation detected frequency** and **roll oscillation detected
  frequency** — the estimated mode frequency in Hz, or not-a-number until the estimator
  has acquired it.
* **pitch/yaw control oscillation** and **roll control oscillation** — the amplitude of
  oscillation in the control output about its slowly-varying trim; near zero for a settled
  hold, approaching 1 for a sustained limit cycle.
* **pitch/yaw control oscillation latched** and **roll control oscillation latched** —
  whether the axis group has been latched from a sustained *control* oscillation with no
  structural signature: a rigid-body limit cycle, typically an aggressively-tuned loop
  driving slow gimbal or aerodynamic actuators (see :ref:`Limitations
  <autopilot-limitations>`). This latch engages the bandwidth floor and output filter but
  not the rate filter or feedforward cut, and is distinct from the structural
  *oscillation latched* flag above.

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

.. _corner-cases:

Corner cases
^^^^^^^^^^^^

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
while the autopilot is still engaged, the autopilot disengages, so that a dropped
connection does not leave the vessel locked under control with no way to release it.
Its configuration and target are left unchanged, so re-engaging resumes from where it
left off.

.. _autopilot-limitations:

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

The same single-figure model can also affect *stability*, not just accuracy. It treats
the commanded torque as taking effect instantly, but a vessel steered by gimbaled engines
or aerodynamic control surfaces responds with a lag — the gimbal takes time to slew, a
control surface to deflect and its aerodynamic force to build. Working only from the
instantaneous torque figure, the auto-tuner can then tune the loop more aggressively than
the delayed response can support, and the controller limit-cycles: the nose orbits the
target and the controls swing hard, even though the craft is perfectly rigid. This is
distinct from structural flexibility — there is no bending, and the oscillation is genuine
rigid-body motion rather than a bending mode showing up in the measured angular velocity,
so the flexibility detector does not see it. Instead, the autopilot treats a sustained
*control* oscillation (its own output swinging) as the trigger and applies the same
:ref:`bandwidth floor <bandwidth-floor>` it uses for a flexible craft — but keeping the
feedforward, which the slower loop needs to hold attitude — which lets it hold cleanly. As
with a flexible craft, the reduced bandwidth makes it track a large, continuous slew less
precisely. Modeling the actuator's *response*, not just its peak torque, would let the
loop keep a higher bandwidth here; until then such a craft is held gently rather than
tracked tightly.

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

.. _info-window:

The info window
^^^^^^^^^^^^^^^

For debugging, the autopilot can display an in-game window showing its live internal
state. It is enabled per vessel, and hidden again when the game restarts:

.. code-block:: python

   vessel.auto_pilot.show_info_ui = True

.. Screenshot: capture the window in-game (ideally on a flexible craft that is engaged and
   holding, so the OSCILLATION section is populated) and save it to
   doc/src/images/tutorials/autopilot-info-window.png. Then delete this comment and
   uncomment the figure directive below.

   .. figure:: /images/tutorials/autopilot-info-window.png
      :align: center
      :alt: The AutoPilot info window

      The AutoPilot info window, showing a flexible craft holding attitude with the
      oscillation mitigations engaged.

The window is laid out like a control panel of annunciator lamps and digital registers.
Lamps are dim when nominal and light amber to flag something worth attention; the
**ENGAGED** lamp reads green (amber **HELD** while the craft is held on the launch
clamps). From top to bottom it shows:

* **TARGET** — the attitude being tracked, in pitch, heading and roll (``CUR``, the target
  the loop is currently tracking, and ``CMD``, the commanded target, shown only while a
  change is being :ref:`smoothed <target-smoothing>` in).
* **ATTITUDE ERROR** — the total and per-axis pointing error to that target.
* **PID GAIN** — the autotuned inner-loop gains (:math:`K_P` and :math:`K_I`) per axis.
* **OSCILLATION** — the flexible-craft handling, mirroring the :ref:`detector, gates and
  mitigations <oscillation>` structure: the detector readouts (structural level,
  inner-loop bandwidth, control-output envelope and estimated mode frequency), the gate
  weights (hold factor, latch, ramp, back-off and the net gate), and a lamp per mitigation
  showing its mode and how strongly it is engaged.

It is purely a diagnostic aid, most useful on a flexible craft where it shows the
oscillation detector, gates and mitigations working in real time.

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
                 \sqrt{2 \alpha \lvert\theta\rvert},
                 \kappa\,\text{bw}\,\lvert\theta\rvert
             \big) \cdot D(\theta) \\
   \text{where} & \\
   \alpha &= \frac{\tau_{max}}{I} \\
   D(\theta) &= \operatorname{clamp}\!\left(
       \frac{\lvert\theta\rvert - \theta_a/2}{\theta_a/2},\ 0,\ 1\right)

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

Speed cone
""""""""""

The square-root profile has infinite gradient at the origin, so near the target
it can command speeds that fall towards zero faster than the inner rate loop
(bandwidth :math:`\text{bw}`, described :ref:`below <tuning-the-controllers>`)
can follow. On a vessel with very high control authority this produces a
sustained oscillation just outside the pointing deadband: the profile commands a
large speed at a degree or so of error, the vessel cannot shed that speed inside
the band, coasts through the target, and is symmetrically re-accelerated on the
far side. The target speed is therefore also capped by a linear *speed cone*
:math:`\kappa\,\text{bw}\,\lvert\theta\rvert` (:math:`\kappa = 1/2`, with
:math:`\text{bw}` the rate loop's closed-loop bandwidth — see
:ref:`Tuning the controllers <tuning-the-controllers>`) — the steepest
straight-line decay the inner loop can actually track down to zero.
Far from the target the cone is above the other terms and has no effect; its
reach scales with the vessel's authority, covering the whole final approach on a
very agile vessel and only the last few degrees on a sluggish one.

Pointing deadband
"""""""""""""""""

To prevent the vessel from oscillating when it is pointing at the target, the
target speed must fall to zero as the error vanishes, rather than following the
square root all the way to the origin — where its infinite gradient would chase
sub-degree jitter into the actuators. The square-root profile is therefore
multiplied by a linear *pointing deadband* :math:`D(\theta)`:

.. math::
   D(\theta) = \operatorname{clamp}\!\left(
       \frac{\lvert\theta\rvert - \theta_a/2}{\theta_a/2},\ 0,\ 1\right)

where :math:`\theta_a` is the attenuation angle. The deadband is 1 at and above
:math:`\theta_a`, ramps linearly to 0 at :math:`\theta_a/2`, and is 0 below that.
The autopilot therefore uses the full bang-bang profile until the vessel is close
to the target direction, then eases the target speed cleanly to zero and lets the
vessel coast to a stop inside the band. The deadband is keyed on the pure pointing
error :math:`\theta`, not the stopping-point prediction :ref:`below
<stopping-distance-feedforward>`, so measured-rate jitter is not fed through its
ramp.

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
<tuning-the-controllers>` for :math:`K_P`, :math:`\zeta` and :math:`\omega_0`). The
*unfloored* proportional gain is used here: on a flexible craft the :ref:`bandwidth floor
<oscillation>` deliberately lowers :math:`K_P` while holding, but the stopping-distance
term uses the gain *before* that reduction, so flooring the bandwidth does not inflate the
predicted :math:`1/\text{bw}` stopping distance. This term improves the approach on small,
rigid craft; on large flexible rockets the autopilot suppresses the bending mode
automatically — see :ref:`Oscillation detection and mitigation <oscillation>`.

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
       \min\big(\omega_{max},\ \sqrt{2\alpha\lvert\boldsymbol{e}_{stop}\rvert},\
       \kappa\,\text{bw}\,\lvert\boldsymbol{e}_{stop}\rvert\big)\,
       D(\lvert\boldsymbol{\theta}\rvert)

where :math:`\boldsymbol{\theta} = (\theta_{pitch}, \theta_{yaw})` is the direction
error and :math:`c\,\boldsymbol{\omega}` is the predicted coasting displacement. The
coefficient :math:`c` is the same stopping distance as in the one-dimensional case —
the larger in magnitude of the quadratic (bang-bang) and linear (PI-lag) terms, which
are both collinear with :math:`\boldsymbol{\omega}` and so collapse to a single scalar.
The acceleration :math:`\alpha`, the cap :math:`\omega_{max}` and the speed-cone
bandwidth :math:`\text{bw}` are projected along :math:`\hat{\boldsymbol{\omega}}` for
the prediction and along :math:`\hat{s}` for the speed profile. This makes the nose
follow the shortest, great-circle path to the target.

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

Rather than estimate this by differencing the setpoint between ticks — which would amplify
the measured-rate jitter by :math:`1/\Delta t` — the autopilot computes it *analytically*
from the profile's own branch decisions. The commanded speed is the product
:math:`g = \text{speed}\cdot D` of the square-root (or capped) speed and the
:ref:`pointing deadband <target-angular-velocity>`, and its rate :math:`\dot g` has a
closed form on each branch of the profile:

.. math::
   \dot g = \begin{cases}
       -\tfrac{1}{2}\,\alpha\,D & \text{quadratic (bang-bang) braking} \\[4pt]
       -\dfrac{\alpha\,\text{bw}\,\text{speed}}{\text{bw}\,\text{speed} + \alpha}\,D
           & \text{linear (PI-lag) braking} \\[4pt]
       0 & \text{velocity cap} \\[4pt]
       -\dfrac{c_{\kappa}\,\text{bw}\,\text{speed}}{\text{bw} + c_{\kappa}}\,D
           & \text{speed cone, linear stopping} \\[4pt]
       -\dfrac{c_{\kappa}\,\alpha\,\text{speed}}{\alpha + c_{\kappa}\,\text{speed}}\,D
           & \text{speed cone, quadratic stopping}
   \end{cases}
   \;+\; \text{speed}\,D'\,\dot\theta

where :math:`c_{\kappa} = \kappa\,\text{bw}` is the speed cone's slope: on the cone the
profile's gradient is the constant :math:`c_{\kappa}` rather than the square root's
:math:`\alpha/\text{speed}`, and the same self-consistent on-profile error rate gives the
two sub-cases above.

The final term is the deadband's own contribution while it is on its ramp
(:math:`D' = 2/\theta_a` is constant there, and :math:`\dot\theta` is the rate of change
of the pointing error); like the braking terms it is scaled by the *tracking fraction*
described below, since it too is an on-profile value — the acceleration needed for the
measured rate to follow the collapsing command through the band, meaningful only while
the vessel is actually tracking the command. Every term is an algebraic product of the current state with
bounded coefficients — there is no :math:`1/\Delta t`, so jitter is never amplified. The
result is normalized by the maximum angular acceleration and aimed along the commanded
direction :math:`-\hat{s}` to give the feedforward control fraction:

.. math::
   x_{ff} = -\hat{s}\,\frac{\dot g}{\alpha}

which is added to the PI controller's output before clamping to :math:`[-1, 1]`:

.. math::
   x = \operatorname{clamp}(x_{ff} + x_{PI},\ -1,\ 1)

When the vessel is following the trajectory perfectly, the PI error
:math:`\omega_{ref} - \omega` is near zero and the feedforward alone drives the
actuators. The PI controller then only needs to correct for disturbances (atmospheric
drag, center-of-mass offsets) rather than the trajectory itself, cleanly separating the
two concerns so they can be tuned independently.

The closed forms above are the *on-profile* accelerations — exact only while the vessel is
actually tracking the profile down towards the target. At the start of a maneuver the
vessel is nearly at rest, far below the commanded speed, and the planned braking is
fiction; applying it there would fight the saturated PI. The braking terms are therefore
scaled by the *tracking fraction* — the attained fraction of the commanded speed along the
command direction, clamped to :math:`[0, 1]` — which is near zero at spin-up (leaving the
saturated PI to provide the acceleration phase) and rises to one as the vessel settles
onto the profile.

The tracking fraction guards only the *slow* side of the profile — the vessel moving below
the commanded speed. The opposite case also needs handling. A vessel with very high control
authority can reach the target region still rotating far *faster* than the profile plans,
and cross the target before it can stop. As it nears the crossing the commanded speed
collapses towards zero, but the predicted stopping point :math:`\boldsymbol{e}_{stop}` swings
past the target and flips the command direction :math:`\hat{s}` to the far side; the
feedforward — dominated near the target by the deadband term
:math:`\text{speed}\,D'\,\dot\theta`, which is large when the vessel sweeps quickly through
the band — then drives *towards* the overshoot instead of braking it, and can pump a
sustained oscillation about the target. To prevent this the whole feedforward is scaled by an
*overshoot gate*, the complementary factor

.. math::
   \min\!\left(1,\ \frac{\text{speed}}{\omega_{\parallel}}\right)

where :math:`\omega_{\parallel}` is the measured angular rate along the command direction
:math:`-\hat{s}`. It is one while the vessel is on the profile
(:math:`\omega_{\parallel} \approx \text{speed}`) or rotating away from the command
(:math:`\omega_{\parallel} \le 0`), so ordinary slews, holds and the feedforward's normal role
are untouched, and it falls below one only once the vessel is crossing faster than the profile
commands — handing the braking there back to the PI controller, which is well damped on its
own.

The setpoint is continuous but not smooth: :math:`\dot g` steps by a bounded amount at the
seams where the profile switches branch (the velocity cap engaging, the quadratic and
linear stopping terms swapping). To smear these few genuine transitions the feedforward is
passed through a short first-order low-pass filter, with a time constant of a few physics
ticks. The lag this introduces is small and is absorbed by the PI controller, which sees
the unfiltered velocity error.

On a craft the autopilot has detected as structurally flexible, this feedforward is cut
while the craft is holding attitude. As an open-loop plant inversion its gain is largest
at high frequency, making it the control path most able to re-excite the bending mode once
the inner-loop bandwidth has been floored (see :ref:`Oscillation detection and mitigation
<oscillation>`). It is restored in full while the craft is slewing, and is always present
on a rigid craft.

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
wobble. A single always-on broadband filter would remove it, but at the cost of phase lag
that destabilizes a responsive, high-authority craft, so the suppression must be applied
only when, and only as strongly as, it is actually needed.

The machinery is organized in three layers. An *observation* layer watches the craft and
decides whether it is flexible and, if so, at what frequency it is oscillating. A *gating*
layer turns those observations into a per-tick, per-axis decision about which mitigations
to apply and how strongly. Four independent *mitigation* primitives — the rate filter,
bandwidth floor, feedforward cut and output filter — carry out the suppression. Because
each primitive only consumes the gating layer's output, any one can be forced or disabled
on its own without disturbing the others (see :ref:`Flexible craft <flexible-craft>` for
the manual controls).

In outline, the signals flow from the two measured inputs, through the observation and
gating layers, to the four mitigations that tap into the control loop:

.. graphviz::
   :align: center
   :caption: Oscillation observation, gating and mitigation pipeline.

   digraph oscillation {
       rankdir=TB;
       bgcolor="transparent";
       node [shape=box, style=rounded, fontname="Helvetica,Arial,sans-serif", fontsize=11];
       edge [fontname="Helvetica,Arial,sans-serif", fontsize=9, color="#555555"];

       // Inputs
       omega [label="measured rate ω\n(root part, carries wobble)", shape=box, style=filled, fillcolor="#eeeeee"];
       uout  [label="delivered control output", shape=box, style=filled, fillcolor="#eeeeee"];
       theta [label="pointing error θ", shape=box, style=filled, fillcolor="#eeeeee"];

       // Observation layer
       subgraph cluster_obs {
           label="OBSERVATION"; labeljust="l"; fontsize=10; fontname="Helvetica,Arial,sans-serif";
           style="dashed,rounded"; color="#999999";
           chatter [label="chatter\ndetector"];
           freq    [label="frequency\nestimator"];
           envel   [label="control-osc.\nenvelope"];
       }

       // Gating layer
       subgraph cluster_gate {
           label="GATING"; labeljust="l"; fontsize=10; fontname="Helvetica,Arial,sans-serif";
           style="dashed,rounded"; color="#999999";
           node [shape=box, style="filled,rounded", fillcolor="#dce8f5", color="#4a6f9c"];
           hold    [label="hold factor\n= f(θ)"];
           backoff [label="back-off = f(envelope)\n[latched axes only]"];
           level   [label="level = ramp ·\nmax(hold factor, back-off)"];
           tool    [label="tool = notch /\nlow-pass / broadband"];
           hold    -> level;
           backoff -> level;
       }

       // Mitigation layer
       subgraph cluster_mit {
           label="MITIGATION"; labeljust="l"; fontsize=10; fontname="Helvetica,Arial,sans-serif";
           style="dashed,rounded"; color="#999999";
           rate  [label="rate filter\n→ measured rate"];
           bw    [label="bandwidth floor\n→ inner-loop gain"];
           ffcut [label="feedforward cut\n→ accel. feedforward"];
           outf  [label="output filter\n→ delivered command"];
       }

       loop [label="control loop  —  actuates in [−1, 1]", shape=box, style=filled, fillcolor="#eeeeee"];

       omega -> chatter;
       omega -> freq;
       uout  -> envel;

       chatter -> level   [label="latch → ramp"];
       freq    -> tool    [label="f"];
       envel   -> backoff [label="envelope"];
       theta   -> hold;

       tool  -> rate  [label="tool, f, latch"];
       level -> bw    [label="level"];
       level -> ffcut [label="level (hold gate)"];
       level -> outf  [label="latch, level"];

       rate  -> loop;
       bw    -> loop;
       ffcut -> loop;
       outf  -> loop;
   }

The observation layer never acts on the craft; it only sets the latch, frequency and
envelope. The gating layer combines those with the pointing error into the per-axis
weights and the tool choice. Each of the four mitigations then reads only what it needs
from the gating layer, which is why any one can be overridden without disturbing the rest.

Observation: detection and frequency estimation
"""""""""""""""""""""""""""""""""""""""""""""""

The primary signal is the *measured angular velocity*. The autopilot watches for the
tell-tale signature of a bending-mode limit cycle: the measured rate changing from one tick
to the next by more than the available torque could physically cause. This test is
independent of the loop gains and of any commanded maneuver. It produces a level between 0
and 1 per axis — rising quickly when excitation appears, decaying slowly once it subsides —
and once that level is high enough the axis is *latched* as flexible. The latch persists if
the autopilot is briefly disengaged and re-engaged, so a craft known to be flexible stays
damped. Pitch and yaw are treated together: a long vehicle's bending mode appears in both,
so if one is found flexible the other is too.

In parallel, a frequency estimator tracks the oscillation frequency from the intervals
between sign changes of the high-passed rate. It is used only to route and tune the rate
filter, not to carry the suppression, and stays not-a-number until several half-periods
agree. It is the weak link — noisy and sometimes multi-modal — which is why the bandwidth
floor, not the notch, does the heavy lifting.

The secondary signal is the autopilot's *own control output*. On an axis already latched as
flexible, the autopilot measures the amplitude of its delivered command about the command's
slowly-varying mean: a genuine slew is a steady, one-sided push, whereas a limit cycle shows
up as a large oscillating command. This is used only for maneuver recovery, described under
:ref:`the hold gate <hold-gate>` below.

The rate filter
"""""""""""""""

The rate filter removes the oscillation from the *measured* angular velocity before any
control loop consumes it, so the loop has no gain at the bending frequency. It uses the
frequency estimate to pick the right tool: a **notch** for a low-frequency mode near the
control band (such as the roughly 1 Hz bending mode of a tall launch vehicle held vertical
on ascent), or a **low-pass** for a high-frequency mode well above the band, with its corner
derived from the estimate (at roughly a third of it). A notch is transparent everywhere
except at the mode frequency, so it can stay on permanently without slowing the craft's
response. Until the estimator has acquired a frequency the autopilot does not guess: rather
than risk a notch at the wrong place it falls back to a fixed, broadband low-pass with a
corner of about 2 Hz. This fallback — together with the bandwidth floor — is what keeps the
suppression robust when the frequency estimate is poor. The rate filter is kept up on a
latched axis whether the craft is holding or slewing.

The bandwidth floor
"""""""""""""""""""

The bandwidth floor is the *primary* stabilizer. On a latched axis it reduces the
inner-loop bandwidth (see :ref:`Controller gain tuning <tuning-the-controllers>`) towards
the oscillation bandwidth floor, dropping the loop's crossover well below the bending
frequency so the loop has little authority left with which to excite the mode. It only ever
*lowers* the bandwidth, so a rigid craft — which is never latched — is untouched. Crucially
it needs no frequency estimate, which is why it, rather than the surgical notch, carries the
suppression. Because the floored gain would otherwise distort the outer loop's
stopping-distance prediction, that prediction uses the *unfloored* gain instead (see
:ref:`Stopping-distance feedforward <stopping-distance-feedforward>`).

The feedforward cut
"""""""""""""""""""

Lowering the bandwidth alone is not enough, because the residual wobble can still leak into
the actuators through the feedforward. The :ref:`acceleration feedforward
<acceleration-feedforward>` is an open-loop plant inversion whose gain rises with frequency,
so once the bandwidth has been floored it becomes the path most able to re-drive the mode.
The feedforward cut removes it on a flexible axis while holding.

The output filter
"""""""""""""""""

Finally, a light first-order low-pass is applied to the delivered actuator command, as a
frequency-independent backstop on any residual chatter that leaks through the other paths. A
lighter version of the same filter is blended in — in proportion to the detector level — on
an axis that is *firing but not yet latched*, a noisy-but-controllable craft, to take the
buzz off the controls without committing to the full latched regime.

.. _hold-gate:

The hold gate
"""""""""""""

The rate filter stays on whenever an axis is latched, but the bandwidth floor, feedforward
cut and output filter are gated on whether the axis is *holding* attitude or *slewing*. The
gating layer computes a continuous hold factor from the pointing error — 1 while holding, 0
while slewing — and applies these three mitigations only while holding, so that a commanded
maneuver runs at full authority and responsiveness. The factor is per axis group: pitch and
yaw key on the pointing error, while roll keys on the larger of the pointing and roll
errors, so a pure roll maneuver releases the roll axis.

This gate has a blind spot. A large maneuver on a very flexible craft can itself excite the
bending mode into a sustained oscillation that holds the pointing error just large enough to
keep the gate released — so the craft never settles. This is where the control-output signal
comes in: when a latched axis shows a sustained control oscillation, the holding mitigations
are re-engaged regardless of the pointing error, until the oscillation dies away. This
recovery runs at the floored bandwidth and so takes a few seconds. Because it is OR-ed into
the hold gate, it only matters while the axis would otherwise be slewing, and cannot cause
hunting once the craft has settled.

A rigid or high-authority craft never latches and so triggers none of this; the adaptation
is safe to leave always on.
