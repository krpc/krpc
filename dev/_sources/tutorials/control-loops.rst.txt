.. _tutorial-control-loops:

Control Loops
=============

A control loop reads the game state, computes something from it and writes the result back,
over and over. The game runs its physics on a fixed step, 50 times a second. A loop that flies
a vessel needs to read and write once per step, acting on the state it read.

The Receive Timeout
-------------------

The server executes calls inside the game's physics update, and waits a short while between
them for a client's next call. That wait is the **receive timeout**, one millisecond by
default.

A control loop spends its time between the calls: it reads, then computes, then writes.
Computing for longer than a millisecond is easy in Python or Lua, and each call then lands in
a different physics tick.

An iteration of five calls therefore costs five ticks, and the state the last write was
computed from is five ticks old.

The receive timeout is a server-wide setting, and the server spends it on every idle update
for every client.

.. _holding-a-tick:

Holding a Tick
--------------

``hold_tick`` holds the game on its current physics tick, and ``release_tick`` lets it move
on. Every call made in between is executed before the game advances. A whole read, compute and
write then happens in the state of one tick:

.. code-block:: python

   while True:
       conn.krpc.hold_tick()
       try:
           pitch = vessel.flight().pitch
           vessel.control.pitch = control_law(pitch)
       finally:
           conn.krpc.release_tick()

Release the tick even when the code in between fails, as the ``finally`` above does. The game
stays on the held tick until the hold is released or times out.

Consecutive Ticks
-----------------

That loop gets whole ticks, but not consecutive ones. The ``hold_tick`` opening an iteration
is itself a call the server has to be waiting for. A program that spends longer than the
receive timeout between releasing one tick and holding the next skips ticks, and cannot tell
which ones.

``next_tick`` closes that gap. It releases the tick being held and holds the one immediately
after it, in a single call. The game then runs no tick the loop has not held:

.. code-block:: python

   try:
       while True:
           conn.krpc.next_tick()
           pitch = vessel.flight().pitch
           vessel.control.pitch = control_law(pitch)
   finally:
       conn.krpc.release_tick()

The call returns once the next tick is held. The loop therefore runs at one iteration per
physics tick, and the game advances one tick per call. With no tick held, ``next_tick`` holds
the soonest one it can, and is the only call the loop needs to open with.

A Worked Example
----------------

Holding a vessel's pitch with a proportional controller, one iteration per tick:

.. code-block:: python

   import krpc

   conn = krpc.connect(name='pitch hold')
   vessel = conn.space_center.active_vessel
   target_pitch = 45

   try:
       for _ in range(500):
           conn.krpc.next_tick()
           flight = vessel.flight()
           error = target_pitch - flight.pitch
           vessel.control.pitch = max(-1, min(1, 0.05 * error))
   finally:
       conn.krpc.release_tick()
       vessel.control.pitch = 0

Every iteration reads the pitch of one tick and writes a command that acts on that same tick.
The 500 iterations cover 500 consecutive ticks, ten seconds of game time.

To actually fly a vessel, use the :ref:`autopilot <using-the-autopilot>`, which implements a
far better control law than the one above.

The AutoPilot
-------------

The autopilot's own control loop also runs once per physics tick. By default it runs after the
calls of that tick, so a target set inside a held tick is flown on that tick.

``AutoPilot.update_mode`` chooses where in the tick the loop runs. Its manual mode splits the
tick at ``update``: calls before it set what the loop flies the tick with, and calls after it
read what the loop did. See :ref:`when the control loop runs <autopilot-update-mode>`.

The Cost of a Hold
------------------

A hold costs real time. The game renders no frame, takes no input and runs no physics while a
tick is held. A hold of five milliseconds costs five milliseconds of the update it happens in,
and a hold of a hundred leaves the game running at about ten frames a second.

A loop using ``next_tick`` holds every tick, and the game runs at the rate of the loop for as
long as the loop runs. Physics runs on a fixed time step. A game whose ticks are held runs
slower than real time and simulates every tick in turn.

Keep the held section short, and keep anything that does not need the tick, such as logging or
plotting, outside it.

Limits of a Hold
----------------

* A hold ends by itself after the **tick hold timeout**, one second by default, and when the
  client disconnects. The timeout is a server setting, in the advanced section of the server
  window, as it bounds how long the server's owner has their game frozen.
* One client holds the tick at a time, and only a client making a call can hold it. A second
  client's attempt is refused, as is a hold attempted from a stream or an event.
* Streams do not update while a tick is held, as the server sends stream updates once it has
  finished executing calls. Read with calls inside a hold. A wait for a stream or an event
  there runs the hold out to its timeout.
* Calling a procedure that takes more than one tick to finish, such as staging or warping,
  ends the hold. Such a call can only finish in the tick the hold is holding back.
* A tick lost to the timeout is not reported. The next ``next_tick`` takes whatever tick it
  lands on, so a loop that has to know whether it kept up reads the game's own clock through
  ``SpaceCenter.ut``.
* A hold gains nothing while the game is paused, since physics is not advancing, and it blocks
  the pause menu for as long as it lasts.
