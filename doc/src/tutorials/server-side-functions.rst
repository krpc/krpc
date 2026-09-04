.. currentmodule:: KRPC

.. _tutorial-server-side-functions:

Server Side Functions
=====================

Server side functions let you build code that runs *inside* the game, rather than in your
program. This has three main uses:

* **Custom events**: wait for a condition to become true. The condition is checked on the
  server every physics tick, and a message is only sent to your program when it fires.
  Polling the condition from your program instead adds network latency to every check, and can
  miss a condition that only holds for a few ticks.
* **Function streams**: stream the result of a computation over multiple RPC values. All of
  the values are read in the same physics tick, so they are consistent with one another, and
  only the combined result is sent to your program, only when it changes.
* **Run-once functions**: run a sequence of calls and computation in a single physics tick,
  started by a single RPC. Side effects, such as setting properties and triggering actions, are
  included. Making the same calls from your program is a round trip per step, with the game
  advancing between them.

This tutorial is in two parts. :ref:`Compiled functions
<server-side-functions-compiled>` covers writing the code in your own language and having the
client compile it, which Python and C# support. :ref:`The expression API
<server-side-functions-api>` covers the underlying API that compiled code translates into.
The C++ and Java clients use it directly, and it sets the bounds of what a function can do.

.. note::

   Events and function streams are built on streams, so they are available in the clients that
   support streams: Python, C#, C++ and Java. These four also decode the result of
   ``RunFunction``, which is otherwise an ordinary procedure returning encoded bytes.

Expression Trees
----------------

A server side function is a tree of *expression objects* held on the server. Each node of the
tree is a constant, an embedded remote procedure call, an operator applied to other
expressions, a statement (an assignment, a loop or an if statement) or a nested function. The
server compiles the tree to native code on its first evaluation, so evaluating it afterwards is
fast.

Three RPCs consume these trees:

* ``AddEvent`` takes a function that evaluates to a boolean, and returns an event object.
  The server evaluates the function on each stream update and triggers the event when it
  returns true.
* ``AddFunctionStream`` takes a function producing any type that can be sent to a client
  (numbers, strings, collections or objects) and returns a stream. The value is recomputed on
  each stream update and sent to the client when it changes.
* ``RunFunction`` evaluates a function *once*, within a single physics tick, and returns
  the value it produces. This is the right consumer for functions with side effects: an event
  or stream re-evaluates its function on every update, repeating the effects each tick.

Two properties of a server side function are worth keeping in mind throughout this tutorial:

* **Remote procedure calls embedded in a function are re-invoked on every evaluation.**
  Constants, and any values your program supplies when building the function, are fixed
  when the function is created.
* **The whole function is evaluated within a single physics tick.** If a function reads
  the fuel level of every engine on a vessel, all of the reads happen in the same tick; the
  values cannot change part way through, as they could if your program made the same calls
  itself.

.. _server-side-functions-compiled:

Compiled Functions
------------------

The Python and C# clients can build the server side trees automatically, by compiling a
function or lambda that takes no arguments. This is the recommended way to use server side
functions in those languages: the code reads exactly like the client side code it replaces.

A First Custom Event
^^^^^^^^^^^^^^^^^^^^

The following waits until the vessel's altitude exceeds 1000m. The condition is checked on the
server each physics tick, with no network traffic while waiting:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var evnt = connection.AddEvent (() => flight.MeanAltitude > 1000);
         lock (evnt.Condition) {
             evnt.Wait ();
             Console.WriteLine ("Altitude reached 1000m");
         }

   .. group-tab:: Python

      .. code-block:: python

         event = conn.add_event(lambda: flight.mean_altitude > 1000)
         with event.condition:
             event.wait()
             print("Altitude reached 1000m")

The lambda is not run by your program. Instead, the client inspects it and builds an
equivalent server side function: the ``mean_altitude`` call becomes an embedded RPC,
re-invoked by the server on each check, and ``1000`` becomes a constant.

Remote Calls and Captured Values
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler splits the function's code into two kinds of sub-expression:

* Reading a property or calling a method of a remote object or service becomes part of the
  function, and is re-evaluated by the server on every evaluation.
* Captured variables, literals, arithmetic on plain values and calls to library functions like
  ``math.sqrt`` are evaluated *once*, when the function is compiled, and embedded as
  constants.

For example:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var threshold = 50000;
         var evnt = connection.AddEvent (
             () => vessel.Orbit.ApoapsisAltitude > threshold * 2);

   .. group-tab:: Python

      .. code-block:: python

         threshold = 50000
         event = conn.add_event(lambda: vessel.orbit.apoapsis_altitude > threshold * 2)

Here ``threshold * 2`` is computed by the client when the event is created, and the function
compares the apoapsis against the constant 100000. The event holds that constant for good;
compile a new function to pick up a new value of ``threshold``. The apoapsis is read by the
server on every check.

Chains of calls also work as expected: both the ``orbit`` call and the ``apoapsis_altitude``
call are embedded in the function, so it follows the vessel's *current* orbit object each
tick, even if the vessel moves to a different orbit.

Streaming Computed Values
^^^^^^^^^^^^^^^^^^^^^^^^^

Passing a function when creating a stream compiles it and streams the computed value. The
computation runs on the server on each stream update, and the result is only sent to your
program when it changes:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // The vessel's altitude, in kilometers, computed on the server
         var stream = connection.AddStream (() => flight.MeanAltitude / 1000);
         Console.WriteLine ("Altitude: " + stream.Get () + " km");

   .. group-tab:: Python

      .. code-block:: python

         # The vessel's altitude, in kilometers, computed on the server
         stream = conn.add_function_stream(lambda: flight.mean_altitude / 1000)
         print("Altitude:", stream(), "km")

Expressions can produce collections as well as single values. This streams the thrust of every
engine on the vessel, read in a single physics tick per update:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var engines = vessel.Parts.Engines;
         var stream = connection.AddStream (
             () => engines.Select (engine => engine.Thrust).ToList ());
         foreach (var thrust in stream.Get ())
             Console.WriteLine (thrust);

   .. group-tab:: Python

      .. code-block:: python

         engines = vessel.parts.engines
         stream = conn.add_function_stream(
             lambda: [engine.thrust for engine in engines]
         )
         for thrust in stream():
             print(thrust)

Note the difference between the two places the engine list could come from. Here ``engines``
is a captured variable: the *list of engines* is fixed when the function is compiled, and
only their thrusts are re-read each update. Writing ``vessel.parts.engines`` *inside* the
function instead embeds the engine list call in it, so the server re-fetches the list each
update. That follows staging events, at the cost of extra work per update.

Working with Collections
^^^^^^^^^^^^^^^^^^^^^^^^

Calls can be applied to each element of a collection, entirely on the server. In Python,
comprehensions and the builtin functions ``len``, ``sum``, ``min``, ``max``, ``any``, ``all``
and ``sorted`` are compiled to the server's collection operations; in C#, the LINQ operators
are. The following creates an event that fires when any engine on the vessel runs out of
fuel. The engine list is re-read on each check, so engines revealed by staging are covered:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var evnt = connection.AddEvent (
             () => vessel.Parts.Engines.Any (engine => !engine.HasFuel));
         lock (evnt.Condition) {
             evnt.Wait ();
             Console.WriteLine ("An engine has run out of fuel");
         }

   .. group-tab:: Python

      .. code-block:: python

         event = conn.add_event(
             lambda: any(not engine.has_fuel for engine in vessel.parts.engines)
         )
         with event.condition:
             event.wait()
             print("An engine has run out of fuel")

In Python, a plain function can be compiled as well as a lambda, and may contain simple
assignments before its return statement:

.. code-block:: python

   def fuel_fraction():
       resources = vessel.resources
       amount = resources.amount("LiquidFuel")
       capacity = resources.max("LiquidFuel")
       return amount / capacity

   stream = conn.add_function_stream(fuel_fraction)

Statements, Loops and Local Variables
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

In Python, a compiled function can have a full statement body: ``if``/``elif``/``else``,
``while`` and ``for`` loops with ``break`` and ``continue``, early returns, and local
variables. A local variable can be created, reassigned and mutated, for example to build up a
collection in a loop. It takes its type from its first assignment, so annotate an assignment of
an empty collection, and annotate a counter that later holds a fraction as ``total: float = 0``:

.. code-block:: python

   def burn_ratings():
       ratings: list[str] = []
       for engine in vessel.parts.engines:
           if not engine.active:
               continue
           if engine.thrust > 0.9 * engine.available_thrust:
               ratings.append("full")
           else:
               ratings.append("partial")
       return ratings

   print(conn.run_function(burn_ratings))

(C# converts only single-expression lambdas to expression trees, so a multi-statement function
in C# is built with the :ref:`expression API <server-side-functions-api>` instead.)

Side Effects and Run-Once Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Compiled functions can also *change* things: assign to the properties of remote objects and
services, and call methods for their effects. A function with side effects should be run with
``run_function`` (``RunFunction`` in C#), which evaluates it exactly once, with every step
happening in the same physics tick:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // A single side-effecting call, run on the server
         connection.RunFunction (() => vessel.Control.ActivateNextStage ());

         // Compute and return a value in one tick
         var altitude = connection.RunFunction (() => flight.MeanAltitude / 1000);

   .. group-tab:: Python

      .. code-block:: python

         def deploy():
             for parachute in vessel.parts.parachutes:
                 if not parachute.deployed:
                     parachute.deploy()

         conn.run_function(deploy)

Running the function in a single tick is the point: unlike making the same calls from your
program, the game does not advance between the steps, and there is no network round trip per
step. A function that returns nothing gives ``None`` (or use the ``void`` overloads in the
statically typed clients).

.. note::

   Passing a side-effecting function to an event or a stream re-runs its effects on *every*
   stream update. Use ``run_function`` for effects, and keep event and stream functions to
   pure computation.

Deferring a Call
^^^^^^^^^^^^^^^^

A few procedures pause execution and resume on a later tick, among them
``SpaceCenter.WarpTo`` and ``SpaceCenter.LaunchVessel``. A function starts one without waiting
for it, and carries on:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/DeferredCall.cs

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/DeferredCall.py

Python spells it ``defer(call)``, imported from ``krpc``, and C# spells it
``Function.Defer(() => call)``. Both are statements, and both discard whatever the procedure
returns, so a procedure whose result you need cannot be deferred. See
:ref:`server-side-functions-deferred-calls` for what the function sees afterwards and where
a failure is reported.

In C# the compiled lambda is an expression tree, which carries a single expression, so
``Function.Defer`` is the whole body of a ``RunFunction`` or ``CompileFunction`` call. Python
places it anywhere a statement goes, including inside a loop or a conditional.

The Standard Library
^^^^^^^^^^^^^^^^^^^^

The ``StdLib`` service provides mathematics for use within server side functions: scalar
functions (square roots, trigonometry, clamping and so on, with the constants pi and e), vector
operations on the position and direction tuples used throughout the ``SpaceCenter`` service
(dot and cross products, magnitudes, angles, interpolation), and quaternion operations on its
rotation tuples (composition, inverses, rotating vectors, interpolation). Angles are in
radians, with converters to and from degrees.

Each midpoint rounding rule is its own procedure, so a program can round the way its language
does:

* :meth:`StdLib.round` rounds a halfway value to the nearer even number, as Python and C# do.
* :meth:`StdLib.round_half_away_from_zero` rounds it away from zero, as C and C++ do.
* :meth:`StdLib.round_half_up` rounds it toward positive infinity, as Java does.

Each takes the number of decimal places to round to, negative for tens and hundreds. A compiled
``round(x, 2)`` or ``Math.Round(x, 2, MidpointRounding.AwayFromZero)`` picks the procedure for
its rule and passes the places through. :meth:`StdLib.log` takes the base of the logarithm, and
is natural without one.

Calls to python's ``math`` module functions, and to C#'s ``System.Math`` methods, compile to
the equivalent ``StdLib`` procedures when their arguments are computed on the server. The
service can also be called directly, which is the way to use the vector and quaternion
operations. For example,
streaming the angle between the vessel's facing direction and its velocity, computed entirely
on the server:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var stdlib = connection.StdLib ();
         var frame = vessel.SurfaceReferenceFrame;
         var stream = connection.AddStream (
             () => stdlib.RadiansToDegrees (stdlib.VectorAngle (
                 vessel.Direction (frame), vessel.Velocity (frame))));

   .. group-tab:: Python

      .. code-block:: python

         stdlib = conn.std_lib
         frame = vessel.surface_reference_frame
         stream = conn.add_function_stream(
             lambda: stdlib.radians_to_degrees(
                 stdlib.vector_angle(
                     vessel.direction(frame), vessel.velocity(frame)
                 )
             )
         )

Supported Constructs
^^^^^^^^^^^^^^^^^^^^

The compilers support the constructs that have a server side equivalent: arithmetic,
comparisons, boolean operators, conditional expressions, collection constructors, indexing,
membership tests, and the collection and string operations described above. Python adds the
statements described above, changing a collection in place, and raising and handling
exceptions. A construct with no server side equivalent, such as string formatting or a call to
a client side function with server side arguments, raises an error naming it
(``krpc.error.FunctionCompilationError`` in Python, ``FunctionCompilationException`` in C#).

Some semantics differ slightly from running the same code in your program:

* The bitwise boolean operators (``&``, ``|``) evaluate both operands, on the server as they do
  locally. ``and``/``or`` and ``&&``/``||`` short-circuit.
* Sub-expressions that do not interact with the server are evaluated once, at compile time,
  as described above. That includes calls to client side functions, so such a call must give
  the same answer whenever it runs.
* The rounding mode given to ``Math.Round`` must be a constant. It names the server side
  procedure, which is chosen when the function is compiled.
* In C#, lambdas passed to ``CompileFunction``, ``AddEvent`` or ``AddStream`` are expression
  trees, so the C# language rules for expression trees apply. Most notably, every argument of a
  call must be given, including the optional ones, and a lambda carries a single expression
  rather than a sequence of statements.

The full lists of supported constructs are in the
`Python client documentation <../python/client.html>`_ and the
`C# client documentation <../csharp/client.html>`_.

