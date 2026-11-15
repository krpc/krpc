.. currentmodule:: KRPC

.. _server-side-functions:

Server Side Functions
=====================

A server side function is code that runs inside the game, on the server. It has three main
uses:

* **Custom events** wait for a condition to become true. The server checks the condition every
  physics tick, and sends your program a message when it fires. Polling from your program adds
  a round trip to every check, and can miss a condition that holds for only a few ticks.
* **Function streams** stream the result of a computation over several RPC values. The server
  reads every value in the same physics tick, so the values are consistent. It sends the result
  to your program when the result changes.
* **Run-once functions** run a sequence of calls and computation in a single physics tick, from
  a single RPC. They can set properties and trigger actions. The same calls made from your
  program take a round trip each, and the game advances between them.

This guide is in two parts. :ref:`Using Server Side Functions <server-side-functions-compiled>`
covers functions written in your own language and compiled by the client, and is all most users
need. :ref:`The Expression API <server-side-functions-api>` covers the expression trees that
compiled code translates into, which every client can build directly.

Every client supports ``Run``. Support for the other features differs:

.. list-table::
   :header-rows: 1

   * - Client
     - Compiled functions
     - Events and function streams
   * - Python
     - ✓
     - ✓
   * - C#
     - ✓
     - ✓
   * - C++
     - ✗
     - ✓
   * - C-nano
     - ✗
     - ✗
   * - Java
     - ✗
     - ✓
   * - Lua
     - ✗
     - ✗
   * - JavaScript
     - ✓
     - ✓

.. _server-side-functions-compiled:

Using Server Side Functions
---------------------------

A client with a compiler turns a function or lambda into a server side function. It is the
simplest way to write one. The constructs a compiler supports depend on the language. Without a compiler, the :ref:`expression API <server-side-functions-api>` defines a
server side function directly.

A First Custom Event
^^^^^^^^^^^^^^^^^^^^

This event fires when the vessel's altitude exceeds 1000 meters:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeEvent.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeEvent.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeEvent.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

The client compiles the lambda into a server side function. The mean altitude call becomes an
embedded RPC, re-invoked on each check. ``1000`` becomes a constant.

The server checks the condition every physics tick until it is true. It then sends your program
a single message, which ends the wait. Your program makes no polling calls while it
waits, so it uses no round trips and no CPU time.

Remote Calls and Captured Values
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler splits a function by what each part depends on:

* Reading a property or calling a method of a remote object or service becomes part of the
  function. The server re-invokes it on every evaluation. So does any operation on its result.
* A sub-expression that uses only client side values, such as captured variables and literals,
  is evaluated once, when the function is compiled. That includes arithmetic and library calls
  on those values. The result becomes a constant.

For example:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/CapturedValue.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/CapturedValue.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/CapturedValue.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

The client computes ``threshold * 2`` when it creates the event, so the function compares the
apoapsis with the constant 100000. To use a new value of ``threshold``, compile a new function
and remove the old event.

Computed Streams
^^^^^^^^^^^^^^^^

A stream created from a function carries the function's result. The server runs the
computation on each stream update:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeStream.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeStream.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeStream.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

A function can produce a collection. This streams the thrust of every engine on the vessel,
read in a single physics tick per update:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/EngineThrusts.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/EngineThrusts.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/EngineThrusts.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Here ``engines`` is a captured variable, so the list of engines is fixed when the function is
compiled. The server re-reads only their thrusts. Read the engine list inside the function to
have the server fetch the list on each update. That follows staging, at the cost of extra work
per update.

Collections
^^^^^^^^^^^

The language's collection operations, such as mapping, filtering, counting, summing and
sorting, compile to the server's collection operations. This event fires when any engine on the
vessel runs out of fuel. The server re-reads the engine list on each check, so the event covers
engines that staging reveals:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/OutOfFuelEvent.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/OutOfFuelEvent.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/OutOfFuelEvent.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Strings
^^^^^^^

The language's string operations, such as length, indexing, searching, splitting and case
conversion, compile to the server's string operations. This streams the vessel's name in upper
case:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/UpperCaseName.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/UpperCaseName.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/UpperCaseName.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Two rules apply to strings:

* **A character is a string of length one.** The protocol carries no character type, so
  indexing a string produces a one character string, which works with every other string
  operation. In a language where indexing a string produces a character, that index is an
  unsupported construct. Take a substring instead.
