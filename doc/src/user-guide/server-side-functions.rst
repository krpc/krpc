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

.. note::

   Compiled functions need the C# or Python client. Events, function streams and ``Run`` need the
   C#, C++, Java or Python client.

.. _server-side-functions-compiled:

Using Server Side Functions
---------------------------

A client with a compiler turns a function or lambda into a server side function. It is the
simplest way to write one. The constructs a compiler supports depend on the language. A
language whose lambdas hold a single expression builds statements and exception handlers with
the :ref:`expression API <server-side-functions-api>`.

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

The client compiles the lambda into a server side function. The mean altitude call becomes an
embedded RPC, re-invoked on each check. ``1000`` becomes a constant.

Remote Calls and Captured Values
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler splits a function by what each part depends on:

* Reading a property or calling a method of a remote object or service becomes part of the
  function. The server re-invokes it on every evaluation. So does any operation on its result,
  such as arithmetic or a square root.
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

The client computes ``threshold * 2`` when it creates the event, so the function compares the
apoapsis with the constant 100000. To use a new value of ``threshold``, compile a new function
and remove the old event.

A chain of calls is embedded whole. Both the orbit and the apoapsis altitude calls are part of
the function, so it follows the vessel's current orbit each tick.

Computed Streams
^^^^^^^^^^^^^^^^

Pass a function when creating a stream to stream its computed value. The server runs the
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

Statements, Loops and Local Variables
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A compiled function can contain statements before its return. This computes the fraction of
liquid fuel left:

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

A compiled function supports these statements:

* If statements, with else branches.
* While and for-each loops, with break and continue.
* Early returns.
* Local variables, which can be reassigned and changed in place. A loop can build up a
  collection in one.

A collection a call returns is a copy, as it is in your program. Changing the copy in a function
leaves the game's own collection unchanged.

A variable takes its type from its first assignment. Give an empty collection its element type
with a type annotation:

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

A counter that starts as an integer and later holds a fraction takes an annotation too, such as
``total: float = 0``.

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

The game holds still between the steps, and the whole function costs one round trip. A function
that returns no value gives the client's null value, or runs through a ``void`` overload where
the client has one.

.. note::

   An event or stream re-runs its function on every stream update, effects included. Keep
   event and stream functions free of side effects.

.. _server-side-functions-arguments:

Function Arguments
^^^^^^^^^^^^^^^^^^

A compiled function can take parameters, and ``Run`` passes it a value for each. One function then
serves many inputs. This computes how long a burn takes, for several changes in velocity:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnTime.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/BurnTime.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

In Python, the function is a ``def`` with a type annotation on each parameter. Its arguments bind
as Python binds them, with defaults and keyword arguments. In C#, the function is a lambda with
typed parameters. An event or a stream runs its function with no arguments.

State Between Updates
^^^^^^^^^^^^^^^^^^^^^

A function can keep a value from one evaluation to the next, such as a running total. In
Python, declare a captured variable ``nonlocal``. This stream counts the stream updates in
which the engines are firing:

.. literalinclude:: /scripts/user-guide/server-side-functions/FiringCounter.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

The variable starts with the value it holds when the function is compiled, and takes its type
from that value. To give it another type, such as a list that starts empty, start the function
with a cast of it: ``history = cast(List[float], history)``.

The server keeps the value apart from the variable on the client. A compiled function keeps its
state across every run, stream and event. The state belongs to the function, so two streams of one
compiled function advance it twice per update. Compile the function twice for separate states.

A function passed straight to ``Run`` or a stream starts again from the captured value on each
call. With the expression API, create the variable with :meth:`Expression.state_variable`.

Strings
^^^^^^^

A string has its own operations, separate from the collection operations. The compilers choose
them from the type of the value. This streams the vessel's name in upper case:

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

The compilers translate the language's string operations, such as length, indexing,
searching, splitting and case conversion. The expression API names them
:meth:`Expression.string_length`, :meth:`Expression.string_get`,
:meth:`Expression.string_substring` and so on. Passing a string to a collection operation is an
error that points at them.

Two rules apply to strings:

* **A character is a string of length one.** The protocol carries no character type, so
  indexing a string produces a one character string, which works with every other string
  operation. In a language where indexing a string produces a character, that index is an
  unsupported construct. Take a substring in its place.