.. _server-side-functions-api:

The Expression API
------------------

Compiled functions are a convenience layer: what they produce, and what the C++ and Java
clients use directly, is a tree of expression objects built by calling the static methods of
the :class:`Expression` class. Every client that supports streams can use this API, and the
examples below are given for all four of them.

Building an Expression
^^^^^^^^^^^^^^^^^^^^^^

Expression objects are remote objects: each factory method is an RPC that creates one node of
the tree on the server and returns a handle to it. Building a function therefore costs one
RPC per node, paid once when the function is created.

The altitude event from the first part of this tutorial is built from three nodes: an embedded
call, a constant, and a comparison.

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/Event.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/Event.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/CustomEvent.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/Event.py

Two kinds of object appear here:

* A **procedure call message** describes an RPC without invoking it. Each client provides a
  way to build one: ``conn.get_call`` in Python, ``Connection.GetCall`` in C#,
  ``connection.getCall`` in Java, and the generated ``..._call()`` methods in C++ (for
  example ``flight.mean_altitude_call()``). Building a call message is a local operation.
* An **expression object**, created by the :class:`Expression` factory methods.
  :meth:`Expression.call` embeds a procedure call message in an expression; the call is then
  re-invoked by the server on each evaluation, and its result used in the surrounding
  expression. Only procedures that return a value can be embedded.