* **Case conversion and comparison are the same in every locale.** Case is converted, and
  strings are compared, by ordinal value. A function produces the same result whatever language
  the game runs in.

Statements, Loops and Local Variables
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A compiled function can be a sequence of statements, like any other function in your program.
This one computes the fraction of liquid fuel left:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/FuelFraction.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/FuelFraction.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/FuelFraction.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

In outline, a compiled function supports these statements:

* If statements, with else branches.
* While and for-each loops, with break and continue.
* Early returns.
* Local variables, which can be reassigned and changed in place. A loop can build up a
  collection in one.

The exact set depends on the language. Each client guide lists what its compiler supports:
:ref:`C# <csharp-client-compiling-lambdas>`, :ref:`JavaScript <js-client-compiling-functions>` and
:ref:`Python <python-client-compiling-functions>`.

A collection a call returns is a copy, as it is in your program. Changing the copy in a function
leaves the game's own collection unchanged.

This function collects a rating for each active engine:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnRatings.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnRatings.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnRatings.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

A variable keeps one type for the whole function. How it gets that type depends on the language:

.. tabs::

   .. group-tab:: Python

      A variable takes its type from its first assignment. A type annotation sets the type, and
      each value assigned then widens to it, as an integer does to a float:

      .. literalinclude:: /scripts/user-guide/server-side-functions/TotalThrust.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

      An empty collection takes its element type the same way, as in ``ratings: list[str] = []``
      above.

   .. group-tab:: C#

      A C# lambda holds a single expression, so it has no local variables. Each value has its C#
      type.

   .. group-tab:: JavaScript

      A number is always a double, so a running total needs no type:

      .. literalinclude:: /scripts/user-guide/server-side-functions/TotalThrust.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

      An empty array, Set or Map takes its type from a later use, such as a ``push``. With no
      such use, give one a type with ``cast``, as the :ref:`client guide
      <js-client-compiling-functions>` describes.

Side Effects and Run-Once Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A compiled function can also change the game. It can assign to the properties of remote objects and
services, and call methods for their effects. Pass such a function to ``Run``, which evaluates it
once, with every step in the same physics tick:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/RunOnce.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/RunOnce.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/RunOnce.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

.. note::

   An event or stream runs its function on every stream update, effects included. See
   :ref:`server-side-functions-control-loops`.

.. note::

   A procedure that runs over several ticks, such as ``SpaceCenter.WarpTo``, is an error in a
   function. See :ref:`server-side-functions-errors`.

.. _server-side-functions-arguments:

Function Arguments
^^^^^^^^^^^^^^^^^^

A compiled function can take parameters, and ``Run`` passes it a value for each. One function then
serves many inputs. This computes how long a burn takes, for several changes in velocity:

.. tabs::

   .. group-tab:: Python

      The function is a ``def`` with a type annotation on each parameter. Its arguments bind as
      Python binds them, with defaults and keyword arguments:

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnTime.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      The function is a lambda with typed parameters:

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnTime.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      The parameters follow the captures, and ``compile`` takes their types:

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnTime.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

An event or a stream runs its function with no arguments.

State Between Updates
^^^^^^^^^^^^^^^^^^^^^

A function can keep a value from one evaluation to the next, such as a running total. This stream
counts the stream updates in which the engines are firing:

.. tabs::

   .. group-tab:: Python

      Declare a captured variable ``nonlocal``:

      .. literalinclude:: /scripts/user-guide/server-side-functions/FiringCounter.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

      The variable takes its type from its value when the function is compiled. To give it
      another type, such as a list that starts empty, start the function with a cast of it:
      ``history = cast(List[float], history)``.

   .. group-tab:: C#

      A C# lambda is an expression tree, which cannot assign to a variable. Build the function
      with the :ref:`expression API <server-side-functions-api>`, and create the variable with
      :meth:`Expression.state_variable`.

   .. group-tab:: JavaScript

      Assign a property of a captured object:

      .. literalinclude:: /scripts/user-guide/server-side-functions/FiringCounter.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

      The property takes its type from its value when the function is compiled. A property that
      starts empty takes its type from the first use that adds to it, or from a cast such as
      ``t.list(t.double).cast(state.history)``.

The state lives on the server. It starts with the variable's value at compile time, and changes
to it do not reach the variable in the client program.

A function passed straight to ``Run`` or a stream is compiled on each call, so each call gets its
own state. A function compiled once with ``compile`` keeps one state across every run, stream and
event that uses it. Two streams of it advance the state twice per update.

