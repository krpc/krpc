.. _tutorial-control-loops:

Control Loops
=============

A control loop reads the game state, computes something from it and writes the result back,
over and over. The game runs its physics on a fixed step, 50 times a second, and a loop that
flies a vessel wants to do its reading and writing once per step, with the values it writes
acting on the state it read them from.

That is not what a plain loop gets. This tutorial explains why, and what to do about it.

Why a plain loop falls behind
-----------------------------

The server executes calls inside the game's physics update, and waits only a short while for
a client's next call before giving up on it and letting the game move on. That wait is the
**receive timeout**, one millisecond by default.

A control loop spends its time between the calls, not during them: it reads, then computes,
then writes. If the computing takes longer than a millisecond, which is easy in Python or
Lua, the client is not waiting when the server looks for it, so each call lands in a
different physics tick. An iteration of five calls costs five ticks, and by the time the last
write lands the state it was computed from is five ticks old.

Raising the receive timeout is not the answer. It is a global setting, and it costs the
server that much time on every idle update for every client.

.. _holding-a-tick:

Holding a tick
--------------

``hold_tick`` holds the game on its current physics tick, and ``release_tick`` lets it move
on. Every call made in between is executed before the game advances, so a whole read, compute
and write happens in the state of one tick:

.. code-block:: python

   while True:
       conn.krpc.hold_tick()
       try:
           pitch = vessel.flight().pitch
           vessel.control.pitch = control_law(pitch)
       finally:
           conn.krpc.release_tick()

Release the tick even when the code in between fails, as the ``finally`` above does. Until
the hold is released the game is waiting, so a program that leaves one behind stops the game
until the hold times out.

What it costs
-------------

Real time. The game renders no frame, takes no input and runs no physics while a tick is
held, so a hold of five milliseconds costs five milliseconds of every update it happens in,
and a hold of a hundred leaves the game running at about ten frames a second.

What it does not cost is the simulation. Physics runs on a fixed time step, so a game whose
ticks are held runs slower than real time rather than differently. Nothing is skipped and
nothing is caught up afterwards.

For a control loop that is usually the right trade. Keep the held section short, and keep
anything that does not need the tick, such as logging or plotting, outside it.

Rules worth knowing
-------------------

* A hold ends by itself after the **tick hold timeout**, one second by default, and when the
  client disconnects. The timeout is a server setting, in the advanced section of the server
  window, because it bounds how long the server's owner has their game frozen.
* Only one client can hold the tick at a time, and only a client making a call can hold it. A
  second client's attempt is refused, as is a hold attempted from a stream or an event.
* Streams do not update while a tick is held, because the server sends stream updates after
  it has finished executing calls. Read with calls inside a hold, and never wait for a stream
  or an event there: it cannot arrive until the hold has ended, so the wait runs out the
  timeout.
* Calling a procedure that takes more than one tick to finish, such as staging or warping,
  ends the hold. Such a call can only finish in the tick the hold is holding back.
* A hold gains nothing while the game is paused, since physics is not advancing anyway, and
  it blocks the pause menu for as long as it lasts.