Operators and Types
^^^^^^^^^^^^^^^^^^^

Constants are created with :meth:`Expression.constant_double`,
:meth:`Expression.constant_float`, :meth:`Expression.constant_int`,
:meth:`Expression.constant_bool` and :meth:`Expression.constant_string`, and a member of an
enumeration a service defines with :meth:`Expression.constant_enum`. The usual arithmetic,
comparison and boolean operators combine expressions into larger ones, along with
:meth:`Expression.conditional` (an if-then-else value) and the bitwise shifts.
:meth:`Expression.and_` and :meth:`Expression.or_` evaluate both operands;
:meth:`Expression.conditional_and` and :meth:`Expression.conditional_or` evaluate the second
only when the first does not decide the result.

Expressions are statically typed, following the types of the server's procedures. Numeric
operands of differing types are converted automatically to a common type, so an integer can be
multiplied by a double directly. The same conversion applies to a procedure's arguments, a
collection's elements, a structure's fields and the variable a value is assigned to, and each
one has to widen. :meth:`Expression.cast` converts a value to another type explicitly, and is
what narrowing a double to an integer requires.

Types are named by the :class:`Type` class, which has factory methods for the primitive types
(:meth:`Type.double`, :meth:`Type.int`, :meth:`Type.string` and so on) and for collection
types. :meth:`Type.class_type`, :meth:`Type.enumeration_type` and :meth:`Type.struct_type` name
the classes, enumerations and structures a service defines:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // Truncate the altitude to a whole number of meters
         var expr = Expression.Cast (connection,
             Expression.Call (connection, meanAltitude),
             Type.Int (connection));

   .. group-tab:: C++

      .. code-block:: cpp

         // Truncate the altitude to a whole number of meters
         auto expr = Expr::cast(conn,
           Expr::call(conn, mean_altitude),
           KType::int_(conn));

   .. group-tab:: Java

      .. code-block:: java

         // Truncate the altitude to a whole number of meters
         Expression expr = Expression.cast(
             connection,
             Expression.call(connection, meanAltitude),
             Type.int_(connection));

   .. group-tab:: Python

      .. code-block:: python

         # Truncate the altitude to a whole number of meters
         expr = expression.cast(
             expression.call(mean_altitude), types.int()
         )