.. _server-side-functions-control-loops:

Control Loops on the Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A stream of a function with no value runs the function on every stream update, for its effects.
Such a stream can run a control loop in the game, with no round trip to the client. This one
throttles back as the vertical speed nears 100 m/s:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrottleLimit.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      A C# lambda is an expression tree, which cannot assign to a property. Build the function
      with the :ref:`expression API <server-side-functions-api>`.

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrottleLimit.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

The stream's value is the client's null value. The server sends it once, after the first run, so
a callback is called once. An error from the function ends the stream and stops the loop. Set the
stream's rate to run the function less often.

.. _server-side-functions-exceptions:

Exceptions
^^^^^^^^^^

A function can raise an exception. It reaches your program as an error when run, or as an error
on the stream or event evaluating the function:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/CheckFuel.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/CheckFuel.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/CheckFuel.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Your program catches the error as an exception, as it does for an error from any other call.

A function can also handle an exception, with the language's try statement:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchModes.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchModes.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchModes.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

These rules apply to handlers:

* **Only the message of an exception is exposed.** The variable a handler binds is a string
  holding the message. In JavaScript, the handler reads it as ``e.message``.
* **A handler also catches exceptions from the calls in the function.** A handler for an
  exception catches it whether the function raises it, or a remote procedure the function calls
  does.
* **A procedure that pauses execution passes through every handler**, including a catch-all.
  It is reported as an error.
* **Some errors are caught only by a catch-all handler**, as they have no exception class to name.
  These are a call to a procedure that is unavailable in the current game scene, and a procedure
  that returns null when its return type does not allow it. Uncaught, each reaches your program
  as a plain error.

The Standard Library
^^^^^^^^^^^^^^^^^^^^

The ``StdLib`` service is a standard library for server side functions, with mathematics,
vector and quaternion operations, and printing. Calls to the language's math library compile to the matching ``StdLib`` procedures when their arguments are computed on
the server. Call the service directly for the vector and quaternion operations. This streams the
angle between the vessel's facing direction and its velocity relative to the surface:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/SurfaceAngle.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/SurfaceAngle.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/SurfaceAngle.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

The service covers:

* Scalar functions, such as square roots, trigonometry and clamping, and the constants pi and e.
* Vector operations on the position and direction tuples of the ``SpaceCenter`` service, such
  as dot and cross products, magnitudes, angles and interpolation.
* Quaternion operations on its rotation tuples, such as composition, inverses, rotating vectors
  and interpolation.

Each midpoint rounding rule has its own procedure, so a function can round the way its language
does:

* :meth:`StdLib.round` rounds a halfway value to the nearer even number, as Python and C# do.
* :meth:`StdLib.round_half_away_from_zero` rounds it away from zero, as C and C++ do.
* :meth:`StdLib.round_half_up` rounds it toward positive infinity, as Java and JavaScript do.

Each takes the number of decimal places, negative for tens and hundreds.

Printing
^^^^^^^^

The language's print function compiles to :meth:`StdLib.print`, which writes a line to the
game's log, ``KSP.log``. This logs the vessel's mass from the server:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/PrintMass.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/PrintMass.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/PrintMass.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

A print in an event or stream function writes a line on every stream update.

Supported Constructs
^^^^^^^^^^^^^^^^^^^^