* **Case conversion and comparison are the same in every locale.** Case is converted, and
  strings are compared, by ordinal value. A function produces the same result whatever language
  the game runs in.

.. _server-side-functions-exceptions:

Exceptions
^^^^^^^^^^

A function can raise an exception. It reaches your program as an error from ``Run``, or as an error
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

A function raises the exceptions services declare, so your program catches a typed exception.
A service's own exception classes map onto themselves.

A compiler maps the language's built-in exceptions onto the general kRPC exceptions, for
example an invalid operation to ``InvalidOperationException`` and a bad index to
``IndexOutOfRangeException``. The language's base exception class raises a ``KRPC.Error``, for
a condition no other exception describes.

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

These rules apply to handlers:

* **Only the message of an exception is exposed.** The variable a handler binds is a string
  holding the message.
* **Naming an exception catches every form of it.** A service raises a .NET exception, which the
  server reports under its kRPC name. A handler for a kRPC exception catches the exceptions
  procedures raise, as well as the ones the function raises.
* **A procedure that pauses execution passes through every handler**, including a catch-all.
  It is reported as an error.
* **Two failures of an embedded call have no exception type**, so only a catch-all handles
  them. Both reach your program as errors without a name:

  * A call to a procedure that is unavailable in the current game scene.
  * A procedure that returns null although its return type is not nullable.

The Standard Library
^^^^^^^^^^^^^^^^^^^^

The ``StdLib`` service provides mathematics for server side functions. Calls to the language's
math library compile to the matching ``StdLib`` procedures when their arguments are computed on
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

The service covers:

* Scalar functions, such as square roots, trigonometry and clamping, and the constants pi and e.
* Vector operations on the position and direction tuples of the ``SpaceCenter`` service, such
  as dot and cross products, magnitudes, angles and interpolation.
* Quaternion operations on its rotation tuples, such as composition, inverses, rotating vectors
  and interpolation.

Angles are in radians, with converters to and from degrees. :meth:`StdLib.log` takes the base of
the logarithm, and is natural without one.

Each midpoint rounding rule has its own procedure, so a function can round the way its language
does:

* :meth:`StdLib.round` rounds a halfway value to the nearer even number, as Python and C# do.
* :meth:`StdLib.round_half_away_from_zero` rounds it away from zero, as C and C++ do.
* :meth:`StdLib.round_half_up` rounds it toward positive infinity, as Java does.

Each takes the number of decimal places, negative for tens and hundreds. A compiled rounding
call uses the procedure for its rounding rule.

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

A print in an event or stream function writes a line on every stream update.

Supported Constructs
^^^^^^^^^^^^^^^^^^^^

The compilers support each construct with a server side equivalent: arithmetic, comparisons,
boolean operators, conditional expressions, collection constructors, indexing, membership tests,
and the collection and string operations. Where the language allows them, the compilers also
support statements, changing a collection in place, and raising and handling exceptions.

Any other construct raises a compilation error that names it. Examples are a format specifier,
and a call to a client side function with server side arguments.

A few semantics differ from running the same code in your program:

* A sub-expression that uses only client side values is evaluated once, when the function is
  compiled. A remote call made inside a client side function would run only once.
* In Python, a call to a client side function must give the same answer whenever it runs. A
  client side function that makes a remote call is an error.
* In C#, a call that can run code outside the .NET base library, the kRPC client and the service
  stubs is an error. Examples are a delegate call, and a method, constructor, computed property or
  operator defined in your program.
* A client side call written as a statement, such as a sleep, is an error.
* A rounding mode must be a constant, as it selects the server side procedure.
* The language's own rules for the code a compiler accepts still apply.

Each client guide lists the constructs its compiler supports, and the rules of its language:
:ref:`C# <csharp-client-compiling-lambdas>` and :ref:`Python <python-client-compiling-functions>`.

.. _server-side-functions-errors:

Errors, Edge Cases and Performance
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

**Errors.** An error raised while evaluating a stream or event function reaches your program
when it reads the stream or event. An example is an embedded call reaching a destroyed part.

The server then removes the stream, as it does for any stream whose result is an error. Build a
new event or stream once the cause clears.