Function Streams
^^^^^^^^^^^^^^^^

Any function whose type can be sent to a client can be streamed. The statically typed clients
supply the expected type when creating the stream. The Python client reads the function's
return type from the server, so decoding is automatic:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/FunctionStream.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/FunctionStream.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/FunctionStream.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/FunctionStream.py

Function streams behave like ordinary streams: the value is recomputed on each stream
update, sent only when it changes, and the stream's rate can be limited in the usual way.

Functions and Collections
^^^^^^^^^^^^^^^^^^^^^^^^^

Expressions can process collections using operations such as :meth:`Expression.select` (apply
a function to every element), :meth:`Expression.where` (filter by a predicate),
:meth:`Expression.any`, :meth:`Expression.all`, :meth:`Expression.count`,
:meth:`Expression.sum`, :meth:`Expression.min`, :meth:`Expression.max`,
:meth:`Expression.average`, :meth:`Expression.order_by`, :meth:`Expression.aggregate`,
:meth:`Expression.contains`, :meth:`Expression.concat` and :meth:`Expression.get` (indexing).
Collections can also be constructed in an expression, with :meth:`Expression.create_list`,
:meth:`Expression.create_set`, :meth:`Expression.create_dictionary` and
:meth:`Expression.create_tuple`.

A single value is selected with :meth:`Expression.first`, :meth:`Expression.last`,
:meth:`Expression.element_at`, and :meth:`Expression.min_by` and :meth:`Expression.max_by`,
which give back the value producing the smallest or largest key rather than the key itself.
A collection is reshaped with :meth:`Expression.distinct`, :meth:`Expression.reverse`,
:meth:`Expression.union`, :meth:`Expression.intersect`, :meth:`Expression.except_`,
:meth:`Expression.zip` and :meth:`Expression.group_by`, which produces a dictionary of each
key to the values that produced it.

