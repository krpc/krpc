.. _internals:

Internals of kRPC
=================

.. _server-performance-settings:

Server Performance Settings
---------------------------

.. figure:: /images/getting-started/server-window-advanced.png
   :align: right

   Server window showing the advanced settings.

kRPC processes its queue of remote procedures when its FixedUpdate method is
invoked. This is called every fixed framerate frame, typically about 60 times a
second. If kRPC were to only execute one RPC per FixedUpdate, it would only be
able to execute at most 60 RPCs per second. In order to achieve a higher RPC
throughput, it can execute multiple RPCs per FixedUpdate. However, if it is
allowed to process too many RPCs per FixedUpdate, the game's framerate would be
adversely affected. The following settings control this behavior, and the
resulting tradeoff between RPC throughput and game FPS:

1. **One RPC per update**. When this is enabled, the server will execute at most
   one RPC per client per update. This will have minimal impact on the games
   framerate, while still allowing kRPC to execute RPCs. If you don't need a
   high RPC throughput, this is a good option to use.

2. **Maximum time per update**. When one RPC per update is not enabled, this
   setting controls the maximum amount of time (in microseconds) that kRPC will
   spend executing RPCs per FixedUpdate.  Setting this to a high value, for
   example 20000 us, will allow the server to process many RPCs at the expense
   of the game's framerate. A low value, for example 1000 us, won't allow the
   server to execute many RPCs per update, but will allow the game to run at a
   much higher framerate.

3. **Adaptive rate control**. When enabled, kRPC will automatically adjust the
   maximum time per update parameter, so that the game has a minimum framerate
   of 60 FPS. Enabling this setting provides a good tradeoff between RPC
   throughput and the game framerate.

Another consideration is the responsiveness of the server. Clients must execute
RPCs in sequence, one after another, and there is usually a (short) delay
between them. This means that when the server finishes executing an RPC, if it
were to immediately check for a new RPC it will not find any and will return
from the FixedUpdate. This means that any new RPCs will have to wait until the
next FixedUpdate, and results in the server only executing a single RPC per
FixedUpdate regardless of the maximum time per update setting.

Instead, higher RPC throughput can be obtained if the server waits briefly after
finishing an RPC to see if any new RPCs are received. This is done in such a way
that the maximum time per update setting (above) is still observed.

This behavior is enabled by the **blocking receives** option. **Receive
timeout** sets the maximum amount of time the server will wait for a new RPC
from a client.

The receive timeout decides how much work a client may do between calls before
it stops being waited for. A client that takes longer than the timeout to send
its next call is not waiting when the server looks, so the server returns from
the FixedUpdate and that call is executed in the next one: the client is served
one RPC per update, however many it makes. The default timeout is one
millisecond, which is easy for a program written in Python or Lua to exceed.

Holding a tick
--------------

A control loop reads the game state, computes with it and writes the result
back, and the computing happens between the calls. Once it takes longer than the
receive timeout, every call in an iteration costs a whole update: an iteration of
five calls takes five of them, and the values written were computed from a state
five updates old.

The ``HoldTick`` procedure holds the game on its current physics tick, and
``ReleaseTick`` lets it move on. Every call made in between is executed
before the game advances, so the reads, the computing and the writes all happen
in the state of a single tick:

.. code-block:: python

   while True:
       conn.krpc.hold_tick()
       try:
           pitch = vessel.flight().pitch
           vessel.control.pitch = control_law(pitch)
       finally:
           conn.krpc.release_tick()

Releasing the tick even when the code in between fails, as the ``finally`` above
does, matters: until the hold is released the game is waiting, and a program that
leaves one behind stops the game until the hold times out.

What this costs is real time. The game renders no frame, takes no input and runs
no physics while the tick is held, so a hold of five milliseconds costs five
milliseconds of every update it happens in, and a hold of a hundred leaves the
game running at about ten frames a second. What it does not cost is the
simulation: physics runs on a fixed time step, so a game whose ticks are held
simply runs slower than real time rather than differently. For a control loop
that is usually the right trade, and for anything else it is not.

The rest of the behavior worth knowing:

* **Tick hold timeout** sets how long a client may hold a tick before the server
  takes it back, one second by default. A hold also ends if the client
  disconnects.
* Only one client can hold the tick at a time, and only a client making a call
  can hold it. Both a second client's attempt and a hold attempted from a stream
  or an event are refused.
* The maximum time per update and one RPC per update settings do not apply while
  a tick is held, and an update that held one is not counted by adaptive rate
  control.
* Streams do not update while a tick is held, since the server sends stream
  updates after it has finished executing calls. Read with calls inside a hold
  rather than from streams, and do not wait for a stream or an event, which
  cannot arrive until the hold has ended.
* Calling a procedure that takes more than one tick to finish, such as staging
  or warping, ends the hold: such a call can only finish in the tick that the
  hold is holding back.