**Loads and unloaded vessels.** A function that reads an object the game has not loaded raises
``KRPC.ObjectNotLoadedException``. Examples are a part of an unloaded vessel, and a vessel part
way through a load. The exception ends a function stream or event like any other. To keep one
running, catch the exception in the function:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/NotLoadedEvent.java

The event's handler gives ``False``, so the event does not fire. A stream's handler can give a
last good value, kept in a state variable. With the expression API, :meth:`Expression.try_catch`
names the exception as ``"KRPC", "ObjectNotLoadedException"``.

**Procedures that pause.** Some procedures pause execution and resume on a later tick, such as
``SpaceCenter.WarpTo`` and ``SpaceCenter.LaunchVessel``. Calling one is an error in any function.
The procedure keeps its effects up to the pause, so a ``WarpTo`` called this way leaves the game
warping.

**Loops run to completion.** A ``while`` loop runs to its end within a single tick, before the
game continues. A loop that never ends hangs the game until the game is closed. Bound every loop
by a value that is certain to change.

**Null values.** A procedure that can return null, such as ``Part.engine``, gives a value to
compare with null, or to test with :meth:`Expression.is_null`. A value of any type can be
nullable, and a function tracks which values are. A nullable number works in arithmetic like
any other number.

A null reaching a position that cannot hold one raises a ``NullReferenceException`` when the
function runs. These positions are the arguments, operands and variables whose types are not
nullable. A client raises it as the closest built-in exception of its language.

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

Elsewhere, give a null its type with a type annotation or a cast. The expression API has
:meth:`Expression.constant_null`.

A function whose result is null gives your program the client's null value. Decoding a null into
a type that cannot hold one is an error.

A client that takes the result type as a type argument needs a type that can hold null. The same
holds for the nulls inside a result, such as the objects in a list a procedure returns.

**Costs.** A function is evaluated on the server as native code, and only its result crosses
the network. Even a large function evaluates quickly.

**Reuse a function.** A function stays on the server until the server stops. Passing a new
lambda to ``Run`` in a loop builds and keeps a new function on each pass. Each pass reads the
lambda's captured variables again, while a compiled function keeps the values from when it was
compiled.

Compile a function once, and run the compiled function as often as needed. To run it with
different values, give it parameters, as in :ref:`server-side-functions-arguments`:

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

Compiling a function also compiles it to native code on the server. A lambda passed straight to
``Run`` is compiled there on every call, on the game's main thread, so a large one can make the game
stutter.

Compile a function before it is needed, such as before launch, and every run is fast. For a
function built with the expression API, ``KRPC.CompileFunction`` compiles it on the
server.

.. _server-side-functions-api:

The Expression API
------------------

The expression API builds a server side function node by node. It is how the C++ and Java
clients write functions, and how C# writes the statements a lambda cannot hold. The compilers
produce the same functions.

Expression Trees
^^^^^^^^^^^^^^^^

A server side function is a tree of *expression objects* held on the server, built with the
static methods of the :class:`Expression` class. The server compiles the tree to native code, so
evaluations are fast.

Each node is a constant, an embedded remote procedure call, an operator, a statement or a nested
function. Statements are assignments, loops and if statements.

Three RPCs consume these trees:

* ``AddEvent`` takes a function that evaluates to a boolean, and returns an event. The server
  evaluates the function on each stream update, and triggers the event when it returns true.
* ``AddFunctionStream`` takes a function whose value can be sent to a client, such as a number,
  a string, a collection or an object. It returns a stream of the value, recomputed on each
  stream update.
* ``RunFunction`` evaluates a function once, within a single physics tick, and returns its
  value. Use it for a function with side effects.

Two rules hold for every server side function:

* **Embedded remote procedure calls are re-invoked on every evaluation.** Constants, and the
  values your program supplies when building the function, are fixed when the function is
  created.
* **The whole function is evaluated within a single physics tick.** A function that reads the
  fuel level of every engine reads them all in the same tick. The same calls made from your
  program read them over several ticks, as the game advances.

Expression Objects
^^^^^^^^^^^^^^^^^^