A collection built within a function can also be changed in place. Each operation covers every
collection it makes sense for, and the type of the collection decides what it does.
:meth:`Expression.append` adds a value to a list or a set, and :meth:`Expression.set` writes an
element of a list or an entry of a dictionary.

:meth:`Expression.remove` takes a value out of a list or a set, and an entry out of a
dictionary by its key. :meth:`Expression.remove_at` removes an element of a list by position,
and :meth:`Expression.clear` empties any of the three.

**A dictionary cannot be iterated over directly.** Iterate over
:meth:`Expression.dictionary_keys` or :meth:`Expression.dictionary_values` instead, each of
which produces a list. Passing a dictionary to a collection operation is reported as an error
pointing at them.

**The index into a tuple must be a constant.** The elements of a tuple differ in type, so a
computed index would leave :meth:`Expression.get` with no type for its result. A list or
dictionary is indexed by any expression.

The functions passed to these operations are built from two pieces:

* :meth:`Expression.parameter` creates a named parameter with a given type, including a class
  type, so a parameter can be an ``Engine``.
* :meth:`Expression.lambda_` combines a list of parameters and a body into a function. Within
  the body, the parameter expressions stand for the function's arguments. (A function can
  also be called directly with :meth:`Expression.invoke`, binding values to its parameters by
  name.)

