.. _user-guide-streams-and-events:

Streams and Events
==================

A script that follows the game reads the same values over and over. A *stream* has the server run
a call on every physics tick and send you the result when it changes. An *event* has the server
check a condition on every physics tick and tell you when it becomes true. Each client guide shows
the syntax, for example :ref:`streams <python-client-streams>` in Python. This guide
describes what the server does, which is the same for every client.

How Streams Update
------------------

The server updates streams once per physics tick, after it has run that tick's calls. So a stream
sees what the calls did. It evaluates every stream of every client in the same pass, so the values
in one update come from the same tick.

Each client receives at most one update message per tick, holding the streams whose value changed.
When no value changed, the server sends nothing.

Each stream is separate. Streaming the same call twice runs it twice, and two clients never share
a stream.

The first value of a new stream arrives in the tick the stream is created, just after the response
that creates it. A call that takes several ticks to finish, such as ``SpaceCenter.WarpTo``, gives
its stream no value until it finishes.

Limiting the Rate
-----------------

A stream's rate limits how many times a second the server updates it. The default, zero, updates
it on every tick. This example updates a stream ten times a second:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/streams-and-events/StreamRate.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/streams-and-events/StreamRate.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/streams-and-events/StreamRate.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      The C-nano client does not support streams.

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/streams-and-events/StreamRate.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      The Lua client does not support streams.

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/streams-and-events/StreamRate.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

The rate is a maximum, counted in real time rather than game time. A stream with a rate can
therefore be a tick or more older than the other streams.

The Cost of a Stream
--------------------

A stream runs its call on every tick it is due. The maximum time per update limits the time the
server spends on calls, but nothing limits the time it spends on streams. Many streams of expensive
calls slow every physics tick, and so the game. Adaptive rate control then gives calls less time
per tick.

Remove a stream once you no longer need it, or give it a rate. The :ref:`server performance guide
<server-performance>` describes the settings.

The server ends the connection of a client that falls too far behind in reading its updates, as
:ref:`the protocol describes <communication-protocol-unread-limit>`.

Events
------

An event's condition is a :ref:`server side function <server-side-functions>` that returns a
boolean. The server checks it on every physics tick, so the event catches a condition that holds
for a moment only, which a loop of calls can miss. This example waits for the vessel to climb above
10 km:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/streams-and-events/Event.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/streams-and-events/Event.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/streams-and-events/Event.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      The C-nano client does not support events.

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/streams-and-events/Event.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      The Lua client does not support events.

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/streams-and-events/Event.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

An event fires when its condition becomes true. It fires again each time the condition becomes
true after being false, so a condition that stays true fires once. On the server, an event is a
stream with a boolean value, and it is updated and removed in the same way.

How Streams End
---------------

A stream ends when its call raises an exception, when you remove it, or when your connection
closes. The client then raises the exception that ended it. Closing the connection removes all of
its streams.

Two exceptions pause a stream instead of ending it: ``KRPC.ObjectNotLoadedException``, for an object
that is not loaded, and ``KRPC.UnsupportedSceneException``, for a call that the current game scene
does not support. A paused stream keeps its last value, and its updates resume once the object or
the scene is available again. Loading a save or changing scene therefore pauses a stream of a
remote call. The :doc:`object lifetime guide </user-guide/object-lifetime>` describes when an object
is not loaded.

A stream of a server side function, and an event, end on these exceptions instead of pausing.
Create them again after the load.

An event that a service provides, rather than one you create, can also end when the service
removes it.

Ticks and Pauses
----------------

Streams do not update while a client :ref:`holds the tick <holding-a-tick>`. The
server sends their updates once the hold ends, so waiting for a stream or an event inside a hold
waits until the hold times out.

While the game is paused, the server keeps running calls and updating streams. The *Pause the
server when the game pauses* setting stops both until the game resumes.