Each client guide lists the constructs its compiler supports, and where their meaning differs
from the language's: :ref:`C# <csharp-client-compiling-lambdas>`, :ref:`JavaScript
<js-client-compiling-functions>` and :ref:`Python <python-client-compiling-functions>`. Any other
construct is a compilation error that names it.

.. _server-side-functions-reuse:

Reusing Functions
^^^^^^^^^^^^^^^^^

Passing a new lambda to run in a loop builds a new function on each pass. Each pass reads the
lambda's captured variables again, while a compiled function keeps the values from when it was
compiled.

In such cases, compile the function once, and run the compiled function as often as needed. To
run it with different values, give it parameters, as in :ref:`server-side-functions-arguments`:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/CompiledStep.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/CompiledStep.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/CompiledStep.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

A large lambda passed on every call can make the game stutter. The server compiles each one to
native code on the game's main thread. Compile a function before it is needed, such as before
launch, and every run is fast.

.. _server-side-functions-errors:

Errors, Edge Cases and Performance
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

**Errors.** An error raised while evaluating a stream or event function reaches your program
when it reads the stream or event. An example is an embedded call reaching a destroyed part.

The server then removes the stream, as it does for any stream whose result is an error. Build a
new event or stream once the cause clears.

**Loads and unloaded vessels.** A stream of a remote procedure pauses while its object is not
loaded, and resumes after. A function stream or event ends instead, with
``KRPC.ObjectNotLoadedException``. Examples are a part of an unloaded vessel, and a vessel part
way through a load. To keep one running, catch the exception in the function:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedCheck.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.js
         :language: javascript

The handler gives ``False``, so the event does not fire while the vessel loads.

**Procedures that pause.** Some procedures pause execution and resume on a later tick, such as
``SpaceCenter.WarpTo`` and ``SpaceCenter.LaunchVessel``. Calling one is an error in any function.
The procedure keeps its effects up to the pause, so a ``WarpTo`` called this way leaves the game
warping.

.. note::

   A future release is planned to support these procedures in server side functions.

**Loops run to completion.** A ``while`` loop runs to its end within a single tick, before the
game continues. A loop that never ends freezes the game for good. The only way out is to force
quit the game, which loses any progress since the last save. No compiler check can catch such a
loop, so bound every loop by a value that is certain to change.

.. note::

   A future release is planned to stop a loop that runs for too long, with a time limit set in
   the server settings.

**Null values.** A null where the function needs a value raises a ``NullReferenceException``
when the function runs. Examples are a null operand in arithmetic, and a null argument to a call.
Your program receives it as the closest built-in exception of its language.

Some procedures return null, such as ``Part.engine`` for a part that is not an engine. Compare
the result with null before using it.

A function can produce a null. A compiled null takes its type from where it is used, such as the
other assignment to a variable or the other branch of a conditional:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/HeaviestEngine.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/HeaviestEngine.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/HeaviestEngine.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Elsewhere, give a null its type with a type annotation or a cast.

**Costs.** A function is evaluated on the server as native code, and only its result crosses
the network. Even a large function evaluates quickly. A function stream can run a full autopilot
control loop every physics tick, as in :ref:`server-side-functions-control-loops`.

.. _server-side-functions-api:

The Expression API
------------------

The expression API builds a server side function node by node. The compilers use it under the
hood.

.. note::

   The C-nano and Lua clients have no events or streams, so their examples run each function
   once, with ``krpc_run_function`` in C and ``conn:run`` in Lua.

Expression Trees
^^^^^^^^^^^^^^^^

A server side function is a tree of *expression objects* held on the server, built with the
static methods of the :class:`Expression` class. The server compiles the tree to native code, so
evaluations are fast.

Each node is a constant, an embedded remote procedure call, an operator, a statement or a nested
function. Statements are assignments, loops and if statements.

You use a tree in one of three ways:

* An event, from a function that evaluates to a boolean. The server evaluates the function on
  each stream update, and triggers the event when it returns true.
* A stream, from a function whose value can be sent to a client, such as a number, a string, a
  collection or an object. The value is recomputed on each stream update.
* A run, which evaluates a function once, within a single physics tick, and returns its value.
  Useful for a function with side effects.

Two rules hold for every server side function:

* **Embedded remote procedure calls are re-invoked on every evaluation.** Constants, and the
  values your program supplies when building the function, are fixed when the function is
  created.
* **The whole function is evaluated within a single physics tick.** A function that reads the
  fuel level of every engine reads them all in the same tick.

Expression Objects
^^^^^^^^^^^^^^^^^^

The altitude event from the start of this guide has three nodes: an embedded call, a constant
and a comparison:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/python/Event.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/csharp/Event.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/cpp/Event.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/Event.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/java/CustomEvent.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/AltitudeCheck.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/CustomEvent.js
         :language: javascript

Two kinds of object appear here:

* A **procedure call message** describes an RPC without invoking it. Each client builds one
  from a call written in its own language, as the examples show. Building a call message is a
  local operation.
* An **expression object** is a node of the tree. :meth:`Expression.call` embeds a procedure
  call message, which the server invokes on each evaluation. A procedure with no return value
  embeds as a statement.

A call message carries its arguments, as the call for a stream does. A call template carries none,
for :meth:`Expression.call_with_arguments` to give each one as an expression. See
:ref:`server-side-functions-collections`.

Each factory method is an RPC that creates one node on the server and returns a handle to it.

The factory methods are static methods of the :class:`Expression` class, and each needs a
connection to send its RPC on:

.. tabs::

   .. group-tab:: Python

      Reach the class through the connection, as ``conn.krpc.Expression``. Its methods use that
      connection.

   .. group-tab:: C#

      ``Expression.Bind(connection)`` returns the class bound to a connection. Its methods pass
      the connection to each static method, so ``e.ConstantDouble(1000)`` needs no connection
      argument.

   .. group-tab:: C++

      ``krpc::services::KRPC::Expression::bind(conn)`` returns the class bound to a connection.
      Its methods pass the connection to each static method, so ``e.constant_double(1000)``
      needs no connection argument.

   .. group-tab:: C

      Each factory method is a function that takes the connection first and the result second,
      such as ``krpc_KRPC_Expression_ConstantDouble(conn, &limit, 1000)``.

   .. group-tab:: Java

      ``Expression.bind(connection)`` returns an ``Expression.Bound`` for a connection. Its
      methods pass the connection to each static method, so ``e.constantDouble(1000)`` needs no
      connection argument.

   .. group-tab:: Lua

      Reach the class through the connection, as ``conn.krpc.Expression``. Its functions use that
      connection.

   .. group-tab:: JavaScript

      Reach the class through the connection, as ``conn.krpc.Expression``. Each factory method
      returns a ``Promise``, so await each node before passing it to the next.

Operators and Types
^^^^^^^^^^^^^^^^^^^

This truncates the altitude to a whole number of meters:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

Expressions are statically typed, with the types of the server's procedures. Numbers of
different types convert to a common type, so an integer multiplies a double directly.

The same conversion applies to a procedure's arguments, a collection's elements, a structure's
fields and an assigned variable, and each conversion must widen. :meth:`Expression.cast`
converts a value explicitly, for example to narrow a double to an integer.

The :class:`Type` class names types, including the classes, enumerations and structures a
service defines. Each type of constant has its own factory method, such as
:meth:`Expression.constant_double`.

A class, an enumeration, a structure or an exception is named by its service and its name, such
as ``"SpaceCenter", "Engine"``. Each procedure that takes the names has a variant that takes the
ids :meth:`KRPC.get_services` reports, such as :meth:`Type.class_type_by_id`. The C examples
use them.

Each operator has a factory method, such as :meth:`Expression.add`.

Function Streams
^^^^^^^^^^^^^^^^

A function whose value can be sent to a client can be streamed. A function with no value can be
streamed too, and runs for its effects, as in :ref:`server-side-functions-control-loops`:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/cpp/FunctionStream.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/java/FunctionStream.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.js
         :language: javascript

Some clients take the type of the stream's values as a type argument, and others read the
function's return type from the server. A function stream behaves like any other stream, and its
rate can be limited in the usual way.

.. _server-side-functions-collections:

Functions and Collections
^^^^^^^^^^^^^^^^^^^^^^^^^

This builds the "any engine out of fuel" event from earlier. The call names the procedure through
the ``Engine`` class, with no engine:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/OutOfFuelCheck.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.js
         :language: javascript

A function passed to a collection operation is built from two pieces:

* :meth:`Expression.parameter` creates a named, typed parameter. The type can be a class, so a
  parameter can be an ``Engine``.
* :meth:`Expression.lambda_` combines a list of parameters and a body into a function. In the
  body, the parameter expressions stand for the arguments.

To call an RPC on each element, take the call's instance from the function's parameter.
:meth:`Expression.call_with_arguments` embeds a call with its arguments given as expressions.
Each expression is keyed by the position of its parameter, and position 0 is the instance.

The call is a **call template**, which names the procedure through its class and carries no
arguments. In Python, Lua and JavaScript, the member itself stands for its call template, as
``has_fuel`` does in the Python example. A position without an expression takes the parameter's
default. A call that carries arguments, such as one built from an engine, raises an error.

:meth:`Expression.invoke` calls a function directly, binding values to its parameters by name.

A function from :meth:`Expression.lambda_` can also be run on its own. ``Run`` passes its arguments
to the function's parameters in order, and :attr:`Expression.parameter_types` gives their types.

:meth:`Expression.select` applies a function to every element, and :meth:`Expression.where`
filters by a predicate. Both produce lazy sequences, which a client cannot decode. Convert one
with :meth:`Expression.to_list` or :meth:`Expression.to_set` before streaming it. The
aggregations, such as :meth:`Expression.sum` and :meth:`Expression.any`, take lazy sequences
directly.

The :class:`Expression` class has the other usual collection operations, such as ordering,
grouping and set operations, and a constructor for each kind of collection. A function can change
a collection in place, with operations such as :meth:`Expression.append` and
:meth:`Expression.remove`.

**A dictionary is iterated through its keys or its values.** :meth:`Expression.dictionary_keys`
and :meth:`Expression.dictionary_values` each produce a list. Passing a dictionary to a
collection operation is an error that points at them. :meth:`Expression.contains_key` tests for
a key. :meth:`Expression.get` of a missing key raises a ``KeyNotFoundException``, and of a list
index out of range an ``IndexOutOfRangeException``.

**The index into a tuple must be a constant.** The elements of a tuple differ in type, and
:meth:`Expression.get` takes its result type from the index. A list or a dictionary takes any
index expression.

Structures
^^^^^^^^^^

A structure a service defines is a value like any other. A function can return, stream, pass
and build one. :meth:`Expression.get_field` reads a field by name:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

:meth:`Expression.create_struct` builds a structure from its type and its field values, in the
order the structure declares them. A field's name is the one the service declares, as shown in
the API documentation. :meth:`Expression.get_field_by_index` reads a field by its position in
that order, counting from 0.

The compilers produce both nodes from ordinary syntax. Reading a field of a structure compiles
to :meth:`Expression.get_field`, and constructing one compiles to
:meth:`Expression.create_struct`.

Statements, Variables and Loops
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This builds and runs ``total = 0; for x in [1, 2, 3]: total += x``, which returns 6:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.js
         :language: javascript

Statements are ordinary expression nodes. :meth:`Expression.block_with_variables` sequences
statements and declares local variables, and its value is the value of its last statement.

The other statements, such as assignments, if statements, loops and early returns, each have a
factory method. A call to a procedure without a return value is a statement, run for its
effects.

Match and Switch Statements
^^^^^^^^^^^^^^^^^^^^^^^^^^^

This loop adds 10 to a total for the values 1 and 2, and 1 for any other value. It returns 21:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/SwitchStatement.js
         :language: javascript

:meth:`Expression.match` takes the value, the test values of each case and the body of each
case. It runs the body of the first case with a test value equal to the value, as
:meth:`Expression.equal` compares them. The test values are evaluated in order, up to the first
match. A case with no test values is the default. A :meth:`Expression.break_` in a body ends the
enclosing loop, as in Python's ``match``. The Python client compiles the cases of a ``match``
statement that have no guard to it.

:meth:`Expression.switch` takes the same arguments, and behaves as ``switch`` in C, C++, Java and
JavaScript. It runs the bodies from the matching case on, and a :meth:`Expression.break_` in a
body ends the switch.

A ``switch`` in C, C++, C# or Java becomes a :meth:`Expression.switch` with its breaks kept. When
each case ends in a break, as in the example, it can also become a :meth:`Expression.match` with
the breaks dropped.

Object Constants
^^^^^^^^^^^^^^^^

:meth:`Expression.constant_object` embeds a reference to an object, such as a vessel, a reference
frame or an engine. A function can compare it for equality, or pass it to a call built with
:meth:`Expression.call_with_arguments`. This event fires when the vessel's target changes to a
particular vessel:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetCheck.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

:meth:`Expression.constant_object` takes the object's identifier in the communication protocol.
Each client has a function that returns it: ``krpc.get_object_id`` in Python and Lua,
``RemoteObject.GetObjectId`` in C#, ``krpc::get_object_id`` in C++, ``RemoteObject.getObjectId``
in Java and ``getObjectId`` in JavaScript. In C, an object's handle is its identifier. The
compilers embed a captured remote object this way.

Exceptions in the Expression API
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

:meth:`Expression.throw` raises an exception, named by its service and class. This builds the
fuel check from :ref:`Exceptions <server-side-functions-exceptions>`:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

:meth:`Expression.try_catch`, :meth:`Expression.try_catch_all` and :meth:`Expression.try_finally`
handle exceptions.

Extensibility
^^^^^^^^^^^^^

A server side function can call any remote procedure, including those of services that other
mods add. The one exception is the ``KRPC`` service, which builds and runs the functions.

To give functions a new capability, :ref:`write a service <extending>`. Its procedures are
available to server side functions on every client.