To call an RPC on each element of a collection, the call's *instance* must come from the
function's parameter. :meth:`Expression.call_with_arguments` embeds a procedure call as
:meth:`Expression.call` does, and supplies some or all of the call's arguments as expressions,
keyed by the position of the parameter each one supplies. Position 0 is the instance the call
is made on. A position with no expression falls back to the value encoded in the call message,
and then to the parameter's default value.

Putting these together, the "any engine out of fuel" event from the first part of this
tutorial is built as follows. The template call message can be built from any convenient
engine, since only the identity of the procedure is used and the parameter supplies the
instance:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/CollectionEvent.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/CollectionEvent.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/CollectionEvent.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/CollectionEvent.py

:meth:`Expression.select` and :meth:`Expression.where` produce lazily evaluated sequences,
which cannot be sent to a client directly; convert them to a concrete collection with
:meth:`Expression.to_list` or :meth:`Expression.to_set` before streaming them. The
aggregations (:meth:`Expression.sum`, :meth:`Expression.min`, :meth:`Expression.max`,
:meth:`Expression.any`, :meth:`Expression.all`) accept lazy sequences directly.

Structures
^^^^^^^^^^

A structure a service defines is an ordinary value in an expression: it can be returned by a
call, streamed, passed as an argument, and built. :meth:`Expression.get_field` reads a field
of one by name, and :meth:`Expression.create_struct` builds one from its type and the values
of its fields, in the order the structure declares them:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // The Name field of a structure a call returns
         var expr = Expression.GetField (connection,
             Expression.Call (connection, siteInfo), "Name");

   .. group-tab:: C++

      .. code-block:: cpp

         // The Name field of a structure a call returns
         auto expr = Expr::get_field(conn, Expr::call(conn, site_info), "Name");

   .. group-tab:: Java

      .. code-block:: java

         // The Name field of a structure a call returns
         Expression expr = Expression.getField(
             connection, Expression.call(connection, siteInfo), "Name");

   .. group-tab:: Python

      .. code-block:: python

         # The Name field of a structure a call returns
         expr = expression.get_field(expression.call(site_info), "Name")

A field is named by the name the service declares it with, which is the name that appears in
the API documentation, rather than by the name it has in a particular client.

The Python and C# compilers produce these nodes from ordinary syntax. Reading an attribute of
a structure valued expression compiles to :meth:`Expression.get_field`. Constructing one, by
calling the structure type in Python or with ``new`` in C#, compiles to
:meth:`Expression.create_struct`.

Statements, Variables and Loops
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Everything the Python statement compiler produces can be built directly with the factory API,
in any client. Statements are ordinary expression nodes:

* :meth:`Expression.block` sequences statements, and its value is the value of its last
  statement. :meth:`Expression.block_with_variables` also declares local variables, created
  with :meth:`Expression.variable` and assigned with :meth:`Expression.assign`.
* :meth:`Expression.if_then` and :meth:`Expression.if_then_else` are if statements.
* :meth:`Expression.while_` and :meth:`Expression.for_each` are loops.
  :meth:`Expression.break_` and :meth:`Expression.continue_` apply to the nearest enclosing
  loop, and :meth:`Expression.return_` ends the enclosing function early.
* A call to a procedure without a return value is a statement, run for its effects.
* A collection is built imperatively with :meth:`Expression.create_empty_list` (and the set and
  dictionary counterparts), :meth:`Expression.append` and :meth:`Expression.set`.

