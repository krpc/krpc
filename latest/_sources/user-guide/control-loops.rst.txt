.. _control-loops:

Control Loops
=============

A control loop reads the game state, computes something from it and writes the result back,
over and over. The game runs its physics on a fixed step, 50 times a second. A loop that flies
a vessel needs to read and write once per step, acting on the state it read.

The Call Wait
-------------

The server executes calls inside the game's physics update. It measures each client's time
between calls, and waits for a client whose next call it expects soon. The **max call wait**
bounds that wait, 5 milliseconds by default.

A control loop spends its time between the calls: it reads, then computes, then writes.
Computing for longer than the max call wait is easy in Python or Lua, and each call then lands
in a different physics tick.

An iteration of five calls therefore costs five ticks, and the state the last write was
computed from is five ticks old.

The max call wait is a server-wide setting. A client that calls once a tick, or is idle, costs
the server no wait.

.. _holding-a-tick:

Holding a Tick
--------------

:meth:`KRPC.hold_tick` holds the game on its current physics tick, and :meth:`KRPC.release_tick`
lets it move on. Every call made in between is executed before the game advances. A whole read,
compute and write then happens in the state of one tick:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/control-loops/HoldTick.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Release the tick even when the code in between fails, as the example does. The game
stays on the held tick until the hold is released or times out.

Consecutive Ticks
-----------------

That loop gets whole ticks, but not consecutive ones. The :meth:`KRPC.hold_tick` opening an
iteration is itself a call the server has to be waiting for. A program that spends longer than
the max call wait between releasing one tick and holding the next skips ticks, and cannot tell
which ones.

:meth:`KRPC.next_tick` closes that gap. It releases the tick being held and holds the one
immediately after it, in a single call. The game then runs no tick the loop has not held:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/control-loops/NextTick.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

The call returns once the next tick is held. The loop therefore runs at one iteration per
physics tick, and the game advances one tick per call. With no tick held, :meth:`KRPC.next_tick`
holds the soonest one it can, and is the only call the loop needs to open with.

A Worked Example
----------------

Holding a vessel's pitch with a proportional controller, one iteration per tick:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/control-loops/PitchHold.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

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

Server Side Functions
---------------------

A :ref:`server side function <server-side-functions>` runs on the server, within a single
tick. One function reads the state, computes the control and writes it, all in that tick.
Python and JavaScript compile the function from code. A C# lambda cannot assign to a property,
so C# compiles the computation and builds the assignment with the :ref:`expression API
<server-side-functions-api>`. The other clients build the whole function with it:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/control-loops/CompiledStep.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Each iteration is one call, and runs in whichever tick the call lands on. The game can run ticks
between the iterations, so a loop that has to act on every tick still uses
:meth:`KRPC.next_tick`. A stream of the function runs it on every tick instead, with no call at
all. See :ref:`server-side-functions-control-loops`.

The Cost of a Hold
------------------

A hold costs real time. The game renders no frame, takes no input and runs no physics while a
tick is held. A hold of five milliseconds costs five milliseconds of the update it happens in,
and a hold of a hundred leaves the game running at about ten frames a second.

A loop using :meth:`KRPC.next_tick` holds every tick, and the game runs at the rate of the loop
for as long as the loop runs. Physics runs on a fixed time step. A game whose ticks are held
runs slower than real time and simulates every tick in turn.

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
* A tick lost to the timeout is not reported. The next :meth:`KRPC.next_tick` takes whatever
  tick it lands on, so a loop that has to know whether it kept up reads the game's own clock
  through ``SpaceCenter.ut``.
* A hold gains nothing while the game is paused, since physics is not advancing, and it blocks
  the pause menu for as long as it lasts.