The altitude event from the start of this guide has three nodes: an embedded call, a constant
and a comparison:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/python/client/Event.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/csharp/client/Event.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/cpp/client/Event.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/java/client/CustomEvent.java

Two kinds of object appear here:

* A **procedure call message** describes an RPC without invoking it. Each client builds one
  from a call written in its own language, as the examples show. Building a call message is a
  local operation.
* An **expression object** is a node of the tree. :meth:`Expression.call` embeds a procedure
  call message, which the server invokes on each evaluation. A procedure with no return value
  embeds as a statement.

Each factory method is an RPC that creates one node on the server and returns a handle to it.
Building a function costs one RPC per node, paid once.

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

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/TruncateAltitude.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

Expressions are statically typed, with the types of the server's procedures. Numbers of
different types convert to a common type, so an integer multiplies a double directly.

The same conversion applies to a procedure's arguments, a collection's elements, a structure's
fields and an assigned variable, and each conversion must widen. :meth:`Expression.cast`
converts a value explicitly, for example to narrow a double to an integer.

The :class:`Type` class names types, including the classes, enumerations and structures a
service defines. Each type of constant has its own factory method, such as
:meth:`Expression.constant_double`.

Each operator has a factory method, such as :meth:`Expression.add`.
:meth:`Expression.and_` and :meth:`Expression.or_` evaluate both operands.
:meth:`Expression.conditional_and` and :meth:`Expression.conditional_or` evaluate the second
operand only when the first does not decide the result.

Function Streams
^^^^^^^^^^^^^^^^

A function whose value can be sent to a client can be streamed:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStream.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/cpp/client/FunctionStream.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/java/client/FunctionStream.java

Some clients take the type of the stream's values as a type argument, and others read the
function's return type from the server. A function stream behaves like any other stream, and its
rate can be limited in the usual way.

Functions and Collections
^^^^^^^^^^^^^^^^^^^^^^^^^

This builds the "any engine out of fuel" event from earlier. The call message only identifies
the procedure, so any engine can supply it:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.py

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/CollectionEvent.java

A function passed to a collection operation is built from two pieces:

* :meth:`Expression.parameter` creates a named, typed parameter. The type can be a class, so a
  parameter can be an ``Engine``.
* :meth:`Expression.lambda_` combines a list of parameters and a body into a function. In the
  body, the parameter expressions stand for the arguments.

To call an RPC on each element, take the call's instance from the function's parameter.
:meth:`Expression.call_with_arguments` embeds a call with some of its arguments given as
expressions. Each expression is keyed by the position of its parameter, and position 0 is the
instance.

A position without an expression takes the value encoded in the call message, then the
parameter's default. :meth:`Expression.invoke` calls a function directly, binding values to its
parameters by name.

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

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/StructureField.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:meth:`Expression.create_struct` builds a structure from its type and its field values, in the
order the structure declares them. A field's name is the one the service declares, as shown in
the API documentation.

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

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/FunctionStatements.java

Statements are ordinary expression nodes. :meth:`Expression.block_with_variables` sequences
statements and declares local variables, and its value is the value of its last statement.

The other statements, such as assignments, if statements, loops and early returns, each have a
factory method. A call to a procedure without a return value is a statement, run for its
effects.

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

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/TargetEvent.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:meth:`Expression.constant_object` takes the object's identifier in the communication protocol.
Each client has a function that returns it: ``krpc.get_object_id`` in Python and Lua, ``RemoteObject.GetObjectId`` in
C#, ``krpc::get_object_id`` in C++ and ``RemoteObject.getObjectId`` in Java. The compilers embed a
captured remote object this way.

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

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/server-side-functions/ThrowException.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:meth:`Expression.try_catch`, :meth:`Expression.try_catch_all` and :meth:`Expression.try_finally`
handle exceptions.

Extensibility
^^^^^^^^^^^^^

A server side function can call the procedures of every installed service except ``KRPC``, the
service that builds and runs the functions. To extend what functions can do, **write a
service**. A service DLL in the game's plugin folder adds its procedures to server side functions
on every client.

The service's documentation and client stubs are generated in the usual way. The ``StdLib``
service is written this way, and ships with the server.

The compilers resolve remote calls through the server's service definitions. The expression API
embeds any procedure with :meth:`Expression.call_with_arguments`.