The following builds and runs a small program, ``total = 0; for x in [1, 2, 3]: total += x``,
returning 6:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/FunctionStatements.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/FunctionStatements.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/FunctionStatements.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/FunctionStatements.py

.. _server-side-functions-deferred-calls:

Deferred Calls
^^^^^^^^^^^^^^

A few procedures pause execution and resume on a later tick, among them
``SpaceCenter.WarpTo`` and ``SpaceCenter.LaunchVessel``. A function starts one with
:meth:`Expression.deferred_call`, and carries on without waiting for it. This warps to one
minute from the time the function runs, with the target time computed on the server:

.. code-block:: python

   ut = conn.get_call(getattr, conn.space_center, "ut")
   warp = conn.get_call(conn.space_center.warp_to, 0)
   expr = expression.deferred_call_with_arguments(warp, {
       0: expression.add(expression.call(ut), expression.constant_double(60)),
   })
   conn.krpc.run_function(expr)

A deferred call is a statement, and any value the procedure returns is discarded: a deferred
``Undock`` cannot hand the function the vessel it produced. Three further consequences are
worth knowing:

* **The function sees the game as it was before the call completes.** Everything after the
  deferred call reads state from the tick the function ran in.
* **A failure after the pause reaches the server's log only.** The function has finished by
  then. A failure to start the call, such as the procedure being unavailable in the current
  game scene, is reported as usual.
* **The call runs until it completes or your client disconnects.** A canceled call leaves its
  effect part way through, so a canceled ``WarpTo`` leaves the game warping. The server
  stopping cancels every deferred call.

The Python and C# compilers produce this node from ``defer(call)`` and
``Function.Defer(() => call)``, described above.

Object Constants
^^^^^^^^^^^^^^^^

:meth:`Expression.constant_object` embeds a reference to an object, such as a vessel, a
reference frame or an engine. Objects can then be compared for equality, or passed as arguments
to calls built with :meth:`Expression.call_with_arguments`. An object is identified by the
identifier used to reference it over the communication protocol, which each client exposes on
its remote object wrappers: the ``id`` field in C# and Java, ``_id`` in C++, and the
``_object_id`` attribute in Python. For example, an event that fires when the vessel's target
changes to a particular vessel:

.. code-block:: python

   target = conn.get_call(getattr, conn.space_center, "target_vessel")
   expr = expression.equal(
       expression.call(target),
       expression.constant_object(some_vessel._object_id),
   )
   event = conn.krpc.add_event(expr)

(The compilers do this automatically whenever a compiled function captures a remote object.)

Strings
-------

A string is not a collection, so it has its own operations rather than sharing the collection
ones. Both compilers reach them from ordinary syntax, dispatching on the type of the value:
``len(s)``, ``s[i]``, ``s[a:b]``, ``x in s``, ``s.upper()``, ``s.split(", ")`` and the rest in
Python, and ``.Length``, ``.Substring()``, ``.ToUpper()``, ``.Contains()`` and the rest in
C#. The factory API names them :meth:`Expression.string_length`,
:meth:`Expression.string_get`, :meth:`Expression.string_substring` and so on.

Two things are worth knowing:

* **A character is a string of length one.** The wire carries no character type, so ``s[0]``
  produces a one character string, which composes with every other string operation. Indexing a
  string in C# produces a ``char``, which has no server side equivalent and is reported as an
  unsupported construct; use ``Substring`` instead.
* **Case conversion and comparison do not depend on the game's language.** Case is converted
  and strings are compared by ordinal value, so a function produces the same result whatever
  locale the game is running in.

Raising and Handling Exceptions
-------------------------------

A function can raise an exception, which reaches the client as an error from ``run_function``,
or as an error on the stream or event evaluating it:

.. code-block:: python

   def check_fuel():
       if vessel.resources.amount("LiquidFuel") < 100:
           raise RuntimeError("not enough fuel")

Only the exceptions a service declares can be raised, so the client receives a typed exception
it can catch. Python maps ``RuntimeError`` and ``ValueError`` onto the general kRPC exceptions
and a service's own exception classes onto themselves; the factory API names one directly with
:meth:`Expression.throw`.

A function can also handle an exception, with ``try``/``except`` in Python or
:meth:`Expression.try_catch`, :meth:`Expression.try_catch_all` and
:meth:`Expression.try_finally` in the factory API:

.. code-block:: python

   def stage_safely() -> str:
       try:
           vessel.control.activate_next_stage()
           return "staged"
       except Exception as error:
           return "could not stage: " + error

**Only the message of an exception is exposed.** The name bound by ``except ... as`` is a
string holding the message, so an exception is never a value in an expression.

**Naming an exception catches every form of it.** Services raise the underlying .NET exception
types, which the server reports to clients under the kRPC name, so catching a kRPC exception
catches the exceptions procedures actually raise rather than only the ones a function raised
itself.

**A procedure that pauses execution passes through every handler**, including a catch-all, and
is reported as an error.

**Two failures of an embedded call cannot be named**: a procedure that is unavailable in the
current game scene, and one that returns no value where the client requires one. Both are
internal errors of the server rather than exceptions a service declares, so only a catch-all
handles them, and they reach the client without a name.

The C# compiler does not reach exceptions: an expression tree lambda can carry neither a throw
expression nor a statement body, so C# reaches them through the factory API.

Extending Server Side Functions
-------------------------------

Server side functions can call the procedures of *any* service installed on the server. The
compilers resolve remote calls through the server's service definitions, and the factory API
embeds any procedure with :meth:`Expression.call_with_arguments`. The extension mechanism for
server side functions is therefore the extension mechanism for the rest of kRPC: **write a
service**. A third party service DLL dropped into the game's plugin folder contributes its
procedures to server side functions on every client, with documentation and client stubs
generated in the usual way. The ``StdLib`` service is written this way, and ships with the
server.

Errors, Edge Cases and Performance
----------------------------------

**Errors.** An error raised while evaluating a function, such as an embedded call reaching
a part that has been destroyed, is delivered to the client through the event or stream, and
raised when your program accesses it. The server then removes the stream, as it does for any
stream whose result carries an error. Build a new event or stream once the condition causing
the error clears.

**Procedures that pause.** A small number of RPCs pause execution and resume on a later tick.
Such a procedure cannot produce a value within a function's single-tick evaluation, and there
is no way to resume the function around it: the only way to make progress would be to evaluate
it again from the start, repeating everything it had already done. Calling one with
:meth:`Expression.call` is therefore reported as an error, by a run-once function and by an
event or stream alike. Start it with :meth:`Expression.deferred_call` instead, which the
compilers spell ``defer(call)`` and ``Function.Defer(() => call)``. Functions that only read
values, the vast majority, are unaffected.

**Loops run to completion.** A ``while`` loop is evaluated within a single tick, and runs to
its end before the game continues. A loop whose condition never becomes false hangs the game,
recoverable only by closing it, so bound every loop by a value that is certain to change.

**Null values.** A function cannot contain a null constant, and an embedded call that returns
null will fail to evaluate if the null flows into an operator. A function whose *result* is
null gives your program its own null: ``None`` in Python, ``null``
in C# and Java, and an empty ``std::optional`` in C++ when the type you name is one. A run-once
function and a function stream report it the same way. A null *within* the result, such as an
element of a list the function returns, is an error naming the position, because the type your
program names for the result has no room to say which positions hold one.

**Side effects.** An event or a stream re-evaluates its function on every update, so any
side effects it has are repeated on every update. Run a function that changes the game with
``run_function`` instead, which evaluates it exactly once.

**Strings are not collections.** A string has its own operations, and passing one to a
collection operation is reported as an error pointing at them.

**Costs.** Building a function costs one RPC per node of the tree, once. Evaluation happens
entirely on the server, on code compiled to native, so it costs nothing on the network and even
large functions evaluate quickly. A function stream then behaves like any other stream:
updates are only sent when the value changes, and the update rate can be limited with the
stream's rate control.

**Frozen inputs.** Everything in a function other than its embedded calls is fixed at
creation time. To change a threshold, a target object or a captured collection, build a new
function and remove the old event or stream.
